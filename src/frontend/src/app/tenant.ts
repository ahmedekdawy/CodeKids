import { environment } from '../environments/environment';

const TENANT_KEY = 'codekids_tenant';

export function setCurrentTenantId(tenantId: string | null | undefined): void {
  const value = (tenantId ?? '').trim();
  if (value) {
    localStorage.setItem(TENANT_KEY, value);
  }
}

export function tenantFromQueryString(): string | null {
  try {
    const value = new URL(globalThis.location?.href ?? '', 'http://localhost').searchParams.get('tenant');
    const trimmed = value?.trim();
    return trimmed ? trimmed : null;
  } catch {
    return null;
  }
}

export function currentTenantId(): string {
  const fromQuery = tenantFromQueryString();
  if (fromQuery) {
    setCurrentTenantId(fromQuery);
    return fromQuery;
  }

  const stored = (localStorage.getItem(TENANT_KEY) ?? '').trim();
  if (stored) return stored;
  const host = (globalThis.location?.hostname ?? '').toLowerCase();
  const mapped = environment.tenantHosts?.[host];
  if (mapped) return mapped;
  return environment.defaultTenant;
}
