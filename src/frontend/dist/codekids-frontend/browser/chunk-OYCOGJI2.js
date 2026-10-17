import {
  SiteBrandService
} from "./chunk-NWC5YJ4M.js";
import {
  LocaleService,
  TranslatePipe
} from "./chunk-MAWF43RP.js";
import {
  Component,
  Injectable,
  Input,
  computed,
  inject,
  input,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵattribute,
  ɵɵclassProp,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdefineInjectable,
  ɵɵdomElement,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵdomListener,
  ɵɵdomProperty,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind1,
  ɵɵsanitizeUrl,
  ɵɵtext,
  ɵɵtextInterpolate
} from "./chunk-EMYLRMBO.js";

// src/app/shared/language-switcher/language-switcher.component.ts
var LanguageSwitcherComponent = class _LanguageSwitcherComponent {
  constructor() {
    this.locale = inject(LocaleService);
  }
  setLang(lang) {
    this.locale.setLang(lang);
  }
  static {
    this.\u0275fac = function LanguageSwitcherComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _LanguageSwitcherComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _LanguageSwitcherComponent, selectors: [["app-language-switcher"]], decls: 6, vars: 7, consts: [["role", "group", 1, "lang-switch"], ["type", "button", 1, "lang-btn", 3, "click"]], template: function LanguageSwitcherComponent_Template(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275domElementStart(0, "div", 0);
        \u0275\u0275pipe(1, "t");
        \u0275\u0275domElementStart(2, "button", 1);
        \u0275\u0275domListener("click", function LanguageSwitcherComponent_Template_button_click_2_listener() {
          return ctx.setLang("en");
        });
        \u0275\u0275text(3, " EN ");
        \u0275\u0275domElementEnd();
        \u0275\u0275domElementStart(4, "button", 1);
        \u0275\u0275domListener("click", function LanguageSwitcherComponent_Template_button_click_4_listener() {
          return ctx.setLang("ar");
        });
        \u0275\u0275text(5, " \u0639 ");
        \u0275\u0275domElementEnd()();
      }
      if (rf & 2) {
        \u0275\u0275attribute("aria-label", \u0275\u0275pipeBind1(1, 5, "lang.switch"));
        \u0275\u0275advance(2);
        \u0275\u0275classProp("active", ctx.locale.lang() === "en");
        \u0275\u0275advance(2);
        \u0275\u0275classProp("active", ctx.locale.lang() === "ar");
      }
    }, dependencies: [TranslatePipe], styles: ["\n.lang-switch[_ngcontent-%COMP%] {\n  display: inline-flex;\n  gap: 0.25rem;\n  padding: 0.2rem;\n  border-radius: var(--radius-pill, 999px);\n  background: var(--surface, rgba(255, 255, 255, 0.06));\n  border: 1px solid var(--border, rgba(255, 255, 255, 0.12));\n}\n.lang-btn[_ngcontent-%COMP%] {\n  min-width: 2.2rem;\n  border: 0;\n  border-radius: var(--radius-pill, 999px);\n  padding: 0.35rem 0.65rem;\n  background: transparent;\n  color: var(--text-muted, rgba(244, 247, 251, 0.72));\n  font-weight: 700;\n  font-size: 0.82rem;\n  cursor: pointer;\n  transition: background 0.15s ease, color 0.15s ease;\n}\n.lang-btn[_ngcontent-%COMP%]:hover {\n  color: var(--text, #fff);\n  background: var(--surface, rgba(255, 255, 255, 0.06));\n}\n.lang-btn.active[_ngcontent-%COMP%] {\n  background: var(--switcher-active-bg);\n  color: var(--accent-fg);\n}\n.shell.collapsed[_nghost-%COMP%]   .lang-switch[_ngcontent-%COMP%], .shell.collapsed   [_nghost-%COMP%]   .lang-switch[_ngcontent-%COMP%] {\n  width: 100%;\n  justify-content: center;\n}\n.shell.collapsed[_nghost-%COMP%]   .lang-btn[_ngcontent-%COMP%], .shell.collapsed   [_nghost-%COMP%]   .lang-btn[_ngcontent-%COMP%] {\n  min-width: 0;\n  flex: 1;\n  padding: 0.3rem 0.35rem;\n}\n/*# sourceMappingURL=language-switcher.component.css.map */"] });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(LanguageSwitcherComponent, [{
    type: Component,
    args: [{ selector: "app-language-switcher", imports: [TranslatePipe], template: `<div class="lang-switch" role="group" [attr.aria-label]="'lang.switch' | t">\r
  <button\r
    type="button"\r
    class="lang-btn"\r
    [class.active]="locale.lang() === 'en'"\r
    (click)="setLang('en')"\r
  >\r
    EN\r
  </button>\r
  <button\r
    type="button"\r
    class="lang-btn"\r
    [class.active]="locale.lang() === 'ar'"\r
    (click)="setLang('ar')"\r
  >\r
    \u0639\r
  </button>\r
</div>\r
`, styles: ["/* src/app/shared/language-switcher/language-switcher.component.css */\n.lang-switch {\n  display: inline-flex;\n  gap: 0.25rem;\n  padding: 0.2rem;\n  border-radius: var(--radius-pill, 999px);\n  background: var(--surface, rgba(255, 255, 255, 0.06));\n  border: 1px solid var(--border, rgba(255, 255, 255, 0.12));\n}\n.lang-btn {\n  min-width: 2.2rem;\n  border: 0;\n  border-radius: var(--radius-pill, 999px);\n  padding: 0.35rem 0.65rem;\n  background: transparent;\n  color: var(--text-muted, rgba(244, 247, 251, 0.72));\n  font-weight: 700;\n  font-size: 0.82rem;\n  cursor: pointer;\n  transition: background 0.15s ease, color 0.15s ease;\n}\n.lang-btn:hover {\n  color: var(--text, #fff);\n  background: var(--surface, rgba(255, 255, 255, 0.06));\n}\n.lang-btn.active {\n  background: var(--switcher-active-bg);\n  color: var(--accent-fg);\n}\n:host-context(.shell.collapsed) .lang-switch {\n  width: 100%;\n  justify-content: center;\n}\n:host-context(.shell.collapsed) .lang-btn {\n  min-width: 0;\n  flex: 1;\n  padding: 0.3rem 0.35rem;\n}\n/*# sourceMappingURL=language-switcher.component.css.map */\n"] }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(LanguageSwitcherComponent, { className: "LanguageSwitcherComponent", filePath: "src/app/shared/language-switcher/language-switcher.component.ts", lineNumber: 11 });
})();

// src/app/theme/theme.service.ts
var THEME_KEY = "codekids_theme";
var ThemeService = class _ThemeService {
  constructor() {
    this.theme = signal(
      this.readStoredTheme(),
      ...ngDevMode ? [{ debugName: "theme" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.applyDocument(this.theme());
  }
  setTheme(theme) {
    if (this.theme() === theme)
      return;
    this.theme.set(theme);
    localStorage.setItem(THEME_KEY, theme);
    this.applyDocument(theme);
  }
  toggleTheme() {
    this.setTheme(this.theme() === "dark" ? "light" : "dark");
  }
  readStoredTheme() {
    return localStorage.getItem(THEME_KEY) === "light" ? "light" : "dark";
  }
  applyDocument(theme) {
    const root = document.documentElement;
    if (theme === "light") {
      root.dataset["theme"] = "light";
    } else {
      delete root.dataset["theme"];
    }
  }
  static {
    this.\u0275fac = function ThemeService_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _ThemeService)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _ThemeService, factory: _ThemeService.\u0275fac, providedIn: "root" });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ThemeService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], () => [], null);
})();

