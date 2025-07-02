export interface Api {
  uiLoaded: () => Promise<void>;
}

export async function loadBackendApi<TApi extends Api>(
  apiName?: string
): Promise<TApi> {
  const property = apiName ?? 'api';
  await (window as any).CefSharp.BindObjectAsync(property);
  const api = (window as any)[property] as TApi;
  if (!api)
    throw new Error(
      'API not found. Ensure the backend is properly initialized.'
    );
  return api;
}

export function exposeApiToBackend(api: any) {
  (window as any).angularApi = api;
}
