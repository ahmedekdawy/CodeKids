import {
  TranslatePipe
} from "./chunk-MAWF43RP.js";
import {
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  Injector,
  Input,
  NG_VALUE_ACCESSOR,
  ViewChild,
  afterNextRender,
  computed,
  forwardRef,
  inject,
  input,
  setClassMetadata,
  signal,
  viewChild,
  ɵsetClassDebugInfo,
  ɵɵProvidersFeature,
  ɵɵadvance,
  ɵɵattribute,
  ɵɵclassProp,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdomElement,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵdomListener,
  ɵɵdomProperty,
  ɵɵgetCurrentView,
  ɵɵlistener,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind1,
  ɵɵqueryAdvance,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵresetView,
  ɵɵresolveDocument,
  ɵɵresolveWindow,
  ɵɵrestoreView,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵviewQuerySignal
} from "./chunk-EMYLRMBO.js";

// src/app/shared/dropdown-panel.util.ts
function attachDropdownPanelToBody(panel) {
  if (panel && panel.parentElement !== document.body) {
    document.body.appendChild(panel);
  }
}
function isInsideDropdown(target, host, panel) {
  if (!(target instanceof Node))
    return false;
  return host.contains(target) || !!panel?.contains(target);
}
function applyDropdownPanelStyle(panel, trigger, compact = false) {
  const rect = trigger.getBoundingClientRect();
  const origin = fixedContainingOrigin(panel);
  const gap = 6;
  const viewportRight = window.innerWidth - 8;
  const minWidth = compact ? 12 * 16 : rect.width;
  const width = Math.min(Math.max(rect.width, minWidth), window.innerWidth - 16);
  const rtl = getComputedStyle(trigger).direction === "rtl";
  let left = rtl ? rect.right - width : rect.left;
  left = Math.min(Math.max(8, left), viewportRight - width);
  const spaceBelow = window.innerHeight - rect.bottom - gap;
  const spaceAbove = rect.top - gap;
  const openUp = spaceBelow < 10 * 16 && spaceAbove > spaceBelow;
  const maxHeight = Math.max(8 * 16, Math.min(22 * 16, openUp ? spaceAbove : spaceBelow));
  panel.dir = getComputedStyle(trigger).direction;
  panel.style.position = "fixed";
  panel.style.inset = "auto";
  panel.style.setProperty("inset-inline-start", "auto");
  panel.style.setProperty("inset-inline-end", "auto");
  panel.style.setProperty("inset-block-start", "auto");
  panel.style.setProperty("inset-block-end", "auto");
  panel.style.margin = "0";
  panel.style.right = "auto";
  panel.style.left = `${left - origin.left}px`;
  panel.style.width = `${width}px`;
  panel.style.minWidth = `${width}px`;
  panel.style.maxWidth = `${width}px`;
  panel.style.zIndex = "2500";
  panel.style.maxHeight = `${maxHeight}px`;
  panel.style.visibility = "visible";
  panel.classList.add("ss-panel--placed", "ms-panel--placed");
  if (openUp) {
    panel.style.top = "auto";
    panel.style.bottom = `${origin.bottom - rect.top + gap}px`;
  } else {
    panel.style.bottom = "auto";
    panel.style.top = `${rect.bottom + gap - origin.top}px`;
  }
}
function createsFixedContainingBlock(style) {
  if (style.transform !== "none")
    return true;
  if (style.translate && style.translate !== "none")
    return true;
  if (style.scale && style.scale !== "none")
    return true;
  if (style.rotate && style.rotate !== "none")
    return true;
  if (style.filter !== "none")
    return true;
  if (style.backdropFilter && style.backdropFilter !== "none")
    return true;
  if (style.perspective !== "none")
    return true;
  if (/paint|layout|strict|content/.test(style.contain))
    return true;
  if (/\b(transform|filter|perspective)\b/.test(style.willChange))
    return true;
  const zoom = style.zoom;
  if (zoom && zoom !== "normal" && zoom !== "1")
    return true;
  return false;
}
function fixedContainingOrigin(el) {
  let node = el.parentElement;
  while (node && node !== document.documentElement) {
    if (createsFixedContainingBlock(getComputedStyle(node))) {
      return node.getBoundingClientRect();
    }
    if (node === document.body)
      break;
    node = node.parentElement;
  }
  return new DOMRect(0, 0, window.innerWidth, window.innerHeight);
}

