import { environment } from '../environments/environment';

export function resolveApiBaseUrl(): string {
  const host = (globalThis.location?.hostname ?? '').toLowerCase();
  const mapped = environment.apiHosts?.[host];
  if (mapped) {
    return mapped.replace(/\/$/, '');
  }

  return environment.apiBaseUrl.replace(/\/$/, '');
}

export function resolveApiOrigin(): string {
  return resolveApiBaseUrl().replace(/\/api\/?$/, '');
}
