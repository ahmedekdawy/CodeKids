export const environment = {
  production: true,
  apiBaseUrl: 'http://abakera.runasp.net/api',
  appBaseUrl: 'http://schoolacadmy.runasp.net',
  zoomCallbackUrl: 'http://abakera.runasp.net/api/zoom/callback',
  zoomFrontendRedirectUrl: 'http://schoolacadmy.runasp.net/teacher/zoom',
  defaultTenant: 'esraa',
  tenantHosts: {
    localhost: 'esraa',
    'schoolacadmy.runasp.net': 'esraa',
    'www.schoolacadmy.runasp.net': 'esraa'
  } as Record<string, string>
};
