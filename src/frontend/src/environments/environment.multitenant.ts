export const environment = {
  production: true,
  apiBaseUrl: '__API_BASE_URL__',
  appBaseUrl: '__APP_BASE_URL__',
  zoomCallbackUrl: '__API_BASE_URL__/zoom/callback',
  zoomFrontendRedirectUrl: '__APP_BASE_URL__/teacher/zoom',
  defaultTenant: 'esraa',
  tenantHosts: {
    localhost: 'esraa',
    'schoolacadmy.runasp.net': 'esraa',
    'www.schoolacadmy.runasp.net': 'esraa'
  } as Record<string, string>
};
