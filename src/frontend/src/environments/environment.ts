export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5078/api',
  appBaseUrl: 'http://localhost:4200',
  zoomCallbackUrl: 'http://localhost:5078/api/zoom/callback',
  zoomFrontendRedirectUrl: 'http://localhost:4200/teacher/zoom',
  defaultTenant: 'abakera',
  tenantHosts: {
    localhost: 'abakera',
    'abakeraadmin.runasp.net': 'abakera',
    'www.abakeraadmin.runasp.net': 'abakera'
  } as Record<string, string>
};
