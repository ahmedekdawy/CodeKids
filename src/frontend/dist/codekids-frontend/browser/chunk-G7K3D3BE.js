import {
  currentTenantId
} from "./chunk-RN452YVM.js";
import {
  DEFAULT_PM_START_MINUTES,
  DEFAULT_SESSION_COUNT,
  LearningApiService,
  normalizePmStartMinutes,
  normalizeSessionCount
} from "./chunk-3MQCOGP4.js";
import {
  Injectable,
  inject,
  resolveApiBaseUrl,
  setClassMetadata,
  signal,
  ɵɵdefineInjectable
} from "./chunk-EMYLRMBO.js";

// src/app/tenant-style.ts
var TENANT_STYLES = ["School", "Academy", "SingleTeacher"];
function normalizeTenantStyle(raw) {
  return TENANT_STYLES.find((style) => style.toLowerCase() === (raw ?? "").trim().toLowerCase()) ?? "School";
}
var SCHOOL_ONLY_PATHS = [
  "/admin/timetable",
  "/admin/grade-certificates",
  "/teacher/timetable",
  "/teacher/grade-certificates"
];
var STAFF_PATHS = ["/admin/admins", "/admin/teachers", "/admin/payroll", "/admin/attendance", "/teacher/attendance"];
var HIDDEN_PATHS = {
  School: [],
  Academy: SCHOOL_ONLY_PATHS,
  SingleTeacher: [...SCHOOL_ONLY_PATHS, ...STAFF_PATHS]
};
function navItemsForStyle(items, style) {
  const hidden = HIDDEN_PATHS[style];
  return hidden.length ? items.filter((item) => !hidden.includes(item.path)) : items;
}
var HIDDEN_FIELDS = {
  School: ["studentSessions"],
  Academy: ["studentSchoolType", "teacherShiftAndStages", "teacherStageAmounts"],
  SingleTeacher: ["studentSchoolType", "teacherShiftAndStages", "teacherStageAmounts", "teacherPay"]
};
function showsField(style, field) {
  return !HIDDEN_FIELDS[style].includes(field);
}

// src/app/pwa-manifest.ts
function useTenantManifest(siteName, logoVersion) {
  const tenant = currentTenantId();
  const link = document.querySelector('link[rel="manifest"]');
  if (!tenant || !link)
    return;
  document.querySelector('meta[name="apple-mobile-web-app-title"]')?.setAttribute("content", siteName);
  if (logoVersion) {
    const icon = (size) => `${resolveApiBaseUrl()}/tenants/icon.png?tenant=${encodeURIComponent(tenant)}&size=${size}&v=${encodeURIComponent(logoVersion)}`;
    document.querySelector('link[rel="apple-touch-icon"]')?.setAttribute("href", icon(180));
    document.querySelector('link[rel="icon"]')?.setAttribute("href", icon(192));
  }
  const href = `${resolveApiBaseUrl()}/tenants/manifest.webmanifest?tenant=${encodeURIComponent(tenant)}&app=${encodeURIComponent(globalThis.location.origin)}`;
  if (link.href === href)
    return;
  fetch(href).then((response) => {
    if (response.ok)
      link.href = href;
  }).catch(() => void 0);
}

// src/app/site-brand.service.ts
var SiteBrandService = class _SiteBrandService {
  constructor() {
    this.api = inject(LearningApiService);
    this.siteName = signal(
      "CodeKids",
      ...ngDevMode ? [{ debugName: "siteName" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.logoUrl = signal(
      null,
      ...ngDevMode ? [{ debugName: "logoUrl" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.bannerUrl = signal(
      null,
      ...ngDevMode ? [{ debugName: "bannerUrl" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.amSessionCount = signal(
      DEFAULT_SESSION_COUNT,
      ...ngDevMode ? [{ debugName: "amSessionCount" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.pmSessionCount = signal(
      DEFAULT_SESSION_COUNT,
      ...ngDevMode ? [{ debugName: "pmSessionCount" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.pmStartMinutes = signal(
      DEFAULT_PM_START_MINUTES,
      ...ngDevMode ? [{ debugName: "pmStartMinutes" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.tenantStyle = signal(
      "School",
      ...ngDevMode ? [{ debugName: "tenantStyle" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.multiTimeZone = signal(
      false,
      ...ngDevMode ? [{ debugName: "multiTimeZone" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.timeZone = signal(
      null,
      ...ngDevMode ? [{ debugName: "timeZone" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.loaded = signal(
      false,
      ...ngDevMode ? [{ debugName: "loaded" }] : (
        /* istanbul ignore next */
        []
      )
    );
  }
  load() {
    this.api.getSiteSettings().subscribe({
      next: (settings) => this.apply(settings),
      error: () => this.loaded.set(true)
    });
  }
  apply(settings) {
    this.siteName.set(settings.siteName || "CodeKids");
    this.tenantStyle.set(normalizeTenantStyle(settings.tenantStyle));
    this.multiTimeZone.set(settings.multiTimeZone === true);
    this.timeZone.set(settings.timeZone || null);
    const stamp = settings.updatedAtUtc || Date.now().toString();
    const logo = this.api.siteAssetUrl(settings.logoUrl);
    const banner = this.api.siteAssetUrl(settings.bannerUrl);
    this.logoUrl.set(logo ? withVersion(logo, stamp) : null);
    this.bannerUrl.set(banner ? withVersion(banner, stamp) : null);
    this.amSessionCount.set(normalizeSessionCount(settings.amSessionCount));
    this.pmSessionCount.set(normalizeSessionCount(settings.pmSessionCount));
    this.pmStartMinutes.set(normalizePmStartMinutes(settings.pmStartMinutes));
    document.title = this.siteName();
    useTenantManifest(this.siteName(), logo ? stamp : null);
    this.loaded.set(true);
  }
  shortName() {
    const name = this.siteName().trim();
    if (!name)
      return "CK";
    const parts = name.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }
  static {
    this.\u0275fac = function SiteBrandService_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _SiteBrandService)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _SiteBrandService, factory: _SiteBrandService.\u0275fac, providedIn: "root" });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(SiteBrandService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();
function withVersion(url, version) {
  return `${url}${url.includes("?") ? "&" : "?"}v=${encodeURIComponent(version)}`;
}

export {
  TENANT_STYLES,
  navItemsForStyle,
  showsField,
  SiteBrandService,
  withVersion
};
//# sourceMappingURL=chunk-G7K3D3BE.js.map
