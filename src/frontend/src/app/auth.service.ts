import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { resolveApiBaseUrl } from './api-base-url';
import { AuthResponse, AuthUser, UserRole } from './models';
import { setCurrentTenantId } from './tenant';

const TOKEN_KEY = 'codekids_token';
const USER_KEY = 'codekids_user';
const IMPERSONATOR_KEY = 'codekids_impersonator';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${resolveApiBaseUrl()}/auth`;

  readonly user = signal<AuthUser | null>(this.readUser());
  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  readonly impersonator = signal<AuthUser | null>(this.readImpersonator());

  login(login: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, { email: login, password }).pipe(
      tap((response) => this.persist(response))
    );
  }

  register(payload: {
    email: string;
    displayName: string;
    password: string;
    role: UserRole;
    parentId?: string | null;
  }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, payload).pipe(
      tap((response) => this.persist(response))
    );
  }

  registerTenant(payload: {
    tenantName: string;
    email: string;
    displayName: string;
    password: string;
    mobilePhone?: string;
  }): Observable<{ accepted: boolean; message: string }> {
    return this.http.post<{ accepted: boolean; message: string }>(
      `${resolveApiBaseUrl()}/tenants/register`,
      payload
    );
  }

  verifyTenant(token: string): Observable<{ tenantId: string; email: string; message: string }> {
    return this.http
      .post<{ tenantId: string; email: string; message: string }>(
        `${resolveApiBaseUrl()}/tenants/verify`,
        { token }
      )
      .pipe(tap((result) => setCurrentTenantId(result.tenantId)));
  }

  forgotPassword(email: string): Observable<{ accepted: boolean; message: string }> {
    return this.http.post<{ accepted: boolean; message: string }>(`${this.baseUrl}/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<{ accepted: boolean }> {
    return this.http.post<{ accepted: boolean }>(`${this.baseUrl}/reset-password`, { token, newPassword });
  }

  updateAccount(payload: {
    email?: string | null;
    mobilePhone?: string | null;
    password?: string | null;
  }): Observable<AuthUser> {
    return this.http.put<AuthUser>(`${this.baseUrl}/account`, payload).pipe(
      tap((user) => this.patchUser(user))
    );
  }

  impersonate(userId: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${resolveApiBaseUrl()}/admin/users/${userId}/impersonate`, {})
      .pipe(tap((response) => this.persistImpersonation(response)));
  }

  impersonateStudentAsTeacher(studentId: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${resolveApiBaseUrl()}/dashboard/teacher/students/${studentId}/impersonate`, {})
      .pipe(tap((response) => this.persistImpersonation(response)));
  }

  stopImpersonating(): void {
    const saved = this.readSavedImpersonator();
    localStorage.removeItem(IMPERSONATOR_KEY);
    this.impersonator.set(null);
    if (!saved) {
      this.logout();
      return;
    }
    this.persist(saved);
    void this.router.navigateByUrl(this.roleHome());
  }

  isImpersonating(): boolean {
    return !!this.impersonator();
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(IMPERSONATOR_KEY);
    this.token.set(null);
    this.user.set(null);
    this.impersonator.set(null);
    void this.router.navigateByUrl('/login');
  }

  isLoggedIn(): boolean {
    return !!this.token();
  }

  patchUser(partial: Partial<AuthUser>): void {
    const current = this.user();
    if (!current) return;
    const next = { ...current, ...partial };
    localStorage.setItem(USER_KEY, JSON.stringify(next));
    this.user.set(next);
  }

  roleHome(): string {
    const role = this.user()?.role;
    if (role === 'Parent') return '/parent';
    if (role === 'Teacher') return '/teacher';
    if (role === 'SuperAdmin') return '/admin';
    return '/student';
  }

  private persist(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    setCurrentTenantId(response.user.tenantId);
    this.token.set(response.token);
    this.user.set(response.user);
  }

  private persistImpersonation(response: AuthResponse): void {
    if (!this.readSavedImpersonator()) {
      const token = this.token();
      const user = this.user();
      if (token && user) {
        const snapshot: AuthResponse = { token, user };
        localStorage.setItem(IMPERSONATOR_KEY, JSON.stringify(snapshot));
        this.impersonator.set(user);
      }
    }
    this.persist(response);
  }

  private readUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  }

  private readImpersonator(): AuthUser | null {
    return this.readSavedImpersonator()?.user ?? null;
  }

  private readSavedImpersonator(): AuthResponse | null {
    const raw = localStorage.getItem(IMPERSONATOR_KEY);
    if (!raw) return null;
    try {
      const parsed = JSON.parse(raw) as AuthResponse;
      return parsed?.token && parsed?.user ? parsed : null;
    } catch {
      return null;
    }
  }
}
