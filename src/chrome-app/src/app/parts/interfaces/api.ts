export interface Api {}

const isBrowser = typeof window !== 'undefined';

export async function loadBackendApi<TApi extends Api>(
  apiName?: string
): Promise<TApi> {
  const property = apiName ?? 'api';
  if (isBrowser) {
    await (window as any).CefSharp.BindObjectAsync(property);
    const api = (window as any)[property] as TApi;
    if (!api)
      throw new Error(
        'API not found. Ensure the backend is properly initialized.'
      );
    return api;
  } else {
    // Return a dummy object for server/prerender
    return {} as TApi;
  }
}

export function exposeApiToBackend(api: any) {
  if (isBrowser) {
    const currentApi = (window as any).angularApi || {};
    (window as any).angularApi = { ...currentApi, ...api };
  }
}
