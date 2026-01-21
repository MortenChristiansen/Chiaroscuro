/// <reference types="vite/client" />

declare namespace Electron {
  interface WebviewTag extends HTMLElement {
    src: string
    allowpopups: string
    goBack(): void
    goForward(): void
    reload(): void
    addEventListener<K extends keyof WebviewTagEventMap>(
      type: K,
      listener: (this: WebviewTag, ev: WebviewTagEventMap[K]) => unknown
    ): void
    removeEventListener<K extends keyof WebviewTagEventMap>(
      type: K,
      listener: (this: WebviewTag, ev: WebviewTagEventMap[K]) => unknown
    ): void
  }

  interface WebviewTagEventMap {
    'did-navigate': DidNavigateEvent
    'did-navigate-in-page': DidNavigateInPageEvent
    'did-start-loading': Event
    'did-stop-loading': Event
  }

  interface DidNavigateEvent extends Event {
    url: string
  }

  interface DidNavigateInPageEvent extends Event {
    url: string
    isMainFrame: boolean
  }
}

declare global {
  namespace JSX {
    interface IntrinsicElements {
      webview: React.DetailedHTMLProps<
        React.HTMLAttributes<Electron.WebviewTag> & {
          src?: string
          allowpopups?: boolean | 'true' | 'false'
        },
        Electron.WebviewTag
      >
    }
  }
}
