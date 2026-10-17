import {
  LearningApiService
} from "./chunk-7YFOTRM2.js";
import {
  Component,
  DestroyRef,
  HttpClient,
  Input,
  Observable,
  RuntimeError,
  __spreadValues,
  assertInInjectionContext,
  assertNotInReactiveContext,
  computed,
  effect,
  inject,
  input,
  setClassMetadata,
  signal,
  takeUntil,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵattribute,
  ɵɵclassMap,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdomElement,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵdomProperty,
  ɵɵnextContext,
  ɵɵsanitizeUrl,
  ɵɵtext,
  ɵɵtextInterpolate
} from "./chunk-EMYLRMBO.js";

// node_modules/@angular/core/fesm2022/rxjs-interop.mjs
/**
 * @license Angular v22.0.8
 * (c) 2010-2026 Google LLC. https://angular.dev/
 * License: MIT
 */
function takeUntilDestroyed(destroyRef) {
  if (!destroyRef) {
    ngDevMode && assertInInjectionContext(takeUntilDestroyed);
    destroyRef = inject(DestroyRef);
  }
  const destroyed$ = new Observable((subscriber) => {
    if (destroyRef.destroyed) {
      subscriber.next();
      return;
    }
    const unregisterFn = destroyRef.onDestroy(subscriber.next.bind(subscriber));
    return unregisterFn;
  });
  return (source) => {
    return source.pipe(takeUntil(destroyed$));
  };
}
function toSignal(source, options) {
  typeof ngDevMode !== "undefined" && ngDevMode && assertNotInReactiveContext(toSignal, "Invoking `toSignal` causes new subscriptions every time. Consider moving `toSignal` outside of the reactive context and read the signal value where needed.");
  const requiresCleanup = !options?.manualCleanup;
  if (ngDevMode && requiresCleanup && !options?.injector) {
    assertInInjectionContext(toSignal);
  }
  const cleanupRef = requiresCleanup ? options?.injector?.get(DestroyRef) ?? inject(DestroyRef) : null;
  const equal = makeToSignalEqual(options?.equal);
  let state;
  if (options?.requireSync) {
    state = signal({
      kind: 0
    }, __spreadValues({
      equal
    }, ngDevMode ? createDebugNameObject(options?.debugName, "state") : void 0));
  } else {
    state = signal({
      kind: 1,
      value: options?.initialValue
    }, __spreadValues({
      equal
    }, ngDevMode ? createDebugNameObject(options?.debugName, "state") : void 0));
  }
  let destroyUnregisterFn;
  const sub = source.subscribe({
    next: (value) => state.set({
      kind: 1,
      value
    }),
    error: (error) => {
      state.set({
        kind: 2,
        error
      });
      destroyUnregisterFn?.();
    },
    complete: () => {
      destroyUnregisterFn?.();
    }
  });
  if (options?.requireSync && state().kind === 0) {
    throw new RuntimeError(601, (typeof ngDevMode === "undefined" || ngDevMode) && "`toSignal()` called with `requireSync` but `Observable` did not emit synchronously.");
  }
  destroyUnregisterFn = cleanupRef?.onDestroy(sub.unsubscribe.bind(sub));
  return computed(() => {
    const current = state();
    switch (current.kind) {
      case 1:
        return current.value;
      case 2:
        throw current.error;
      case 0:
        throw new RuntimeError(601, (typeof ngDevMode === "undefined" || ngDevMode) && "`toSignal()` called with `requireSync` but `Observable` did not emit synchronously.");
    }
  }, __spreadValues({
    equal: options?.equal
  }, ngDevMode ? createDebugNameObject(options?.debugName, "source") : void 0));
}
function makeToSignalEqual(userEquality = Object.is) {
  return (a, b) => a.kind === 1 && b.kind === 1 && userEquality(a.value, b.value);
}
function createDebugNameObject(toSignalDebugName, internalSignalDebugName) {
  return {
    debugName: `toSignal${toSignalDebugName ? "#" + toSignalDebugName : ""}.${internalSignalDebugName}`
  };
}