// src/app/shared/searchable-select/searchable-select.component.ts
var _c0 = ["panel"];
var _forTrack0 = ($index, $item) => $index + ":" + $item.value;
function SearchableSelectComponent_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275domElementStart(0, "span", 7);
    \u0275\u0275pipe(1, "t");
    \u0275\u0275domListener("click", function SearchableSelectComponent_Conditional_4_Template_span_click_0_listener($event) {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.clearSelection($event));
    })("keydown.enter", function SearchableSelectComponent_Conditional_4_Template_span_keydown_enter_0_listener($event) {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.clearSelection($event));
    })("keydown.space", function SearchableSelectComponent_Conditional_4_Template_span_keydown_space_0_listener($event) {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      ctx_r1.clearSelection($event);
      return \u0275\u0275resetView($event.preventDefault());
    });
    \u0275\u0275text(2, "\xD7");
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    \u0275\u0275attribute("aria-label", \u0275\u0275pipeBind1(1, 1, "common.clear"));
  }
}
function SearchableSelectComponent_Conditional_6_For_6_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275domElementStart(0, "button", 12);
    \u0275\u0275domListener("click", function SearchableSelectComponent_Conditional_6_For_6_Template_button_click_0_listener() {
      const opt_r5 = \u0275\u0275restoreView(_r4).$implicit;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.selectOption(opt_r5));
    });
    \u0275\u0275domElementStart(1, "span", 13);
    \u0275\u0275text(2);
    \u0275\u0275domElementEnd()();
  }
  if (rf & 2) {
    const opt_r5 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275classProp("ss-option--selected", ctx_r1.isSelected(opt_r5.value));
    \u0275\u0275domProperty("disabled", !!opt_r5.disabled);
    \u0275\u0275attribute("aria-selected", ctx_r1.isSelected(opt_r5.value));
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(opt_r5.label);
  }
}
function SearchableSelectComponent_Conditional_6_ForEmpty_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "p", 11);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(2, 1, "common.none"));
  }
}
function SearchableSelectComponent_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    const _r3 = \u0275\u0275getCurrentView();
    \u0275\u0275domElementStart(0, "div", 6, 0)(2, "input", 8);
    \u0275\u0275pipe(3, "t");
    \u0275\u0275domListener("input", function SearchableSelectComponent_Conditional_6_Template_input_input_2_listener($event) {
      \u0275\u0275restoreView(_r3);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.onQueryInput($event));
    });
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(4, "div", 9);
    \u0275\u0275repeaterCreate(5, SearchableSelectComponent_Conditional_6_For_6_Template, 3, 5, "button", 10, _forTrack0, false, SearchableSelectComponent_Conditional_6_ForEmpty_7_Template, 3, 3, "p", 11);
    \u0275\u0275domElementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275domProperty("value", ctx_r1.query())("placeholder", ctx_r1.searchPlaceholder() || \u0275\u0275pipeBind1(3, 3, "common.search"));
    \u0275\u0275advance(3);
    \u0275\u0275repeater(ctx_r1.filteredOptions());
  }
}
var SearchableSelectComponent = class _SearchableSelectComponent {
  constructor() {
    this.hostEl = inject(ElementRef);
    this.destroyRef = inject(DestroyRef);
    this.injector = inject(Injector);
    this.panel = viewChild(
      "panel",
      ...ngDevMode ? [{ debugName: "panel" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.options = input(
      [],
      ...ngDevMode ? [{ debugName: "options" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.placeholder = input(
      "",
      ...ngDevMode ? [{ debugName: "placeholder" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.searchPlaceholder = input(
      "",
      ...ngDevMode ? [{ debugName: "searchPlaceholder" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.emptyLabel = input(
      "",
      ...ngDevMode ? [{ debugName: "emptyLabel" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.emptyValue = input(
      "",
      ...ngDevMode ? [{ debugName: "emptyValue" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.compact = input(
      false,
      ...ngDevMode ? [{ debugName: "compact" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.disabled = input(
      false,
      ...ngDevMode ? [{ debugName: "disabled" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.clearable = input(
      true,
      ...ngDevMode ? [{ debugName: "clearable" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.open = signal(
      false,
      ...ngDevMode ? [{ debugName: "open" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.query = signal(
      "",
      ...ngDevMode ? [{ debugName: "query" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.selected = signal(
      "",
      ...ngDevMode ? [{ debugName: "selected" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.cvaDisabled = signal(
      false,
      ...ngDevMode ? [{ debugName: "cvaDisabled" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.isDisabled = computed(
      () => this.disabled() || this.cvaDisabled(),
      ...ngDevMode ? [{ debugName: "isDisabled" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.allOptions = computed(
      () => {
        const emptyLabel = this.emptyLabel().trim();
        const opts = this.options();
        if (!emptyLabel)
          return opts;
        return [{ value: this.emptyValue(), label: emptyLabel }, ...opts];
      },
      ...ngDevMode ? [{ debugName: "allOptions" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.filteredOptions = computed(
      () => {
        const q = this.query().trim().toLowerCase();
        const opts = this.allOptions();
        if (!q)
          return opts;
        return opts.filter((o) => o.label.toLowerCase().includes(q));
      },
      ...ngDevMode ? [{ debugName: "filteredOptions" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.selectedLabel = computed(
      () => {
        const current = this.selected();
        const match = this.allOptions().find((o) => this.sameValue(o.value, current));
        if (match)
          return match.label;
        if (current === "" || current === null || current === void 0) {
          return this.emptyLabel() || this.placeholder();
        }
        return String(current);
      },
      ...ngDevMode ? [{ debugName: "selectedLabel" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.isPlaceholder = computed(
      () => {
        const current = this.selected();
        return this.sameValue(current, this.emptyValue()) || current === "" || current === null;
      },
      ...ngDevMode ? [{ debugName: "isPlaceholder" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.canClear = computed(
      () => this.clearable() && !this.isDisabled() && !this.isPlaceholder(),
      ...ngDevMode ? [{ debugName: "canClear" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.onChange = () => void 0;
    this.onTouched = () => void 0;
    this.onScrollReposition = () => {
      if (this.open())
        this.positionPanel();
    };
    this.onPointerDown = (event) => {
      if (!this.open())
        return;
      if (isInsideDropdown(event.target, this.hostEl.nativeElement, this.panel()?.nativeElement)) {
        return;
      }
      this.close();
    };
    document.addEventListener("scroll", this.onScrollReposition, true);
    document.addEventListener("pointerdown", this.onPointerDown, true);
    this.destroyRef.onDestroy(() => {
      document.removeEventListener("scroll", this.onScrollReposition, true);
      document.removeEventListener("pointerdown", this.onPointerDown, true);
    });
  }
  onEscape() {
    if (this.open())
      this.close();
  }
  onViewportChange() {
    if (this.open())
      this.positionPanel();
  }
  writeValue(value) {
    this.selected.set(value ?? this.emptyValue());
  }
  registerOnChange(fn) {
    this.onChange = fn;
  }
  registerOnTouched(fn) {
    this.onTouched = fn;
  }
  setDisabledState(isDisabled) {
    this.cvaDisabled.set(isDisabled);
  }
  toggleOpen() {
    if (this.isDisabled())
      return;
    if (this.open()) {
      this.close();
      return;
    }
    this.query.set("");
    this.open.set(true);
    afterNextRender(() => {
      this.positionPanel();
      this.panel()?.nativeElement.querySelector(".ss-search")?.focus();
    }, { injector: this.injector });
  }
  isSelected(value) {
    return this.sameValue(this.selected(), value);
  }
  selectOption(option) {
    if (this.isDisabled() || option.disabled)
      return;
    this.selected.set(option.value);
    this.onChange(option.value);
    this.onTouched();
    this.close();
  }
  clearSelection(event) {
    event.preventDefault();
    event.stopPropagation();
    if (!this.canClear())
      return;
    const empty = this.emptyValue();
    this.selected.set(empty);
    this.onChange(empty);
    this.onTouched();
    this.close();
  }
  onQueryInput(event) {
    this.query.set(event.target.value);
  }
  sameValue(a, b) {
    if (a === b)
      return true;
    if ((a === "" || a === null || a === void 0) && (b === "" || b === null || b === void 0)) {
      return true;
    }
    return String(a) === String(b);
  }
  close() {
    this.open.set(false);
    this.query.set("");
    this.onTouched();
  }
  positionPanel() {
    const panel = this.panel()?.nativeElement ?? this.hostEl.nativeElement.querySelector(".ss-panel");
    const trigger = this.hostEl.nativeElement.querySelector(".ss-trigger");
    if (!panel || !trigger)
      return;
    attachDropdownPanelToBody(panel);
    applyDropdownPanelStyle(panel, trigger, this.compact());
  }
  static {
    this.\u0275fac = function SearchableSelectComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _SearchableSelectComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _SearchableSelectComponent, selectors: [["app-searchable-select"]], viewQuery: function SearchableSelectComponent_Query(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275viewQuerySignal(ctx.panel, _c0, 5);
      }
      if (rf & 2) {
        \u0275\u0275queryAdvance();
      }
    }, hostVars: 6, hostBindings: function SearchableSelectComponent_HostBindings(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275listener("keydown.escape", function SearchableSelectComponent_keydown_escape_HostBindingHandler() {
          return ctx.onEscape();
        }, \u0275\u0275resolveDocument)("resize", function SearchableSelectComponent_resize_HostBindingHandler() {
          return ctx.onViewportChange();
        }, \u0275\u0275resolveWindow);
      }
      if (rf & 2) {
        \u0275\u0275classProp("ss--compact", ctx.compact())("ss--open", ctx.open())("ss--disabled", ctx.isDisabled());
      }
    }, inputs: { options: [1, "options"], placeholder: [1, "placeholder"], searchPlaceholder: [1, "searchPlaceholder"], emptyLabel: [1, "emptyLabel"], emptyValue: [1, "emptyValue"], compact: [1, "compact"], disabled: [1, "disabled"], clearable: [1, "clearable"] }, features: [\u0275\u0275ProvidersFeature([
      {
        provide: NG_VALUE_ACCESSOR,
        useExisting: forwardRef(() => _SearchableSelectComponent),
        multi: true
      }
    ])], decls: 7, vars: 7, consts: [["panel", ""], [1, "ss"], ["type", "button", 1, "ss-trigger", 3, "click", "disabled"], [1, "ss-summary"], ["role", "button", "tabindex", "0", 1, "ss-clear"], ["aria-hidden", "true", 1, "ss-caret"], ["role", "listbox", 1, "ss-panel", "ss-panel--overlay"], ["role", "button", "tabindex", "0", 1, "ss-clear", 3, "click", "keydown.enter", "keydown.space"], ["type", "search", "autocomplete", "off", 1, "ss-search", 3, "input", "value", "placeholder"], [1, "ss-options"], ["type", "button", "role", "option", 1, "ss-option", 3, "disabled", "ss-option--selected"], [1, "ss-empty"], ["type", "button", "role", "option", 1, "ss-option", 3, "click", "disabled"], [1, "ss-label"]], template: function SearchableSelectComponent_Template(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275domElementStart(0, "div", 1)(1, "button", 2);
        \u0275\u0275domListener("click", function SearchableSelectComponent_Template_button_click_1_listener() {
          return ctx.toggleOpen();
        });
        \u0275\u0275domElementStart(2, "span", 3);
        \u0275\u0275text(3);
        \u0275\u0275domElementEnd();
        \u0275\u0275conditionalCreate(4, SearchableSelectComponent_Conditional_4_Template, 3, 3, "span", 4);
        \u0275\u0275domElement(5, "span", 5);
        \u0275\u0275domElementEnd();
        \u0275\u0275conditionalCreate(6, SearchableSelectComponent_Conditional_6_Template, 8, 5, "div", 6);
        \u0275\u0275domElementEnd();
      }
      if (rf & 2) {
        \u0275\u0275advance();
        \u0275\u0275domProperty("disabled", ctx.isDisabled());
        \u0275\u0275attribute("aria-expanded", ctx.open());
        \u0275\u0275advance();
        \u0275\u0275classProp("ss-summary--placeholder", ctx.isPlaceholder());
        \u0275\u0275advance();
        \u0275\u0275textInterpolate(ctx.selectedLabel());
        \u0275\u0275advance();
        \u0275\u0275conditional(ctx.canClear() ? 4 : -1);
        \u0275\u0275advance(2);
        \u0275\u0275conditional(ctx.open() ? 6 : -1);
      }
    }, dependencies: [TranslatePipe], styles: ["\n[_nghost-%COMP%] {\n  display: block;\n  width: 100%;\n}\n.ss[_ngcontent-%COMP%] {\n  position: relative;\n  width: 100%;\n}\n.ss--compact[_nghost-%COMP%]   .ss-trigger[_ngcontent-%COMP%] {\n  min-height: 2.2rem;\n  padding: 0.4rem 0.55rem;\n  font-size: 0.9rem;\n}\n.ss--compact[_nghost-%COMP%]   .ss-panel[_ngcontent-%COMP%] {\n  min-width: 0;\n}\n.ss--open[_nghost-%COMP%]   .ss-caret[_ngcontent-%COMP%] {\n  transform: rotate(-135deg);\n  margin-bottom: -0.1rem;\n}\n.ss--disabled[_nghost-%COMP%]   .ss-trigger[_ngcontent-%COMP%] {\n  opacity: 0.55;\n  cursor: not-allowed;\n}\n.ss-trigger[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 0.4rem;\n  width: 100%;\n  min-height: 2.5rem;\n  padding: 0.5rem 0.65rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  font: inherit;\n  text-align: left;\n  cursor: pointer;\n  box-shadow: none;\n}\n.ss-trigger[_ngcontent-%COMP%]:hover:not(:disabled) {\n  border-color: rgba(255, 214, 10, 0.45);\n}\n.ss-trigger[_ngcontent-%COMP%]:focus-visible {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  box-shadow: var(--focus-ring);\n}\n.ss-summary[_ngcontent-%COMP%] {\n  flex: 1;\n  min-width: 0;\n  overflow: hidden;\n  text-overflow: ellipsis;\n  white-space: nowrap;\n}\n.ss-summary--placeholder[_ngcontent-%COMP%] {\n  color: var(--text-soft);\n}\n.ss-clear[_ngcontent-%COMP%] {\n  flex-shrink: 0;\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n  width: 1.25rem;\n  height: 1.25rem;\n  border-radius: 999px;\n  background: var(--elevated-bg-hover);\n  font-size: 0.95rem;\n  line-height: 1;\n  cursor: pointer;\n}\n.ss-clear[_ngcontent-%COMP%]:hover {\n  background: rgba(255, 107, 107, 0.35);\n}\n.ss-caret[_ngcontent-%COMP%] {\n  flex-shrink: 0;\n  width: 0.45rem;\n  height: 0.45rem;\n  border-right: 2px solid currentColor;\n  border-bottom: 2px solid currentColor;\n  transform: rotate(45deg);\n  opacity: 0.7;\n  margin-bottom: 0.2rem;\n  transition: transform 0.15s ease;\n}\n.ss-panel[_ngcontent-%COMP%] {\n  position: absolute;\n  z-index: 40;\n  top: calc(100% + 0.35rem);\n  inset-inline-start: 0;\n  inset-inline-end: 0;\n  min-width: 0;\n  width: 100%;\n  max-height: 20rem;\n  display: flex;\n  flex-direction: column;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--bg-elevated);\n  box-shadow: var(--shadow-md);\n  overflow: hidden;\n}\n.ss-panel.ss-panel--overlay[_ngcontent-%COMP%] {\n  position: fixed;\n  top: 0;\n  left: 0;\n  right: auto;\n  bottom: auto;\n  inset-inline-start: auto;\n  inset-inline-end: auto;\n  width: auto;\n  min-width: 0;\n  visibility: hidden;\n}\n.ss-panel.ss-panel--overlay.ss-panel--placed[_ngcontent-%COMP%] {\n  visibility: visible;\n}\n.ss-search[_ngcontent-%COMP%] {\n  width: 100%;\n  border: 0;\n  border-bottom: 1px solid var(--border);\n  border-radius: 0;\n  background: transparent;\n  color: var(--text);\n  padding: 0.65rem 0.75rem;\n  font: inherit;\n  flex-shrink: 0;\n}\n.ss-search[_ngcontent-%COMP%]:focus {\n  outline: none;\n  background: var(--elevated-bg);\n}\n.ss-options[_ngcontent-%COMP%] {\n  overflow-y: auto;\n  flex: 1 1 auto;\n  min-height: 0;\n  max-height: none;\n  padding: 0.35rem;\n}\n.ss-option[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: flex-start;\n  width: 100%;\n  padding: 0.55rem 0.55rem;\n  border: 0;\n  border-radius: var(--radius-sm);\n  background: transparent;\n  color: var(--text);\n  font: inherit;\n  text-align: left;\n  cursor: pointer;\n  box-shadow: none;\n}\n.ss-option[_ngcontent-%COMP%]:hover:not(:disabled) {\n  background: var(--elevated-bg-hover);\n}\n.ss-option[_ngcontent-%COMP%]:disabled {\n  opacity: 0.45;\n  cursor: not-allowed;\n}\n.ss-option--selected[_ngcontent-%COMP%] {\n  background: rgba(95, 211, 188, 0.14);\n}\n.ss-label[_ngcontent-%COMP%] {\n  min-width: 0;\n  flex: 1;\n  line-height: 1.35;\n  white-space: normal;\n  overflow-wrap: anywhere;\n}\n.ss-empty[_ngcontent-%COMP%] {\n  margin: 0;\n  padding: 0.75rem;\n  color: var(--text-soft);\n  font-size: 0.9rem;\n  text-align: center;\n}\n/*# sourceMappingURL=searchable-select.component.css.map */"] });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(SearchableSelectComponent, [{
    type: Component,
    args: [{ selector: "app-searchable-select", imports: [TranslatePipe], host: {
      "[class.ss--compact]": "compact()",
      "[class.ss--open]": "open()",
      "[class.ss--disabled]": "isDisabled()"
    }, providers: [
      {
        provide: NG_VALUE_ACCESSOR,
        useExisting: forwardRef(() => SearchableSelectComponent),
        multi: true
      }
    ], template: `<div class="ss">\r
  <button type="button" class="ss-trigger" (click)="toggleOpen()" [disabled]="isDisabled()" [attr.aria-expanded]="open()">\r
    <span class="ss-summary" [class.ss-summary--placeholder]="isPlaceholder()">{{ selectedLabel() }}</span>\r
    @if (canClear()) {\r
      <span\r
        class="ss-clear"\r
        role="button"\r
        tabindex="0"\r
        (click)="clearSelection($event)"\r
        (keydown.enter)="clearSelection($event)"\r
        (keydown.space)="clearSelection($event); $event.preventDefault()"\r
        [attr.aria-label]="'common.clear' | t"\r
      >\xD7</span>\r
    }\r
    <span class="ss-caret" aria-hidden="true"></span>\r
  </button>\r
\r
  @if (open()) {\r
    <div #panel class="ss-panel ss-panel--overlay" role="listbox">\r
      <input\r
        class="ss-search"\r
        type="search"\r
        [value]="query()"\r
        (input)="onQueryInput($event)"\r
        [placeholder]="searchPlaceholder() || ('common.search' | t)"\r
        autocomplete="off"\r
      />\r
      <div class="ss-options">\r
        @for (opt of filteredOptions(); track $index + ':' + opt.value) {\r
          <button\r
            type="button"\r
            class="ss-option"\r
            role="option"\r
            [disabled]="!!opt.disabled"\r
            [attr.aria-selected]="isSelected(opt.value)"\r
            [class.ss-option--selected]="isSelected(opt.value)"\r
            (click)="selectOption(opt)"\r
          >\r
            <span class="ss-label">{{ opt.label }}</span>\r
          </button>\r
        } @empty {\r
          <p class="ss-empty">{{ 'common.none' | t }}</p>\r
        }\r
      </div>\r
    </div>\r
  }\r
</div>\r
`, styles: ["/* src/app/shared/searchable-select/searchable-select.component.css */\n:host {\n  display: block;\n  width: 100%;\n}\n.ss {\n  position: relative;\n  width: 100%;\n}\n:host.ss--compact .ss-trigger {\n  min-height: 2.2rem;\n  padding: 0.4rem 0.55rem;\n  font-size: 0.9rem;\n}\n:host.ss--compact .ss-panel {\n  min-width: 0;\n}\n:host.ss--open .ss-caret {\n  transform: rotate(-135deg);\n  margin-bottom: -0.1rem;\n}\n:host.ss--disabled .ss-trigger {\n  opacity: 0.55;\n  cursor: not-allowed;\n}\n.ss-trigger {\n  display: flex;\n  align-items: center;\n  gap: 0.4rem;\n  width: 100%;\n  min-height: 2.5rem;\n  padding: 0.5rem 0.65rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  font: inherit;\n  text-align: left;\n  cursor: pointer;\n  box-shadow: none;\n}\n.ss-trigger:hover:not(:disabled) {\n  border-color: rgba(255, 214, 10, 0.45);\n}\n.ss-trigger:focus-visible {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  box-shadow: var(--focus-ring);\n}\n.ss-summary {\n  flex: 1;\n  min-width: 0;\n  overflow: hidden;\n  text-overflow: ellipsis;\n  white-space: nowrap;\n}\n.ss-summary--placeholder {\n  color: var(--text-soft);\n}\n.ss-clear {\n  flex-shrink: 0;\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n  width: 1.25rem;\n  height: 1.25rem;\n  border-radius: 999px;\n  background: var(--elevated-bg-hover);\n  font-size: 0.95rem;\n  line-height: 1;\n  cursor: pointer;\n}\n.ss-clear:hover {\n  background: rgba(255, 107, 107, 0.35);\n}\n.ss-caret {\n  flex-shrink: 0;\n  width: 0.45rem;\n  height: 0.45rem;\n  border-right: 2px solid currentColor;\n  border-bottom: 2px solid currentColor;\n  transform: rotate(45deg);\n  opacity: 0.7;\n  margin-bottom: 0.2rem;\n  transition: transform 0.15s ease;\n}\n.ss-panel {\n  position: absolute;\n  z-index: 40;\n  top: calc(100% + 0.35rem);\n  inset-inline-start: 0;\n  inset-inline-end: 0;\n  min-width: 0;\n  width: 100%;\n  max-height: 20rem;\n  display: flex;\n  flex-direction: column;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--bg-elevated);\n  box-shadow: var(--shadow-md);\n  overflow: hidden;\n}\n.ss-panel.ss-panel--overlay {\n  position: fixed;\n  top: 0;\n  left: 0;\n  right: auto;\n  bottom: auto;\n  inset-inline-start: auto;\n  inset-inline-end: auto;\n  width: auto;\n  min-width: 0;\n  visibility: hidden;\n}\n.ss-panel.ss-panel--overlay.ss-panel--placed {\n  visibility: visible;\n}\n.ss-search {\n  width: 100%;\n  border: 0;\n  border-bottom: 1px solid var(--border);\n  border-radius: 0;\n  background: transparent;\n  color: var(--text);\n  padding: 0.65rem 0.75rem;\n  font: inherit;\n  flex-shrink: 0;\n}\n.ss-search:focus {\n  outline: none;\n  background: var(--elevated-bg);\n}\n.ss-options {\n  overflow-y: auto;\n  flex: 1 1 auto;\n  min-height: 0;\n  max-height: none;\n  padding: 0.35rem;\n}\n.ss-option {\n  display: flex;\n  align-items: flex-start;\n  width: 100%;\n  padding: 0.55rem 0.55rem;\n  border: 0;\n  border-radius: var(--radius-sm);\n  background: transparent;\n  color: var(--text);\n  font: inherit;\n  text-align: left;\n  cursor: pointer;\n  box-shadow: none;\n}\n.ss-option:hover:not(:disabled) {\n  background: var(--elevated-bg-hover);\n}\n.ss-option:disabled {\n  opacity: 0.45;\n  cursor: not-allowed;\n}\n.ss-option--selected {\n  background: rgba(95, 211, 188, 0.14);\n}\n.ss-label {\n  min-width: 0;\n  flex: 1;\n  line-height: 1.35;\n  white-space: normal;\n  overflow-wrap: anywhere;\n}\n.ss-empty {\n  margin: 0;\n  padding: 0.75rem;\n  color: var(--text-soft);\n  font-size: 0.9rem;\n  text-align: center;\n}\n/*# sourceMappingURL=searchable-select.component.css.map */\n"] }]
  }], () => [], { panel: [{ type: ViewChild, args: ["panel", { isSignal: true }] }], options: [{ type: Input, args: [{ isSignal: true, alias: "options", required: false }] }], placeholder: [{ type: Input, args: [{ isSignal: true, alias: "placeholder", required: false }] }], searchPlaceholder: [{ type: Input, args: [{ isSignal: true, alias: "searchPlaceholder", required: false }] }], emptyLabel: [{ type: Input, args: [{ isSignal: true, alias: "emptyLabel", required: false }] }], emptyValue: [{ type: Input, args: [{ isSignal: true, alias: "emptyValue", required: false }] }], compact: [{ type: Input, args: [{ isSignal: true, alias: "compact", required: false }] }], disabled: [{ type: Input, args: [{ isSignal: true, alias: "disabled", required: false }] }], clearable: [{ type: Input, args: [{ isSignal: true, alias: "clearable", required: false }] }], onEscape: [{
    type: HostListener,
    args: ["document:keydown.escape"]
  }], onViewportChange: [{
    type: HostListener,
    args: ["window:resize"]
  }] });
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(SearchableSelectComponent, { className: "SearchableSelectComponent", filePath: "src/app/shared/searchable-select/searchable-select.component.ts", lineNumber: 47 });
})();

export {
  attachDropdownPanelToBody,
  isInsideDropdown,
  applyDropdownPanelStyle,
  SearchableSelectComponent
};
//# sourceMappingURL=chunk-AEKLNI6J.js.map
