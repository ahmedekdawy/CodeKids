import {
  DomSanitizer
} from "./chunk-RLZFB7T3.js";
import {
  Pipe,
  inject,
  setClassMetadata,
  ɵɵdefinePipe
} from "./chunk-EMYLRMBO.js";

// src/app/shared/safe-html.pipe.ts
var SafeHtmlPipe = class _SafeHtmlPipe {
  constructor() {
    this.sanitizer = inject(DomSanitizer);
  }
  transform(value) {
    return this.sanitizer.bypassSecurityTrustHtml(value || "");
  }
  static {
    this.\u0275fac = function SafeHtmlPipe_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _SafeHtmlPipe)();
    };
  }
  static {
    this.\u0275pipe = /* @__PURE__ */ \u0275\u0275definePipe({ name: "safeHtml", type: _SafeHtmlPipe, pure: true });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(SafeHtmlPipe, [{
    type: Pipe,
    args: [{ name: "safeHtml", standalone: true }]
  }], null, null);
})();

export {
  SafeHtmlPipe
};
//# sourceMappingURL=chunk-3ZZ6X64X.js.map
