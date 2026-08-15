export const environment = {
  production: true,
  apiBaseUrl: 'http://abakera.runasp.net/api',
  appBaseUrl: 'http://abakeraadmin.runasp.net',
  zoomCallbackUrl: 'http://abakera.runasp.net/api/zoom/callback',
  zoomFrontendRedirectUrl: 'http://abakeraadmin.runasp.net/teacher/zoom',
  defaultTenant: 'abakera',
  tenantHosts: {
    localhost: 'abakera',
    'abakeraadmin.runasp.net': 'abakera',
    'www.abakeraadmin.runasp.net': 'abakera'
  } as Record<string, string>
};