// src/app/shared/theme-switcher/theme-switcher.component.ts
var ThemeSwitcherComponent = class _ThemeSwitcherComponent {
  constructor() {
    this.theme = inject(ThemeService);
  }
  setTheme(mode) {
    this.theme.setTheme(mode);
  }
  static {
    this.\u0275fac = function ThemeSwitcherComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _ThemeSwitcherComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ThemeSwitcherComponent, selectors: [["app-theme-switcher"]], decls: 10, vars: 19, consts: [["role", "group", 1, "theme-switch"], ["type", "button", 1, "theme-btn", 3, "click", "title"], ["aria-hidden", "true", 1, "fa-solid", "fa-sun"], ["aria-hidden", "true", 1, "fa-solid", "fa-moon"]], template: function ThemeSwitcherComponent_Template(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275domElementStart(0, "div", 0);
        \u0275\u0275pipe(1, "t");
        \u0275\u0275domElementStart(2, "button", 1);
        \u0275\u0275pipe(3, "t");
        \u0275\u0275pipe(4, "t");
        \u0275\u0275domListener("click", function ThemeSwitcherComponent_Template_button_click_2_listener() {
          return ctx.setTheme("light");
        });
        \u0275\u0275domElement(5, "i", 2);
        \u0275\u0275domElementEnd();
        \u0275\u0275domElementStart(6, "button", 1);
        \u0275\u0275pipe(7, "t");
        \u0275\u0275pipe(8, "t");
        \u0275\u0275domListener("click", function ThemeSwitcherComponent_Template_button_click_6_listener() {
          return ctx.setTheme("dark");
        });
        \u0275\u0275domElement(9, "i", 3);
        \u0275\u0275domElementEnd()();
      }
      if (rf & 2) {
        \u0275\u0275attribute("aria-label", \u0275\u0275pipeBind1(1, 9, "theme.switch"));
        \u0275\u0275advance(2);
        \u0275\u0275classProp("active", ctx.theme.theme() === "light");
        \u0275\u0275domProperty("title", \u0275\u0275pipeBind1(3, 11, "theme.light"));
        \u0275\u0275attribute("aria-label", \u0275\u0275pipeBind1(4, 13, "theme.light"));
        \u0275\u0275advance(4);
        \u0275\u0275classProp("active", ctx.theme.theme() === "dark");
        \u0275\u0275domProperty("title", \u0275\u0275pipeBind1(7, 15, "theme.dark"));
        \u0275\u0275attribute("aria-label", \u0275\u0275pipeBind1(8, 17, "theme.dark"));
      }
    }, dependencies: [TranslatePipe], styles: ["\n.theme-switch[_ngcontent-%COMP%] {\n  display: inline-flex;\n  gap: 0.25rem;\n  padding: 0.2rem;\n  border-radius: var(--radius-pill, 999px);\n  background: var(--surface, rgba(255, 255, 255, 0.06));\n  border: 1px solid var(--border, rgba(255, 255, 255, 0.12));\n}\n.theme-btn[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n  width: 2rem;\n  height: 2rem;\n  border: 0;\n  border-radius: var(--radius-pill, 999px);\n  padding: 0;\n  background: transparent;\n  color: var(--text-muted, rgba(244, 247, 251, 0.72));\n  font-size: 0.9rem;\n  cursor: pointer;\n  transition: background 0.15s ease, color 0.15s ease;\n}\n.theme-btn[_ngcontent-%COMP%]:hover {\n  color: var(--text, #fff);\n  background: var(--surface, rgba(255, 255, 255, 0.06));\n}\n.theme-btn.active[_ngcontent-%COMP%] {\n  background: var(--switcher-active-bg);\n  color: var(--accent-fg);\n}\n.shell.collapsed[_nghost-%COMP%]   .theme-switch[_ngcontent-%COMP%], .shell.collapsed   [_nghost-%COMP%]   .theme-switch[_ngcontent-%COMP%] {\n  width: 100%;\n  justify-content: center;\n}\n.shell.collapsed[_nghost-%COMP%]   .theme-btn[_ngcontent-%COMP%], .shell.collapsed   [_nghost-%COMP%]   .theme-btn[_ngcontent-%COMP%] {\n  width: auto;\n  flex: 1;\n  height: 1.85rem;\n}\n/*# sourceMappingURL=theme-switcher.component.css.map */"] });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ThemeSwitcherComponent, [{
    type: Component,
    args: [{ selector: "app-theme-switcher", imports: [TranslatePipe], template: `<div class="theme-switch" role="group" [attr.aria-label]="'theme.switch' | t">\r
  <button\r
    type="button"\r
    class="theme-btn"\r
    [class.active]="theme.theme() === 'light'"\r
    (click)="setTheme('light')"\r
    [attr.aria-label]="'theme.light' | t"\r
    [title]="'theme.light' | t"\r
  >\r
    <i class="fa-solid fa-sun" aria-hidden="true"></i>\r
  </button>\r
  <button\r
    type="button"\r
    class="theme-btn"\r
    [class.active]="theme.theme() === 'dark'"\r
    (click)="setTheme('dark')"\r
    [attr.aria-label]="'theme.dark' | t"\r
    [title]="'theme.dark' | t"\r
  >\r
    <i class="fa-solid fa-moon" aria-hidden="true"></i>\r
  </button>\r
</div>\r
`, styles: ["/* src/app/shared/theme-switcher/theme-switcher.component.css */\n.theme-switch {\n  display: inline-flex;\n  gap: 0.25rem;\n  padding: 0.2rem;\n  border-radius: var(--radius-pill, 999px);\n  background: var(--surface, rgba(255, 255, 255, 0.06));\n  border: 1px solid var(--border, rgba(255, 255, 255, 0.12));\n}\n.theme-btn {\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n  width: 2rem;\n  height: 2rem;\n  border: 0;\n  border-radius: var(--radius-pill, 999px);\n  padding: 0;\n  background: transparent;\n  color: var(--text-muted, rgba(244, 247, 251, 0.72));\n  font-size: 0.9rem;\n  cursor: pointer;\n  transition: background 0.15s ease, color 0.15s ease;\n}\n.theme-btn:hover {\n  color: var(--text, #fff);\n  background: var(--surface, rgba(255, 255, 255, 0.06));\n}\n.theme-btn.active {\n  background: var(--switcher-active-bg);\n  color: var(--accent-fg);\n}\n:host-context(.shell.collapsed) .theme-switch {\n  width: 100%;\n  justify-content: center;\n}\n:host-context(.shell.collapsed) .theme-btn {\n  width: auto;\n  flex: 1;\n  height: 1.85rem;\n}\n/*# sourceMappingURL=theme-switcher.component.css.map */\n"] }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ThemeSwitcherComponent, { className: "ThemeSwitcherComponent", filePath: "src/app/shared/theme-switcher/theme-switcher.component.ts", lineNumber: 11 });
})();

