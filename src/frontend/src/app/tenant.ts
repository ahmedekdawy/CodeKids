import { environment } from '../environments/environment';
import { resolveApiBaseUrl } from './api-base-url';

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

/** Tenant ids are slugs: letters, digits, dashes and underscores. */
export function normalizeTenantId(raw: string | null | undefined): string | null {
  const value = (raw ?? '').trim().toLowerCase();
  return /^[a-z0-9_-]{1,64}$/.test(value) ? value : null;
}

/** Null when nothing identifies the tenant; the login page then asks the user for it. */
export function currentTenantId(): string | null {
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

  return null;
}

/** When the browser knows no tenant, adopt the API's Tenants:Default (if one is configured). */
export async function loadDefaultTenant(): Promise<void> {
  if (currentTenantId()) return;
  try {
    const response = await fetch(`${resolveApiBaseUrl()}/tenants/default`);
    if (!response.ok) return;
    const body = (await response.json()) as { tenantId?: string | null };
    setCurrentTenantId(body.tenantId);
  } catch {
    // API unreachable: leave the tenant unset so the login page asks for it.
  }
}
