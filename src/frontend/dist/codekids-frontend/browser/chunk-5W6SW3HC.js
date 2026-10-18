import {
  ApiBusyIndicatorComponent,
  LanguageSwitcherComponent,
  SiteBrandComponent,
  ThemeSwitcherComponent
} from "./chunk-NM7TRCB5.js";
import {
  AuthService
} from "./chunk-MMGMRKLA.js";
import "./chunk-NWC5YJ4M.js";
import {
  RouterLink
} from "./chunk-RN452YVM.js";
import "./chunk-RLZFB7T3.js";
import {
  LocaleService,
  TranslatePipe
} from "./chunk-5TRF6X2H.js";
import {
  LearningApiService
} from "./chunk-7YFOTRM2.js";
import {
  Component,
  DefaultValueAccessor,
  FormsModule,
  NgControlStatus,
  NgControlStatusGroup,
  NgForm,
  NgModel,
  inject,
  setClassMetadata,
  signal,
  ɵNgNoValidate,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵcontrol,
  ɵɵcontrolCreate,
  ɵɵdefineComponent,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵlistener,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind1,
  ɵɵproperty,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1,
  ɵɵtwoWayBindingSet,
  ɵɵtwoWayListener,
  ɵɵtwoWayProperty
} from "./chunk-EMYLRMBO.js";