// src/app/shared/site-brand/site-brand.component.ts
function SiteBrandComponent_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElement(0, "img", 1);
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275domProperty("src", ctx, \u0275\u0275sanitizeUrl)("alt", ctx_r0.brand.siteName());
  }
}
var SiteBrandComponent = class _SiteBrandComponent {
  constructor() {
    this.brand = inject(SiteBrandService);
    this.compact = input(
      false,
      ...ngDevMode ? [{ debugName: "compact" }] : (
        /* istanbul ignore next */
        []
      )
    );
  }
  static {
    this.\u0275fac = function SiteBrandComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _SiteBrandComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _SiteBrandComponent, selectors: [["app-site-brand"]], inputs: { compact: [1, "compact"] }, decls: 4, vars: 4, consts: [[1, "site-brand"], [1, "site-brand-logo", 3, "src", "alt"], [1, "brand"]], template: function SiteBrandComponent_Template(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275domElementStart(0, "div", 0);
        \u0275\u0275conditionalCreate(1, SiteBrandComponent_Conditional_1_Template, 1, 2, "img", 1);
        \u0275\u0275domElementStart(2, "p", 2);
        \u0275\u0275text(3);
        \u0275\u0275domElementEnd()();
      }
      if (rf & 2) {
        let tmp_1_0;
        \u0275\u0275classProp("compact", ctx.compact());
        \u0275\u0275advance();
        \u0275\u0275conditional((tmp_1_0 = ctx.brand.logoUrl()) ? 1 : -1, tmp_1_0);
        \u0275\u0275advance(2);
        \u0275\u0275textInterpolate(ctx.brand.siteName());
      }
    }, styles: ["\n.site-brand[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 0.65rem;\n  min-width: 0;\n}\n.site-brand-logo[_ngcontent-%COMP%] {\n  width: 40px;\n  height: 40px;\n  object-fit: contain;\n  border-radius: 10px;\n  background: rgba(255, 255, 255, 0.08);\n  padding: 0.15rem;\n  flex-shrink: 0;\n}\n.compact[_ngcontent-%COMP%]   .site-brand-logo[_ngcontent-%COMP%] {\n  width: 32px;\n  height: 32px;\n}\n.brand[_ngcontent-%COMP%] {\n  margin: 0;\n  font-family: var(--font-display);\n  font-size: 1.35rem;\n  font-weight: 800;\n  text-transform: uppercase;\n  letter-spacing: 0.05em;\n  line-height: 1.1;\n}\n.compact[_ngcontent-%COMP%]   .brand[_ngcontent-%COMP%] {\n  font-size: 1.1rem;\n}\n/*# sourceMappingURL=site-brand.component.css.map */"] });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(SiteBrandComponent, [{
    type: Component,
    args: [{ selector: "app-site-brand", standalone: true, template: `
    <div class="site-brand" [class.compact]="compact()">
      @if (brand.logoUrl(); as logo) {
        <img class="site-brand-logo" [src]="logo" [alt]="brand.siteName()" />
      }
      <p class="brand">{{ brand.siteName() }}</p>
    </div>
  `, styles: ["/* angular:styles/component:css;51f58a8790b31503894259d88b4ae027309f2effd2321466b00eb13064270ba9;D:/Projects/CodeKids/src/frontend/src/app/shared/site-brand/site-brand.component.ts */\n.site-brand {\n  display: flex;\n  align-items: center;\n  gap: 0.65rem;\n  min-width: 0;\n}\n.site-brand-logo {\n  width: 40px;\n  height: 40px;\n  object-fit: contain;\n  border-radius: 10px;\n  background: rgba(255, 255, 255, 0.08);\n  padding: 0.15rem;\n  flex-shrink: 0;\n}\n.compact .site-brand-logo {\n  width: 32px;\n  height: 32px;\n}\n.brand {\n  margin: 0;\n  font-family: var(--font-display);\n  font-size: 1.35rem;\n  font-weight: 800;\n  text-transform: uppercase;\n  letter-spacing: 0.05em;\n  line-height: 1.1;\n}\n.compact .brand {\n  font-size: 1.1rem;\n}\n/*# sourceMappingURL=site-brand.component.css.map */\n"] }]
  }], null, { compact: [{ type: Input, args: [{ isSignal: true, alias: "compact", required: false }] }] });
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(SiteBrandComponent, { className: "SiteBrandComponent", filePath: "src/app/shared/site-brand/site-brand.component.ts", lineNumber: 53 });
})();

