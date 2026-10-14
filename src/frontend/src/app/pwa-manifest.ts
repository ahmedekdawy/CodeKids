import { resolveApiBaseUrl } from './api-base-url';
import { currentTenantId } from './tenant';

/**
 * Points the page at the tenant's own web manifest, so the installed app carries the school's name
 * and logo instead of the generic ones. The static manifest in index.html stays in place if the
 * tenant's manifest cannot be fetched, so the app remains installable either way.
 *
 * iPhone and iPad ignore manifest icons and use the page's apple-touch-icon, so that link (and the tab
 * icon) are pointed at the tenant's logo too when it has one. The version keeps them fresh after a new logo.
 */
export function useTenantManifest(siteName: string, logoVersion: string | null): void {
  const tenant = currentTenantId();
  const link = document.querySelector<HTMLLinkElement>('link[rel="manifest"]');
  if (!tenant || !link) return;

  document.querySelector('meta[name="apple-mobile-web-app-title"]')?.setAttribute('content', siteName);
  if (logoVersion) {
    const icon = (size: number) =>
      `${resolveApiBaseUrl()}/tenants/icon.png?tenant=${encodeURIComponent(tenant)}&size=${size}&v=${encodeURIComponent(logoVersion)}`;
    document.querySelector('link[rel="apple-touch-icon"]')?.setAttribute('href', icon(180));
    document.querySelector('link[rel="icon"]')?.setAttribute('href', icon(192));
  }

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