// src/app/shared/user-photo/user-photo.component.ts
function UserPhotoComponent_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElement(0, "img", 1);
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275domProperty("src", ctx, \u0275\u0275sanitizeUrl)("alt", ctx_r0.name());
  }
}
function UserPhotoComponent_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 2);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r0.initials());
  }
}
var UserPhotoComponent = class _UserPhotoComponent {
  constructor() {
    this.http = inject(HttpClient);
    this.api = inject(LearningApiService);
    this.destroyRef = inject(DestroyRef);
    this.photoUrl = input(
      null,
      ...ngDevMode ? [{ debugName: "photoUrl" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.name = input(
      "",
      ...ngDevMode ? [{ debugName: "name" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.size = input(
      "md",
      ...ngDevMode ? [{ debugName: "size" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.blobUrl = signal(
      null,
      ...ngDevMode ? [{ debugName: "blobUrl" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.initials = computed(
      () => {
        const words = this.name().trim().split(/\s+/).filter(Boolean);
        if (words.length === 0)
          return "?";
        return words.slice(0, 2).map((word) => [...word][0]).join("").toUpperCase();
      },
      ...ngDevMode ? [{ debugName: "initials" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.activeObjectUrl = null;
    effect(() => {
      const path = this.photoUrl();
      this.clearObjectUrl();
      this.blobUrl.set(null);
      if (!path)
        return;
      const fullUrl = this.api.siteAssetUrl(path);
      if (!fullUrl)
        return;
      this.http.get(fullUrl, { responseType: "blob" }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (blob) => {
          this.clearObjectUrl();
          this.activeObjectUrl = URL.createObjectURL(blob);
          this.blobUrl.set(this.activeObjectUrl);
        },
        error: () => {
          this.clearObjectUrl();
          this.blobUrl.set(null);
        }
      });
    });
    this.destroyRef.onDestroy(() => this.clearObjectUrl());
  }
  clearObjectUrl() {
    if (this.activeObjectUrl) {
      URL.revokeObjectURL(this.activeObjectUrl);
      this.activeObjectUrl = null;
    }
  }
  static {
    this.\u0275fac = function UserPhotoComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _UserPhotoComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _UserPhotoComponent, selectors: [["app-user-photo"]], inputs: { photoUrl: [1, "photoUrl"], name: [1, "name"], size: [1, "size"] }, decls: 3, vars: 4, consts: [[1, "user-photo"], [3, "src", "alt"], ["aria-hidden", "true", 1, "initials"]], template: function UserPhotoComponent_Template(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275domElementStart(0, "span", 0);
        \u0275\u0275conditionalCreate(1, UserPhotoComponent_Conditional_1_Template, 1, 2, "img", 1)(2, UserPhotoComponent_Conditional_2_Template, 2, 1, "span", 2);
        \u0275\u0275domElementEnd();
      }
      if (rf & 2) {
        let tmp_2_0;
        \u0275\u0275classMap(ctx.size());
        \u0275\u0275attribute("title", ctx.name() || null);
        \u0275\u0275advance();
        \u0275\u0275conditional((tmp_2_0 = ctx.blobUrl()) ? 1 : 2, tmp_2_0);
      }
    }, styles: ["\n.user-photo[_ngcontent-%COMP%] {\n  display: inline-grid;\n  place-items: center;\n  flex-shrink: 0;\n  overflow: hidden;\n  border-radius: 50%;\n  background: var(--elevated-bg);\n  border: 2px solid var(--border-strong);\n  color: var(--heading);\n  font-weight: 700;\n  line-height: 1;\n  text-transform: uppercase;\n}\n.user-photo[_ngcontent-%COMP%]   img[_ngcontent-%COMP%] {\n  width: 100%;\n  height: 100%;\n  object-fit: cover;\n  display: block;\n}\n.user-photo.sm[_ngcontent-%COMP%] {\n  width: 2rem;\n  height: 2rem;\n  font-size: 0.7rem;\n}\n.user-photo.md[_ngcontent-%COMP%] {\n  width: 2.75rem;\n  height: 2.75rem;\n  font-size: 0.9rem;\n}\n.user-photo.lg[_ngcontent-%COMP%] {\n  width: 4.75rem;\n  height: 4.75rem;\n  font-size: 1.5rem;\n  box-shadow: 0 0 0 6px rgba(255, 214, 10, 0.06);\n}\n/*# sourceMappingURL=user-photo.component.css.map */"] });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(UserPhotoComponent, [{
    type: Component,
    args: [{ selector: "app-user-photo", template: `
    <span class="user-photo" [class]="size()" [attr.title]="name() || null">
      @if (blobUrl(); as src) {
        <img [src]="src" [alt]="name()" />
      } @else {
        <span class="initials" aria-hidden="true">{{ initials() }}</span>
      }
    </span>
  `, styles: ["/* src/app/shared/user-photo/user-photo.component.css */\n.user-photo {\n  display: inline-grid;\n  place-items: center;\n  flex-shrink: 0;\n  overflow: hidden;\n  border-radius: 50%;\n  background: var(--elevated-bg);\n  border: 2px solid var(--border-strong);\n  color: var(--heading);\n  font-weight: 700;\n  line-height: 1;\n  text-transform: uppercase;\n}\n.user-photo img {\n  width: 100%;\n  height: 100%;\n  object-fit: cover;\n  display: block;\n}\n.user-photo.sm {\n  width: 2rem;\n  height: 2rem;\n  font-size: 0.7rem;\n}\n.user-photo.md {\n  width: 2.75rem;\n  height: 2.75rem;\n  font-size: 0.9rem;\n}\n.user-photo.lg {\n  width: 4.75rem;\n  height: 4.75rem;\n  font-size: 1.5rem;\n  box-shadow: 0 0 0 6px rgba(255, 214, 10, 0.06);\n}\n/*# sourceMappingURL=user-photo.component.css.map */\n"] }]
  }], () => [], { photoUrl: [{ type: Input, args: [{ isSignal: true, alias: "photoUrl", required: false }] }], name: [{ type: Input, args: [{ isSignal: true, alias: "name", required: false }] }], size: [{ type: Input, args: [{ isSignal: true, alias: "size", required: false }] }] });
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(UserPhotoComponent, { className: "UserPhotoComponent", filePath: "src/app/shared/user-photo/user-photo.component.ts", lineNumber: 26 });
})();

export {
  takeUntilDestroyed,
  toSignal,
  UserPhotoComponent
};
//# sourceMappingURL=chunk-YI56DT2Q.js.map