// src/app/api-busy.service.ts
var ApiBusyService = class _ApiBusyService {
  constructor() {
    this.pending = signal(
      0,
      ...ngDevMode ? [{ debugName: "pending" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.busy = computed(
      () => this.pending() > 0,
      ...ngDevMode ? [{ debugName: "busy" }] : (
        /* istanbul ignore next */
        []
      )
    );
  }
  begin() {
    this.pending.update((count) => count + 1);
  }
  end() {
    this.pending.update((count) => Math.max(0, count - 1));
  }
  static {
    this.\u0275fac = function ApiBusyService_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _ApiBusyService)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _ApiBusyService, factory: _ApiBusyService.\u0275fac, providedIn: "root" });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApiBusyService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/shared/api-busy-indicator/api-busy-indicator.component.ts
function ApiBusyIndicatorComponent_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 0);
    \u0275\u0275domElement(1, "span", 1);
    \u0275\u0275domElementStart(2, "p");
    \u0275\u0275text(3);
    \u0275\u0275pipe(4, "t");
    \u0275\u0275domElementEnd()();
  }
  if (rf & 2) {
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(4, 1, "common.loading"));
  }
}
var ApiBusyIndicatorComponent = class _ApiBusyIndicatorComponent {
  constructor() {
    this.apiBusy = inject(ApiBusyService);
    this.busy = input(
      void 0,
      ...ngDevMode ? [{ debugName: "busy" }] : (
        /* istanbul ignore next */
        []
      )
    );
  }
  static {
    this.\u0275fac = function ApiBusyIndicatorComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _ApiBusyIndicatorComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ApiBusyIndicatorComponent, selectors: [["app-api-busy-indicator"]], inputs: { busy: [1, "busy"] }, decls: 1, vars: 1, consts: [["role", "status", "aria-live", "polite", "aria-busy", "true", 1, "api-busy"], ["aria-hidden", "true", 1, "api-busy-spinner"]], template: function ApiBusyIndicatorComponent_Template(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275conditionalCreate(0, ApiBusyIndicatorComponent_Conditional_0_Template, 5, 3, "div", 0);
      }
      if (rf & 2) {
        \u0275\u0275conditional(ctx.busy() ?? ctx.apiBusy.busy() ? 0 : -1);
      }
    }, dependencies: [TranslatePipe], styles: ["\n[_nghost-%COMP%] {\n  display: contents;\n}\n.api-busy[_ngcontent-%COMP%] {\n  position: absolute;\n  inset: 0;\n  z-index: 20;\n  display: grid;\n  place-content: center;\n  justify-items: center;\n  gap: 0.65rem;\n  pointer-events: none;\n  background: rgba(7, 17, 31, 0.28);\n}\n.api-busy[_ngcontent-%COMP%]   p[_ngcontent-%COMP%] {\n  margin: 0;\n  padding: 0.35rem 0.7rem;\n  border-radius: var(--radius-pill);\n  background: var(--bg-elevated);\n  border: 1px solid var(--border);\n  color: var(--text);\n  font-weight: 600;\n  font-size: 0.92rem;\n}\n.api-busy-spinner[_ngcontent-%COMP%] {\n  width: 1.85rem;\n  height: 1.85rem;\n  border-radius: 50%;\n  border: 3px solid rgba(255, 214, 10, 0.22);\n  border-top-color: var(--accent);\n  animation: _ngcontent-%COMP%_api-spin 0.7s linear infinite;\n}\n@keyframes _ngcontent-%COMP%_api-spin {\n  to {\n    transform: rotate(360deg);\n  }\n}\n/*# sourceMappingURL=api-busy-indicator.component.css.map */"] });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApiBusyIndicatorComponent, [{
    type: Component,
    args: [{ selector: "app-api-busy-indicator", imports: [TranslatePipe], template: `
    @if (busy() ?? apiBusy.busy()) {
      <div class="api-busy" role="status" aria-live="polite" aria-busy="true">
        <span class="api-busy-spinner" aria-hidden="true"></span>
        <p>{{ 'common.loading' | t }}</p>
      </div>
    }
  `, styles: ["/* angular:styles/component:css;5c4f6c8066f5c4ae09be35fec2e2eed6e4f976bdc50d8ea90e56067d280ac671;D:/Projects/CodeKids/src/frontend/src/app/shared/api-busy-indicator/api-busy-indicator.component.ts */\n:host {\n  display: contents;\n}\n.api-busy {\n  position: absolute;\n  inset: 0;\n  z-index: 20;\n  display: grid;\n  place-content: center;\n  justify-items: center;\n  gap: 0.65rem;\n  pointer-events: none;\n  background: rgba(7, 17, 31, 0.28);\n}\n.api-busy p {\n  margin: 0;\n  padding: 0.35rem 0.7rem;\n  border-radius: var(--radius-pill);\n  background: var(--bg-elevated);\n  border: 1px solid var(--border);\n  color: var(--text);\n  font-weight: 600;\n  font-size: 0.92rem;\n}\n.api-busy-spinner {\n  width: 1.85rem;\n  height: 1.85rem;\n  border-radius: 50%;\n  border: 3px solid rgba(255, 214, 10, 0.22);\n  border-top-color: var(--accent);\n  animation: api-spin 0.7s linear infinite;\n}\n@keyframes api-spin {\n  to {\n    transform: rotate(360deg);\n  }\n}\n/*# sourceMappingURL=api-busy-indicator.component.css.map */\n"] }]
  }], null, { busy: [{ type: Input, args: [{ isSignal: true, alias: "busy", required: false }] }] });
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ApiBusyIndicatorComponent, { className: "ApiBusyIndicatorComponent", filePath: "src/app/shared/api-busy-indicator/api-busy-indicator.component.ts", lineNumber: 60 });
})();

export {
  LanguageSwitcherComponent,
  ThemeService,
  ThemeSwitcherComponent,
  SiteBrandComponent,
  ApiBusyService,
  ApiBusyIndicatorComponent
};
//# sourceMappingURL=chunk-OYCOGJI2.js.map