// src/app/pages/parent-account/parent-account.component.ts
function ParentAccountComponent_Conditional_17_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 5);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r0.error());
  }
}
function ParentAccountComponent_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 6);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r0.message());
  }
}
var ParentAccountComponent = class _ParentAccountComponent {
  constructor() {
    this.auth = inject(AuthService);
    this.api = inject(LearningApiService);
    this.locale = inject(LocaleService);
    this.saving = signal(
      false,
      ...ngDevMode ? [{ debugName: "saving" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.message = signal(
      "",
      ...ngDevMode ? [{ debugName: "message" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.error = signal(
      "",
      ...ngDevMode ? [{ debugName: "error" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.parentEmail = "";
    this.parentMobile = "";
    this.parentPassword = "";
    this.parentPasswordConfirm = "";
    this.api.getParentDashboard().subscribe({
      next: (dashboard) => {
        this.parentEmail = dashboard.parentEmail ?? "";
        this.parentMobile = dashboard.parentMobilePhone ?? "";
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, "parent.loadChildFailed"))
    });
  }
  saveAccount() {
    this.message.set("");
    this.error.set("");
    const email = this.parentEmail.trim();
    const mobilePhone = this.parentMobile.trim();
    if (!email && !mobilePhone) {
      this.error.set(this.locale.t("admin.users.emailOrMobileRequired"));
      return;
    }
    if (this.parentPassword || this.parentPasswordConfirm) {
      if (this.parentPassword !== this.parentPasswordConfirm) {
        this.error.set(this.locale.t("auth.reset.mismatch"));
        return;
      }
      if (this.parentPassword.trim().length < 6) {
        this.error.set(this.locale.t("api.errors.auth.passwordTooShort"));
        return;
      }
    }
    const userId = this.auth.user()?.id;
    if (!userId)
      return;
    this.saving.set(true);
    this.api.updateParentManagedAccount(userId, {
      email: email || null,
      mobilePhone: mobilePhone || null,
      password: this.parentPassword.trim() || null
    }).subscribe({
      next: (account) => {
        this.saving.set(false);
        this.parentPassword = "";
        this.parentPasswordConfirm = "";
        this.auth.patchUser({ email: account.email, mobilePhone: account.mobilePhone });
        this.message.set(this.locale.t("parent.accountSaved"));
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.locale.fromApiError(err, "parent.accountSaveFailed"));
      }
    });
  }
  static {
    this.\u0275fac = function ParentAccountComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _ParentAccountComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ParentAccountComponent, selectors: [["app-parent-account"]], decls: 49, vars: 39, consts: [[1, "page", "parent-page"], [1, "topbar"], [1, "topbar-actions"], ["routerLink", "/parent", 1, "chip", "quiz"], ["type", "button", 1, "ghost", 3, "click"], [1, "feedback"], [1, "feedback", "ok"], [1, "block"], [1, "section-head"], [1, "section-hint"], [1, "meeting-form", 3, "ngSubmit"], ["name", "parentEmail", "type", "email", "autocomplete", "email", 3, "ngModelChange", "ngModel"], ["name", "parentMobile", "autocomplete", "tel", 3, "ngModelChange", "ngModel"], ["name", "parentPassword", "type", "password", "autocomplete", "new-password", 3, "ngModelChange", "ngModel"], ["name", "parentPasswordConfirm", "type", "password", "autocomplete", "new-password", 3, "ngModelChange", "ngModel"], ["type", "submit", 3, "disabled"]], template: function ParentAccountComponent_Template(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275elementStart(0, "div", 0);
        \u0275\u0275element(1, "app-api-busy-indicator");
        \u0275\u0275elementStart(2, "header", 1)(3, "div");
        \u0275\u0275element(4, "app-site-brand");
        \u0275\u0275elementStart(5, "h1");
        \u0275\u0275text(6);
        \u0275\u0275pipe(7, "t");
        \u0275\u0275elementEnd()();
        \u0275\u0275elementStart(8, "div", 2)(9, "a", 3);
        \u0275\u0275text(10);
        \u0275\u0275pipe(11, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275element(12, "app-theme-switcher")(13, "app-language-switcher");
        \u0275\u0275elementStart(14, "button", 4);
        \u0275\u0275listener("click", function ParentAccountComponent_Template_button_click_14_listener() {
          return ctx.auth.logout();
        });
        \u0275\u0275text(15);
        \u0275\u0275pipe(16, "t");
        \u0275\u0275elementEnd()()();
        \u0275\u0275conditionalCreate(17, ParentAccountComponent_Conditional_17_Template, 2, 1, "p", 5);
        \u0275\u0275conditionalCreate(18, ParentAccountComponent_Conditional_18_Template, 2, 1, "p", 6);
        \u0275\u0275elementStart(19, "section", 7)(20, "header", 8)(21, "div")(22, "h2");
        \u0275\u0275text(23);
        \u0275\u0275pipe(24, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(25, "p", 9);
        \u0275\u0275text(26);
        \u0275\u0275pipe(27, "t");
        \u0275\u0275elementEnd()()();
        \u0275\u0275elementStart(28, "form", 10);
        \u0275\u0275listener("ngSubmit", function ParentAccountComponent_Template_form_ngSubmit_28_listener() {
          return ctx.saveAccount();
        });
        \u0275\u0275elementStart(29, "label");
        \u0275\u0275text(30);
        \u0275\u0275pipe(31, "t");
        \u0275\u0275elementStart(32, "input", 11);
        \u0275\u0275twoWayListener("ngModelChange", function ParentAccountComponent_Template_input_ngModelChange_32_listener($event) {
          \u0275\u0275twoWayBindingSet(ctx.parentEmail, $event) || (ctx.parentEmail = $event);
          return $event;
        });
        \u0275\u0275elementEnd();
        \u0275\u0275controlCreate();
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(33, "label");
        \u0275\u0275text(34);
        \u0275\u0275pipe(35, "t");
        \u0275\u0275elementStart(36, "input", 12);
        \u0275\u0275twoWayListener("ngModelChange", function ParentAccountComponent_Template_input_ngModelChange_36_listener($event) {
          \u0275\u0275twoWayBindingSet(ctx.parentMobile, $event) || (ctx.parentMobile = $event);
          return $event;
        });
        \u0275\u0275elementEnd();
        \u0275\u0275controlCreate();
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(37, "label");
        \u0275\u0275text(38);
        \u0275\u0275pipe(39, "t");
        \u0275\u0275elementStart(40, "input", 13);
        \u0275\u0275twoWayListener("ngModelChange", function ParentAccountComponent_Template_input_ngModelChange_40_listener($event) {
          \u0275\u0275twoWayBindingSet(ctx.parentPassword, $event) || (ctx.parentPassword = $event);
          return $event;
        });
        \u0275\u0275elementEnd();
        \u0275\u0275controlCreate();
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(41, "label");
        \u0275\u0275text(42);
        \u0275\u0275pipe(43, "t");
        \u0275\u0275elementStart(44, "input", 14);
        \u0275\u0275twoWayListener("ngModelChange", function ParentAccountComponent_Template_input_ngModelChange_44_listener($event) {
          \u0275\u0275twoWayBindingSet(ctx.parentPasswordConfirm, $event) || (ctx.parentPasswordConfirm = $event);
          return $event;
        });
        \u0275\u0275elementEnd();
        \u0275\u0275controlCreate();
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(45, "button", 15);
        \u0275\u0275text(46);
        \u0275\u0275pipe(47, "t");
        \u0275\u0275pipe(48, "t");
        \u0275\u0275elementEnd()()()();
      }
      if (rf & 2) {
        \u0275\u0275advance(6);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(7, 17, "parent.accountTitle"));
        \u0275\u0275advance(4);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(11, 19, "parent.backToChildren"));
        \u0275\u0275advance(5);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(16, 21, "common.signOut"));
        \u0275\u0275advance(2);
        \u0275\u0275conditional(ctx.error() ? 17 : -1);
        \u0275\u0275advance();
        \u0275\u0275conditional(ctx.message() ? 18 : -1);
        \u0275\u0275advance(5);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(24, 23, "parent.accountTitle"));
        \u0275\u0275advance(3);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(27, 25, "parent.accountHint"));
        \u0275\u0275advance(4);
        \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(31, 27, "common.email"), " ");
        \u0275\u0275advance(2);
        \u0275\u0275twoWayProperty("ngModel", ctx.parentEmail);
        \u0275\u0275control();
        \u0275\u0275advance(2);
        \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(35, 29, "common.mobile"), " ");
        \u0275\u0275advance(2);
        \u0275\u0275twoWayProperty("ngModel", ctx.parentMobile);
        \u0275\u0275control();
        \u0275\u0275advance(2);
        \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(39, 31, "common.newPasswordOptional"), " ");
        \u0275\u0275advance(2);
        \u0275\u0275twoWayProperty("ngModel", ctx.parentPassword);
        \u0275\u0275control();
        \u0275\u0275advance(2);
        \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(43, 33, "auth.reset.confirmPassword"), " ");
        \u0275\u0275advance(2);
        \u0275\u0275twoWayProperty("ngModel", ctx.parentPasswordConfirm);
        \u0275\u0275control();
        \u0275\u0275advance();
        \u0275\u0275property("disabled", ctx.saving());
        \u0275\u0275advance();
        \u0275\u0275textInterpolate1(" ", ctx.saving() ? \u0275\u0275pipeBind1(47, 35, "common.saving") : \u0275\u0275pipeBind1(48, 37, "common.save"), " ");
      }
    }, dependencies: [
      FormsModule,
      \u0275NgNoValidate,
      DefaultValueAccessor,
      NgControlStatus,
      NgControlStatusGroup,
      NgModel,
      NgForm,
      RouterLink,
      SiteBrandComponent,
      ThemeSwitcherComponent,
      LanguageSwitcherComponent,
      ApiBusyIndicatorComponent,
      TranslatePipe
    ], styles: ["\n.page[_ngcontent-%COMP%] {\n  position: relative;\n  min-height: 100vh;\n  padding: var(--space-5) 6vw 4rem;\n  color: var(--text);\n  background:\n    radial-gradient(\n      circle at 88% 0%,\n      var(--page-glow-1),\n      transparent 28%),\n    radial-gradient(\n      circle at 8% 12%,\n      var(--page-glow-2),\n      transparent 22%),\n    var(--bg);\n}\n.page[_ngcontent-%COMP%]   p[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   span[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   small[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   label[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   li[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   td[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   th[_ngcontent-%COMP%] {\n  color: inherit;\n}\n.topbar[_ngcontent-%COMP%], \n.hero-strip[_ngcontent-%COMP%], \n.grid-two[_ngcontent-%COMP%], \n.grid-cards[_ngcontent-%COMP%], \n.chip-row[_ngcontent-%COMP%], \n.avatar-row[_ngcontent-%COMP%] {\n  display: flex;\n  gap: var(--space-3);\n}\n.topbar[_ngcontent-%COMP%] {\n  justify-content: space-between;\n  align-items: center;\n  margin-bottom: var(--space-5);\n}\n.brand[_ngcontent-%COMP%] {\n  margin: 0;\n  font-family: var(--font-display);\n  font-size: clamp(1.75rem, 3vw, 2.15rem);\n  font-weight: 800;\n  text-transform: uppercase;\n  color: var(--heading);\n  letter-spacing: 0.04em;\n}\nh1[_ngcontent-%COMP%], \nh2[_ngcontent-%COMP%], \nh3[_ngcontent-%COMP%], \nh4[_ngcontent-%COMP%] {\n  font-family: var(--font-display);\n  margin: 0.15rem 0;\n  color: var(--heading);\n  letter-spacing: 0.01em;\n  line-height: 1.15;\n}\nh1[_ngcontent-%COMP%] {\n  font-size: clamp(1.8rem, 3vw, 2.4rem);\n}\nh2[_ngcontent-%COMP%] {\n  font-size: clamp(1.4rem, 2.4vw, 1.85rem);\n}\nh3[_ngcontent-%COMP%] {\n  font-size: 1.2rem;\n}\n.hero-strip[_ngcontent-%COMP%] {\n  justify-content: space-between;\n  align-items: center;\n  gap: var(--space-4);\n  padding: 1.5rem 1.6rem;\n  border-radius: var(--radius-xl);\n  margin-bottom: var(--space-5);\n  background: var(--hero-bg);\n  border: 1px solid var(--hero-border);\n  box-shadow: var(--shadow-sm);\n  color: var(--hero-fg);\n}\n.hero-strip[_ngcontent-%COMP%]   p[_ngcontent-%COMP%], \n.hero-strip[_ngcontent-%COMP%]   h2[_ngcontent-%COMP%] {\n  color: var(--hero-fg);\n}\n.eyebrow[_ngcontent-%COMP%], \n.meta[_ngcontent-%COMP%], \n.back[_ngcontent-%COMP%] {\n  color: var(--text-muted);\n}\n.eyebrow[_ngcontent-%COMP%] {\n  margin: 0 0 0.35rem;\n  font-size: 0.78rem;\n  font-weight: 700;\n  letter-spacing: 0.08em;\n  text-transform: uppercase;\n  color: var(--teal);\n}\n.back[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.35rem;\n  margin-bottom: var(--space-3);\n  text-decoration: none;\n  font-weight: 600;\n  transition: color 0.15s ease;\n}\n.back[_ngcontent-%COMP%]:hover {\n  color: var(--heading);\n}\n.xp-pill[_ngcontent-%COMP%], \nbutton[_ngcontent-%COMP%], \n.chip[_ngcontent-%COMP%], \n.list-btn[_ngcontent-%COMP%], \n.avatar[_ngcontent-%COMP%], \n.badge[_ngcontent-%COMP%] {\n  border: none;\n  border-radius: var(--radius-pill);\n  font: inherit;\n}\n.xp-pill[_ngcontent-%COMP%], \nbutton[_ngcontent-%COMP%] {\n  padding: 0.8rem 1.15rem;\n  background:\n    linear-gradient(\n      135deg,\n      var(--accent),\n      var(--accent-hot));\n  color: var(--accent-ink);\n  font-weight: 800;\n  cursor: pointer;\n  transition:\n    transform 0.15s ease,\n    box-shadow 0.15s ease,\n    opacity 0.15s ease;\n  box-shadow: var(--btn-shadow);\n}\nbutton[_ngcontent-%COMP%]:hover:not(:disabled) {\n  transform: translateY(-1px);\n  box-shadow: var(--btn-shadow-hover);\n}\nbutton[_ngcontent-%COMP%]:active:not(:disabled) {\n  transform: translateY(0);\n}\nbutton[_ngcontent-%COMP%]:disabled {\n  opacity: 0.55;\n  cursor: not-allowed;\n  box-shadow: none;\n}\nbutton[_ngcontent-%COMP%]:focus-visible, \n.chip[_ngcontent-%COMP%]:focus-visible, \n.list-btn[_ngcontent-%COMP%]:focus-visible, \na[_ngcontent-%COMP%]:focus-visible, \ninput[_ngcontent-%COMP%]:focus-visible, \nselect[_ngcontent-%COMP%]:focus-visible, \ntextarea[_ngcontent-%COMP%]:focus-visible {\n  outline: none;\n  box-shadow: var(--focus-ring);\n}\nbutton.ghost[_ngcontent-%COMP%], \n.ghost-btn[_ngcontent-%COMP%] {\n  background: transparent;\n  color: var(--ghost-fg);\n  border: 1px solid var(--border-strong);\n  box-shadow: none;\n}\nbutton.ghost[_ngcontent-%COMP%]:hover:not(:disabled), \n.ghost-btn[_ngcontent-%COMP%]:hover:not(:disabled) {\n  background: var(--surface-strong);\n  box-shadow: none;\n}\n.grid-two[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: 1.3fr 0.9fr;\n  gap: var(--space-4);\n}\n.grid-cards[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));\n}\n.side-stack[_ngcontent-%COMP%] {\n  display: grid;\n  gap: var(--space-4);\n}\n.block[_ngcontent-%COMP%], \n.badge[_ngcontent-%COMP%], \n.avatar[_ngcontent-%COMP%] {\n  background: var(--surface);\n  border: 1px solid var(--border);\n  border-radius: var(--radius-lg);\n  padding: 1.25rem;\n  color: var(--text);\n}\n.block[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.85rem;\n  margin-bottom: var(--space-3);\n  box-shadow: var(--shadow-sm);\n  position: relative;\n  z-index: 0;\n}\n.block[_ngcontent-%COMP%]:has(app-searchable-select.ss--open), \n.block[_ngcontent-%COMP%]:has(app-searchable-multi-select.ms--open) {\n  z-index: 50;\n}\n.block[_ngcontent-%COMP%]    > h3[_ngcontent-%COMP%] {\n  padding-bottom: 0.55rem;\n  border-bottom: 1px solid var(--border);\n}\n.block[_ngcontent-%COMP%]   p[_ngcontent-%COMP%], \n.block[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.block[_ngcontent-%COMP%]   small[_ngcontent-%COMP%] {\n  color: var(--text);\n}\n.chip-row[_ngcontent-%COMP%], \n.avatar-row[_ngcontent-%COMP%] {\n  flex-wrap: wrap;\n}\n.chip[_ngcontent-%COMP%], \n.list-btn[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.4rem;\n  padding: 0.65rem 0.95rem;\n  background: var(--chip-bg);\n  border: 1px solid var(--chip-border);\n  color: var(--chip-fg);\n  text-decoration: none;\n  cursor: pointer;\n  transition:\n    background 0.15s ease,\n    border-color 0.15s ease,\n    transform 0.15s ease;\n}\n.chip[_ngcontent-%COMP%]:hover, \n.list-btn[_ngcontent-%COMP%]:hover {\n  background: var(--chip-bg);\n  border-color: var(--chip-border);\n  filter: brightness(0.97);\n  transform: translateY(-1px);\n}\n.chip.quiz[_ngcontent-%COMP%] {\n  background: rgba(95, 211, 188, 0.16);\n  border-color: rgba(95, 211, 188, 0.22);\n}\n.chip.video[_ngcontent-%COMP%] {\n  background: rgba(255, 214, 10, 0.16);\n  border-color: rgba(255, 214, 10, 0.28);\n}\n.list-btn[_ngcontent-%COMP%] {\n  width: 100%;\n  text-align: left;\n  margin-bottom: 0.45rem;\n  border-radius: var(--radius-md);\n}\n.list-btn.active[_ngcontent-%COMP%], \n.avatar.selected[_ngcontent-%COMP%], \n.badge.earned[_ngcontent-%COMP%] {\n  background:\n    linear-gradient(\n      135deg,\n      var(--accent),\n      var(--accent-hot));\n  border-color: transparent;\n  color: var(--accent-ink);\n}\n.avatar[_ngcontent-%COMP%] {\n  width: 9.5rem;\n  display: grid;\n  gap: 0.3rem;\n  text-align: left;\n  color: var(--text);\n  cursor: pointer;\n  transition: transform 0.15s ease, border-color 0.15s ease;\n}\n.avatar[_ngcontent-%COMP%]:hover:not(:disabled) {\n  transform: translateY(-2px);\n  border-color: var(--border-strong);\n}\n.avatar[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.avatar[_ngcontent-%COMP%]   small[_ngcontent-%COMP%], \n.badge[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.badge[_ngcontent-%COMP%]   small[_ngcontent-%COMP%] {\n  color: inherit;\n}\n.avatar[_ngcontent-%COMP%]:disabled {\n  opacity: 0.45;\n  cursor: not-allowed;\n}\n.emoji[_ngcontent-%COMP%] {\n  font-size: 2rem;\n}\ntextarea[_ngcontent-%COMP%], \ninput[type=radio][_ngcontent-%COMP%], \ninput[type=checkbox][_ngcontent-%COMP%] {\n  accent-color: var(--accent);\n}\ntextarea[_ngcontent-%COMP%], \ninput[type=text][_ngcontent-%COMP%], \ninput[type=email][_ngcontent-%COMP%], \ninput[type=password][_ngcontent-%COMP%], \ninput[type=number][_ngcontent-%COMP%], \ninput[type=datetime-local][_ngcontent-%COMP%], \ninput[type=file][_ngcontent-%COMP%], \nselect[_ngcontent-%COMP%] {\n  width: 100%;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.8rem 0.95rem;\n  font: inherit;\n  transition:\n    border-color 0.15s ease,\n    background 0.15s ease,\n    box-shadow 0.15s ease;\n}\ntextarea[_ngcontent-%COMP%] {\n  min-height: 9rem;\n  resize: vertical;\n  line-height: 1.45;\n}\ntextarea[_ngcontent-%COMP%]::placeholder, \ninput[_ngcontent-%COMP%]::placeholder {\n  color: var(--text-soft);\n}\ntextarea[_ngcontent-%COMP%]:hover, \ninput[_ngcontent-%COMP%]:hover, \nselect[_ngcontent-%COMP%]:hover {\n  border-color: var(--input-border-hover);\n}\ntextarea[_ngcontent-%COMP%]:focus, \ninput[_ngcontent-%COMP%]:focus, \nselect[_ngcontent-%COMP%]:focus {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  background: var(--input-bg-focus);\n  box-shadow: var(--focus-ring);\n}\nselect[_ngcontent-%COMP%]   option[_ngcontent-%COMP%] {\n  background: var(--bg-elevated);\n  color: var(--text);\n}\nlabel[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.4rem;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n  font-weight: 600;\n}\nlabel[_ngcontent-%COMP%]    > span[_ngcontent-%COMP%] {\n  color: var(--text-muted);\n}\n.feedback[_ngcontent-%COMP%] {\n  padding: 0.9rem 1rem;\n  border-radius: var(--radius-md);\n  background: var(--auth-error-bg);\n  border: 1px solid var(--auth-error-border);\n  color: var(--feedback-error-fg);\n}\n.feedback.ok[_ngcontent-%COMP%] {\n  background: rgba(81, 207, 102, 0.14);\n  border-color: rgba(125, 222, 160, 0.28);\n  color: var(--feedback-ok-fg);\n}\n[data-theme=light][_ngcontent-%COMP%]   .feedback.ok[_ngcontent-%COMP%] {\n  background: #f0fdf4;\n  border-color: #bbf7d0;\n}\n.question[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.5rem;\n  margin-bottom: var(--space-3);\n  padding: 1rem;\n  border-radius: var(--radius-md);\n  background: var(--elevated-bg);\n  border: 1px solid var(--border);\n  color: var(--text);\n}\n.prompt-html[_ngcontent-%COMP%] {\n  color: var(--prompt-fg);\n}\n.prompt-html[_ngcontent-%COMP%]   b[_ngcontent-%COMP%], \n.prompt-html[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%] {\n  font-weight: 800;\n}\n.table[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.35rem;\n}\n.table-row[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: 1.4fr repeat(4, 1fr);\n  gap: 0.5rem;\n  padding: 0.85rem 0.4rem;\n  border-bottom: 1px solid var(--border);\n  color: var(--text);\n  align-items: center;\n}\n.table-row.head[_ngcontent-%COMP%] {\n  color: var(--text-soft);\n  font-size: 0.82rem;\n  font-weight: 700;\n  letter-spacing: 0.04em;\n  text-transform: uppercase;\n  border-bottom-color: var(--border-strong);\n}\n@media (max-width: 900px) {\n  .grid-two[_ngcontent-%COMP%], \n   .table-row[_ngcontent-%COMP%] {\n    grid-template-columns: 1fr;\n  }\n}\n.panel-page[_ngcontent-%COMP%] {\n  display: grid;\n  gap: var(--space-4);\n  color: var(--text);\n  animation: _ngcontent-%COMP%_pageIn 0.35s ease;\n}\n.panel-page[_ngcontent-%COMP%]    > h2[_ngcontent-%COMP%] {\n  margin: 0;\n  color: var(--heading);\n}\n.panel-page[_ngcontent-%COMP%]    > .meta[_ngcontent-%COMP%] {\n  margin-top: -0.55rem;\n}\n.meeting-form[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));\n  gap: var(--space-3);\n  align-items: end;\n}\n.meeting-form[_ngcontent-%COMP%]   label[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   label[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.4rem;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n}\n.meeting-form[_ngcontent-%COMP%]   label.checkbox[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   label.checkbox[_ngcontent-%COMP%], \nlabel.checkbox[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 0.55rem;\n  padding: 0.7rem 0.85rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--surface);\n}\n.meeting-form[_ngcontent-%COMP%]   input[_ngcontent-%COMP%], \n.meeting-form[_ngcontent-%COMP%]   select[_ngcontent-%COMP%], \n.meeting-form[_ngcontent-%COMP%]   textarea[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   input[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   select[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   textarea[_ngcontent-%COMP%] {\n  width: 100%;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.75rem 0.9rem;\n  font: inherit;\n}\n.meeting-row[_ngcontent-%COMP%] {\n  display: flex;\n  justify-content: space-between;\n  gap: var(--space-3);\n  align-items: center;\n  padding: 0.95rem 0.15rem;\n  border-bottom: 1px solid var(--border);\n  color: var(--text);\n}\n.meeting-row[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.meeting-row[_ngcontent-%COMP%]   .meta[_ngcontent-%COMP%] {\n  color: inherit;\n}\n.form-card[_ngcontent-%COMP%] {\n  display: grid;\n  gap: var(--space-3);\n  padding: 1.35rem;\n  border-radius: var(--radius-lg);\n  background: var(--surface);\n  border: 1px solid var(--border);\n  box-shadow: var(--shadow-sm);\n}\n.form-actions[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.75rem;\n  align-items: center;\n  padding-top: 0.35rem;\n}\n.stat-row[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));\n  gap: var(--space-3);\n}\n.stat-card[_ngcontent-%COMP%] {\n  padding: 1rem 1.1rem;\n  border-radius: var(--radius-md);\n  background: var(--surface-strong);\n  border: 1px solid var(--border);\n}\n.stat-card[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%] {\n  display: block;\n  font-family: var(--font-display);\n  font-size: 1.55rem;\n  color: var(--stat-strong);\n}\n.stat-card[_ngcontent-%COMP%]   span[_ngcontent-%COMP%] {\n  color: var(--text-muted);\n  font-size: 0.85rem;\n}\nbutton.stat-card-link[_ngcontent-%COMP%] {\n  display: block;\n  width: 100%;\n  text-align: start;\n  font: inherit;\n  font-weight: inherit;\n  color: inherit;\n  cursor: pointer;\n  background: var(--surface-strong);\n  box-shadow: none;\n  transition:\n    transform 0.2s ease,\n    border-color 0.2s ease,\n    background 0.2s ease;\n}\nbutton.stat-card-link[_ngcontent-%COMP%]:hover, \nbutton.stat-card-link[_ngcontent-%COMP%]:focus-visible {\n  transform: translateY(-2px);\n  border-color: var(--border-strong);\n  box-shadow: none;\n  outline: none;\n}\nbutton.stat-card-link.active[_ngcontent-%COMP%] {\n  border-color: var(--border-strong);\n  box-shadow: 0 0 0 1px var(--border-strong);\n}\n@keyframes _ngcontent-%COMP%_pageIn {\n  from {\n    opacity: 0;\n    transform: translateY(6px);\n  }\n  to {\n    opacity: 1;\n    transform: translateY(0);\n  }\n}\n@media (max-width: 700px) {\n  .page[_ngcontent-%COMP%] {\n    padding: 1.35rem 1rem 3rem;\n  }\n  .meeting-row[_ngcontent-%COMP%] {\n    flex-direction: column;\n    align-items: flex-start;\n  }\n  .hero-strip[_ngcontent-%COMP%] {\n    flex-direction: column;\n    align-items: flex-start;\n  }\n}\n.student-side-tools[_ngcontent-%COMP%] {\n  position: fixed;\n  inset-inline-end: 0;\n  top: 38%;\n  z-index: 46;\n  display: grid;\n  gap: 0.55rem;\n  justify-items: end;\n}\n.student-side-tools[_ngcontent-%COMP%]   .side-tab[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.45rem;\n  padding: 0.7rem 0.75rem;\n  border: 1px solid var(--border-strong);\n  border-inline-end: none;\n  border-radius: var(--radius-md) 0 0 var(--radius-md);\n  background: var(--surface);\n  color: var(--heading);\n  text-decoration: none;\n  font-weight: 800;\n  box-shadow: var(--shadow-md);\n  max-width: 2.75rem;\n  overflow: hidden;\n  transition: max-width 0.2s ease;\n}\nhtml[dir=rtl][_ngcontent-%COMP%]   .student-side-tools[_ngcontent-%COMP%]   .side-tab[_ngcontent-%COMP%] {\n  border-radius: 0 var(--radius-md) var(--radius-md) 0;\n}\n.student-side-tools[_ngcontent-%COMP%]   .side-tab-icon[_ngcontent-%COMP%] {\n  flex-shrink: 0;\n  width: 1.2rem;\n  text-align: center;\n  font-size: 1.05rem;\n  line-height: 1;\n}\n.student-side-tools[_ngcontent-%COMP%]   .side-tab-label[_ngcontent-%COMP%] {\n  white-space: nowrap;\n  font-size: 0.88rem;\n}\n.student-side-tools[_ngcontent-%COMP%]   .side-tab[_ngcontent-%COMP%]:hover, \n.student-side-tools[_ngcontent-%COMP%]   .side-tab[_ngcontent-%COMP%]:focus-visible {\n  max-width: 12rem;\n}\n@media (max-width: 700px) {\n  .student-side-tools[_ngcontent-%COMP%] {\n    top: auto;\n    inset-block-end: 5.25rem;\n    z-index: 62;\n  }\n}\n\n\n.parent-page[_ngcontent-%COMP%] {\n  animation: _ngcontent-%COMP%_pageIn 0.4s ease;\n}\n.topbar-actions[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 0.75rem;\n}\n.section-head[_ngcontent-%COMP%] {\n  display: flex;\n  justify-content: space-between;\n  align-items: flex-start;\n  gap: 1rem;\n  margin-bottom: 1rem;\n}\n.section-head[_ngcontent-%COMP%]   h2[_ngcontent-%COMP%] {\n  margin: 0;\n  font-size: 1.25rem;\n}\n.section-hint[_ngcontent-%COMP%] {\n  margin: 0.3rem 0 0;\n  color: var(--text-muted);\n  font-size: 0.88rem;\n}\n/*# sourceMappingURL=parent-account.component.css.map */"] });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ParentAccountComponent, [{
    type: Component,
    args: [{ selector: "app-parent-account", imports: [
      FormsModule,
      RouterLink,
      TranslatePipe,
      SiteBrandComponent,
      ThemeSwitcherComponent,
      LanguageSwitcherComponent,
      ApiBusyIndicatorComponent
    ], template: `<div class="page parent-page">\r
  <app-api-busy-indicator />\r
  <header class="topbar">\r
    <div>\r
      <app-site-brand />\r
      <h1>{{ 'parent.accountTitle' | t }}</h1>\r
    </div>\r
    <div class="topbar-actions">\r
      <a class="chip quiz" routerLink="/parent">{{ 'parent.backToChildren' | t }}</a>\r
      <app-theme-switcher />\r
      <app-language-switcher />\r
      <button type="button" class="ghost" (click)="auth.logout()">{{ 'common.signOut' | t }}</button>\r
    </div>\r
  </header>\r
\r
  @if (error()) {\r
    <p class="feedback">{{ error() }}</p>\r
  }\r
  @if (message()) {\r
    <p class="feedback ok">{{ message() }}</p>\r
  }\r
\r
  <section class="block">\r
    <header class="section-head">\r
      <div>\r
        <h2>{{ 'parent.accountTitle' | t }}</h2>\r
        <p class="section-hint">{{ 'parent.accountHint' | t }}</p>\r
      </div>\r
    </header>\r
    <form class="meeting-form" (ngSubmit)="saveAccount()">\r
      <label>\r
        {{ 'common.email' | t }}\r
        <input [(ngModel)]="parentEmail" name="parentEmail" type="email" autocomplete="email" />\r
      </label>\r
      <label>\r
        {{ 'common.mobile' | t }}\r
        <input [(ngModel)]="parentMobile" name="parentMobile" autocomplete="tel" />\r
      </label>\r
      <label>\r
        {{ 'common.newPasswordOptional' | t }}\r
        <input [(ngModel)]="parentPassword" name="parentPassword" type="password" autocomplete="new-password" />\r
      </label>\r
      <label>\r
        {{ 'auth.reset.confirmPassword' | t }}\r
        <input [(ngModel)]="parentPasswordConfirm" name="parentPasswordConfirm" type="password" autocomplete="new-password" />\r
      </label>\r
      <button type="submit" [disabled]="saving()">\r
        {{ saving() ? ('common.saving' | t) : ('common.save' | t) }}\r
      </button>\r
    </form>\r
  </section>\r
</div>\r
`, styles: ["/* src/app/styles/dashboard-shared.css */\n.page {\n  position: relative;\n  min-height: 100vh;\n  padding: var(--space-5) 6vw 4rem;\n  color: var(--text);\n  background:\n    radial-gradient(\n      circle at 88% 0%,\n      var(--page-glow-1),\n      transparent 28%),\n    radial-gradient(\n      circle at 8% 12%,\n      var(--page-glow-2),\n      transparent 22%),\n    var(--bg);\n}\n.page p,\n.page span,\n.page strong,\n.page small,\n.page label,\n.page li,\n.page td,\n.page th {\n  color: inherit;\n}\n.topbar,\n.hero-strip,\n.grid-two,\n.grid-cards,\n.chip-row,\n.avatar-row {\n  display: flex;\n  gap: var(--space-3);\n}\n.topbar {\n  justify-content: space-between;\n  align-items: center;\n  margin-bottom: var(--space-5);\n}\n.brand {\n  margin: 0;\n  font-family: var(--font-display);\n  font-size: clamp(1.75rem, 3vw, 2.15rem);\n  font-weight: 800;\n  text-transform: uppercase;\n  color: var(--heading);\n  letter-spacing: 0.04em;\n}\nh1,\nh2,\nh3,\nh4 {\n  font-family: var(--font-display);\n  margin: 0.15rem 0;\n  color: var(--heading);\n  letter-spacing: 0.01em;\n  line-height: 1.15;\n}\nh1 {\n  font-size: clamp(1.8rem, 3vw, 2.4rem);\n}\nh2 {\n  font-size: clamp(1.4rem, 2.4vw, 1.85rem);\n}\nh3 {\n  font-size: 1.2rem;\n}\n.hero-strip {\n  justify-content: space-between;\n  align-items: center;\n  gap: var(--space-4);\n  padding: 1.5rem 1.6rem;\n  border-radius: var(--radius-xl);\n  margin-bottom: var(--space-5);\n  background: var(--hero-bg);\n  border: 1px solid var(--hero-border);\n  box-shadow: var(--shadow-sm);\n  color: var(--hero-fg);\n}\n.hero-strip p,\n.hero-strip h2 {\n  color: var(--hero-fg);\n}\n.eyebrow,\n.meta,\n.back {\n  color: var(--text-muted);\n}\n.eyebrow {\n  margin: 0 0 0.35rem;\n  font-size: 0.78rem;\n  font-weight: 700;\n  letter-spacing: 0.08em;\n  text-transform: uppercase;\n  color: var(--teal);\n}\n.back {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.35rem;\n  margin-bottom: var(--space-3);\n  text-decoration: none;\n  font-weight: 600;\n  transition: color 0.15s ease;\n}\n.back:hover {\n  color: var(--heading);\n}\n.xp-pill,\nbutton,\n.chip,\n.list-btn,\n.avatar,\n.badge {\n  border: none;\n  border-radius: var(--radius-pill);\n  font: inherit;\n}\n.xp-pill,\nbutton {\n  padding: 0.8rem 1.15rem;\n  background:\n    linear-gradient(\n      135deg,\n      var(--accent),\n      var(--accent-hot));\n  color: var(--accent-ink);\n  font-weight: 800;\n  cursor: pointer;\n  transition:\n    transform 0.15s ease,\n    box-shadow 0.15s ease,\n    opacity 0.15s ease;\n  box-shadow: var(--btn-shadow);\n}\nbutton:hover:not(:disabled) {\n  transform: translateY(-1px);\n  box-shadow: var(--btn-shadow-hover);\n}\nbutton:active:not(:disabled) {\n  transform: translateY(0);\n}\nbutton:disabled {\n  opacity: 0.55;\n  cursor: not-allowed;\n  box-shadow: none;\n}\nbutton:focus-visible,\n.chip:focus-visible,\n.list-btn:focus-visible,\na:focus-visible,\ninput:focus-visible,\nselect:focus-visible,\ntextarea:focus-visible {\n  outline: none;\n  box-shadow: var(--focus-ring);\n}\nbutton.ghost,\n.ghost-btn {\n  background: transparent;\n  color: var(--ghost-fg);\n  border: 1px solid var(--border-strong);\n  box-shadow: none;\n}\nbutton.ghost:hover:not(:disabled),\n.ghost-btn:hover:not(:disabled) {\n  background: var(--surface-strong);\n  box-shadow: none;\n}\n.grid-two {\n  display: grid;\n  grid-template-columns: 1.3fr 0.9fr;\n  gap: var(--space-4);\n}\n.grid-cards {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));\n}\n.side-stack {\n  display: grid;\n  gap: var(--space-4);\n}\n.block,\n.badge,\n.avatar {\n  background: var(--surface);\n  border: 1px solid var(--border);\n  border-radius: var(--radius-lg);\n  padding: 1.25rem;\n  color: var(--text);\n}\n.block {\n  display: grid;\n  gap: 0.85rem;\n  margin-bottom: var(--space-3);\n  box-shadow: var(--shadow-sm);\n  position: relative;\n  z-index: 0;\n}\n.block:has(app-searchable-select.ss--open),\n.block:has(app-searchable-multi-select.ms--open) {\n  z-index: 50;\n}\n.block > h3 {\n  padding-bottom: 0.55rem;\n  border-bottom: 1px solid var(--border);\n}\n.block p,\n.block strong,\n.block small {\n  color: var(--text);\n}\n.chip-row,\n.avatar-row {\n  flex-wrap: wrap;\n}\n.chip,\n.list-btn {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.4rem;\n  padding: 0.65rem 0.95rem;\n  background: var(--chip-bg);\n  border: 1px solid var(--chip-border);\n  color: var(--chip-fg);\n  text-decoration: none;\n  cursor: pointer;\n  transition:\n    background 0.15s ease,\n    border-color 0.15s ease,\n    transform 0.15s ease;\n}\n.chip:hover,\n.list-btn:hover {\n  background: var(--chip-bg);\n  border-color: var(--chip-border);\n  filter: brightness(0.97);\n  transform: translateY(-1px);\n}\n.chip.quiz {\n  background: rgba(95, 211, 188, 0.16);\n  border-color: rgba(95, 211, 188, 0.22);\n}\n.chip.video {\n  background: rgba(255, 214, 10, 0.16);\n  border-color: rgba(255, 214, 10, 0.28);\n}\n.list-btn {\n  width: 100%;\n  text-align: left;\n  margin-bottom: 0.45rem;\n  border-radius: var(--radius-md);\n}\n.list-btn.active,\n.avatar.selected,\n.badge.earned {\n  background:\n    linear-gradient(\n      135deg,\n      var(--accent),\n      var(--accent-hot));\n  border-color: transparent;\n  color: var(--accent-ink);\n}\n.avatar {\n  width: 9.5rem;\n  display: grid;\n  gap: 0.3rem;\n  text-align: left;\n  color: var(--text);\n  cursor: pointer;\n  transition: transform 0.15s ease, border-color 0.15s ease;\n}\n.avatar:hover:not(:disabled) {\n  transform: translateY(-2px);\n  border-color: var(--border-strong);\n}\n.avatar strong,\n.avatar small,\n.badge strong,\n.badge small {\n  color: inherit;\n}\n.avatar:disabled {\n  opacity: 0.45;\n  cursor: not-allowed;\n}\n.emoji {\n  font-size: 2rem;\n}\ntextarea,\ninput[type=radio],\ninput[type=checkbox] {\n  accent-color: var(--accent);\n}\ntextarea,\ninput[type=text],\ninput[type=email],\ninput[type=password],\ninput[type=number],\ninput[type=datetime-local],\ninput[type=file],\nselect {\n  width: 100%;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.8rem 0.95rem;\n  font: inherit;\n  transition:\n    border-color 0.15s ease,\n    background 0.15s ease,\n    box-shadow 0.15s ease;\n}\ntextarea {\n  min-height: 9rem;\n  resize: vertical;\n  line-height: 1.45;\n}\ntextarea::placeholder,\ninput::placeholder {\n  color: var(--text-soft);\n}\ntextarea:hover,\ninput:hover,\nselect:hover {\n  border-color: var(--input-border-hover);\n}\ntextarea:focus,\ninput:focus,\nselect:focus {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  background: var(--input-bg-focus);\n  box-shadow: var(--focus-ring);\n}\nselect option {\n  background: var(--bg-elevated);\n  color: var(--text);\n}\nlabel {\n  display: grid;\n  gap: 0.4rem;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n  font-weight: 600;\n}\nlabel > span {\n  color: var(--text-muted);\n}\n.feedback {\n  padding: 0.9rem 1rem;\n  border-radius: var(--radius-md);\n  background: var(--auth-error-bg);\n  border: 1px solid var(--auth-error-border);\n  color: var(--feedback-error-fg);\n}\n.feedback.ok {\n  background: rgba(81, 207, 102, 0.14);\n  border-color: rgba(125, 222, 160, 0.28);\n  color: var(--feedback-ok-fg);\n}\n[data-theme=light] .feedback.ok {\n  background: #f0fdf4;\n  border-color: #bbf7d0;\n}\n.question {\n  display: grid;\n  gap: 0.5rem;\n  margin-bottom: var(--space-3);\n  padding: 1rem;\n  border-radius: var(--radius-md);\n  background: var(--elevated-bg);\n  border: 1px solid var(--border);\n  color: var(--text);\n}\n.prompt-html {\n  color: var(--prompt-fg);\n}\n.prompt-html b,\n.prompt-html strong {\n  font-weight: 800;\n}\n.table {\n  display: grid;\n  gap: 0.35rem;\n}\n.table-row {\n  display: grid;\n  grid-template-columns: 1.4fr repeat(4, 1fr);\n  gap: 0.5rem;\n  padding: 0.85rem 0.4rem;\n  border-bottom: 1px solid var(--border);\n  color: var(--text);\n  align-items: center;\n}\n.table-row.head {\n  color: var(--text-soft);\n  font-size: 0.82rem;\n  font-weight: 700;\n  letter-spacing: 0.04em;\n  text-transform: uppercase;\n  border-bottom-color: var(--border-strong);\n}\n@media (max-width: 900px) {\n  .grid-two,\n  .table-row {\n    grid-template-columns: 1fr;\n  }\n}\n.panel-page {\n  display: grid;\n  gap: var(--space-4);\n  color: var(--text);\n  animation: pageIn 0.35s ease;\n}\n.panel-page > h2 {\n  margin: 0;\n  color: var(--heading);\n}\n.panel-page > .meta {\n  margin-top: -0.55rem;\n}\n.meeting-form,\n.form-grid {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));\n  gap: var(--space-3);\n  align-items: end;\n}\n.meeting-form label,\n.form-grid label {\n  display: grid;\n  gap: 0.4rem;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n}\n.meeting-form label.checkbox,\n.form-grid label.checkbox,\nlabel.checkbox {\n  display: flex;\n  align-items: center;\n  gap: 0.55rem;\n  padding: 0.7rem 0.85rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--surface);\n}\n.meeting-form input,\n.meeting-form select,\n.meeting-form textarea,\n.form-grid input,\n.form-grid select,\n.form-grid textarea {\n  width: 100%;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.75rem 0.9rem;\n  font: inherit;\n}\n.meeting-row {\n  display: flex;\n  justify-content: space-between;\n  gap: var(--space-3);\n  align-items: center;\n  padding: 0.95rem 0.15rem;\n  border-bottom: 1px solid var(--border);\n  color: var(--text);\n}\n.meeting-row strong,\n.meeting-row .meta {\n  color: inherit;\n}\n.form-card {\n  display: grid;\n  gap: var(--space-3);\n  padding: 1.35rem;\n  border-radius: var(--radius-lg);\n  background: var(--surface);\n  border: 1px solid var(--border);\n  box-shadow: var(--shadow-sm);\n}\n.form-actions {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.75rem;\n  align-items: center;\n  padding-top: 0.35rem;\n}\n.stat-row {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));\n  gap: var(--space-3);\n}\n.stat-card {\n  padding: 1rem 1.1rem;\n  border-radius: var(--radius-md);\n  background: var(--surface-strong);\n  border: 1px solid var(--border);\n}\n.stat-card strong {\n  display: block;\n  font-family: var(--font-display);\n  font-size: 1.55rem;\n  color: var(--stat-strong);\n}\n.stat-card span {\n  color: var(--text-muted);\n  font-size: 0.85rem;\n}\nbutton.stat-card-link {\n  display: block;\n  width: 100%;\n  text-align: start;\n  font: inherit;\n  font-weight: inherit;\n  color: inherit;\n  cursor: pointer;\n  background: var(--surface-strong);\n  box-shadow: none;\n  transition:\n    transform 0.2s ease,\n    border-color 0.2s ease,\n    background 0.2s ease;\n}\nbutton.stat-card-link:hover,\nbutton.stat-card-link:focus-visible {\n  transform: translateY(-2px);\n  border-color: var(--border-strong);\n  box-shadow: none;\n  outline: none;\n}\nbutton.stat-card-link.active {\n  border-color: var(--border-strong);\n  box-shadow: 0 0 0 1px var(--border-strong);\n}\n@keyframes pageIn {\n  from {\n    opacity: 0;\n    transform: translateY(6px);\n  }\n  to {\n    opacity: 1;\n    transform: translateY(0);\n  }\n}\n@media (max-width: 700px) {\n  .page {\n    padding: 1.35rem 1rem 3rem;\n  }\n  .meeting-row {\n    flex-direction: column;\n    align-items: flex-start;\n  }\n  .hero-strip {\n    flex-direction: column;\n    align-items: flex-start;\n  }\n}\n.student-side-tools {\n  position: fixed;\n  inset-inline-end: 0;\n  top: 38%;\n  z-index: 46;\n  display: grid;\n  gap: 0.55rem;\n  justify-items: end;\n}\n.student-side-tools .side-tab {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.45rem;\n  padding: 0.7rem 0.75rem;\n  border: 1px solid var(--border-strong);\n  border-inline-end: none;\n  border-radius: var(--radius-md) 0 0 var(--radius-md);\n  background: var(--surface);\n  color: var(--heading);\n  text-decoration: none;\n  font-weight: 800;\n  box-shadow: var(--shadow-md);\n  max-width: 2.75rem;\n  overflow: hidden;\n  transition: max-width 0.2s ease;\n}\nhtml[dir=rtl] .student-side-tools .side-tab {\n  border-radius: 0 var(--radius-md) var(--radius-md) 0;\n}\n.student-side-tools .side-tab-icon {\n  flex-shrink: 0;\n  width: 1.2rem;\n  text-align: center;\n  font-size: 1.05rem;\n  line-height: 1;\n}\n.student-side-tools .side-tab-label {\n  white-space: nowrap;\n  font-size: 0.88rem;\n}\n.student-side-tools .side-tab:hover,\n.student-side-tools .side-tab:focus-visible {\n  max-width: 12rem;\n}\n@media (max-width: 700px) {\n  .student-side-tools {\n    top: auto;\n    inset-block-end: 5.25rem;\n    z-index: 62;\n  }\n}\n\n/* src/app/pages/parent-account/parent-account.component.css */\n.parent-page {\n  animation: pageIn 0.4s ease;\n}\n.topbar-actions {\n  display: flex;\n  align-items: center;\n  gap: 0.75rem;\n}\n.section-head {\n  display: flex;\n  justify-content: space-between;\n  align-items: flex-start;\n  gap: 1rem;\n  margin-bottom: 1rem;\n}\n.section-head h2 {\n  margin: 0;\n  font-size: 1.25rem;\n}\n.section-hint {\n  margin: 0.3rem 0 0;\n  color: var(--text-muted);\n  font-size: 0.88rem;\n}\n/*# sourceMappingURL=parent-account.component.css.map */\n"] }]
  }], () => [], null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ParentAccountComponent, { className: "ParentAccountComponent", filePath: "src/app/pages/parent-account/parent-account.component.ts", lineNumber: 31 });
})();
export {
  ParentAccountComponent
};
//# sourceMappingURL=chunk-5W6SW3HC.js.map
