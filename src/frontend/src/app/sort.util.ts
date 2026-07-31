export type SortDir = 'asc' | 'desc';

export function sortBy<T>(
  rows: T[],
  key: keyof T | string,
  dir: SortDir
): T[] {
  const factor = dir === 'asc' ? 1 : -1;
  return [...rows].sort((a, b) => {
    const left = valueAt(a, key);
    const right = valueAt(b, key);
    if (left == null && right == null) return 0;
    if (left == null) return -1 * factor;
    if (right == null) return 1 * factor;
    if (typeof left === 'number' && typeof right === 'number') {
      return (left - right) * factor;
    }
    return String(left).localeCompare(String(right), undefined, { sensitivity: 'base' }) * factor;
  });
}

function valueAt(row: unknown, key: string | number | symbol): unknown {
  if (row && typeof row === 'object' && key in (row as object)) {
    return (row as Record<string | number | symbol, unknown>)[key];
  }
  return undefined;
}

export function nextSort(currentKey: string, nextKey: string, currentDir: SortDir): SortDir {
  if (currentKey === nextKey) {
    return currentDir === 'asc' ? 'desc' : 'asc';
  }
  return 'asc';
}
