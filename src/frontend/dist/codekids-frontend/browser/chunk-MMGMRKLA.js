import {
  Router,
  setCurrentTenantId
} from "./chunk-RN452YVM.js";
import {
  HttpClient,
  Injectable,
  __spreadValues,
  inject,
  resolveApiBaseUrl,
  setClassMetadata,
  signal,
  tap,
  ɵɵdefineInjectable
} from "./chunk-EMYLRMBO.js";

// src/app/auth.service.ts
var TOKEN_KEY = "codekids_token";
var USER_KEY = "codekids_user";
var IMPERSONATOR_KEY = "codekids_impersonator";
var AuthService = class _AuthService {
  constructor() {
    this.http = inject(HttpClient);
    this.router = inject(Router);
    this.baseUrl = `${resolveApiBaseUrl()}/auth`;
    this.user = signal(
      this.readUser(),
      ...ngDevMode ? [{ debugName: "user" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.token = signal(
      localStorage.getItem(TOKEN_KEY),
      ...ngDevMode ? [{ debugName: "token" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.impersonator = signal(
      this.readImpersonator(),
      ...ngDevMode ? [{ debugName: "impersonator" }] : (
        /* istanbul ignore next */
        []
      )
    );
    setCurrentTenantId(this.user()?.tenantId);
  }
  login(login, password) {
    return this.http.post(`${this.baseUrl}/login`, { email: login, password }).pipe(tap((response) => this.persist(response)));
  }
  redeemAssessmentLink(key) {
    return this.http.post(`${this.baseUrl}/assessment-link/redeem`, { key }).pipe(tap((response) => this.persist({
      token: response.token,
      user: response.user
    })));
  }
  register(payload) {
    return this.http.post(`${this.baseUrl}/register`, payload).pipe(tap((response) => this.persist(response)));
  }
  registerTenant(payload) {
    return this.http.post(`${resolveApiBaseUrl()}/tenants/register`, payload);
  }
  verifyTenant(token) {
    return this.http.post(`${resolveApiBaseUrl()}/tenants/verify`, { token }).pipe(tap((result) => setCurrentTenantId(result.tenantId)));
  }
  forgotPassword(email, channel = "whatsapp") {
    return this.http.post(`${this.baseUrl}/forgot-password`, {
      email,
      channel
    });
  }
  resetPassword(token, newPassword) {
    return this.http.post(`${this.baseUrl}/reset-password`, { token, newPassword });
  }
  updateAccount(payload) {
    return this.http.put(`${this.baseUrl}/account`, payload).pipe(tap((user) => this.patchUser(user)));
  }
  uploadProfilePhoto(file) {
    const form = new FormData();
    form.append("file", file, file.name);
    return this.http.post(`${resolveApiBaseUrl()}/profile/photo`, form).pipe(tap((user) => this.patchUser(user)));
  }
  removeProfilePhoto() {
    return this.http.delete(`${resolveApiBaseUrl()}/profile/photo`).pipe(tap((user) => this.patchUser(user)));
  }
  impersonate(userId) {
    return this.http.post(`${resolveApiBaseUrl()}/admin/users/${userId}/impersonate`, {}).pipe(tap((response) => this.persistImpersonation(response)));
  }
  impersonateStudentAsTeacher(studentId) {
    return this.http.post(`${resolveApiBaseUrl()}/dashboard/teacher/students/${studentId}/impersonate`, {}).pipe(tap((response) => this.persistImpersonation(response)));
  }
  impersonateChildAsParent(childId) {
    return this.http.post(`${resolveApiBaseUrl()}/parent/children/${childId}/impersonate`, {}).pipe(tap((response) => this.persistImpersonation(response)));
  }
  stopImpersonating() {
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
  isImpersonating() {
    return !!this.impersonator();
  }
  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(IMPERSONATOR_KEY);
    this.token.set(null);
    this.user.set(null);
    this.impersonator.set(null);
    void this.router.navigateByUrl("/login");
  }
  isLoggedIn() {
    return !!this.token();
  }
  patchUser(partial) {
    const current = this.user();
    if (!current)
      return;
    const next = __spreadValues(__spreadValues({}, current), partial);
    localStorage.setItem(USER_KEY, JSON.stringify(next));
    this.user.set(next);
  }
  /** Admins, and the owner of a single-teacher tenant. */
  hasAdminRights() {
    const user = this.user();
    return user?.role === "SuperAdmin" || user?.isTenantOwner === true;
  }
  roleHome() {
    const role = this.user()?.role;
    if (role === "Parent")
      return "/parent";
    if (role === "Teacher")
      return "/teacher";
    if (role === "SuperAdmin")
      return "/admin";
    return "/student";
  }
  persist(response) {
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    setCurrentTenantId(response.user.tenantId);
    this.token.set(response.token);
    this.user.set(response.user);
  }
  persistImpersonation(response) {
    if (!this.readSavedImpersonator()) {
      const token = this.token();
      const user = this.user();
      if (token && user) {
        const snapshot = { token, user };
        localStorage.setItem(IMPERSONATOR_KEY, JSON.stringify(snapshot));
        this.impersonator.set(user);
      }
    }
    this.persist(response);
  }
  readUser() {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  }
  readImpersonator() {
    return this.readSavedImpersonator()?.user ?? null;
  }
  readSavedImpersonator() {
    const raw = localStorage.getItem(IMPERSONATOR_KEY);
    if (!raw)
      return null;
    try {
      const parsed = JSON.parse(raw);
      return parsed?.token && parsed?.user ? parsed : null;
    } catch (e) {
      return null;
    }
  }
  static {
    this.\u0275fac = function AuthService_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _AuthService)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _AuthService, factory: _AuthService.\u0275fac, providedIn: "root" });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(AuthService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], () => [], null);
})();

export {
  AuthService
};
//# sourceMappingURL=chunk-MMGMRKLA.js.map
