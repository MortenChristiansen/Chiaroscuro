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

class ApiMethod {
  private implementations: ((...args: any[]) => void)[] = [];

  constructor(public methodName: string) {}

  addImplementation(fn: (...args: any[]) => void) {
    if (typeof fn === 'function') {
      this.implementations.push(fn);
    } else {
      throw new Error(
        `Implementation for ${this.methodName} is not a function`
      );
    }
  }

  call(...args: any[]) {
    return this.implementations.map((fn) => fn(...args));
  }
}

export function exposeApiToBackend(api: any) {
  if (isBrowser) {
    const win = window as any;
    win.angularApi = win.angularApi || {};
    for (const key of Object.keys(api)) {
      const value = api[key];
      if (typeof value === 'function') {
        if (!win.angularApi[key]) {
          win.angularApi[key] = new ApiMethod(key);
        }
        win.angularApi[key].addImplementation(value);
      } else {
        throw new Error(
          `API method ${key} is not a function. Only functions can be exposed to the backend.`
        );
      }
    }
  }
}
