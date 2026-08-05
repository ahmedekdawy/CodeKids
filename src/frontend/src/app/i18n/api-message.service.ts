import { Injectable, inject } from '@angular/core';
import { LocaleService } from './locale.service';
import { hasApiErrorCode, resolveApiError, resolveApiFeedback } from './api-message.util';

export type { ApiMessageBody } from './api-message.util';

@Injectable({ providedIn: 'root' })
export class ApiMessageService {
  private readonly locale = inject(LocaleService);

  fromError(err: unknown, fallbackKey?: string): string {
    return resolveApiError((key, params) => this.locale.t(key, params), err, fallbackKey);
  }

  fromFeedback(body: { feedbackCode?: string | null; feedback?: string | null }): string {
    return resolveApiFeedback((key, params) => this.locale.t(key, params), body);
  }

  hasErrorCode(err: unknown, code: string): boolean {
    return hasApiErrorCode(err, code);
  }
}
