export const environment = {
  production: true,
  apiBaseUrl: 'http://schoolacadmyapi.runasp.net/api',
  appBaseUrl: 'http://schoolacadmy.runasp.net',
  teraboxBaseUrl: 'https://www.1024terabox.com',
  zoomCallbackUrl: 'http://abakera.runasp.net/api/zoom/callback',
  zoomFrontendRedirectUrl: 'http://schoolacadmy.runasp.net/teacher/zoom',
  defaultTenant: 'abakera',
  tenantHosts: {
    localhost: 'abakera',
    'schoolacadmy.runasp.net': 'esraa',
    'www.schoolacadmy.runasp.net': 'esraa'
  } as Record<string, string>,
  apiHosts: {
    localhost: 'http://localhost:5078/api',
    '127.0.0.1': 'http://localhost:5078/api',
    'abakera.runasp.net': 'https://abakera.runasp.net/api',
    'abakeraadmin.runasp.net': 'https://abakera.runasp.net/api',
    'www.abakeraadmin.runasp.net': 'https://abakera.runasp.net/api',
    'schoolacadmy.runasp.net': 'http://schoolacadmyapi.runasp.net/api',
    'www.schoolacadmy.runasp.net': 'http://schoolacadmyapi.runasp.net/api'
  } as Record<string, string>
};
