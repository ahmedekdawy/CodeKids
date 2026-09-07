import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { UserRole } from './models';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn()) {
    return true;
  }

  const returnUrl = state.url;
  return router.createUrlTree(['/login'], {
    queryParams: returnUrl && returnUrl !== '/login' ? { returnUrl } : undefined
  });
};

export const roleGuard = (roles: UserRole[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.user();
  if (user && roles.includes(user.role)) {
    return true;
  }
  return router.createUrlTree([auth.roleHome()]);
};

/** Only allow same-origin relative paths to avoid open redirects. */
export function safeReturnUrl(raw: string | null | undefined): string | null {
  const value = (raw ?? '').trim();
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return null;
  }
  if (value === '/login' || value.startsWith('/login?') || value.startsWith('/login#')) {
    return null;
  }
  return value;
}
