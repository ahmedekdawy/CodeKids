import { HttpErrorResponse } from '@angular/common/http';

export interface ApiMessageBody {
  code?: string;
  message?: string;
  args?: Record<string, string | number>;
  feedbackCode?: string;
  feedback?: string;
}

type TranslateFn = (key: string, params?: Record<string, string | number>) => string;

const LEGACY_MESSAGE_CODES: Record<string, string> = {
  'Invalid email or password.': 'api.errors.auth.invalidCredentials',
  'An account with that email already exists.': 'api.errors.auth.emailExists',
  'Reset token is invalid or has expired.': 'api.errors.auth.resetTokenInvalid',
  'Password must be at least 6 characters.': 'api.errors.auth.passwordTooShort',
  'Exam already submitted.': 'api.errors.exam.alreadySubmitted',
  'Assignment already submitted.': 'api.errors.assignment.alreadySubmitted'
};

function readApiBody(err: unknown): ApiMessageBody | null {
  if (err instanceof HttpErrorResponse) {
    return (err.error as ApiMessageBody) ?? null;
  }
  if (typeof err === 'object' && err !== null && 'error' in err) {
    return ((err as { error: unknown }).error as ApiMessageBody) ?? null;
  }
  return null;
}

function mapLegacyMessage(message: string): { code: string; args?: Record<string, string | number> } | null {
  const code = LEGACY_MESSAGE_CODES[message];
  return code ? { code } : null;
}

export function resolveApiError(t: TranslateFn, err: unknown, fallbackKey?: string): string {
  const body = readApiBody(err);
  if (body?.code) {
    return t(body.code, body.args);
  }
  if (body?.message) {
    const mapped = mapLegacyMessage(body.message);
    if (mapped) return t(mapped.code, mapped.args);
    return body.message;
  }
  return fallbackKey ? t(fallbackKey) : t('api.errors.unknown');
}

export function resolveApiFeedback(
  t: TranslateFn,
  body: { feedbackCode?: string | null; feedback?: string | null }
): string {
  if (body.feedbackCode) return t(body.feedbackCode);
  return body.feedback || '';
}

export function hasApiErrorCode(err: unknown, code: string): boolean {
  const body = readApiBody(err);
  if (body?.code === code) return true;
  if (body?.message) {
    const mapped = mapLegacyMessage(body.message);
    return mapped?.code === code;
  }
  return false;
}
