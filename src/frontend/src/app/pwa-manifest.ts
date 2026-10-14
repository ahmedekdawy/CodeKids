import { resolveApiBaseUrl } from './api-base-url';
import { currentTenantId } from './tenant';

/**
 * Points the page at the tenant's own web manifest, so the installed app carries the school's name
 * instead of the generic one. The static manifest in index.html stays in place if the tenant's
 * manifest cannot be fetched, so the app remains installable either way.
 */
export function useTenantManifest(siteName: string): void {
  const tenant = currentTenantId();
  const link = document.querySelector<HTMLLinkElement>('link[rel="manifest"]');
  if (!tenant || !link) return;

  document.querySelector('meta[name="apple-mobile-web-app-title"]')?.setAttribute('content', siteName);

  const href =
    `${resolveApiBaseUrl()}/tenants/manifest.webmanifest` +
    `?tenant=${encodeURIComponent(tenant)}&app=${encodeURIComponent(globalThis.location.origin)}`;
  if (link.href === href) return;

  fetch(href)
    .then((response) => {
      if (response.ok) link.href = href;
    })
    .catch(() => undefined);
}
