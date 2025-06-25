export interface Api {
  uiLoaded: () => Promise<void>;
}

export async function loadBackendApi<TApi extends Api>() {
  await (window as any).CefSharp.BindObjectAsync('api');
  return (window as any).api as TApi;
}

export function exposeApiToBackend(api: any) {
  (window as any).angularApi = api;
}
