import { useState, useRef, useCallback, useEffect } from 'react'

const DEFAULT_URL = 'https://www.google.com'

function normalizeUrl(input: string): string {
  let url = input.trim()
  if (!url.startsWith('http://') && !url.startsWith('https://')) {
    url = 'https://' + url
  }
  return url
}

export default function App() {
  const [url, setUrl] = useState(DEFAULT_URL)
  const [inputValue, setInputValue] = useState(DEFAULT_URL)
  const [isLoading, setIsLoading] = useState(false)
  const webviewRef = useRef<Electron.WebviewTag | null>(null)

  const navigate = useCallback((targetUrl: string) => {
    const normalized = normalizeUrl(targetUrl)
    setUrl(normalized)
    setInputValue(normalized)
    if (webviewRef.current) {
      webviewRef.current.src = normalized
    }
  }, [])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    navigate(inputValue)
  }

  const handleBack = () => {
    webviewRef.current?.goBack()
  }

  const handleForward = () => {
    webviewRef.current?.goForward()
  }

  const handleRefresh = () => {
    webviewRef.current?.reload()
  }

  useEffect(() => {
    const webview = webviewRef.current
    if (!webview) return

    const handleNavigate = (e: Electron.DidNavigateEvent) => {
      setInputValue(e.url)
      setUrl(e.url)
    }

    const handleStartLoading = () => setIsLoading(true)
    const handleStopLoading = () => setIsLoading(false)

    webview.addEventListener('did-navigate', handleNavigate)
    webview.addEventListener('did-navigate-in-page', handleNavigate as unknown as EventListener)
    webview.addEventListener('did-start-loading', handleStartLoading)
    webview.addEventListener('did-stop-loading', handleStopLoading)

    return () => {
      webview.removeEventListener('did-navigate', handleNavigate)
      webview.removeEventListener('did-navigate-in-page', handleNavigate as unknown as EventListener)
      webview.removeEventListener('did-start-loading', handleStartLoading)
      webview.removeEventListener('did-stop-loading', handleStopLoading)
    }
  }, [])

  return (
    <div className="app">
      <header className="toolbar">
        <div className="nav-buttons">
          <button onClick={handleBack} title="Back">&larr;</button>
          <button onClick={handleForward} title="Forward">&rarr;</button>
          <button onClick={handleRefresh} title="Refresh">&#8635;</button>
        </div>
        <form onSubmit={handleSubmit} className="url-form">
          <input
            type="text"
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            placeholder="Enter URL..."
            className="url-input"
          />
          <button type="submit">Go</button>
        </form>
        {isLoading && <span className="loading-indicator">Loading...</span>}
      </header>
      <webview
        ref={webviewRef as React.RefObject<Electron.WebviewTag>}
        src={url}
        className="webview"
        // @ts-expect-error allowpopups is an Electron webview attribute
        allowpopups="true"
      />
    </div>
  )
}
