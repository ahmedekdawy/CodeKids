import {
  Component,
  Injectable,
  Input,
  __spreadProps,
  __spreadValues,
  effect,
  inject,
  input,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdefineInjectable,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵnextContext,
  ɵɵtext,
  ɵɵtextInterpolate
} from "./chunk-EMYLRMBO.js";

// src/app/shared/toast/toast.service.ts
var ToastService = class _ToastService {
  constructor() {
    this.nextId = 1;
    this.items = signal(
      [],
      ...ngDevMode ? [{ debugName: "items" }] : (
        /* istanbul ignore next */
        []
      )
    );
  }
  success(text) {
    this.push({ kind: "success", text });
  }
  error(text) {
    this.push({ kind: "error", text });
  }
  chat(title, text, href) {
    this.push({ kind: "chat", title, text, href });
  }
  notification(title, text, href) {
    this.push({ kind: "notification", title, text, href });
  }
  dismiss(id) {
    this.items.update((list) => list.filter((item) => item.id !== id));
  }
  push(item) {
    const trimmed = (item.text ?? "").trim();
    if (!trimmed && !item.title)
      return;
    const id = this.nextId++;
    this.items.update((list) => [...list, __spreadProps(__spreadValues({}, item), { id, text: trimmed })]);
    window.setTimeout(() => this.dismiss(id), item.kind === "chat" || item.kind === "notification" ? 12e3 : 5200);
  }
  static {
    this.\u0275fac = function ToastService_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _ToastService)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _ToastService, factory: _ToastService.\u0275fac, providedIn: "root" });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ToastService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/shared/page-feedback/page-feedback.component.ts
function PageFeedbackComponent_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "p", 0);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r0.ok());
  }
}
function PageFeedbackComponent_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "p", 1);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r0.error());
  }
}
var PageFeedbackComponent = class _PageFeedbackComponent {
  constructor() {
    this.ok = input(
      "",
      ...ngDevMode ? [{ debugName: "ok" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.error = input(
      "",
      ...ngDevMode ? [{ debugName: "error" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.toasts = inject(ToastService);
    effect(() => {
      const text = this.ok().trim();
      if (text)
        this.toasts.success(text);
    });
    effect(() => {
      const text = this.error().trim();
      if (text)
        this.toasts.error(text);
    });
  }
  static {
    this.\u0275fac = function PageFeedbackComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _PageFeedbackComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _PageFeedbackComponent, selectors: [["app-page-feedback"]], inputs: { ok: [1, "ok"], error: [1, "error"] }, decls: 2, vars: 2, consts: [[1, "feedback", "ok"], [1, "feedback"]], template: function PageFeedbackComponent_Template(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275conditionalCreate(0, PageFeedbackComponent_Conditional_0_Template, 2, 1, "p", 0);
        \u0275\u0275conditionalCreate(1, PageFeedbackComponent_Conditional_1_Template, 2, 1, "p", 1);
      }
      if (rf & 2) {
        \u0275\u0275conditional(ctx.ok() ? 0 : -1);
        \u0275\u0275advance();
        \u0275\u0275conditional(ctx.error() ? 1 : -1);
      }
    }, encapsulation: 2 });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PageFeedbackComponent, [{
    type: Component,
    args: [{
      selector: "app-page-feedback",
      template: `
    @if (ok()) {
      <p class="feedback ok">{{ ok() }}</p>
    }
    @if (error()) {
      <p class="feedback">{{ error() }}</p>
    }
  `
    }]
  }], () => [], { ok: [{ type: Input, args: [{ isSignal: true, alias: "ok", required: false }] }], error: [{ type: Input, args: [{ isSignal: true, alias: "error", required: false }] }] });
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(PageFeedbackComponent, { className: "PageFeedbackComponent", filePath: "src/app/shared/page-feedback/page-feedback.component.ts", lineNumber: 15 });
})();

export {
  ToastService,
  PageFeedbackComponent
};
//# sourceMappingURL=chunk-GSQJCOEO.js.map
