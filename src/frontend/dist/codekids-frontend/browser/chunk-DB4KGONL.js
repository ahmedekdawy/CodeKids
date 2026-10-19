import {
  SafeHtmlPipe
} from "./chunk-3ZZ6X64X.js";
import "./chunk-RLZFB7T3.js";
import {
  LearningApiService
} from "./chunk-3MQCOGP4.js";
import {
  PageFeedbackComponent
} from "./chunk-GSQJCOEO.js";
import {
  Component,
  DefaultValueAccessor,
  FormsModule,
  HttpClient,
  Injectable,
  MaxValidator,
  MinValidator,
  NgControlStatus,
  NgModel,
  NgSelectOption,
  NumberValueAccessor,
  SelectControlValueAccessor,
  ViewChild,
  __async,
  __spreadProps,
  __spreadValues,
  computed,
  firstValueFrom,
  inject,
  resolveApiBaseUrl,
  setClassMetadata,
  signal,
  viewChild,
  ɵNgSelectMultipleOption,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵcontrol,
  ɵɵcontrolCreate,
  ɵɵdefineComponent,
  ɵɵdefineInjectable,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵgetCurrentView,
  ɵɵlistener,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind1,
  ɵɵproperty,
  ɵɵqueryAdvance,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵrepeaterTrackByIndex,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵsanitizeHtml,
  ɵɵsanitizeUrl,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1,
  ɵɵtwoWayBindingSet,
  ɵɵtwoWayListener,
  ɵɵtwoWayProperty,
  ɵɵviewQuerySignal
} from "./chunk-EMYLRMBO.js";

