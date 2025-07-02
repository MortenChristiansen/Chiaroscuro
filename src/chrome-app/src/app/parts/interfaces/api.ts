export interface Api {
  uiLoaded: () => Promise<void>;
}

export async function loadBackendApi<TApi extends Api>() {
  if (typeof window === 'undefined') {
    throw new Error('loadBackendApi can only be called in browser environment');
  }
  await (window as any).CefSharp.BindObjectAsync('api');
  return (window as any).api as TApi;
}

export function exposeApiToBackend(api: any) {
  if (typeof window === 'undefined') {
    return; // Skip in server environment
  }
  (window as any).angularApi = api;
}
