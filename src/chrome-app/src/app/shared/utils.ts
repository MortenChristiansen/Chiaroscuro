// Simple debounce utility for Angular signals and callbacks
export function debounce<T extends (...args: any[]) => void>(
  fn: T,
  delay: number
) {
  let timer: any;
  return (...args: Parameters<T>) => {
    if (timer) {
      clearTimeout(timer);
    }
    timer = setTimeout(() => {
      fn(...args);
    }, delay);
  };
}

type LowercaseFirst<S extends string> = S extends `${infer F}${infer R}`
  ? `${Lowercase<F>}${R}`
  : S;

/**
 * Returns a copy of `obj` where each own key is changed to be camelCase (if not already).
 *
 * This is intentionally shallow: it only changes the top-level property names.
 * If multiple keys normalize to the same key (e.g. `Name` and `name`), later keys win.
 */
export function normalizeBackendModel<T>(obj: T): {
  [K in keyof T as LowercaseFirst<K & string>]: T[K];
} {
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(obj as any)) {
    if (key.length === 0) {
      out[key] = value;
      continue;
    }
    const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
    out[normalizedKey] = value;
  }
  return out as { [K in keyof T as LowercaseFirst<K & string>]: T[K] };
}