// src/app/smart-study-assistant.service.ts
var SmartStudyAssistantService = class _SmartStudyAssistantService {
  constructor() {
    this.http = inject(HttpClient);
    this.baseUrl = resolveApiBaseUrl();
  }
  generate(payload) {
    const form = new FormData();
    form.set("action", payload.action);
    form.set("courseId", payload.courseId);
    if (payload.unitId)
      form.set("unitId", payload.unitId);
    if (payload.lessonId)
      form.set("lessonId", payload.lessonId);
    form.set("language", payload.language ?? "ar");
    if (payload.prompt?.trim())
      form.set("prompt", payload.prompt.trim());
    if (payload.questionCount)
      form.set("questionCount", String(payload.questionCount));
    for (const file of payload.files ?? []) {
      form.append("files", file, file.name);
    }
    return this.http.post(`${this.baseUrl}/smart-study-assistant/generate`, form);
  }
  apply(payload) {
    return this.http.post(`${this.baseUrl}/smart-study-assistant/apply`, payload);
  }
  static {
    this.\u0275fac = function SmartStudyAssistantService_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _SmartStudyAssistantService)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _SmartStudyAssistantService, factory: _SmartStudyAssistantService.\u0275fac, providedIn: "root" });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(SmartStudyAssistantService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/shared/markdown-render.ts
function renderMarkdown(source) {
  let text = escapeHtml(source ?? "");
  const lines = text.split(/\r?\n/);
  const out = [];
  let inList = null;
  let inCode = false;
  let tableRows = [];
  const closeList = () => {
    if (inList) {
      out.push(`</${inList}>`);
      inList = null;
    }
  };
  const flushTable = () => {
    if (!tableRows.length)
      return;
    const [head, ...body] = tableRows;
    const cell = (c, tag) => `<${tag}>${inline(c)}</${tag}>`;
    out.push("<table><thead><tr>" + head.map((c) => cell(c, "th")).join("") + "</tr></thead>");
    if (body.length) {
      out.push("<tbody>" + body.map((r) => "<tr>" + r.map((c) => cell(c, "td")).join("") + "</tr>").join("") + "</tbody>");
    }
    out.push("</table>");
    tableRows = [];
  };
  for (const rawLine of lines) {
    const line = rawLine.trimEnd();
    if (/^```/.test(line.trim())) {
      flushTable();
      closeList();
      out.push(inCode ? "</code></pre>" : "<pre><code>");
      inCode = !inCode;
      continue;
    }
    if (inCode) {
      out.push(line);
      continue;
    }
    if (/^\|.*\|/.test(line)) {
      const cells = line.split("|").slice(1, -1).map((c) => c.trim());
      if (cells.every((c) => /^:?-{2,}:?$/.test(c)))
        continue;
      tableRows.push(cells);
      continue;
    }
    flushTable();
    const heading = line.match(/^(#{1,6})\s+(.*)$/);
    if (heading) {
      closeList();
      const level = heading[1].length;
      out.push(`<h${level}>${inline(heading[2])}</h${level}>`);
      continue;
    }
    if (/^(---+|\*\*\*+)\s*$/.test(line.trim())) {
      closeList();
      out.push("<hr>");
      continue;
    }
    const quote = line.match(/^&gt;\s?(.*)$/);
    if (quote) {
      closeList();
      out.push(`<blockquote>${inline(quote[1])}</blockquote>`);
      continue;
    }
    const ul = line.match(/^[-*+]\s+(.*)$/);
    if (ul) {
      if (inList !== "ul") {
        closeList();
        out.push("<ul>");
        inList = "ul";
      }
      out.push(`<li>${inline(ul[1])}</li>`);
      continue;
    }
    const ol = line.match(/^\d+[.)]\s+(.*)$/);
    if (ol) {
      if (inList !== "ol") {
        closeList();
        out.push("<ol>");
        inList = "ol";
      }
      out.push(`<li>${inline(ol[1])}</li>`);
      continue;
    }
    closeList();
    if (line.trim().length > 0) {
      out.push(`<p>${inline(line)}</p>`);
    }
  }
  flushTable();
  closeList();
  if (inCode)
    out.push("</code></pre>");
  return out.join("\n");
}
function inline(text) {
  return text.replace(/`([^`]+)`/g, "<code>$1</code>").replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>").replace(/\*([^*]+)\*/g, "<em>$1</em>").replace(/!\[([^\]]*)\]\(([^)\s]+)\)/g, "<span>$1</span>").replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, (_m, label, href) => /^https?:\/\//i.test(href) ? `<a href="${href}" target="_blank" rel="noopener noreferrer">${label}</a>` : label).replace(/ {2}$/, "<br>");
}
function escapeHtml(value) {
  return (value ?? "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;");
}

// src/app/pages/teacher/smart-study-assistant/smart-study-assistant.component.ts
var _c0 = ["fileInput"];
var _forTrack0 = ($index, $item) => $item.id;
var _forTrack1 = ($index, $item) => $item.key;
function SmartStudyAssistantComponent_For_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 8);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const course_r2 = ctx.$implicit;
    \u0275\u0275property("value", course_r2.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(course_r2.title);
  }
}
function SmartStudyAssistantComponent_For_26_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 8);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const unit_r3 = ctx.$implicit;
    \u0275\u0275property("value", unit_r3.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(unit_r3.title);
  }
}
function SmartStudyAssistantComponent_For_34_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 8);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const lesson_r4 = ctx.$implicit;
    \u0275\u0275property("value", lesson_r4.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(lesson_r4.title);
  }
}
function SmartStudyAssistantComponent_Conditional_52_For_2_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "img", 24);
  }
  if (rf & 2) {
    const entry_r7 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("src", entry_r7.preview, \u0275\u0275sanitizeUrl);
  }
}
function SmartStudyAssistantComponent_Conditional_52_For_2_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 25);
    \u0275\u0275text(1, "\u{1F4C4}");
    \u0275\u0275elementEnd();
  }
}
function SmartStudyAssistantComponent_Conditional_52_For_2_Template(rf, ctx) {
  if (rf & 1) {
    const _r6 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 21);
    \u0275\u0275conditionalCreate(1, SmartStudyAssistantComponent_Conditional_52_For_2_Conditional_1_Template, 1, 1, "img", 24)(2, SmartStudyAssistantComponent_Conditional_52_For_2_Conditional_2_Template, 2, 0, "span", 25);
    \u0275\u0275elementStart(3, "div")(4, "strong");
    \u0275\u0275text(5);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "small");
    \u0275\u0275text(7);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(8, "button", 26);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_52_For_2_Template_button_click_8_listener($event) {
      const $index_r8 = \u0275\u0275restoreView(_r6).$index;
      const ctx_r8 = \u0275\u0275nextContext(2);
      ctx_r8.removeFile($index_r8);
      return \u0275\u0275resetView($event.stopPropagation());
    });
    \u0275\u0275text(9, "\u2715");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const entry_r7 = ctx.$implicit;
    \u0275\u0275advance();
    \u0275\u0275conditional(entry_r7.preview ? 1 : 2);
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(entry_r7.file.name);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1("", (entry_r7.file.size / 1024 / 1024).toFixed(2), " \u0645\u064A\u062C\u0627\u0628\u0627\u064A\u062A");
  }
}
function SmartStudyAssistantComponent_Conditional_52_Template(rf, ctx) {
  if (rf & 1) {
    const _r5 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 20);
    \u0275\u0275repeaterCreate(1, SmartStudyAssistantComponent_Conditional_52_For_2_Template, 10, 3, "div", 21, \u0275\u0275repeaterTrackByIndex);
    \u0275\u0275elementStart(3, "button", 22);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_52_Template_button_click_3_listener($event) {
      \u0275\u0275restoreView(_r5);
      const ctx_r8 = \u0275\u0275nextContext();
      ctx_r8.clearFiles();
      return \u0275\u0275resetView($event.stopPropagation());
    });
    \u0275\u0275text(4, "\u{1F5D1}\uFE0F \u0645\u0633\u062D \u0627\u0644\u0643\u0644");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(5, "small", 23);
    \u0275\u0275text(6, "\u0627\u0646\u0642\u0631 \u0623\u0648 \u0623\u0641\u0644\u062A \u0644\u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u0645\u0632\u064A\u062F \u0645\u0646 \u0627\u0644\u0645\u0644\u0641\u0627\u062A");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r8 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275repeater(ctx_r8.files());
  }
}
function SmartStudyAssistantComponent_Conditional_53_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15)(1, "span", 27);
    \u0275\u0275text(2, "\u2B06\uFE0F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "p")(4, "strong");
    \u0275\u0275text(5, "\u0627\u0633\u062D\u0628 \u0648\u0623\u0641\u0644\u062A");
    \u0275\u0275elementEnd();
    \u0275\u0275text(6, " \u0635\u0648\u0631 \u0623\u0648 \u0645\u0644\u0641\u0627\u062A PDF \u0647\u0646\u0627");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "small");
    \u0275\u0275text(8, "\u0623\u0648 \u0627\u0646\u0642\u0631 \u0644\u0644\u0627\u062E\u062A\u064A\u0627\u0631 \u2014 JPG / PNG / PDF \u062D\u062A\u0649 25 \u0645\u064A\u062C\u0627\u0628\u0627\u064A\u062A \u0644\u0643\u0644 \u0645\u0644\u0641");
    \u0275\u0275elementEnd()();
  }
}
function SmartStudyAssistantComponent_For_59_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 30);
  }
}
function SmartStudyAssistantComponent_For_59_Template(rf, ctx) {
  if (rf & 1) {
    const _r10 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 28);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_For_59_Template_button_click_0_listener() {
      const item_r11 = \u0275\u0275restoreView(_r10).$implicit;
      const ctx_r8 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r8.runAction(item_r11.key));
    });
    \u0275\u0275elementStart(1, "span", 29);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "strong");
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "small");
    \u0275\u0275text(6);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(7, SmartStudyAssistantComponent_For_59_Conditional_7_Template, 1, 0, "span", 30);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const item_r11 = ctx.$implicit;
    const ctx_r8 = \u0275\u0275nextContext();
    \u0275\u0275property("disabled", ctx_r8.loading());
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r11.icon);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r11.label);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r11.hint);
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r8.loading() && ctx_r8.activeAction() === item_r11.key ? 7 : -1);
  }
}
function SmartStudyAssistantComponent_Conditional_60_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 18);
    \u0275\u0275text(1, "\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u062A\u062D\u0644\u064A\u0644 \u0628\u0648\u0627\u0633\u0637\u0629 Gemini... \u0642\u062F \u064A\u0633\u062A\u063A\u0631\u0642 \u0630\u0644\u0643 \u0644\u062D\u0638\u0627\u062A\u060C \u0644\u0627 \u062A\u063A\u0644\u0642 \u0627\u0644\u0635\u0641\u062D\u0629.");
    \u0275\u0275elementEnd();
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Conditional_11_For_2_Template(rf, ctx) {
  if (rf & 1) {
    const _r15 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "label", 45)(1, "input", 46);
    \u0275\u0275listener("change", function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Conditional_11_For_2_Template_input_change_1_listener() {
      const \u0275$index_209_r16 = \u0275\u0275restoreView(_r15).$index;
      const \u0275$index_186_r14 = \u0275\u0275nextContext(2).$index;
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.setCorrectOption(\u0275$index_186_r14, \u0275$index_209_r16));
    });
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(2, "span");
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "input", 47);
    \u0275\u0275listener("ngModelChange", function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Conditional_11_For_2_Template_input_ngModelChange_4_listener($event) {
      const \u0275$index_209_r16 = \u0275\u0275restoreView(_r15).$index;
      const \u0275$index_186_r14 = \u0275\u0275nextContext(2).$index;
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.updateOption(\u0275$index_186_r14, \u0275$index_209_r16, $event));
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const option_r17 = ctx.$implicit;
    const \u0275$index_209_r16 = ctx.$index;
    const ctx_r17 = \u0275\u0275nextContext(2);
    const question_r19 = ctx_r17.$implicit;
    const \u0275$index_186_r14 = ctx_r17.$index;
    const ctx_r8 = \u0275\u0275nextContext(3);
    \u0275\u0275advance();
    \u0275\u0275property("name", "ssa-correct-" + \u0275$index_186_r14)("checked", ctx_r8.isCorrectOption(question_r19, \u0275$index_209_r16));
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(ctx_r8.optionLetter(\u0275$index_209_r16));
    \u0275\u0275advance();
    \u0275\u0275property("ngModel", option_r17);
    \u0275\u0275control();
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 43);
    \u0275\u0275repeaterCreate(1, SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Conditional_11_For_2_Template, 5, 4, "label", 45, \u0275\u0275repeaterTrackByIndex);
    \u0275\u0275elementStart(3, "small");
    \u0275\u0275text(4, "\u062D\u062F\u062F \u0627\u0644\u062F\u0627\u0626\u0631\u0629 \u0628\u062C\u0627\u0646\u0628 \u0627\u0644\u0625\u062C\u0627\u0628\u0629 \u0627\u0644\u0635\u062D\u064A\u062D\u0629.");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const question_r19 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275repeater(question_r19.options);
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    const _r20 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "label", 44)(1, "span");
    \u0275\u0275text(2, "\u0627\u0644\u0625\u062C\u0627\u0628\u0629 \u0627\u0644\u0646\u0645\u0648\u0630\u062C\u064A\u0629");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "textarea", 48);
    \u0275\u0275listener("ngModelChange", function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Conditional_12_Template_textarea_ngModelChange_3_listener($event) {
      \u0275\u0275restoreView(_r20);
      const \u0275$index_186_r14 = \u0275\u0275nextContext().$index;
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.updateQuestion(\u0275$index_186_r14, { correctAnswer: $event }));
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const question_r19 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance(3);
    \u0275\u0275property("ngModel", question_r19.correctAnswer);
    \u0275\u0275control();
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Template(rf, ctx) {
  if (rf & 1) {
    const _r13 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 38)(1, "div", 39)(2, "strong");
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "label", 40)(5, "span");
    \u0275\u0275text(6, "\u0627\u0644\u062F\u0631\u062C\u0629");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "input", 41);
    \u0275\u0275listener("ngModelChange", function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Template_input_ngModelChange_7_listener($event) {
      const \u0275$index_186_r14 = \u0275\u0275restoreView(_r13).$index;
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.updatePoints(\u0275$index_186_r14, $event));
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "button", 26);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Template_button_click_8_listener() {
      const \u0275$index_186_r14 = \u0275\u0275restoreView(_r13).$index;
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.removeQuestion(\u0275$index_186_r14));
    });
    \u0275\u0275text(9, "\u{1F5D1}\uFE0F \u062D\u0630\u0641");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(10, "textarea", 42);
    \u0275\u0275listener("ngModelChange", function SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Template_textarea_ngModelChange_10_listener($event) {
      const \u0275$index_186_r14 = \u0275\u0275restoreView(_r13).$index;
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.updateQuestion(\u0275$index_186_r14, { prompt: $event }));
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275conditionalCreate(11, SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Conditional_11_Template, 5, 0, "div", 43)(12, SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Conditional_12_Template, 4, 1, "label", 44);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const question_r19 = ctx.$implicit;
    const \u0275$index_186_r14 = ctx.$index;
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1("\u0627\u0644\u0633\u0624\u0627\u0644 ", \u0275$index_186_r14 + 1);
    \u0275\u0275advance(4);
    \u0275\u0275property("ngModel", question_r19.points);
    \u0275\u0275control();
    \u0275\u0275advance(3);
    \u0275\u0275property("ngModel", question_r19.prompt);
    \u0275\u0275control();
    \u0275\u0275advance();
    \u0275\u0275conditional(question_r19.options.length > 0 ? 11 : 12);
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 34)(1, "h3");
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "small");
    \u0275\u0275text(4, "\u064A\u0645\u0643\u0646\u0643 \u062A\u0639\u062F\u064A\u0644 \u0623\u064A \u0633\u0624\u0627\u0644 \u0623\u0648 \u062D\u0630\u0641\u0647 \u2014 \u0645\u0627 \u064A\u0638\u0647\u0631 \u0647\u0646\u0627 \u0647\u0648 \u0645\u0627 \u0633\u064A\u062A\u0645 \u062D\u0641\u0638\u0647.");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(5, SmartStudyAssistantComponent_Conditional_61_Conditional_9_For_6_Template, 13, 4, "div", 38, \u0275\u0275repeaterTrackByIndex);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r8 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1("\u0631\u0627\u062C\u0639 \u0627\u0644\u0623\u0633\u0626\u0644\u0629 \u0642\u0628\u0644 \u0627\u0644\u062D\u0641\u0638 (", ctx_r8.questions().length, ")");
    \u0275\u0275advance(3);
    \u0275\u0275repeater(ctx_r8.questions());
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_10_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    const _r21 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 50);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_61_Conditional_10_Conditional_3_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r21);
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.applyTo("tree"));
    });
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r8 = \u0275\u0275nextContext(3);
    \u0275\u0275property("disabled", ctx_r8.applying() !== null);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r8.applying() === "tree" ? "\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u0625\u0636\u0627\u0641\u0629..." : "\u{1F333} \u0641\u0647\u0631\u0633 \u0627\u0644\u0645\u0627\u062F\u0629 (\u0648\u062D\u062F\u0627\u062A \u0648\u062F\u0631\u0648\u0633)", " ");
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_10_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    const _r22 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 50);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_61_Conditional_10_Conditional_4_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r22);
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.applyTo("bank"));
    });
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(2, "button", 50);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_61_Conditional_10_Conditional_4_Template_button_click_2_listener() {
      \u0275\u0275restoreView(_r22);
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.applyTo("quiz"));
    });
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "button", 50);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_61_Conditional_10_Conditional_4_Template_button_click_4_listener() {
      \u0275\u0275restoreView(_r22);
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.applyTo("assignment"));
    });
    \u0275\u0275text(5);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "button", 50);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_61_Conditional_10_Conditional_4_Template_button_click_6_listener() {
      \u0275\u0275restoreView(_r22);
      const ctx_r8 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r8.applyTo("exam"));
    });
    \u0275\u0275text(7);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r8 = \u0275\u0275nextContext(3);
    \u0275\u0275property("disabled", ctx_r8.applying() !== null);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r8.applying() === "bank" ? "\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u062D\u0641\u0638..." : "\u{1F5C3}\uFE0F \u0628\u0646\u0643 \u0627\u0644\u0623\u0633\u0626\u0644\u0629", " ");
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r8.applying() !== null);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r8.applying() === "quiz" ? "\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u0625\u0646\u0634\u0627\u0621..." : "\u2753 \u0643\u0648\u064A\u0632 \u062C\u062F\u064A\u062F", " ");
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r8.applying() !== null);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r8.applying() === "assignment" ? "\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u0625\u0646\u0634\u0627\u0621..." : "\u{1F4DA} \u0648\u0627\u062C\u0628 \u062C\u062F\u064A\u062F", " ");
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r8.applying() !== null);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r8.applying() === "exam" ? "\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u062D\u0641\u0638..." : "\u{1F4DD} \u0623\u0633\u0626\u0644\u0629 \u0627\u062E\u062A\u0628\u0627\u0631", " ");
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 35)(1, "span");
    \u0275\u0275text(2, "\u0623\u0636\u0641 \u0627\u0644\u0646\u062A\u064A\u062C\u0629 \u0625\u0644\u0649:");
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(3, SmartStudyAssistantComponent_Conditional_61_Conditional_10_Conditional_3_Template, 2, 2, "button", 49);
    \u0275\u0275conditionalCreate(4, SmartStudyAssistantComponent_Conditional_61_Conditional_10_Conditional_4_Template, 8, 8);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r8 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r8.hasUnits() ? 3 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r8.hasQuestions() ? 4 : -1);
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "details", 36)(1, "summary");
    \u0275\u0275text(2, "\u0639\u0631\u0636 \u0627\u0644\u0646\u0635 \u0627\u0644\u0623\u0635\u0644\u064A \u0643\u0645\u0627 \u0623\u0646\u0634\u0623\u0647 \u0627\u0644\u0645\u0633\u0627\u0639\u062F (\u0642\u0628\u0644 \u0627\u0644\u062A\u0639\u062F\u064A\u0644)");
    \u0275\u0275elementEnd();
    \u0275\u0275element(3, "article", 37);
    \u0275\u0275pipe(4, "safeHtml");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r8 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(3);
    \u0275\u0275property("innerHTML", \u0275\u0275pipeBind1(4, 1, ctx_r8.renderedHtml()), \u0275\u0275sanitizeHtml);
  }
}
function SmartStudyAssistantComponent_Conditional_61_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "article", 37);
    \u0275\u0275pipe(1, "safeHtml");
  }
  if (rf & 2) {
    const ctx_r8 = \u0275\u0275nextContext(2);
    \u0275\u0275property("innerHTML", \u0275\u0275pipeBind1(1, 1, ctx_r8.renderedHtml()), \u0275\u0275sanitizeHtml);
  }
}
function SmartStudyAssistantComponent_Conditional_61_Template(rf, ctx) {
  if (rf & 1) {
    const _r12 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 19)(1, "div", 31)(2, "h2");
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "div", 32)(5, "button", 22);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_61_Template_button_click_5_listener() {
      \u0275\u0275restoreView(_r12);
      const ctx_r8 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r8.copyResult());
    });
    \u0275\u0275text(6, "\u{1F4CB} \u0646\u0633\u062E \u0627\u0644\u0646\u0635");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "button", 33);
    \u0275\u0275listener("click", function SmartStudyAssistantComponent_Conditional_61_Template_button_click_7_listener() {
      \u0275\u0275restoreView(_r12);
      const ctx_r8 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r8.downloadPdf());
    });
    \u0275\u0275text(8, "\u2B07\uFE0F \u062A\u062D\u0645\u064A\u0644 \u0643\u0640 PDF");
    \u0275\u0275elementEnd()()();
    \u0275\u0275conditionalCreate(9, SmartStudyAssistantComponent_Conditional_61_Conditional_9_Template, 7, 1, "div", 34);
    \u0275\u0275conditionalCreate(10, SmartStudyAssistantComponent_Conditional_61_Conditional_10_Template, 5, 2, "div", 35);
    \u0275\u0275conditionalCreate(11, SmartStudyAssistantComponent_Conditional_61_Conditional_11_Template, 5, 3, "details", 36)(12, SmartStudyAssistantComponent_Conditional_61_Conditional_12_Template, 2, 3, "article", 37);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r8 = \u0275\u0275nextContext();
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(ctx_r8.resultTitle() || "\u0627\u0644\u0646\u062A\u064A\u062C\u0629");
    \u0275\u0275advance(6);
    \u0275\u0275conditional(ctx_r8.hasQuestions() ? 9 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r8.hasQuestions() || ctx_r8.hasUnits() ? 10 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r8.hasQuestions() ? 11 : 12);
  }
}
var SmartStudyAssistantComponent = class _SmartStudyAssistantComponent {
  constructor() {
    this.api = inject(LearningApiService);
    this.ai = inject(SmartStudyAssistantService);
    this.fileInput = viewChild(
      "fileInput",
      ...ngDevMode ? [{ debugName: "fileInput" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.courses = signal(
      [],
      ...ngDevMode ? [{ debugName: "courses" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.units = signal(
      [],
      ...ngDevMode ? [{ debugName: "units" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.lessons = signal(
      [],
      ...ngDevMode ? [{ debugName: "lessons" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.loading = signal(
      false,
      ...ngDevMode ? [{ debugName: "loading" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.applying = signal(
      null,
      ...ngDevMode ? [{ debugName: "applying" }] : (
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
    this.message = signal(
      "",
      ...ngDevMode ? [{ debugName: "message" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.courseId = signal(
      "",
      ...ngDevMode ? [{ debugName: "courseId" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.unitId = signal(
      "",
      ...ngDevMode ? [{ debugName: "unitId" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.lessonId = signal(
      "",
      ...ngDevMode ? [{ debugName: "lessonId" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.selectedCourseId = "";
    this.selectedUnitId = "";
    this.selectedLessonId = "";
    this.files = signal(
      [],
      ...ngDevMode ? [{ debugName: "files" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.prompt = "";
    this.questionCount = 5;
    this.activeAction = signal(
      null,
      ...ngDevMode ? [{ debugName: "activeAction" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.result = signal(
      null,
      ...ngDevMode ? [{ debugName: "result" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.resultTitle = signal(
      "",
      ...ngDevMode ? [{ debugName: "resultTitle" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.resultMarkdown = signal(
      "",
      ...ngDevMode ? [{ debugName: "resultMarkdown" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.renderedHtml = computed(
      () => renderMarkdown(this.resultMarkdown()),
      ...ngDevMode ? [{ debugName: "renderedHtml" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.questions = signal(
      [],
      ...ngDevMode ? [{ debugName: "questions" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.hasQuestions = computed(
      () => this.questions().length > 0,
      ...ngDevMode ? [{ debugName: "hasQuestions" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.hasUnits = computed(
      () => (this.result()?.units?.length ?? 0) > 0,
      ...ngDevMode ? [{ debugName: "hasUnits" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.actions = [
      { key: "Outline", label: "\u0625\u0646\u0634\u0627\u0621 \u0641\u0647\u0631\u0633 \u0644\u0644\u0645\u0627\u062F\u0629", icon: "\u{1F5C2}\uFE0F", hint: "\u0627\u0633\u062A\u062E\u0631\u0627\u062C \u0627\u0644\u0639\u0646\u0627\u0648\u064A\u0646 \u0627\u0644\u0631\u0626\u064A\u0633\u064A\u0629 \u0648\u0627\u0644\u0641\u0631\u0639\u064A\u0629 \u0641\u064A \u0642\u0627\u0626\u0645\u0629 \u0634\u062C\u0631\u064A\u0629" },
      { key: "Summary", label: "\u062A\u0644\u062E\u064A\u0635 \u0627\u0644\u0645\u062D\u062A\u0648\u0649", icon: "\u{1F4DD}", hint: "\u0645\u0644\u062E\u0635 \u062F\u0631\u0627\u0633\u064A \u0645\u0646\u0638\u0645 \u0628\u0623\u0647\u0645 \u0627\u0644\u0646\u0642\u0627\u0637" },
      { key: "Assignment", label: "\u062A\u0648\u0644\u064A\u062F \u0648\u0627\u062C\u0628 / \u0623\u0633\u0626\u0644\u0629 \u0645\u0642\u0627\u0644\u064A\u0629", icon: "\u{1F4DA}", hint: "\u0623\u0633\u0626\u0644\u0629 \u062A\u0637\u0628\u064A\u0642\u064A\u0629 \u062A\u0642\u064A\u0633 \u0641\u0647\u0645 \u0627\u0644\u062F\u0631\u0633 \u0645\u0639 \u0627\u0644\u0625\u062C\u0627\u0628\u0627\u062A" },
      { key: "Quiz", label: "\u0625\u0646\u0634\u0627\u0621 \u0643\u0648\u064A\u0632 / \u0627\u062E\u062A\u064A\u0627\u0631 \u0645\u0646 \u0645\u062A\u0639\u062F\u062F", icon: "\u2753", hint: "\u0623\u0633\u0626\u0644\u0629 \u0627\u062E\u062A\u064A\u0627\u0631 \u0645\u0646 \u0645\u062A\u0639\u062F\u062F \u0645\u0639 \u0645\u0641\u062A\u0627\u062D \u0627\u0644\u0625\u062C\u0627\u0628\u0627\u062A" }
    ];
    void this.loadCourses();
  }
  loadCourses() {
    return __async(this, null, function* () {
      this.error.set("");
      try {
        const courses = yield firstValueFrom(this.api.getCourses(false));
        this.courses.set(courses.map((c) => ({ id: c.id, title: c.title })));
      } catch (e) {
        this.error.set("\u062A\u0639\u0630\u0631 \u062A\u062D\u0645\u064A\u0644 \u0627\u0644\u0643\u0648\u0631\u0633\u0627\u062A. \u062A\u062D\u0642\u0642 \u0645\u0646 \u0627\u0644\u0627\u062A\u0635\u0627\u0644 \u0648\u0623\u0639\u062F \u0627\u0644\u0645\u062D\u0627\u0648\u0644\u0629.");
      }
    });
  }
  onCourseChange(courseId) {
    return __async(this, null, function* () {
      this.courseId.set(courseId);
      this.unitId.set("");
      this.lessonId.set("");
      this.selectedUnitId = "";
      this.selectedLessonId = "";
      this.units.set([]);
      this.lessons.set([]);
      if (!courseId)
        return;
      try {
        const course = yield firstValueFrom(this.api.getCourse(courseId));
        this.units.set((course.units ?? []).map((u) => ({ id: u.id, title: u.title })));
      } catch (e) {
        this.error.set("\u062A\u0639\u0630\u0631 \u062A\u062D\u0645\u064A\u0644 \u0648\u062D\u062F\u0627\u062A \u0647\u0630\u0627 \u0627\u0644\u0643\u0648\u0631\u0633.");
      }
    });
  }
  onUnitChange(unitId) {
    return __async(this, null, function* () {
      this.unitId.set(unitId);
      this.lessonId.set("");
      this.selectedLessonId = "";
      this.lessons.set([]);
      if (!unitId)
        return;
      try {
        const course = yield firstValueFrom(this.api.getCourse(this.courseId()));
        const unit = (course.units ?? []).find((u) => u.id === unitId);
        this.lessons.set((unit?.lessons ?? []).map((l) => ({ id: l.id, title: l.title, unitId })));
      } catch (e) {
        this.error.set("\u062A\u0639\u0630\u0631 \u062A\u062D\u0645\u064A\u0644 \u062F\u0631\u0648\u0633 \u0647\u0630\u0647 \u0627\u0644\u0648\u062D\u062F\u0629.");
      }
    });
  }
  onFilesChosen(event) {
    const input = event.target;
    this.addFiles(Array.from(input.files ?? []));
  }
  onDrop(event) {
    event.preventDefault();
    this.addFiles(Array.from(event.dataTransfer?.files ?? []));
  }
  onDragOver(event) {
    event.preventDefault();
  }
  openFilePicker() {
    this.fileInput()?.nativeElement.click();
  }
  addFiles(incoming) {
    this.error.set("");
    if (incoming.length === 0)
      return;
    const accepted = [...this.files()];
    for (const file of incoming) {
      const ok = /\.(pdf|jpe?g|png)$/i.test(file.name) || file.type === "application/pdf" || file.type.startsWith("image/");
      if (!ok) {
        this.error.set(`\u0627\u0644\u0645\u0644\u0641 "${file.name}" \u063A\u064A\u0631 \u0645\u062F\u0639\u0648\u0645. \u0627\u0631\u0641\u0639 \u0635\u0648\u0631 (JPG / PNG) \u0623\u0648 \u0645\u0644\u0641\u0627\u062A PDF \u0641\u0642\u0637.`);
        continue;
      }
      if (file.size > 25 * 1024 * 1024) {
        this.error.set(`\u0627\u0644\u0645\u0644\u0641 "${file.name}" \u0643\u0628\u064A\u0631 \u062C\u062F\u0627\u064B. \u0627\u0644\u062D\u062F \u0627\u0644\u0623\u0642\u0635\u0649 25 \u0645\u064A\u062C\u0627\u0628\u0627\u064A\u062A.`);
        continue;
      }
      if (accepted.length >= 10) {
        this.error.set("\u0627\u0644\u062D\u062F \u0627\u0644\u0623\u0642\u0635\u0649 10 \u0645\u0644\u0641\u0627\u062A \u0641\u064A \u0627\u0644\u0645\u0631\u0629 \u0627\u0644\u0648\u0627\u062D\u062F\u0629.");
        break;
      }
      const entry = { file, preview: "" };
      if (file.type.startsWith("image/")) {
        const reader = new FileReader();
        reader.onload = () => {
          entry.preview = String(reader.result ?? "");
          this.files.set([...this.files()]);
        };
        reader.readAsDataURL(file);
      }
      accepted.push(entry);
    }
    this.files.set(accepted);
  }
  removeFile(index) {
    this.files.set(this.files().filter((_, i) => i !== index));
    const input = this.fileInput()?.nativeElement;
    if (input)
      input.value = "";
  }
  clearFiles() {
    this.files.set([]);
    const input = this.fileInput()?.nativeElement;
    if (input)
      input.value = "";
  }
  onCourseModelChange(value) {
    this.selectedCourseId = value;
    void this.onCourseChange(value);
  }
  onUnitModelChange(value) {
    this.selectedUnitId = value;
    void this.onUnitChange(value);
  }
  onLessonModelChange(value) {
    this.selectedLessonId = value;
    this.lessonId.set(value);
  }
  runAction(action) {
    return __async(this, null, function* () {
      if (!this.courseId()) {
        this.error.set("\u0627\u062E\u062A\u0631 \u0627\u0644\u0643\u0648\u0631\u0633 \u0623\u0648\u0644\u0627\u064B.");
        return;
      }
      if (!this.files().length && !this.prompt.trim()) {
        this.error.set("\u0627\u0631\u0641\u0639 \u0645\u0644\u0641\u0627\u064B \u0623\u0648 \u0627\u0643\u062A\u0628 \u0648\u0635\u0641\u0627\u064B \u0646\u0635\u064A\u0627\u064B \u0644\u0644\u0645\u062D\u062A\u0648\u0649 \u0627\u0644\u0645\u0637\u0644\u0648\u0628.");
        return;
      }
      if (this.loading())
        return;
      const wantsQuestions = action === "Assignment" || action === "Quiz";
      const questionCount = Math.trunc(Number(this.questionCount));
      if (wantsQuestions && !(questionCount >= 1 && questionCount <= 50)) {
        this.error.set("\u0639\u062F\u062F \u0627\u0644\u0623\u0633\u0626\u0644\u0629 \u064A\u062C\u0628 \u0623\u0646 \u064A\u0643\u0648\u0646 \u0628\u064A\u0646 1 \u0648 50.");
        return;
      }
      this.error.set("");
      this.message.set("");
      this.activeAction.set(action);
      this.loading.set(true);
      this.result.set(null);
      this.questions.set([]);
      this.resultTitle.set("");
      this.resultMarkdown.set("");
      try {
        const result = yield firstValueFrom(this.ai.generate({
          action,
          courseId: this.courseId(),
          unitId: this.unitId() || null,
          lessonId: this.lessonId() || null,
          language: "ar",
          prompt: this.prompt.trim() || null,
          questionCount: wantsQuestions ? questionCount : null,
          files: this.files().map((f) => f.file)
        }));
        this.result.set(result);
        this.questions.set((result.questions ?? []).map((q) => __spreadProps(__spreadValues({}, q), { options: [...q.options ?? []] })));
        this.resultTitle.set(result.title);
        this.resultMarkdown.set(result.markdown);
        this.message.set("\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0645\u062D\u062A\u0648\u0649 \u0628\u0646\u062C\u0627\u062D \u2728");
      } catch (ex) {
        this.error.set(this.describeError(ex));
      } finally {
        this.loading.set(false);
        this.activeAction.set(null);
      }
    });
  }
  applyTo(target) {
    return __async(this, null, function* () {
      const result = this.result();
      if (!result || !this.courseId() || this.applying())
        return;
      const questions = this.questions().map((q, i) => __spreadProps(__spreadValues({}, q), {
        prompt: q.prompt.trim(),
        options: q.options.map((o) => o.trim()),
        sortOrder: i + 1
      }));
      if (target !== "tree") {
        const invalid = questions.findIndex((q) => !q.prompt || q.options.some((o) => !o));
        if (invalid >= 0) {
          this.error.set(`\u0627\u0644\u0633\u0624\u0627\u0644 \u0631\u0642\u0645 ${invalid + 1} \u064A\u062D\u062A\u0648\u064A \u0639\u0644\u0649 \u0646\u0635 \u0633\u0624\u0627\u0644 \u0623\u0648 \u0627\u062E\u062A\u064A\u0627\u0631 \u0641\u0627\u0631\u063A. \u0623\u0643\u0645\u0644\u0647 \u0623\u0648 \u0627\u062D\u0630\u0641\u0647 \u0642\u0628\u0644 \u0627\u0644\u062D\u0641\u0638.`);
          return;
        }
      }
      this.error.set("");
      this.applying.set(target);
      try {
        const response = yield firstValueFrom(this.ai.apply({
          courseId: this.courseId(),
          target,
          title: result.title || "\u0627\u0644\u0645\u0633\u0627\u0639\u062F \u0627\u0644\u0630\u0643\u064A \u0644\u0644\u062F\u0631\u0627\u0633\u0629",
          description: null,
          unitId: this.unitId() || null,
          lessonId: this.lessonId() || null,
          mode: "update",
          questions: questions.length ? questions : null,
          units: result.units?.length ? result.units : null
        }));
        this.message.set(response.message);
      } catch (ex) {
        this.error.set(this.describeError(ex));
      } finally {
        this.applying.set(null);
      }
    });
  }
  optionLetter(index) {
    return String.fromCharCode(65 + index);
  }
  isCorrectOption(question, index) {
    const key = (question.correctOption ?? "").trim().toUpperCase();
    if (/^[A-Z]$/.test(key))
      return key === this.optionLetter(index);
    const answer = (question.correctAnswer ?? "").trim();
    return answer.length > 0 && answer === (question.options[index] ?? "").trim();
  }
  updateQuestion(index, patch) {
    this.questions.update((list) => list.map((q, i) => i === index ? __spreadValues(__spreadValues({}, q), patch) : q));
  }
  updateOption(index, optionIndex, value) {
    const question = this.questions()[index];
    if (!question)
      return;
    const wasCorrect = this.isCorrectOption(question, optionIndex);
    const options = question.options.map((o, i) => i === optionIndex ? value : o);
    this.updateQuestion(index, wasCorrect ? { options, correctOption: this.optionLetter(optionIndex), correctAnswer: value } : { options });
  }
  setCorrectOption(index, optionIndex) {
    const question = this.questions()[index];
    if (!question)
      return;
    this.updateQuestion(index, {
      correctOption: this.optionLetter(optionIndex),
      correctAnswer: question.options[optionIndex] ?? ""
    });
  }
  updatePoints(index, value) {
    const points = Math.trunc(Number(value));
    this.updateQuestion(index, { points: points >= 1 ? points : 1 });
  }
  removeQuestion(index) {
    this.questions.update((list) => list.filter((_, i) => i !== index));
  }
  copyResult() {
    return __async(this, null, function* () {
      const text = this.resultMarkdown();
      if (!text)
        return;
      try {
        yield navigator.clipboard.writeText(text);
        this.message.set("\u062A\u0645 \u0646\u0633\u062E \u0627\u0644\u0646\u0635 \u{1F4CB}");
      } catch (e) {
        this.error.set("\u062A\u0639\u0630\u0631 \u0627\u0644\u0646\u0633\u062E \u0625\u0644\u0649 \u0627\u0644\u062D\u0627\u0641\u0638\u0629.");
      }
    });
  }
  downloadPdf() {
    const title = this.resultTitle() || "\u0627\u0644\u0645\u0633\u0627\u0639\u062F \u0627\u0644\u0630\u0643\u064A \u0644\u0644\u062F\u0631\u0627\u0633\u0629";
    const html = this.renderedHtml();
    if (!html)
      return;
    const win = window.open("", "_blank");
    if (!win) {
      this.error.set("\u0645\u0646 \u0641\u0636\u0644\u0643 \u0627\u0633\u0645\u062D \u0628\u0627\u0644\u0646\u0648\u0627\u0641\u0630 \u0627\u0644\u0645\u0646\u0628\u062B\u0642\u0629 \u0644\u062A\u062D\u0645\u064A\u0644 \u0627\u0644\u0645\u0644\u0641.");
      return;
    }
    win.document.write(`<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
  <meta charset="utf-8">
  <title>${title.replace(/[<>&]/g, "")}</title>
  <style>
    body { font-family: 'Segoe UI', Tahoma, sans-serif; padding: 24px; color: #1c2434; line-height: 1.8; }
    h1,h2,h3 { color: #14344b; }
    table { border-collapse: collapse; width: 100%; margin: 12px 0; }
    th, td { border: 1px solid #c6d2dd; padding: 6px 10px; }
    th { background: #eef4f9; }
    pre { background: #f4f7fa; padding: 12px; border-radius: 8px; white-space: pre-wrap; }
    code { font-family: Consolas, monospace; }
    blockquote { border-inline-start: 4px solid #7aa7c7; margin: 8px 0; padding: 4px 14px; color: #44566b; }
  </style>
</head>
<body>
  <h1>${title.replace(/[<>&]/g, "")}</h1>
  ${html}
</body>
</html>`);
    win.document.close();
    win.focus();
    setTimeout(() => win.print(), 400);
  }
  describeError(ex) {
    if (typeof ex === "object" && ex !== null && "error" in ex) {
      const problem = ex.error;
      const detail = problem?.detail || problem?.title;
      if (detail)
        return detail;
    }
    if (typeof ex === "object" && ex !== null && "status" in ex) {
      const status = ex.status;
      if (status === 0)
        return "\u0627\u0646\u0642\u0637\u0639 \u0627\u0644\u0627\u062A\u0635\u0627\u0644 \u0628\u0627\u0644\u062E\u0627\u062F\u0645. \u062A\u062D\u0642\u0642 \u0645\u0646 \u0627\u0644\u0634\u0628\u0643\u0629 \u0648\u0623\u0639\u062F \u0627\u0644\u0645\u062D\u0627\u0648\u0644\u0629.";
      if (status === 401)
        return "\u0627\u0646\u062A\u0647\u062A \u0635\u0644\u0627\u062D\u064A\u0629 \u0627\u0644\u062C\u0644\u0633\u0629. \u0645\u0646 \u0641\u0636\u0644\u0643 \u0633\u062C\u0651\u0644 \u0627\u0644\u062F\u062E\u0648\u0644 \u0645\u0631\u0629 \u0623\u062E\u0631\u0649.";
      if (status === 403)
        return "\u0644\u0627 \u062A\u0645\u0644\u0643 \u0635\u0644\u0627\u062D\u064A\u0629 \u062A\u0646\u0641\u064A\u0630 \u0647\u0630\u0627 \u0627\u0644\u0625\u062C\u0631\u0627\u0621 \u0639\u0644\u0649 \u0647\u0630\u0627 \u0627\u0644\u0643\u0648\u0631\u0633.";
      if (status === 404)
        return "\u0627\u0644\u0639\u0646\u0635\u0631 \u0627\u0644\u0645\u0637\u0644\u0648\u0628 \u063A\u064A\u0631 \u0645\u0648\u062C\u0648\u062F.";
      return `\u062D\u062F\u062B \u062E\u0637\u0623 \u0623\u062B\u0646\u0627\u0621 \u0627\u0644\u0627\u062A\u0635\u0627\u0644 \u0628\u0627\u0644\u062E\u0627\u062F\u0645 (\u0631\u0645\u0632 ${status}).`;
    }
    return "\u062D\u062F\u062B \u062E\u0637\u0623 \u063A\u064A\u0631 \u0645\u062A\u0648\u0642\u0639. \u0623\u0639\u062F \u0627\u0644\u0645\u062D\u0627\u0648\u0644\u0629.";
  }
  static {
    this.\u0275fac = function SmartStudyAssistantComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _SmartStudyAssistantComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _SmartStudyAssistantComponent, selectors: [["app-smart-study-assistant"]], viewQuery: function SmartStudyAssistantComponent_Query(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275viewQuerySignal(ctx.fileInput, _c0, 5);
      }
      if (rf & 2) {
        \u0275\u0275queryAdvance();
      }
    }, decls: 62, vars: 12, consts: [["fileInput", ""], [1, "ssa-page"], [1, "ssa-header"], [3, "ok", "error"], [1, "ssa-card"], [1, "ssa-grid"], [3, "ngModelChange", "ngModel"], ["value", ""], [3, "value"], [3, "ngModelChange", "ngModel", "disabled"], ["type", "number", "min", "1", "max", "50", "step", "1", 3, "ngModelChange", "ngModel"], [1, "ssa-prompt-label"], ["rows", "5", "placeholder", "\u0645\u062B\u0627\u0644: \u0627\u0643\u062A\u0628 \u0644\u064A \u0645\u0644\u062E\u0635\u0627\u064B \u0639\u0646 \u062F\u0631\u0633 \u0627\u0644\u0643\u0633\u0648\u0631 \u0627\u0644\u0639\u0634\u0631\u064A\u0629 \u0644\u0644\u0635\u0641 \u0627\u0644\u062E\u0627\u0645\u0633\u060C \u064A\u0634\u0631\u062D \u062A\u062D\u0648\u064A\u0644 \u0627\u0644\u0643\u0633\u0648\u0631 \u0625\u0644\u0649 \u0643\u0633\u0648\u0631 \u0639\u0634\u0631\u064A\u0629 \u0645\u0639 \u0623\u0645\u062B\u0644\u0629 \u0645\u062D\u0644\u0648\u0644\u0629\u060C \u0648\u0623\u0646\u0634\u0626 \u0643\u0648\u064A\u0632\u064B\u0627 \u0645\u0646 5 \u0623\u0633\u0626\u0644\u0629 \u0639\u0646 \u0630\u0644\u0643.", 1, "ssa-prompt", 3, "ngModelChange", "ngModel"], [1, "ssa-dropzone", 3, "drop", "dragover", "click"], ["type", "file", "accept", ".pdf,.jpg,.jpeg,.png", "multiple", "", "hidden", "", 3, "change"], [1, "ssa-dropzone-hint"], [1, "ssa-actions"], ["type", "button", 1, "ssa-action", 3, "disabled"], [1, "ssa-loading"], [1, "ssa-card", "ssa-result"], [1, "ssa-files"], [1, "ssa-file"], ["type", "button", 1, "ssa-tool", 3, "click"], [1, "ssa-addmore"], ["alt", "\u0645\u0639\u0627\u064A\u0646\u0629", 3, "src"], [1, "ssa-file-icon"], ["type", "button", 1, "ssa-remove", 3, "click"], [1, "ssa-dropzone-icon"], ["type", "button", 1, "ssa-action", 3, "click", "disabled"], [1, "ssa-action-icon"], ["aria-hidden", "true", 1, "ssa-spinner"], [1, "ssa-result-head"], [1, "ssa-result-tools"], ["type", "button", 1, "ssa-tool", "primary", 3, "click"], [1, "ssa-questions"], [1, "ssa-apply"], [1, "ssa-original"], [1, "ssa-markdown", 3, "innerHTML"], [1, "ssa-question"], [1, "ssa-question-head"], [1, "ssa-points"], ["type", "number", "min", "1", "step", "1", 3, "ngModelChange", "ngModel"], ["rows", "2", 1, "ssa-question-text", 3, "ngModelChange", "ngModel"], [1, "ssa-options"], [1, "ssa-answer"], [1, "ssa-option"], ["type", "radio", "title", "\u0627\u0644\u0625\u062C\u0627\u0628\u0629 \u0627\u0644\u0635\u062D\u064A\u062D\u0629", 3, "change", "name", "checked"], ["type", "text", 3, "ngModelChange", "ngModel"], ["rows", "2", 3, "ngModelChange", "ngModel"], ["type", "button", 1, "ssa-tool", 3, "disabled"], ["type", "button", 1, "ssa-tool", 3, "click", "disabled"]], template: function SmartStudyAssistantComponent_Template(rf, ctx) {
      if (rf & 1) {
        const _r1 = \u0275\u0275getCurrentView();
        \u0275\u0275elementStart(0, "section", 1)(1, "header", 2)(2, "h1");
        \u0275\u0275text(3, "\u{1F916} \u0627\u0644\u0645\u0633\u0627\u0639\u062F \u0627\u0644\u0630\u0643\u064A \u0644\u0644\u062F\u0631\u0627\u0633\u0629");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(4, "p");
        \u0275\u0275text(5, "\u0627\u0631\u0641\u0639 \u0635\u0648\u0631\u0627\u064B \u0623\u0648 \u0645\u0644\u0641\u0627\u062A PDF\u060C \u0627\u062E\u062A\u0631 \u0627\u0644\u0643\u0648\u0631\u0633 \u0648\u0627\u0644\u0648\u062D\u062F\u0629 \u0648\u0627\u0644\u062F\u0631\u0633\u060C \u062B\u0645 \u0627\u062E\u062A\u0631 \u0627\u0644\u0625\u062C\u0631\u0627\u0621 \u2014 \u0648\u0633\u064A\u062D\u0644\u0644 Gemini \u0627\u0644\u0645\u062D\u062A\u0648\u0649 \u0648\u064A\u0646\u0634\u0626 \u0627\u0644\u0646\u062A\u064A\u062C\u0629\u060C \u0648\u064A\u0645\u0643\u0646\u0643 \u0625\u0636\u0627\u0641\u062A\u0647\u0627 \u0645\u0628\u0627\u0634\u0631\u0629 \u0644\u0644\u0645\u0646\u0647\u062C \u0623\u0648 \u0627\u0644\u0643\u0648\u064A\u0632\u0627\u062A \u0623\u0648 \u0627\u0644\u0648\u0627\u062C\u0628\u0627\u062A.");
        \u0275\u0275elementEnd()();
        \u0275\u0275element(6, "app-page-feedback", 3);
        \u0275\u0275elementStart(7, "div", 4)(8, "h2");
        \u0275\u0275text(9, "1\uFE0F\u20E3 \u062A\u062D\u062F\u064A\u062F \u0627\u0644\u0633\u064A\u0627\u0642");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(10, "div", 5)(11, "label")(12, "span");
        \u0275\u0275text(13, "\u0627\u0644\u0643\u0648\u0631\u0633");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(14, "select", 6);
        \u0275\u0275twoWayListener("ngModelChange", function SmartStudyAssistantComponent_Template_select_ngModelChange_14_listener($event) {
          \u0275\u0275restoreView(_r1);
          \u0275\u0275twoWayBindingSet(ctx.selectedCourseId, $event) || (ctx.selectedCourseId = $event);
          return \u0275\u0275resetView($event);
        });
        \u0275\u0275listener("ngModelChange", function SmartStudyAssistantComponent_Template_select_ngModelChange_14_listener($event) {
          return ctx.onCourseModelChange($event);
        });
        \u0275\u0275elementStart(15, "option", 7);
        \u0275\u0275text(16, "\u2014 \u0627\u062E\u062A\u0631 \u0627\u0644\u0643\u0648\u0631\u0633 \u2014");
        \u0275\u0275elementEnd();
        \u0275\u0275repeaterCreate(17, SmartStudyAssistantComponent_For_18_Template, 2, 2, "option", 8, _forTrack0);
        \u0275\u0275elementEnd();
        \u0275\u0275controlCreate();
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(19, "label")(20, "span");
        \u0275\u0275text(21, "\u0627\u0644\u0648\u062D\u062F\u0629");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(22, "select", 9);
        \u0275\u0275twoWayListener("ngModelChange", function SmartStudyAssistantComponent_Template_select_ngModelChange_22_listener($event) {
          \u0275\u0275restoreView(_r1);
          \u0275\u0275twoWayBindingSet(ctx.selectedUnitId, $event) || (ctx.selectedUnitId = $event);
          return \u0275\u0275resetView($event);
        });
        \u0275\u0275listener("ngModelChange", function SmartStudyAssistantComponent_Template_select_ngModelChange_22_listener($event) {
          return ctx.onUnitModelChange($event);
        });
        \u0275\u0275elementStart(23, "option", 7);
        \u0275\u0275text(24, "\u2014 \u0627\u0644\u0643\u0644 / \u0627\u062E\u062A\u064A\u0627\u0631\u064A \u2014");
        \u0275\u0275elementEnd();
        \u0275\u0275repeaterCreate(25, SmartStudyAssistantComponent_For_26_Template, 2, 2, "option", 8, _forTrack0);
        \u0275\u0275elementEnd();
        \u0275\u0275controlCreate();
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(27, "label")(28, "span");
        \u0275\u0275text(29, "\u0627\u0644\u062F\u0631\u0633");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(30, "select", 9);
        \u0275\u0275twoWayListener("ngModelChange", function SmartStudyAssistantComponent_Template_select_ngModelChange_30_listener($event) {
          \u0275\u0275restoreView(_r1);
          \u0275\u0275twoWayBindingSet(ctx.selectedLessonId, $event) || (ctx.selectedLessonId = $event);
          return \u0275\u0275resetView($event);
        });
        \u0275\u0275listener("ngModelChange", function SmartStudyAssistantComponent_Template_select_ngModelChange_30_listener($event) {
          return ctx.onLessonModelChange($event);
        });
        \u0275\u0275elementStart(31, "option", 7);
        \u0275\u0275text(32, "\u2014 \u0627\u0644\u0643\u0644 / \u0627\u062E\u062A\u064A\u0627\u0631\u064A \u2014");
        \u0275\u0275elementEnd();
        \u0275\u0275repeaterCreate(33, SmartStudyAssistantComponent_For_34_Template, 2, 2, "option", 8, _forTrack0);
        \u0275\u0275elementEnd();
        \u0275\u0275controlCreate();
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(35, "label")(36, "span");
        \u0275\u0275text(37, "\u0639\u062F\u062F \u0627\u0644\u0623\u0633\u0626\u0644\u0629 (\u0644\u0644\u0648\u0627\u062C\u0628 \u0648\u0627\u0644\u0643\u0648\u064A\u0632)");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(38, "input", 10);
        \u0275\u0275twoWayListener("ngModelChange", function SmartStudyAssistantComponent_Template_input_ngModelChange_38_listener($event) {
          \u0275\u0275restoreView(_r1);
          \u0275\u0275twoWayBindingSet(ctx.questionCount, $event) || (ctx.questionCount = $event);
          return \u0275\u0275resetView($event);
        });
        \u0275\u0275elementEnd();
        \u0275\u0275controlCreate();
        \u0275\u0275elementEnd()()();
        \u0275\u0275elementStart(39, "div", 4)(40, "h2");
        \u0275\u0275text(41, "2\uFE0F\u20E3 \u0627\u0643\u062A\u0628 \u0648\u0635\u0641\u0627\u064B \u0646\u0635\u064A\u0627\u064B (\u0628\u062F\u064A\u0644 \u0639\u0646 \u0631\u0641\u0639 \u0627\u0644\u0645\u0644\u0641\u0627\u062A)");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(42, "label", 11)(43, "textarea", 12);
        \u0275\u0275twoWayListener("ngModelChange", function SmartStudyAssistantComponent_Template_textarea_ngModelChange_43_listener($event) {
          \u0275\u0275restoreView(_r1);
          \u0275\u0275twoWayBindingSet(ctx.prompt, $event) || (ctx.prompt = $event);
          return \u0275\u0275resetView($event);
        });
        \u0275\u0275elementEnd();
        \u0275\u0275controlCreate();
        \u0275\u0275elementStart(44, "small");
        \u0275\u0275text(45, "\u064A\u0645\u0643\u0646\u0643 \u0627\u0644\u0627\u0643\u062A\u0641\u0627\u0621 \u0628\u0643\u062A\u0627\u0628\u0629 \u0627\u0644\u0645\u0637\u0644\u0648\u0628 \u0646\u0635\u064A\u0627\u064B \u0628\u062F\u0644\u0627\u064B \u0645\u0646 \u0631\u0641\u0639 \u0635\u0648\u0631 \u0623\u0648 \u0645\u0644\u0641\u0627\u062A PDF \u2014 \u0623\u0648 \u0627\u0644\u062C\u0645\u0639 \u0628\u064A\u0646 \u0627\u0644\u0627\u062B\u0646\u064A\u0646.");
        \u0275\u0275elementEnd()()();
        \u0275\u0275elementStart(46, "div", 4)(47, "h2");
        \u0275\u0275text(48, "3\uFE0F\u20E3 \u0631\u0641\u0639 \u0627\u0644\u0645\u0644\u0641\u0627\u062A (\u0627\u062E\u062A\u064A\u0627\u0631\u064A \u2014 \u062D\u062A\u0649 10 \u0645\u0644\u0641\u0627\u062A)");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(49, "div", 13);
        \u0275\u0275listener("drop", function SmartStudyAssistantComponent_Template_div_drop_49_listener($event) {
          return ctx.onDrop($event);
        })("dragover", function SmartStudyAssistantComponent_Template_div_dragover_49_listener($event) {
          return ctx.onDragOver($event);
        })("click", function SmartStudyAssistantComponent_Template_div_click_49_listener() {
          return ctx.openFilePicker();
        });
        \u0275\u0275elementStart(50, "input", 14, 0);
        \u0275\u0275listener("change", function SmartStudyAssistantComponent_Template_input_change_50_listener($event) {
          return ctx.onFilesChosen($event);
        });
        \u0275\u0275elementEnd();
        \u0275\u0275conditionalCreate(52, SmartStudyAssistantComponent_Conditional_52_Template, 7, 0)(53, SmartStudyAssistantComponent_Conditional_53_Template, 9, 0, "div", 15);
        \u0275\u0275elementEnd()();
        \u0275\u0275elementStart(54, "div", 4)(55, "h2");
        \u0275\u0275text(56, "4\uFE0F\u20E3 \u0627\u062E\u062A\u0631 \u0627\u0644\u0625\u062C\u0631\u0627\u0621");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(57, "div", 16);
        \u0275\u0275repeaterCreate(58, SmartStudyAssistantComponent_For_59_Template, 8, 5, "button", 17, _forTrack1);
        \u0275\u0275elementEnd();
        \u0275\u0275conditionalCreate(60, SmartStudyAssistantComponent_Conditional_60_Template, 2, 0, "p", 18);
        \u0275\u0275elementEnd();
        \u0275\u0275conditionalCreate(61, SmartStudyAssistantComponent_Conditional_61_Template, 13, 4, "div", 19);
        \u0275\u0275elementEnd();
      }
      if (rf & 2) {
        \u0275\u0275advance(6);
        \u0275\u0275property("ok", ctx.message())("error", ctx.error());
        \u0275\u0275advance(8);
        \u0275\u0275twoWayProperty("ngModel", ctx.selectedCourseId);
        \u0275\u0275control();
        \u0275\u0275advance(3);
        \u0275\u0275repeater(ctx.courses());
        \u0275\u0275advance(5);
        \u0275\u0275twoWayProperty("ngModel", ctx.selectedUnitId);
        \u0275\u0275property("disabled", !ctx.courseId());
        \u0275\u0275control();
        \u0275\u0275advance(3);
        \u0275\u0275repeater(ctx.units());
        \u0275\u0275advance(5);
        \u0275\u0275twoWayProperty("ngModel", ctx.selectedLessonId);
        \u0275\u0275property("disabled", !ctx.unitId());
        \u0275\u0275control();
        \u0275\u0275advance(3);
        \u0275\u0275repeater(ctx.lessons());
        \u0275\u0275advance(5);
        \u0275\u0275twoWayProperty("ngModel", ctx.questionCount);
        \u0275\u0275control();
        \u0275\u0275advance(5);
        \u0275\u0275twoWayProperty("ngModel", ctx.prompt);
        \u0275\u0275control();
        \u0275\u0275advance(9);
        \u0275\u0275conditional(ctx.files().length > 0 ? 52 : 53);
        \u0275\u0275advance(6);
        \u0275\u0275repeater(ctx.actions);
        \u0275\u0275advance(2);
        \u0275\u0275conditional(ctx.loading() ? 60 : -1);
        \u0275\u0275advance();
        \u0275\u0275conditional(ctx.resultMarkdown() ? 61 : -1);
      }
    }, dependencies: [FormsModule, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, NumberValueAccessor, SelectControlValueAccessor, NgControlStatus, MinValidator, MaxValidator, NgModel, PageFeedbackComponent, SafeHtmlPipe], styles: ["\n.ssa-page[_ngcontent-%COMP%] {\n  max-width: 960px;\n  margin: 0 auto;\n  display: grid;\n  gap: 18px;\n  padding-bottom: 48px;\n}\n.ssa-header[_ngcontent-%COMP%]   h1[_ngcontent-%COMP%] {\n  margin: 0 0 6px;\n  font-size: 1.5rem;\n}\n.ssa-header[_ngcontent-%COMP%]   p[_ngcontent-%COMP%] {\n  margin: 0;\n  color: var(--text-muted, #5b6b7c);\n  font-size: 0.95rem;\n}\n.ssa-card[_ngcontent-%COMP%] {\n  background: var(--surface, #fff);\n  border: 1px solid var(--border, #dbe4ec);\n  border-radius: 14px;\n  padding: 18px 20px;\n  box-shadow: 0 1px 3px rgb(16 24 40 / 4%);\n}\n.ssa-card[_ngcontent-%COMP%]   h2[_ngcontent-%COMP%] {\n  margin: 0 0 14px;\n  font-size: 1.05rem;\n  color: var(--heading, #14344b);\n}\n.ssa-grid[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));\n  gap: 12px;\n}\n.ssa-grid[_ngcontent-%COMP%]   label[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 6px;\n  font-size: 0.85rem;\n  color: var(--text-muted, #5b6b7c);\n}\n.ssa-grid[_ngcontent-%COMP%]   select[_ngcontent-%COMP%], \n.ssa-grid[_ngcontent-%COMP%]   input[_ngcontent-%COMP%] {\n  padding: 9px 12px;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 9px;\n  background: var(--input-bg, #fff);\n  font: inherit;\n  color: var(--text, #1c2434);\n}\n.ssa-grid[_ngcontent-%COMP%]   select[_ngcontent-%COMP%]   option[_ngcontent-%COMP%] {\n  background: var(--bg-elevated, #fff);\n  color: var(--text, #1c2434);\n}\n.ssa-dropzone[_ngcontent-%COMP%] {\n  border: 2px dashed var(--border, #b8c8d8);\n  border-radius: 12px;\n  padding: 26px 16px;\n  text-align: center;\n  cursor: pointer;\n  transition: border-color 0.15s ease, background 0.15s ease;\n}\n.ssa-dropzone[_ngcontent-%COMP%]:hover {\n  border-color: var(--accent, #3d84c6);\n  background: rgb(61 132 198 / 4%);\n}\n.ssa-dropzone-icon[_ngcontent-%COMP%] {\n  font-size: 1.8rem;\n}\n.ssa-dropzone-hint[_ngcontent-%COMP%]   p[_ngcontent-%COMP%] {\n  margin: 6px 0 2px;\n}\n.ssa-dropzone-hint[_ngcontent-%COMP%]   small[_ngcontent-%COMP%] {\n  color: var(--text-muted, #7a8a9a);\n}\n.ssa-files[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 10px;\n  width: 100%;\n  text-align: start;\n}\n.ssa-addmore[_ngcontent-%COMP%] {\n  display: block;\n  margin-top: 10px;\n  color: var(--text-muted, #7a8a9a);\n}\n.ssa-apply[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 8px;\n  margin-top: 12px;\n  padding: 10px 12px;\n  background: rgb(61 132 198 / 6%);\n  border-radius: 10px;\n  font-size: 0.9rem;\n}\n.ssa-apply[_ngcontent-%COMP%]   span[_ngcontent-%COMP%] {\n  font-weight: 600;\n}\n.ssa-file[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 14px;\n  text-align: start;\n}\n.ssa-file[_ngcontent-%COMP%]   img[_ngcontent-%COMP%] {\n  width: 72px;\n  height: 72px;\n  object-fit: cover;\n  border-radius: 10px;\n  border: 1px solid var(--border, #dbe4ec);\n}\n.ssa-file-icon[_ngcontent-%COMP%] {\n  font-size: 2.2rem;\n}\n.ssa-file[_ngcontent-%COMP%]   div[_ngcontent-%COMP%] {\n  flex: 1;\n  display: grid;\n}\n.ssa-file[_ngcontent-%COMP%]   small[_ngcontent-%COMP%] {\n  color: var(--text-muted, #7a8a9a);\n}\n.ssa-remove[_ngcontent-%COMP%] {\n  border: 1px solid var(--border, #dbe4ec);\n  background: transparent;\n  color: #b3413c;\n  border-radius: 8px;\n  padding: 6px 12px;\n  cursor: pointer;\n  font: inherit;\n}\n.ssa-remove[_ngcontent-%COMP%]:hover {\n  background: #fdecea;\n}\n.ssa-actions[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));\n  gap: 12px;\n}\n.ssa-action[_ngcontent-%COMP%] {\n  position: relative;\n  display: grid;\n  gap: 4px;\n  justify-items: start;\n  padding: 14px 16px;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 12px;\n  background: var(--surface, #fff);\n  cursor: pointer;\n  text-align: start;\n  font: inherit;\n  transition:\n    border-color 0.15s ease,\n    transform 0.1s ease,\n    box-shadow 0.15s ease;\n}\n.ssa-action[_ngcontent-%COMP%]:hover:not(:disabled) {\n  border-color: var(--accent, #3d84c6);\n  box-shadow: 0 4px 12px rgb(61 132 198 / 15%);\n  transform: translateY(-1px);\n}\n.ssa-action[_ngcontent-%COMP%]:disabled {\n  opacity: 0.55;\n  cursor: wait;\n}\n.ssa-action-icon[_ngcontent-%COMP%] {\n  font-size: 1.4rem;\n}\n.ssa-action[_ngcontent-%COMP%]   small[_ngcontent-%COMP%] {\n  color: var(--text-muted, #7a8a9a);\n  font-size: 0.78rem;\n  line-height: 1.5;\n}\n.ssa-spinner[_ngcontent-%COMP%] {\n  position: absolute;\n  inset-inline-end: 14px;\n  top: 14px;\n  width: 16px;\n  height: 16px;\n  border: 2px solid var(--border, #c6d2dd);\n  border-top-color: var(--accent, #3d84c6);\n  border-radius: 50%;\n  animation: _ngcontent-%COMP%_ssa-spin 0.8s linear infinite;\n}\n@keyframes _ngcontent-%COMP%_ssa-spin {\n  to {\n    transform: rotate(360deg);\n  }\n}\n.ssa-loading[_ngcontent-%COMP%] {\n  margin: 14px 0 0;\n  color: var(--accent, #2f6da8);\n  font-size: 0.9rem;\n}\n.ssa-result-head[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  justify-content: space-between;\n  gap: 10px;\n}\n.ssa-result-head[_ngcontent-%COMP%]   h2[_ngcontent-%COMP%] {\n  margin: 0;\n}\n.ssa-result-tools[_ngcontent-%COMP%] {\n  display: flex;\n  gap: 8px;\n}\n.ssa-tool[_ngcontent-%COMP%] {\n  border: 1px solid var(--border, #c6d2dd);\n  background: var(--surface, #fff);\n  border-radius: 9px;\n  padding: 7px 14px;\n  font: inherit;\n  cursor: pointer;\n}\n.ssa-tool[_ngcontent-%COMP%]:hover {\n  border-color: var(--accent, #3d84c6);\n}\n.ssa-tool.primary[_ngcontent-%COMP%] {\n  background: var(--accent, #2f6da8);\n  border-color: var(--accent, #2f6da8);\n  color: #fff;\n}\n.ssa-markdown[_ngcontent-%COMP%] {\n  margin-top: 12px;\n  line-height: 1.9;\n  overflow-x: auto;\n}\n.ssa-markdown[_ngcontent-%COMP%]   h1[_ngcontent-%COMP%], \n.ssa-markdown[_ngcontent-%COMP%]   h2[_ngcontent-%COMP%], \n.ssa-markdown[_ngcontent-%COMP%]   h3[_ngcontent-%COMP%] {\n  color: var(--heading, #14344b);\n  margin: 1.2em 0 0.4em;\n}\n.ssa-markdown[_ngcontent-%COMP%]   h1[_ngcontent-%COMP%]:first-child, \n.ssa-markdown[_ngcontent-%COMP%]   h2[_ngcontent-%COMP%]:first-child, \n.ssa-markdown[_ngcontent-%COMP%]   h3[_ngcontent-%COMP%]:first-child {\n  margin-top: 0;\n}\n.ssa-markdown[_ngcontent-%COMP%]   ul[_ngcontent-%COMP%], \n.ssa-markdown[_ngcontent-%COMP%]   ol[_ngcontent-%COMP%] {\n  padding-inline-start: 24px;\n}\n.ssa-markdown[_ngcontent-%COMP%]   table[_ngcontent-%COMP%] {\n  border-collapse: collapse;\n  width: 100%;\n  margin: 12px 0;\n}\n.ssa-markdown[_ngcontent-%COMP%]   th[_ngcontent-%COMP%], \n.ssa-markdown[_ngcontent-%COMP%]   td[_ngcontent-%COMP%] {\n  border: 1px solid var(--border, #c6d2dd);\n  padding: 6px 10px;\n  text-align: start;\n}\n.ssa-markdown[_ngcontent-%COMP%]   th[_ngcontent-%COMP%] {\n  background: rgb(61 132 198 / 8%);\n}\n.ssa-markdown[_ngcontent-%COMP%]   pre[_ngcontent-%COMP%] {\n  background: var(--surface-muted, #f4f7fa);\n  padding: 12px;\n  border-radius: 8px;\n  white-space: pre-wrap;\n}\n.ssa-markdown[_ngcontent-%COMP%]   blockquote[_ngcontent-%COMP%] {\n  border-inline-start: 4px solid var(--accent, #7aa7c7);\n  margin: 8px 0;\n  padding: 4px 14px;\n  color: var(--text-muted, #44566b);\n}\n.ssa-markdown[_ngcontent-%COMP%]   code[_ngcontent-%COMP%] {\n  background: var(--surface-muted, #f0f4f8);\n  padding: 1px 5px;\n  border-radius: 4px;\n  font-size: 0.9em;\n}\n.ssa-markdown[_ngcontent-%COMP%]   hr[_ngcontent-%COMP%] {\n  border: none;\n  border-top: 1px solid var(--border, #dbe4ec);\n}\n.ssa-prompt-label[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.4rem;\n}\n.ssa-prompt[_ngcontent-%COMP%] {\n  width: 100%;\n  resize: vertical;\n  min-height: 110px;\n  padding: 0.7rem 0.9rem;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 10px;\n  font: inherit;\n  line-height: 1.7;\n  background: var(--surface, #fff);\n  color: var(--text, #1c2434);\n}\n.ssa-prompt[_ngcontent-%COMP%]:focus {\n  outline: 2px solid #0f766e;\n  outline-offset: 1px;\n}\n.ssa-questions[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 12px;\n  margin-top: 14px;\n}\n.ssa-questions[_ngcontent-%COMP%]   h3[_ngcontent-%COMP%] {\n  margin: 0;\n  font-size: 1rem;\n  color: var(--heading, #14344b);\n}\n.ssa-questions[_ngcontent-%COMP%]   small[_ngcontent-%COMP%] {\n  color: var(--text-muted, #7a8a9a);\n}\n.ssa-question[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 10px;\n  padding: 12px 14px;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 12px;\n}\n.ssa-question-head[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 10px;\n}\n.ssa-question-head[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%] {\n  flex: 1;\n}\n.ssa-points[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 6px;\n  font-size: 0.85rem;\n  color: var(--text-muted, #5b6b7c);\n}\n.ssa-points[_ngcontent-%COMP%]   input[_ngcontent-%COMP%] {\n  width: 70px;\n}\n.ssa-question[_ngcontent-%COMP%]   input[type=text][_ngcontent-%COMP%], \n.ssa-question[_ngcontent-%COMP%]   input[type=number][_ngcontent-%COMP%], \n.ssa-question[_ngcontent-%COMP%]   textarea[_ngcontent-%COMP%] {\n  padding: 8px 11px;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 9px;\n  background: var(--input-bg, #fff);\n  font: inherit;\n  color: var(--text, #1c2434);\n}\n.ssa-question[_ngcontent-%COMP%]   textarea[_ngcontent-%COMP%] {\n  width: 100%;\n  resize: vertical;\n  line-height: 1.7;\n}\n.ssa-options[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 8px;\n}\n.ssa-option[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 8px;\n}\n.ssa-option[_ngcontent-%COMP%]   input[type=text][_ngcontent-%COMP%] {\n  flex: 1;\n  min-width: 0;\n}\n.ssa-answer[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 6px;\n  font-size: 0.85rem;\n  color: var(--text-muted, #5b6b7c);\n}\n.ssa-original[_ngcontent-%COMP%] {\n  margin-top: 14px;\n}\n.ssa-original[_ngcontent-%COMP%]   summary[_ngcontent-%COMP%] {\n  cursor: pointer;\n  color: var(--text-muted, #5b6b7c);\n}\n/*# sourceMappingURL=smart-study-assistant.component.css.map */"] });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(SmartStudyAssistantComponent, [{
    type: Component,
    args: [{ selector: "app-smart-study-assistant", imports: [FormsModule, SafeHtmlPipe, PageFeedbackComponent], template: `<section class="ssa-page">\r
  <header class="ssa-header">\r
    <h1>\u{1F916} \u0627\u0644\u0645\u0633\u0627\u0639\u062F \u0627\u0644\u0630\u0643\u064A \u0644\u0644\u062F\u0631\u0627\u0633\u0629</h1>\r
    <p>\u0627\u0631\u0641\u0639 \u0635\u0648\u0631\u0627\u064B \u0623\u0648 \u0645\u0644\u0641\u0627\u062A PDF\u060C \u0627\u062E\u062A\u0631 \u0627\u0644\u0643\u0648\u0631\u0633 \u0648\u0627\u0644\u0648\u062D\u062F\u0629 \u0648\u0627\u0644\u062F\u0631\u0633\u060C \u062B\u0645 \u0627\u062E\u062A\u0631 \u0627\u0644\u0625\u062C\u0631\u0627\u0621 \u2014 \u0648\u0633\u064A\u062D\u0644\u0644 Gemini \u0627\u0644\u0645\u062D\u062A\u0648\u0649 \u0648\u064A\u0646\u0634\u0626 \u0627\u0644\u0646\u062A\u064A\u062C\u0629\u060C \u0648\u064A\u0645\u0643\u0646\u0643 \u0625\u0636\u0627\u0641\u062A\u0647\u0627 \u0645\u0628\u0627\u0634\u0631\u0629 \u0644\u0644\u0645\u0646\u0647\u062C \u0623\u0648 \u0627\u0644\u0643\u0648\u064A\u0632\u0627\u062A \u0623\u0648 \u0627\u0644\u0648\u0627\u062C\u0628\u0627\u062A.</p>\r
  </header>\r
\r
  <app-page-feedback [ok]="message()" [error]="error()" />\r
\r
  <div class="ssa-card">\r
    <h2>1\uFE0F\u20E3 \u062A\u062D\u062F\u064A\u062F \u0627\u0644\u0633\u064A\u0627\u0642</h2>\r
    <div class="ssa-grid">\r
      <label>\r
        <span>\u0627\u0644\u0643\u0648\u0631\u0633</span>\r
        <select [(ngModel)]="selectedCourseId" (ngModelChange)="onCourseModelChange($event)">\r
          <option value="">\u2014 \u0627\u062E\u062A\u0631 \u0627\u0644\u0643\u0648\u0631\u0633 \u2014</option>\r
          @for (course of courses(); track course.id) {\r
            <option [value]="course.id">{{ course.title }}</option>\r
          }\r
        </select>\r
      </label>\r
\r
      <label>\r
        <span>\u0627\u0644\u0648\u062D\u062F\u0629</span>\r
        <select [(ngModel)]="selectedUnitId" (ngModelChange)="onUnitModelChange($event)" [disabled]="!courseId()">\r
          <option value="">\u2014 \u0627\u0644\u0643\u0644 / \u0627\u062E\u062A\u064A\u0627\u0631\u064A \u2014</option>\r
          @for (unit of units(); track unit.id) {\r
            <option [value]="unit.id">{{ unit.title }}</option>\r
          }\r
        </select>\r
      </label>\r
\r
      <label>\r
        <span>\u0627\u0644\u062F\u0631\u0633</span>\r
        <select [(ngModel)]="selectedLessonId" (ngModelChange)="onLessonModelChange($event)" [disabled]="!unitId()">\r
          <option value="">\u2014 \u0627\u0644\u0643\u0644 / \u0627\u062E\u062A\u064A\u0627\u0631\u064A \u2014</option>\r
          @for (lesson of lessons(); track lesson.id) {\r
            <option [value]="lesson.id">{{ lesson.title }}</option>\r
          }\r
        </select>\r
      </label>\r
\r
      <label>\r
        <span>\u0639\u062F\u062F \u0627\u0644\u0623\u0633\u0626\u0644\u0629 (\u0644\u0644\u0648\u0627\u062C\u0628 \u0648\u0627\u0644\u0643\u0648\u064A\u0632)</span>\r
        <input type="number" min="1" max="50" step="1" [(ngModel)]="questionCount">\r
      </label>\r
    </div>\r
  </div>\r
\r
  <div class="ssa-card">\r
    <h2>2\uFE0F\u20E3 \u0627\u0643\u062A\u0628 \u0648\u0635\u0641\u0627\u064B \u0646\u0635\u064A\u0627\u064B (\u0628\u062F\u064A\u0644 \u0639\u0646 \u0631\u0641\u0639 \u0627\u0644\u0645\u0644\u0641\u0627\u062A)</h2>\r
    <label class="ssa-prompt-label">\r
      <textarea [(ngModel)]="prompt" rows="5" class="ssa-prompt"\r
                placeholder="\u0645\u062B\u0627\u0644: \u0627\u0643\u062A\u0628 \u0644\u064A \u0645\u0644\u062E\u0635\u0627\u064B \u0639\u0646 \u062F\u0631\u0633 \u0627\u0644\u0643\u0633\u0648\u0631 \u0627\u0644\u0639\u0634\u0631\u064A\u0629 \u0644\u0644\u0635\u0641 \u0627\u0644\u062E\u0627\u0645\u0633\u060C \u064A\u0634\u0631\u062D \u062A\u062D\u0648\u064A\u0644 \u0627\u0644\u0643\u0633\u0648\u0631 \u0625\u0644\u0649 \u0643\u0633\u0648\u0631 \u0639\u0634\u0631\u064A\u0629 \u0645\u0639 \u0623\u0645\u062B\u0644\u0629 \u0645\u062D\u0644\u0648\u0644\u0629\u060C \u0648\u0623\u0646\u0634\u0626 \u0643\u0648\u064A\u0632\u064B\u0627 \u0645\u0646 5 \u0623\u0633\u0626\u0644\u0629 \u0639\u0646 \u0630\u0644\u0643."></textarea>\r
      <small>\u064A\u0645\u0643\u0646\u0643 \u0627\u0644\u0627\u0643\u062A\u0641\u0627\u0621 \u0628\u0643\u062A\u0627\u0628\u0629 \u0627\u0644\u0645\u0637\u0644\u0648\u0628 \u0646\u0635\u064A\u0627\u064B \u0628\u062F\u0644\u0627\u064B \u0645\u0646 \u0631\u0641\u0639 \u0635\u0648\u0631 \u0623\u0648 \u0645\u0644\u0641\u0627\u062A PDF \u2014 \u0623\u0648 \u0627\u0644\u062C\u0645\u0639 \u0628\u064A\u0646 \u0627\u0644\u0627\u062B\u0646\u064A\u0646.</small>\r
    </label>\r
  </div>\r
\r
  <div class="ssa-card">\r
    <h2>3\uFE0F\u20E3 \u0631\u0641\u0639 \u0627\u0644\u0645\u0644\u0641\u0627\u062A (\u0627\u062E\u062A\u064A\u0627\u0631\u064A \u2014 \u062D\u062A\u0649 10 \u0645\u0644\u0641\u0627\u062A)</h2>\r
    <div class="ssa-dropzone"\r
         (drop)="onDrop($event)" (dragover)="onDragOver($event)"\r
         (click)="openFilePicker()">\r
      <input #fileInput type="file" accept=".pdf,.jpg,.jpeg,.png" multiple (change)="onFilesChosen($event)" hidden>\r
      @if (files().length > 0) {\r
        <div class="ssa-files">\r
          @for (entry of files(); track $index) {\r
            <div class="ssa-file">\r
              @if (entry.preview) {\r
                <img [src]="entry.preview" alt="\u0645\u0639\u0627\u064A\u0646\u0629">\r
              } @else {\r
                <span class="ssa-file-icon">\u{1F4C4}</span>\r
              }\r
              <div>\r
                <strong>{{ entry.file.name }}</strong>\r
                <small>{{ (entry.file.size / 1024 / 1024).toFixed(2) }} \u0645\u064A\u062C\u0627\u0628\u0627\u064A\u062A</small>\r
              </div>\r
              <button type="button" class="ssa-remove" (click)="removeFile($index); $event.stopPropagation()">\u2715</button>\r
            </div>\r
          }\r
          <button type="button" class="ssa-tool" (click)="clearFiles(); $event.stopPropagation()">\u{1F5D1}\uFE0F \u0645\u0633\u062D \u0627\u0644\u0643\u0644</button>\r
        </div>\r
        <small class="ssa-addmore">\u0627\u0646\u0642\u0631 \u0623\u0648 \u0623\u0641\u0644\u062A \u0644\u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u0645\u0632\u064A\u062F \u0645\u0646 \u0627\u0644\u0645\u0644\u0641\u0627\u062A</small>\r
      } @else {\r
        <div class="ssa-dropzone-hint">\r
          <span class="ssa-dropzone-icon">\u2B06\uFE0F</span>\r
          <p><strong>\u0627\u0633\u062D\u0628 \u0648\u0623\u0641\u0644\u062A</strong> \u0635\u0648\u0631 \u0623\u0648 \u0645\u0644\u0641\u0627\u062A PDF \u0647\u0646\u0627</p>\r
          <small>\u0623\u0648 \u0627\u0646\u0642\u0631 \u0644\u0644\u0627\u062E\u062A\u064A\u0627\u0631 \u2014 JPG / PNG / PDF \u062D\u062A\u0649 25 \u0645\u064A\u062C\u0627\u0628\u0627\u064A\u062A \u0644\u0643\u0644 \u0645\u0644\u0641</small>\r
        </div>\r
      }\r
    </div>\r
  </div>\r
\r
  <div class="ssa-card">\r
    <h2>4\uFE0F\u20E3 \u0627\u062E\u062A\u0631 \u0627\u0644\u0625\u062C\u0631\u0627\u0621</h2>\r
    <div class="ssa-actions">\r
      @for (item of actions; track item.key) {\r
        <button type="button"\r
                class="ssa-action"\r
                [disabled]="loading()"\r
                (click)="runAction(item.key)">\r
          <span class="ssa-action-icon">{{ item.icon }}</span>\r
          <strong>{{ item.label }}</strong>\r
          <small>{{ item.hint }}</small>\r
          @if (loading() && activeAction() === item.key) {\r
            <span class="ssa-spinner" aria-hidden="true"></span>\r
          }\r
        </button>\r
      }\r
    </div>\r
    @if (loading()) {\r
      <p class="ssa-loading">\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u062A\u062D\u0644\u064A\u0644 \u0628\u0648\u0627\u0633\u0637\u0629 Gemini... \u0642\u062F \u064A\u0633\u062A\u063A\u0631\u0642 \u0630\u0644\u0643 \u0644\u062D\u0638\u0627\u062A\u060C \u0644\u0627 \u062A\u063A\u0644\u0642 \u0627\u0644\u0635\u0641\u062D\u0629.</p>\r
    }\r
  </div>\r
\r
  @if (resultMarkdown()) {\r
    <div class="ssa-card ssa-result">\r
      <div class="ssa-result-head">\r
        <h2>{{ resultTitle() || '\u0627\u0644\u0646\u062A\u064A\u062C\u0629' }}</h2>\r
        <div class="ssa-result-tools">\r
          <button type="button" class="ssa-tool" (click)="copyResult()">\u{1F4CB} \u0646\u0633\u062E \u0627\u0644\u0646\u0635</button>\r
          <button type="button" class="ssa-tool primary" (click)="downloadPdf()">\u2B07\uFE0F \u062A\u062D\u0645\u064A\u0644 \u0643\u0640 PDF</button>\r
        </div>\r
      </div>\r
\r
      @if (hasQuestions()) {\r
        <div class="ssa-questions">\r
          <h3>\u0631\u0627\u062C\u0639 \u0627\u0644\u0623\u0633\u0626\u0644\u0629 \u0642\u0628\u0644 \u0627\u0644\u062D\u0641\u0638 ({{ questions().length }})</h3>\r
          <small>\u064A\u0645\u0643\u0646\u0643 \u062A\u0639\u062F\u064A\u0644 \u0623\u064A \u0633\u0624\u0627\u0644 \u0623\u0648 \u062D\u0630\u0641\u0647 \u2014 \u0645\u0627 \u064A\u0638\u0647\u0631 \u0647\u0646\u0627 \u0647\u0648 \u0645\u0627 \u0633\u064A\u062A\u0645 \u062D\u0641\u0638\u0647.</small>\r
          @for (question of questions(); track $index; let qi = $index) {\r
            <div class="ssa-question">\r
              <div class="ssa-question-head">\r
                <strong>\u0627\u0644\u0633\u0624\u0627\u0644 {{ qi + 1 }}</strong>\r
                <label class="ssa-points">\r
                  <span>\u0627\u0644\u062F\u0631\u062C\u0629</span>\r
                  <input type="number" min="1" step="1"\r
                         [ngModel]="question.points" (ngModelChange)="updatePoints(qi, $event)">\r
                </label>\r
                <button type="button" class="ssa-remove" (click)="removeQuestion(qi)">\u{1F5D1}\uFE0F \u062D\u0630\u0641</button>\r
              </div>\r
              <textarea rows="2" class="ssa-question-text"\r
                        [ngModel]="question.prompt" (ngModelChange)="updateQuestion(qi, { prompt: $event })"></textarea>\r
\r
              @if (question.options.length > 0) {\r
                <div class="ssa-options">\r
                  @for (option of question.options; track $index; let oi = $index) {\r
                    <label class="ssa-option">\r
                      <input type="radio" [name]="'ssa-correct-' + qi" title="\u0627\u0644\u0625\u062C\u0627\u0628\u0629 \u0627\u0644\u0635\u062D\u064A\u062D\u0629"\r
                             [checked]="isCorrectOption(question, oi)" (change)="setCorrectOption(qi, oi)">\r
                      <span>{{ optionLetter(oi) }}</span>\r
                      <input type="text" [ngModel]="option" (ngModelChange)="updateOption(qi, oi, $event)">\r
                    </label>\r
                  }\r
                  <small>\u062D\u062F\u062F \u0627\u0644\u062F\u0627\u0626\u0631\u0629 \u0628\u062C\u0627\u0646\u0628 \u0627\u0644\u0625\u062C\u0627\u0628\u0629 \u0627\u0644\u0635\u062D\u064A\u062D\u0629.</small>\r
                </div>\r
              } @else {\r
                <label class="ssa-answer">\r
                  <span>\u0627\u0644\u0625\u062C\u0627\u0628\u0629 \u0627\u0644\u0646\u0645\u0648\u0630\u062C\u064A\u0629</span>\r
                  <textarea rows="2"\r
                            [ngModel]="question.correctAnswer" (ngModelChange)="updateQuestion(qi, { correctAnswer: $event })"></textarea>\r
                </label>\r
              }\r
            </div>\r
          }\r
        </div>\r
      }\r
\r
      @if (hasQuestions() || hasUnits()) {\r
        <div class="ssa-apply">\r
          <span>\u0623\u0636\u0641 \u0627\u0644\u0646\u062A\u064A\u062C\u0629 \u0625\u0644\u0649:</span>\r
          @if (hasUnits()) {\r
            <button type="button" class="ssa-tool"\r
                    [disabled]="applying() !== null"\r
                    (click)="applyTo('tree')">\r
              {{ applying() === 'tree' ? '\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u0625\u0636\u0627\u0641\u0629...' : '\u{1F333} \u0641\u0647\u0631\u0633 \u0627\u0644\u0645\u0627\u062F\u0629 (\u0648\u062D\u062F\u0627\u062A \u0648\u062F\u0631\u0648\u0633)' }}\r
            </button>\r
          }\r
          @if (hasQuestions()) {\r
            <button type="button" class="ssa-tool"\r
                    [disabled]="applying() !== null"\r
                    (click)="applyTo('bank')">\r
              {{ applying() === 'bank' ? '\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u062D\u0641\u0638...' : '\u{1F5C3}\uFE0F \u0628\u0646\u0643 \u0627\u0644\u0623\u0633\u0626\u0644\u0629' }}\r
            </button>\r
            <button type="button" class="ssa-tool"\r
                    [disabled]="applying() !== null"\r
                    (click)="applyTo('quiz')">\r
              {{ applying() === 'quiz' ? '\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u0625\u0646\u0634\u0627\u0621...' : '\u2753 \u0643\u0648\u064A\u0632 \u062C\u062F\u064A\u062F' }}\r
            </button>\r
            <button type="button" class="ssa-tool"\r
                    [disabled]="applying() !== null"\r
                    (click)="applyTo('assignment')">\r
              {{ applying() === 'assignment' ? '\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u0625\u0646\u0634\u0627\u0621...' : '\u{1F4DA} \u0648\u0627\u062C\u0628 \u062C\u062F\u064A\u062F' }}\r
            </button>\r
            <button type="button" class="ssa-tool"\r
                    [disabled]="applying() !== null"\r
                    (click)="applyTo('exam')">\r
              {{ applying() === 'exam' ? '\u23F3 \u062C\u0627\u0631\u064D \u0627\u0644\u062D\u0641\u0638...' : '\u{1F4DD} \u0623\u0633\u0626\u0644\u0629 \u0627\u062E\u062A\u0628\u0627\u0631' }}\r
            </button>\r
          }\r
        </div>\r
      }\r
\r
      @if (hasQuestions()) {\r
        <details class="ssa-original">\r
          <summary>\u0639\u0631\u0636 \u0627\u0644\u0646\u0635 \u0627\u0644\u0623\u0635\u0644\u064A \u0643\u0645\u0627 \u0623\u0646\u0634\u0623\u0647 \u0627\u0644\u0645\u0633\u0627\u0639\u062F (\u0642\u0628\u0644 \u0627\u0644\u062A\u0639\u062F\u064A\u0644)</summary>\r
          <article class="ssa-markdown" [innerHTML]="renderedHtml() | safeHtml"></article>\r
        </details>\r
      } @else {\r
        <article class="ssa-markdown" [innerHTML]="renderedHtml() | safeHtml"></article>\r
      }\r
    </div>\r
  }\r
</section>\r
`, styles: ["/* src/app/pages/teacher/smart-study-assistant/smart-study-assistant.component.css */\n.ssa-page {\n  max-width: 960px;\n  margin: 0 auto;\n  display: grid;\n  gap: 18px;\n  padding-bottom: 48px;\n}\n.ssa-header h1 {\n  margin: 0 0 6px;\n  font-size: 1.5rem;\n}\n.ssa-header p {\n  margin: 0;\n  color: var(--text-muted, #5b6b7c);\n  font-size: 0.95rem;\n}\n.ssa-card {\n  background: var(--surface, #fff);\n  border: 1px solid var(--border, #dbe4ec);\n  border-radius: 14px;\n  padding: 18px 20px;\n  box-shadow: 0 1px 3px rgb(16 24 40 / 4%);\n}\n.ssa-card h2 {\n  margin: 0 0 14px;\n  font-size: 1.05rem;\n  color: var(--heading, #14344b);\n}\n.ssa-grid {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));\n  gap: 12px;\n}\n.ssa-grid label {\n  display: grid;\n  gap: 6px;\n  font-size: 0.85rem;\n  color: var(--text-muted, #5b6b7c);\n}\n.ssa-grid select,\n.ssa-grid input {\n  padding: 9px 12px;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 9px;\n  background: var(--input-bg, #fff);\n  font: inherit;\n  color: var(--text, #1c2434);\n}\n.ssa-grid select option {\n  background: var(--bg-elevated, #fff);\n  color: var(--text, #1c2434);\n}\n.ssa-dropzone {\n  border: 2px dashed var(--border, #b8c8d8);\n  border-radius: 12px;\n  padding: 26px 16px;\n  text-align: center;\n  cursor: pointer;\n  transition: border-color 0.15s ease, background 0.15s ease;\n}\n.ssa-dropzone:hover {\n  border-color: var(--accent, #3d84c6);\n  background: rgb(61 132 198 / 4%);\n}\n.ssa-dropzone-icon {\n  font-size: 1.8rem;\n}\n.ssa-dropzone-hint p {\n  margin: 6px 0 2px;\n}\n.ssa-dropzone-hint small {\n  color: var(--text-muted, #7a8a9a);\n}\n.ssa-files {\n  display: grid;\n  gap: 10px;\n  width: 100%;\n  text-align: start;\n}\n.ssa-addmore {\n  display: block;\n  margin-top: 10px;\n  color: var(--text-muted, #7a8a9a);\n}\n.ssa-apply {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 8px;\n  margin-top: 12px;\n  padding: 10px 12px;\n  background: rgb(61 132 198 / 6%);\n  border-radius: 10px;\n  font-size: 0.9rem;\n}\n.ssa-apply span {\n  font-weight: 600;\n}\n.ssa-file {\n  display: flex;\n  align-items: center;\n  gap: 14px;\n  text-align: start;\n}\n.ssa-file img {\n  width: 72px;\n  height: 72px;\n  object-fit: cover;\n  border-radius: 10px;\n  border: 1px solid var(--border, #dbe4ec);\n}\n.ssa-file-icon {\n  font-size: 2.2rem;\n}\n.ssa-file div {\n  flex: 1;\n  display: grid;\n}\n.ssa-file small {\n  color: var(--text-muted, #7a8a9a);\n}\n.ssa-remove {\n  border: 1px solid var(--border, #dbe4ec);\n  background: transparent;\n  color: #b3413c;\n  border-radius: 8px;\n  padding: 6px 12px;\n  cursor: pointer;\n  font: inherit;\n}\n.ssa-remove:hover {\n  background: #fdecea;\n}\n.ssa-actions {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));\n  gap: 12px;\n}\n.ssa-action {\n  position: relative;\n  display: grid;\n  gap: 4px;\n  justify-items: start;\n  padding: 14px 16px;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 12px;\n  background: var(--surface, #fff);\n  cursor: pointer;\n  text-align: start;\n  font: inherit;\n  transition:\n    border-color 0.15s ease,\n    transform 0.1s ease,\n    box-shadow 0.15s ease;\n}\n.ssa-action:hover:not(:disabled) {\n  border-color: var(--accent, #3d84c6);\n  box-shadow: 0 4px 12px rgb(61 132 198 / 15%);\n  transform: translateY(-1px);\n}\n.ssa-action:disabled {\n  opacity: 0.55;\n  cursor: wait;\n}\n.ssa-action-icon {\n  font-size: 1.4rem;\n}\n.ssa-action small {\n  color: var(--text-muted, #7a8a9a);\n  font-size: 0.78rem;\n  line-height: 1.5;\n}\n.ssa-spinner {\n  position: absolute;\n  inset-inline-end: 14px;\n  top: 14px;\n  width: 16px;\n  height: 16px;\n  border: 2px solid var(--border, #c6d2dd);\n  border-top-color: var(--accent, #3d84c6);\n  border-radius: 50%;\n  animation: ssa-spin 0.8s linear infinite;\n}\n@keyframes ssa-spin {\n  to {\n    transform: rotate(360deg);\n  }\n}\n.ssa-loading {\n  margin: 14px 0 0;\n  color: var(--accent, #2f6da8);\n  font-size: 0.9rem;\n}\n.ssa-result-head {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  justify-content: space-between;\n  gap: 10px;\n}\n.ssa-result-head h2 {\n  margin: 0;\n}\n.ssa-result-tools {\n  display: flex;\n  gap: 8px;\n}\n.ssa-tool {\n  border: 1px solid var(--border, #c6d2dd);\n  background: var(--surface, #fff);\n  border-radius: 9px;\n  padding: 7px 14px;\n  font: inherit;\n  cursor: pointer;\n}\n.ssa-tool:hover {\n  border-color: var(--accent, #3d84c6);\n}\n.ssa-tool.primary {\n  background: var(--accent, #2f6da8);\n  border-color: var(--accent, #2f6da8);\n  color: #fff;\n}\n.ssa-markdown {\n  margin-top: 12px;\n  line-height: 1.9;\n  overflow-x: auto;\n}\n.ssa-markdown h1,\n.ssa-markdown h2,\n.ssa-markdown h3 {\n  color: var(--heading, #14344b);\n  margin: 1.2em 0 0.4em;\n}\n.ssa-markdown h1:first-child,\n.ssa-markdown h2:first-child,\n.ssa-markdown h3:first-child {\n  margin-top: 0;\n}\n.ssa-markdown ul,\n.ssa-markdown ol {\n  padding-inline-start: 24px;\n}\n.ssa-markdown table {\n  border-collapse: collapse;\n  width: 100%;\n  margin: 12px 0;\n}\n.ssa-markdown th,\n.ssa-markdown td {\n  border: 1px solid var(--border, #c6d2dd);\n  padding: 6px 10px;\n  text-align: start;\n}\n.ssa-markdown th {\n  background: rgb(61 132 198 / 8%);\n}\n.ssa-markdown pre {\n  background: var(--surface-muted, #f4f7fa);\n  padding: 12px;\n  border-radius: 8px;\n  white-space: pre-wrap;\n}\n.ssa-markdown blockquote {\n  border-inline-start: 4px solid var(--accent, #7aa7c7);\n  margin: 8px 0;\n  padding: 4px 14px;\n  color: var(--text-muted, #44566b);\n}\n.ssa-markdown code {\n  background: var(--surface-muted, #f0f4f8);\n  padding: 1px 5px;\n  border-radius: 4px;\n  font-size: 0.9em;\n}\n.ssa-markdown hr {\n  border: none;\n  border-top: 1px solid var(--border, #dbe4ec);\n}\n.ssa-prompt-label {\n  display: grid;\n  gap: 0.4rem;\n}\n.ssa-prompt {\n  width: 100%;\n  resize: vertical;\n  min-height: 110px;\n  padding: 0.7rem 0.9rem;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 10px;\n  font: inherit;\n  line-height: 1.7;\n  background: var(--surface, #fff);\n  color: var(--text, #1c2434);\n}\n.ssa-prompt:focus {\n  outline: 2px solid #0f766e;\n  outline-offset: 1px;\n}\n.ssa-questions {\n  display: grid;\n  gap: 12px;\n  margin-top: 14px;\n}\n.ssa-questions h3 {\n  margin: 0;\n  font-size: 1rem;\n  color: var(--heading, #14344b);\n}\n.ssa-questions small {\n  color: var(--text-muted, #7a8a9a);\n}\n.ssa-question {\n  display: grid;\n  gap: 10px;\n  padding: 12px 14px;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 12px;\n}\n.ssa-question-head {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 10px;\n}\n.ssa-question-head strong {\n  flex: 1;\n}\n.ssa-points {\n  display: flex;\n  align-items: center;\n  gap: 6px;\n  font-size: 0.85rem;\n  color: var(--text-muted, #5b6b7c);\n}\n.ssa-points input {\n  width: 70px;\n}\n.ssa-question input[type=text],\n.ssa-question input[type=number],\n.ssa-question textarea {\n  padding: 8px 11px;\n  border: 1px solid var(--border, #c6d2dd);\n  border-radius: 9px;\n  background: var(--input-bg, #fff);\n  font: inherit;\n  color: var(--text, #1c2434);\n}\n.ssa-question textarea {\n  width: 100%;\n  resize: vertical;\n  line-height: 1.7;\n}\n.ssa-options {\n  display: grid;\n  gap: 8px;\n}\n.ssa-option {\n  display: flex;\n  align-items: center;\n  gap: 8px;\n}\n.ssa-option input[type=text] {\n  flex: 1;\n  min-width: 0;\n}\n.ssa-answer {\n  display: grid;\n  gap: 6px;\n  font-size: 0.85rem;\n  color: var(--text-muted, #5b6b7c);\n}\n.ssa-original {\n  margin-top: 14px;\n}\n.ssa-original summary {\n  cursor: pointer;\n  color: var(--text-muted, #5b6b7c);\n}\n/*# sourceMappingURL=smart-study-assistant.component.css.map */\n"] }]
  }], () => [], { fileInput: [{ type: ViewChild, args: ["fileInput", { isSignal: true }] }] });
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(SmartStudyAssistantComponent, { className: "SmartStudyAssistantComponent", filePath: "src/app/pages/teacher/smart-study-assistant/smart-study-assistant.component.ts", lineNumber: 29 });
})();
export {
  SmartStudyAssistantComponent
};
//# sourceMappingURL=chunk-DB4KGONL.js.map
