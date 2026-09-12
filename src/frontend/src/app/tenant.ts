import { environment } from '../environments/environment';

const TENANT_KEY = 'codekids_tenant';
const USER_KEY = 'codekids_user';

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

function tenantFromStorage(): string | null {
  try {
    const value = localStorage.getItem(TENANT_KEY)?.trim();
    return value ? value : null;
  } catch {
    return null;
  }
}

function tenantFromSavedUser(): string | null {
  try {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    const user = JSON.parse(raw) as { tenantId?: string | null };
    const value = user?.tenantId?.trim();
    return value ? value : null;
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

  const fromUser = tenantFromSavedUser();
  if (fromUser) {
    setCurrentTenantId(fromUser);
    return fromUser;
  }

  const fromStorage = tenantFromStorage();
  if (fromStorage) {
    return fromStorage;
  }

  const host = (globalThis.location?.hostname ?? '').toLowerCase();
  const mapped = environment.tenantHosts?.[host];
  if (mapped) {
    setCurrentTenantId(mapped);
    return mapped;
  }

  const fallback = environment.defaultTenant || 'abakera';
  setCurrentTenantId(fallback);
  return fallback;
}
