export const environment = {
  production: true,
  apiBaseUrl: 'https://abakera.runasp.net/api',
  appBaseUrl: 'https://abakeraadmin.runasp.net',
  zoomCallbackUrl: 'https://abakera.runasp.net/api/zoom/callback',
  zoomFrontendRedirectUrl: 'https://abakeraadmin.runasp.net/teacher/zoom',
  defaultTenant: 'abakera',
  tenantHosts: {
    localhost: 'abakera',
    'abakeraadmin.runasp.net': 'abakera',
    'www.abakeraadmin.runasp.net': 'abakera'
  } as Record<string, string>
};
