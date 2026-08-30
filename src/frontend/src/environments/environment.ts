export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5078/api',
  appBaseUrl: 'http://localhost:4200',
  teraboxBaseUrl: 'https://www.1024terabox.com',
  zoomCallbackUrl: 'http://localhost:5078/api/zoom/callback',
  zoomFrontendRedirectUrl: 'http://localhost:4200/teacher/zoom',
  defaultTenant: 'abakera',
  tenantHosts: {
    localhost: 'abakera',
    'abakeraadmin.runasp.net': 'abakera',
    'www.abakeraadmin.runasp.net': 'abakera'
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
