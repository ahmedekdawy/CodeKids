import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { ApiBusyService } from './api-busy.service';

const silentUrlParts = ['/media/watch-events', '/weekly-reports/top-students', '/photo'];

export const apiBusyInterceptor: HttpInterceptorFn = (req, next) => {
  if (silentUrlParts.some((part) => req.url.includes(part))) {
    return next(req);
  }

  const busy = inject(ApiBusyService);
  busy.begin();
  return next(req).pipe(finalize(() => busy.end()));
};
