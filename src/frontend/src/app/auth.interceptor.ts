import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { currentTenantId } from './tenant';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();
  const headers: Record<string, string> = {
    'X-Tenant-Id': currentTenantId()
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  return next(req.clone({ setHeaders: headers }));
};
