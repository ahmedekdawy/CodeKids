export const environment = {
  production: true,
  apiBaseUrl: 'http://schoolacadmyapi.runasp.net/api',
  appBaseUrl: 'http://schoolacadmy.runasp.net',
  zoomCallbackUrl: 'http://abakera.runasp.net/api/zoom/callback',
  zoomFrontendRedirectUrl: 'http://schoolacadmy.runasp.net/teacher/zoom',
  defaultTenant: 'abakera',
  tenantHosts: {
    localhost: 'abakera',
    'schoolacadmy.runasp.net': 'esraa',
    'www.schoolacadmy.runasp.net': 'esraa'
  } as Record<string, string>
};
