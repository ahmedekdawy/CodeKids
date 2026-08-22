export const environment = {
  production: true,
  apiBaseUrl: 'https://schoolacadmyapi.runasp.net/api',
  appBaseUrl: 'https://schoolacadmy.runasp.net',
  zoomCallbackUrl: 'https://abakera.runasp.net/api/zoom/callback',
  zoomFrontendRedirectUrl: 'https://schoolacadmy.runasp.net/teacher/zoom',
  defaultTenant: 'esraa',
  tenantHosts: {
    localhost: 'esraa',
    'schoolacadmy.runasp.net': 'esraa',
    'www.schoolacadmy.runasp.net': 'esraa'
  } as Record<string, string>
};
