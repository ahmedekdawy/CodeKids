import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../environments/environment';
import { AuthResponse, AuthUser, UserRole } from './models';

const TOKEN_KEY = 'codekids_token';
const USER_KEY = 'codekids_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  readonly user = signal<AuthUser | null>(this.readUser());
  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));

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

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.token.set(null);
    this.user.set(null);
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
    this.token.set(response.token);
    this.user.set(response.user);
  }

  private readUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) as AuthUser : null;
  }
}
