import {
  IconActionButtonComponent,
  certificateMaxTotal,
  certificateTotal,
  exportCertificateModelImages,
  printCertificateModels,
  safeCertificateFileName
} from "./chunk-JHH4QVXF.js";
import {
  AuthService
} from "./chunk-MMGMRKLA.js";
import {
  formatGradeLabel
} from "./chunk-KZPOLEER.js";
import {
  SiteBrandService
} from "./chunk-V326I5SR.js";
import "./chunk-RN452YVM.js";
import "./chunk-RLZFB7T3.js";
import {
  SearchableSelectComponent
} from "./chunk-4UWTYRDU.js";
import {
  LocaleService,
  TranslatePipe
} from "./chunk-WEWBZC6G.js";
import {
  LearningApiService
} from "./chunk-ET3TNS53.js";
import {
  PageFeedbackComponent
} from "./chunk-GSQJCOEO.js";
import {
  CheckboxControlValueAccessor,
  Component,
  DefaultValueAccessor,
  FormsModule,
  HttpClient,
  MaxLengthValidator,
  MaxValidator,
  MinValidator,
  NgControlStatus,
  NgControlStatusGroup,
  NgForm,
  NgModel,
  NumberValueAccessor,
  __async,
  computed,
  firstValueFrom,
  inject,
  setClassMetadata,
  signal,
  ɵNgNoValidate,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵattribute,
  ɵɵclassProp,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵcontrol,
  ɵɵcontrolCreate,
  ɵɵdefineComponent,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵgetCurrentView,
  ɵɵlistener,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind1,
  ɵɵpipeBind2,
  ɵɵproperty,
  ɵɵpureFunction2,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1,
  ɵɵtextInterpolate2,
  ɵɵtwoWayBindingSet,
  ɵɵtwoWayListener,
  ɵɵtwoWayProperty
} from "./chunk-EMYLRMBO.js";

// src/app/pages/admin/grade-certificates.component.ts
var _c0 = (a0, a1) => ({ approved: a0, pending: a1 });
var _forTrack0 = ($index, $item) => $item.id;
var _forTrack1 = ($index, $item) => $item.courseId;
var _forTrack2 = ($index, $item) => $item.studentId;
function GradeCertificatesComponent_Conditional_8_Conditional_16_For_15_Template(rf, ctx) {
  if (rf & 1) {
    const _r3 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td")(2, "label", 19)(3, "input", 20);
    \u0275\u0275twoWayListener("ngModelChange", function GradeCertificatesComponent_Conditional_8_Conditional_16_For_15_Template_input_ngModelChange_3_listener($event) {
      const draft_r4 = \u0275\u0275restoreView(_r3).$implicit;
      \u0275\u0275twoWayBindingSet(draft_r4.selected, $event) || (draft_r4.selected = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementStart(4, "span");
    \u0275\u0275text(5);
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(6, "td")(7, "input", 21);
    \u0275\u0275twoWayListener("ngModelChange", function GradeCertificatesComponent_Conditional_8_Conditional_16_For_15_Template_input_ngModelChange_7_listener($event) {
      const draft_r4 = \u0275\u0275restoreView(_r3).$implicit;
      \u0275\u0275twoWayBindingSet(draft_r4.maxDegree, $event) || (draft_r4.maxDegree = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "td")(9, "input", 22);
    \u0275\u0275twoWayListener("ngModelChange", function GradeCertificatesComponent_Conditional_8_Conditional_16_For_15_Template_input_ngModelChange_9_listener($event) {
      const draft_r4 = \u0275\u0275restoreView(_r3).$implicit;
      \u0275\u0275twoWayBindingSet(draft_r4.includedInTotal, $event) || (draft_r4.includedInTotal = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const draft_r4 = ctx.$implicit;
    const \u0275$index_61_r5 = ctx.$index;
    \u0275\u0275advance(3);
    \u0275\u0275twoWayProperty("ngModel", draft_r4.selected);
    \u0275\u0275property("name", "subject-selected-" + \u0275$index_61_r5);
    \u0275\u0275control();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(draft_r4.courseTitle);
    \u0275\u0275advance(2);
    \u0275\u0275twoWayProperty("ngModel", draft_r4.maxDegree);
    \u0275\u0275property("name", "subject-max-" + \u0275$index_61_r5)("disabled", !draft_r4.selected);
    \u0275\u0275control();
    \u0275\u0275advance(2);
    \u0275\u0275twoWayProperty("ngModel", draft_r4.includedInTotal);
    \u0275\u0275property("name", "subject-total-" + \u0275$index_61_r5)("disabled", !draft_r4.selected);
    \u0275\u0275control();
  }
}
function GradeCertificatesComponent_Conditional_8_Conditional_16_ForEmpty_16_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 23);
    \u0275\u0275text(2);
    \u0275\u0275pipe(3, "t");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(3, 1, "certificates.noSubjects"));
  }
}
function GradeCertificatesComponent_Conditional_8_Conditional_16_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 7)(1, "table", 18)(2, "thead")(3, "tr")(4, "th");
    \u0275\u0275text(5);
    \u0275\u0275pipe(6, "t");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "th");
    \u0275\u0275text(8);
    \u0275\u0275pipe(9, "t");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "th");
    \u0275\u0275text(11);
    \u0275\u0275pipe(12, "t");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(13, "tbody");
    \u0275\u0275repeaterCreate(14, GradeCertificatesComponent_Conditional_8_Conditional_16_For_15_Template, 10, 9, "tr", null, _forTrack1, false, GradeCertificatesComponent_Conditional_8_Conditional_16_ForEmpty_16_Template, 4, 3, "tr");
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(6, 4, "certificates.subject"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(9, 6, "certificates.maxDegree"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(12, 8, "certificates.includedInTotal"));
    \u0275\u0275advance(3);
    \u0275\u0275repeater(ctx_r1.subjectDrafts);
  }
}
function GradeCertificatesComponent_Conditional_8_Conditional_21_Template(rf, ctx) {
  if (rf & 1) {
    const _r6 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 24);
    \u0275\u0275listener("click", function GradeCertificatesComponent_Conditional_8_Conditional_21_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r6);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.cancelEdit());
    });
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(2, 1, "common.cancel"));
  }
}
function GradeCertificatesComponent_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "section", 3)(1, "h3");
    \u0275\u0275text(2);
    \u0275\u0275pipe(3, "t");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "form", 11);
    \u0275\u0275listener("ngSubmit", function GradeCertificatesComponent_Conditional_8_Template_form_ngSubmit_4_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.saveCertificate());
    });
    \u0275\u0275elementStart(5, "div", 12)(6, "label");
    \u0275\u0275text(7);
    \u0275\u0275pipe(8, "t");
    \u0275\u0275elementStart(9, "input", 13);
    \u0275\u0275pipe(10, "t");
    \u0275\u0275twoWayListener("ngModelChange", function GradeCertificatesComponent_Conditional_8_Template_input_ngModelChange_9_listener($event) {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      \u0275\u0275twoWayBindingSet(ctx_r1.formTitle, $event) || (ctx_r1.formTitle = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "label");
    \u0275\u0275text(12);
    \u0275\u0275pipe(13, "t");
    \u0275\u0275elementStart(14, "app-searchable-select", 14);
    \u0275\u0275pipe(15, "t");
    \u0275\u0275listener("ngModelChange", function GradeCertificatesComponent_Conditional_8_Template_app_searchable_select_ngModelChange_14_listener($event) {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.onFormClassroomChange($event));
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(16, GradeCertificatesComponent_Conditional_8_Conditional_16_Template, 17, 10, "div", 7);
    \u0275\u0275elementStart(17, "div", 15)(18, "button", 16);
    \u0275\u0275text(19);
    \u0275\u0275pipe(20, "t");
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(21, GradeCertificatesComponent_Conditional_8_Conditional_21_Template, 3, 3, "button", 17);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(3, 13, ctx_r1.editingId ? "certificates.editTitle" : "certificates.createTitle"));
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(8, 15, "certificates.name"), " ");
    \u0275\u0275advance(2);
    \u0275\u0275twoWayProperty("ngModel", ctx_r1.formTitle);
    \u0275\u0275property("placeholder", \u0275\u0275pipeBind1(10, 17, "certificates.namePlaceholder"));
    \u0275\u0275control();
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(13, 19, "common.classroom"), " ");
    \u0275\u0275advance(2);
    \u0275\u0275property("ngModel", ctx_r1.formClassroomId)("disabled", !!ctx_r1.editingId)("emptyLabel", \u0275\u0275pipeBind1(15, 21, "certificates.chooseClassroom"))("options", ctx_r1.classroomOptions());
    \u0275\u0275control();
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.formClassroomId ? 16 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275property("disabled", ctx_r1.saving());
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(20, 23, ctx_r1.editingId ? "common.save" : "certificates.create"), " ");
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.editingId ? 21 : -1);
  }
}
function GradeCertificatesComponent_Conditional_15_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 1);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind2(2, 1, "certificates.approvalSummary", \u0275\u0275pureFunction2(4, _c0, ctx_r1.approvedCount(), ctx_r1.pendingCount())), " ");
  }
}
function GradeCertificatesComponent_Conditional_16_Template(rf, ctx) {
  if (rf & 1) {
    const _r7 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 25);
    \u0275\u0275listener("click", function GradeCertificatesComponent_Conditional_16_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r7);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.approveAllCertificates());
    });
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275property("disabled", ctx_r1.approving());
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(2, 2, ctx_r1.approving() ? "certificates.approving" : "certificates.approveAll"), " ");
  }
}
function GradeCertificatesComponent_Conditional_36_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "th");
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(2, 1, "certificates.approval"));
  }
}
function GradeCertificatesComponent_For_42_Conditional_12_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 28);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\u2713 ", \u0275\u0275pipeBind1(2, 1, "certificates.approvedChip"));
  }
}
function GradeCertificatesComponent_For_42_Conditional_12_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    const _r9 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 30);
    \u0275\u0275listener("click", function GradeCertificatesComponent_For_42_Conditional_12_Conditional_2_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r9);
      const certificate_r10 = \u0275\u0275nextContext(2).$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.approveCertificate(certificate_r10));
    });
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(3);
    \u0275\u0275property("disabled", ctx_r1.approving());
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(2, 2, "certificates.approve"), " ");
  }
}
function GradeCertificatesComponent_For_42_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "td");
    \u0275\u0275conditionalCreate(1, GradeCertificatesComponent_For_42_Conditional_12_Conditional_1_Template, 3, 3, "span", 28)(2, GradeCertificatesComponent_For_42_Conditional_12_Conditional_2_Template, 3, 4, "button", 29);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const certificate_r10 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275conditional(certificate_r10.isApproved ? 1 : 2);
  }
}
function GradeCertificatesComponent_For_42_Conditional_18_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    const _r12 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 34);
    \u0275\u0275listener("click", function GradeCertificatesComponent_For_42_Conditional_18_Conditional_0_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r12);
      const certificate_r10 = \u0275\u0275nextContext(2).$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.revokeApproval(certificate_r10));
    });
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(3);
    \u0275\u0275property("disabled", ctx_r1.approving());
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(2, 2, "certificates.revokeApproval"), " ");
  }
}
function GradeCertificatesComponent_For_42_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    const _r11 = \u0275\u0275getCurrentView();
    \u0275\u0275conditionalCreate(0, GradeCertificatesComponent_For_42_Conditional_18_Conditional_0_Template, 3, 4, "button", 31);
    \u0275\u0275elementStart(1, "app-icon-action-button", 32);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275listener("action", function GradeCertificatesComponent_For_42_Conditional_18_Template_app_icon_action_button_action_1_listener() {
      \u0275\u0275restoreView(_r11);
      const certificate_r10 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.startEdit(certificate_r10));
    });
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "app-icon-action-button", 33);
    \u0275\u0275pipe(4, "t");
    \u0275\u0275listener("action", function GradeCertificatesComponent_For_42_Conditional_18_Template_app_icon_action_button_action_3_listener() {
      \u0275\u0275restoreView(_r11);
      const certificate_r10 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.deleteCertificate(certificate_r10));
    });
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const certificate_r10 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275conditional(certificate_r10.isApproved ? 0 : -1);
    \u0275\u0275advance();
    \u0275\u0275property("label", \u0275\u0275pipeBind1(2, 3, "common.edit"));
    \u0275\u0275advance(2);
    \u0275\u0275property("label", \u0275\u0275pipeBind1(4, 5, "common.delete"));
  }
}
function GradeCertificatesComponent_For_42_Template(rf, ctx) {
  if (rf & 1) {
    const _r8 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td")(2, "strong");
    \u0275\u0275text(3);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(4, "td");
    \u0275\u0275text(5);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "td");
    \u0275\u0275text(7);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "td");
    \u0275\u0275text(9);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "td");
    \u0275\u0275text(11);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(12, GradeCertificatesComponent_For_42_Conditional_12_Template, 3, 1, "td");
    \u0275\u0275elementStart(13, "td")(14, "div", 26)(15, "button", 27);
    \u0275\u0275listener("click", function GradeCertificatesComponent_For_42_Template_button_click_15_listener() {
      const certificate_r10 = \u0275\u0275restoreView(_r8).$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.openSheet(certificate_r10));
    });
    \u0275\u0275text(16);
    \u0275\u0275pipe(17, "t");
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(18, GradeCertificatesComponent_For_42_Conditional_18_Template, 5, 7);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const certificate_r10 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275classProp("is-open", ctx_r1.sheet()?.id === certificate_r10.id);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(certificate_r10.title);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(certificate_r10.classroomName);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(ctx_r1.gradeLabel(certificate_r10.grade));
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(certificate_r10.subjectCount);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(certificate_r10.studentCount);
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.isAdmin() ? 12 : -1);
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(17, 10, "certificates.enterDegrees"), " ");
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.isAdmin() ? 18 : -1);
  }
}
function GradeCertificatesComponent_ForEmpty_43_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 1);
    \u0275\u0275text(2);
    \u0275\u0275pipe(3, "t");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275attribute("colspan", ctx_r1.isAdmin() ? 7 : 6);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(3, 2, ctx_r1.isAdmin() ? "certificates.emptyAdmin" : "certificates.emptyTeacher"), " ");
  }
}
function GradeCertificatesComponent_Conditional_44_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    const _r14 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 25);
    \u0275\u0275listener("click", function GradeCertificatesComponent_Conditional_44_Conditional_8_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r14);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.printCertificates());
    });
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "button", 25);
    \u0275\u0275listener("click", function GradeCertificatesComponent_Conditional_44_Conditional_8_Template_button_click_3_listener() {
      \u0275\u0275restoreView(_r14);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.exportCertificateImages());
    });
    \u0275\u0275text(4);
    \u0275\u0275pipe(5, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const current_r15 = \u0275\u0275nextContext();
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275property("disabled", !current_r15.students.length);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(2, 4, "certificates.printAll"), " ");
    \u0275\u0275advance(2);
    \u0275\u0275property("disabled", !current_r15.students.length || ctx_r1.exporting());
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(5, 6, ctx_r1.exporting() ? "certificates.exporting" : "certificates.imageAll"), " ");
  }
}
function GradeCertificatesComponent_Conditional_44_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 1);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(2, 1, "certificates.teacherHint"));
  }
}
function GradeCertificatesComponent_Conditional_44_Conditional_13_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 1);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\u2713 ", \u0275\u0275pipeBind1(2, 1, "certificates.sheetApprovedHint"));
  }
}
function GradeCertificatesComponent_Conditional_44_Conditional_14_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 1);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(2, 1, "certificates.sheetPendingHint"));
  }
}
function GradeCertificatesComponent_Conditional_44_For_23_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "th");
    \u0275\u0275text(1);
    \u0275\u0275elementStart(2, "span", 37);
    \u0275\u0275text(3);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const subject_r16 = ctx.$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", subject_r16.courseTitle, " ");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(subject_r16.maxDegree);
  }
}
function GradeCertificatesComponent_Conditional_44_Conditional_24_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "th");
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementStart(3, "span", 37);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const current_r15 = \u0275\u0275nextContext();
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(2, 2, "certificates.total"), " ");
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(ctx_r1.maxTotal(current_r15));
  }
}
function GradeCertificatesComponent_Conditional_44_For_26_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "th");
    \u0275\u0275text(1);
    \u0275\u0275elementStart(2, "span", 37);
    \u0275\u0275text(3);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const subject_r17 = ctx.$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", subject_r17.courseTitle, " ");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(subject_r17.maxDegree);
  }
}
function GradeCertificatesComponent_Conditional_44_Conditional_27_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "th");
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(2, 1, "common.actions"));
  }
}
function GradeCertificatesComponent_Conditional_44_For_30_For_4_Template(rf, ctx) {
  if (rf & 1) {
    const _r18 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "td")(1, "input", 39);
    \u0275\u0275twoWayListener("ngModelChange", function GradeCertificatesComponent_Conditional_44_For_30_For_4_Template_input_ngModelChange_1_listener($event) {
      const subject_r19 = \u0275\u0275restoreView(_r18).$implicit;
      const student_r20 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(2);
      \u0275\u0275twoWayBindingSet(ctx_r1.marks[student_r20.studentId][subject_r19.id], $event) || (ctx_r1.marks[student_r20.studentId][subject_r19.id] = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const subject_r19 = ctx.$implicit;
    const student_r20 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance();
    \u0275\u0275property("max", subject_r19.maxDegree);
    \u0275\u0275twoWayProperty("ngModel", ctx_r1.marks[student_r20.studentId][subject_r19.id]);
    \u0275\u0275attribute("aria-label", subject_r19.courseTitle + " \u2014 " + student_r20.studentName);
    \u0275\u0275control();
  }
}
function GradeCertificatesComponent_Conditional_44_For_30_Conditional_5_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "td", 38);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "t");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const student_r20 = \u0275\u0275nextContext().$implicit;
    const current_r15 = \u0275\u0275nextContext();
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.studentTotal(current_r15, student_r20.studentId) ?? \u0275\u0275pipeBind1(2, 1, "common.emDash"));
  }
}
function GradeCertificatesComponent_Conditional_44_For_30_For_7_Template(rf, ctx) {
  if (rf & 1) {
    const _r21 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "td")(1, "input", 39);
    \u0275\u0275twoWayListener("ngModelChange", function GradeCertificatesComponent_Conditional_44_For_30_For_7_Template_input_ngModelChange_1_listener($event) {
      const subject_r22 = \u0275\u0275restoreView(_r21).$implicit;
      const student_r20 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(2);
      \u0275\u0275twoWayBindingSet(ctx_r1.marks[student_r20.studentId][subject_r22.id], $event) || (ctx_r1.marks[student_r20.studentId][subject_r22.id] = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementEnd();
    \u0275\u0275controlCreate();
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const subject_r22 = ctx.$implicit;
    const student_r20 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance();
    \u0275\u0275property("max", subject_r22.maxDegree);
    \u0275\u0275twoWayProperty("ngModel", ctx_r1.marks[student_r20.studentId][subject_r22.id]);
    \u0275\u0275attribute("aria-label", subject_r22.courseTitle + " \u2014 " + student_r20.studentName);
    \u0275\u0275control();
  }
}
function GradeCertificatesComponent_Conditional_44_For_30_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    const _r23 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "td")(1, "div", 26)(2, "button", 27);
    \u0275\u0275listener("click", function GradeCertificatesComponent_Conditional_44_For_30_Conditional_8_Template_button_click_2_listener() {
      \u0275\u0275restoreView(_r23);
      const student_r20 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.printCertificates(student_r20));
    });
    \u0275\u0275text(3);
    \u0275\u0275pipe(4, "t");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "button", 34);
    \u0275\u0275listener("click", function GradeCertificatesComponent_Conditional_44_For_30_Conditional_8_Template_button_click_5_listener() {
      \u0275\u0275restoreView(_r23);
      const student_r20 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.exportCertificateImages(student_r20));
    });
    \u0275\u0275text(6);
    \u0275\u0275pipe(7, "t");
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(3);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(4, 3, "certificates.print"), " ");
    \u0275\u0275advance(2);
    \u0275\u0275property("disabled", ctx_r1.exporting());
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(7, 5, "certificates.image"), " ");
  }
}
function GradeCertificatesComponent_Conditional_44_For_30_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td");
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(3, GradeCertificatesComponent_Conditional_44_For_30_For_4_Template, 2, 3, "td", null, _forTrack0);
    \u0275\u0275conditionalCreate(5, GradeCertificatesComponent_Conditional_44_For_30_Conditional_5_Template, 3, 3, "td", 38);
    \u0275\u0275repeaterCreate(6, GradeCertificatesComponent_Conditional_44_For_30_For_7_Template, 2, 3, "td", null, _forTrack0);
    \u0275\u0275conditionalCreate(8, GradeCertificatesComponent_Conditional_44_For_30_Conditional_8_Template, 8, 7, "td");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const student_r20 = ctx.$implicit;
    const current_r15 = \u0275\u0275nextContext();
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(student_r20.studentName);
    \u0275\u0275advance();
    \u0275\u0275repeater(ctx_r1.totalSubjects(current_r15));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.isAdmin() ? 5 : -1);
    \u0275\u0275advance();
    \u0275\u0275repeater(ctx_r1.extraSubjects(current_r15));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.isAdmin() ? 8 : -1);
  }
}
function GradeCertificatesComponent_Conditional_44_ForEmpty_31_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 1);
    \u0275\u0275text(2);
    \u0275\u0275pipe(3, "t");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const current_r15 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275attribute("colspan", current_r15.subjects.length + 3);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(3, 2, "certificates.noStudents"));
  }
}
function GradeCertificatesComponent_Conditional_44_Template(rf, ctx) {
  if (rf & 1) {
    const _r13 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "section", 10)(1, "div", 5)(2, "div")(3, "h3");
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "p", 1);
    \u0275\u0275text(6);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(7, "div", 26);
    \u0275\u0275conditionalCreate(8, GradeCertificatesComponent_Conditional_44_Conditional_8_Template, 6, 8);
    \u0275\u0275elementStart(9, "button", 24);
    \u0275\u0275listener("click", function GradeCertificatesComponent_Conditional_44_Template_button_click_9_listener() {
      \u0275\u0275restoreView(_r13);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.closeSheet());
    });
    \u0275\u0275text(10);
    \u0275\u0275pipe(11, "t");
    \u0275\u0275elementEnd()()();
    \u0275\u0275conditionalCreate(12, GradeCertificatesComponent_Conditional_44_Conditional_12_Template, 3, 3, "p", 1)(13, GradeCertificatesComponent_Conditional_44_Conditional_13_Template, 3, 3, "p", 1)(14, GradeCertificatesComponent_Conditional_44_Conditional_14_Template, 3, 3, "p", 1);
    \u0275\u0275elementStart(15, "div", 7)(16, "table", 35)(17, "thead")(18, "tr")(19, "th");
    \u0275\u0275text(20);
    \u0275\u0275pipe(21, "t");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(22, GradeCertificatesComponent_Conditional_44_For_23_Template, 4, 2, "th", null, _forTrack0);
    \u0275\u0275conditionalCreate(24, GradeCertificatesComponent_Conditional_44_Conditional_24_Template, 5, 4, "th");
    \u0275\u0275repeaterCreate(25, GradeCertificatesComponent_Conditional_44_For_26_Template, 4, 2, "th", null, _forTrack0);
    \u0275\u0275conditionalCreate(27, GradeCertificatesComponent_Conditional_44_Conditional_27_Template, 3, 3, "th");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(28, "tbody");
    \u0275\u0275repeaterCreate(29, GradeCertificatesComponent_Conditional_44_For_30_Template, 9, 3, "tr", null, _forTrack2, false, GradeCertificatesComponent_Conditional_44_ForEmpty_31_Template, 4, 4, "tr");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(32, "div", 15)(33, "button", 36);
    \u0275\u0275listener("click", function GradeCertificatesComponent_Conditional_44_Template_button_click_33_listener() {
      \u0275\u0275restoreView(_r13);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.saveMarks());
    });
    \u0275\u0275text(34);
    \u0275\u0275pipe(35, "t");
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const current_r15 = ctx;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(current_r15.title);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate2("", current_r15.classroomName, " \xB7 ", ctx_r1.gradeLabel(current_r15.grade));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.isAdmin() ? 8 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(11, 12, "common.close"));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(!ctx_r1.isAdmin() ? 12 : current_r15.isApproved ? 13 : 14);
    \u0275\u0275advance(8);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(21, 14, "common.student"));
    \u0275\u0275advance(2);
    \u0275\u0275repeater(ctx_r1.totalSubjects(current_r15));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.isAdmin() ? 24 : -1);
    \u0275\u0275advance();
    \u0275\u0275repeater(ctx_r1.extraSubjects(current_r15));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.isAdmin() ? 27 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(current_r15.students);
    \u0275\u0275advance(4);
    \u0275\u0275property("disabled", ctx_r1.saving() || !current_r15.students.length);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind1(35, 16, "certificates.saveDegrees"), " ");
  }
}
var GradeCertificatesComponent = class _GradeCertificatesComponent {
  constructor() {
    this.api = inject(LearningApiService);
    this.locale = inject(LocaleService);
    this.auth = inject(AuthService);
    this.brand = inject(SiteBrandService);
    this.http = inject(HttpClient);
    this.isAdmin = computed(
      () => this.auth.hasAdminRights(),
      ...ngDevMode ? [{ debugName: "isAdmin" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.certificates = signal(
      [],
      ...ngDevMode ? [{ debugName: "certificates" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.classrooms = signal(
      [],
      ...ngDevMode ? [{ debugName: "classrooms" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.sheet = signal(
      null,
      ...ngDevMode ? [{ debugName: "sheet" }] : (
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
    this.saving = signal(
      false,
      ...ngDevMode ? [{ debugName: "saving" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.exporting = signal(
      false,
      ...ngDevMode ? [{ debugName: "exporting" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.approving = signal(
      false,
      ...ngDevMode ? [{ debugName: "approving" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.editingId = null;
    this.formTitle = "";
    this.formClassroomId = "";
    this.subjectDrafts = [];
    this.marks = {};
    this.classroomOptions = computed(
      () => this.classrooms().map((room) => ({ value: room.id, label: room.name })),
      ...ngDevMode ? [{ debugName: "classroomOptions" }] : (
        /* istanbul ignore next */
        []
      )
    );
    this.reload();
    if (this.isAdmin()) {
      this.api.getClassrooms().subscribe({
        next: (classrooms) => this.classrooms.set(classrooms ?? []),
        error: (err) => this.error.set(this.locale.fromApiError(err, "certificates.loadFailed"))
      });
    }
  }
  gradeLabel(grade) {
    if (grade == null)
      return this.locale.t("common.emDash");
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }
  reload() {
    this.api.listGradeCertificates().subscribe({
      next: (certificates) => this.certificates.set(certificates ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, "certificates.loadFailed"))
    });
  }
  // ---- Admin: define the certificate -------------------------------------------------
  onFormClassroomChange(classroomId) {
    this.formClassroomId = classroomId;
    this.subjectDrafts = this.draftsForClassroom(classroomId, []);
  }
  startEdit(certificate) {
    this.clearFeedback();
    this.api.getGradeCertificate(certificate.id).subscribe({
      next: (sheet) => {
        this.editingId = sheet.id;
        this.formTitle = sheet.title;
        this.formClassroomId = sheet.classroomId;
        this.subjectDrafts = this.draftsForClassroom(sheet.classroomId, sheet.subjects);
        document.getElementById("certificate-form")?.scrollIntoView({ behavior: "smooth", block: "start" });
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, "certificates.loadFailed"))
    });
  }
  cancelEdit() {
    this.editingId = null;
    this.formTitle = "";
    this.formClassroomId = "";
    this.subjectDrafts = [];
  }
  saveCertificate() {
    this.clearFeedback();
    const subjects = this.subjectDrafts.filter((draft) => draft.selected).map((draft) => ({
      courseId: draft.courseId,
      maxDegree: Number(draft.maxDegree),
      includedInTotal: draft.includedInTotal
    }));
    if (!this.formTitle.trim() || !this.formClassroomId || !subjects.length) {
      this.error.set(this.locale.t("certificates.required"));
      return;
    }
    if (subjects.some((subject) => !(subject.maxDegree > 0))) {
      this.error.set(this.locale.t("certificates.maxRequired"));
      return;
    }
    const payload = { classroomId: this.formClassroomId, title: this.formTitle.trim(), subjects };
    const request = this.editingId ? this.api.updateGradeCertificate(this.editingId, payload) : this.api.createGradeCertificate(payload);
    this.saving.set(true);
    request.subscribe({
      next: (sheet) => {
        this.saving.set(false);
        this.cancelEdit();
        this.message.set(this.locale.t("certificates.saved"));
        this.reload();
        this.showSheet(sheet);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.locale.fromApiError(err, "certificates.saveFailed"));
      }
    });
  }
  deleteCertificate(certificate) {
    if (!confirm(this.locale.t("certificates.confirmDelete", { title: certificate.title })))
      return;
    this.clearFeedback();
    this.api.deleteGradeCertificate(certificate.id).subscribe({
      next: () => {
        if (this.sheet()?.id === certificate.id)
          this.sheet.set(null);
        if (this.editingId === certificate.id)
          this.cancelEdit();
        this.message.set(this.locale.t("certificates.deleted"));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, "certificates.deleteFailed"))
    });
  }
  // ---- Admin: approval ----------------------------------------------------------------
  /** Approves one certificate; afterwards the student and their parent can see it. */
  approveCertificate(certificate) {
    if (this.approving())
      return;
    this.clearFeedback();
    this.approving.set(true);
    this.api.approveGradeCertificate(certificate.id).subscribe({
      next: () => {
        this.approving.set(false);
        this.message.set(this.locale.t("certificates.approved"));
        this.reload();
      },
      error: (err) => {
        this.approving.set(false);
        this.error.set(this.locale.fromApiError(err, "certificates.approveFailed"));
      }
    });
  }
  /** Approves every not-yet-approved certificate in one go. */
  approveAllCertificates() {
    if (this.approving())
      return;
    this.clearFeedback();
    this.approving.set(true);
    this.api.approveAllGradeCertificates().subscribe({
      next: (result) => {
        this.approving.set(false);
        this.message.set(this.locale.t("certificates.approvedAll", { count: result?.approvedCount ?? 0 }));
        this.reload();
      },
      error: (err) => {
        this.approving.set(false);
        this.error.set(this.locale.fromApiError(err, "certificates.approveFailed"));
      }
    });
  }
  /** Takes the approval back so the certificate disappears from the student and parent views. */
  revokeApproval(certificate) {
    if (this.approving())
      return;
    this.clearFeedback();
    this.approving.set(true);
    this.api.revokeGradeCertificateApproval(certificate.id).subscribe({
      next: () => {
        this.approving.set(false);
        this.message.set(this.locale.t("certificates.approvalRevoked"));
        this.reload();
      },
      error: (err) => {
        this.approving.set(false);
        this.error.set(this.locale.fromApiError(err, "certificates.approveFailed"));
      }
    });
  }
  approvedCount() {
    return this.certificates().filter((certificate) => certificate.isApproved).length;
  }
  pendingCount() {
    return this.certificates().filter((certificate) => !certificate.isApproved).length;
  }
  /** Classroom subjects as form rows; subjects already on the certificate keep their settings. */
  draftsForClassroom(classroomId, existing) {
    const room = this.classrooms().find((item) => item.id === classroomId);
    const byCourse = new Map(existing.map((subject) => [subject.courseId, subject]));
    const drafts = /* @__PURE__ */ new Map();
    for (const subject of existing) {
      drafts.set(subject.courseId, {
        courseId: subject.courseId,
        courseTitle: subject.courseTitle,
        selected: true,
        maxDegree: subject.maxDegree,
        includedInTotal: subject.includedInTotal
      });
    }
    for (const course of room?.courses ?? []) {
      if (!course.courseId || drafts.has(course.courseId))
        continue;
      drafts.set(course.courseId, {
        courseId: course.courseId,
        courseTitle: course.courseTitle,
        // A brand new certificate starts with every classroom subject ticked.
        selected: byCourse.size === 0,
        maxDegree: 20,
        includedInTotal: true
      });
    }
    return [...drafts.values()];
  }
  // ---- Degrees sheet -----------------------------------------------------------------
  openSheet(certificate) {
    this.clearFeedback();
    this.api.getGradeCertificate(certificate.id).subscribe({
      next: (sheet) => {
        this.showSheet(sheet);
        document.getElementById("certificate-sheet")?.scrollIntoView({ behavior: "smooth", block: "start" });
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, "certificates.loadFailed"))
    });
  }
  closeSheet() {
    this.sheet.set(null);
    this.marks = {};
  }
  saveMarks() {
    const sheet = this.sheet();
    if (!sheet)
      return;
    this.clearFeedback();
    const entries = [];
    for (const student of sheet.students) {
      for (const subject of sheet.subjects) {
        const degree = this.degreeOf(student.studentId, subject.id);
        if (degree != null && (degree < 0 || degree > subject.maxDegree)) {
          this.error.set(this.locale.t("certificates.degreeRange", {
            student: student.studentName,
            subject: subject.courseTitle,
            max: subject.maxDegree
          }));
          return;
        }
        entries.push({ subjectId: subject.id, studentId: student.studentId, degree });
      }
    }
    this.saving.set(true);
    this.api.saveGradeCertificateMarks(sheet.id, entries).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.showSheet(saved);
        this.message.set(this.locale.t("certificates.marksSaved"));
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.locale.fromApiError(err, "certificates.marksSaveFailed"));
      }
    });
  }
  /** Subjects that count toward the total come first, as on the printed certificate. */
  totalSubjects(sheet) {
    return sheet.subjects.filter((subject) => subject.includedInTotal);
  }
  extraSubjects(sheet) {
    return sheet.subjects.filter((subject) => !subject.includedInTotal);
  }
  maxTotal(sheet) {
    return this.totalSubjects(sheet).reduce((sum, subject) => sum + subject.maxDegree, 0);
  }
  /** Null until at least one counted subject has a degree. */
  studentTotal(sheet, studentId) {
    const degrees = this.totalSubjects(sheet).map((subject) => this.degreeOf(studentId, subject.id)).filter((degree) => degree != null);
    return degrees.length ? degrees.reduce((sum, degree) => sum + degree, 0) : null;
  }
  degreeOf(studentId, subjectId) {
    const value = this.marks[studentId]?.[subjectId];
    if (value == null || value === "")
      return null;
    const degree = Number(value);
    return Number.isFinite(degree) ? degree : null;
  }
  showSheet(sheet) {
    const marks = {};
    for (const student of sheet.students) {
      const row = {};
      for (const subject of sheet.subjects)
        row[subject.id] = null;
      for (const mark of student.marks)
        row[mark.subjectId] = mark.degree;
      marks[student.studentId] = row;
    }
    this.marks = marks;
    this.sheet.set(sheet);
  }
  clearFeedback() {
    this.message.set("");
    this.error.set("");
  }
  // ---- Printing and image export ----------------------------------------------------
  /** Prints one certificate per page: for one student, or for the whole classroom. */
  printCertificates(student) {
    return __async(this, null, function* () {
      const sheet = this.sheet();
      if (!sheet)
        return;
      const students = student ? [student] : sheet.students;
      if (!students.length)
        return;
      const brand = yield this.loadPrintBrand();
      const models = students.map((item) => this.printModel(sheet, item, brand));
      const printed = yield printCertificateModels(models, this.locale.lang() === "ar", sheet.title);
      if (!printed) {
        this.error.set(this.locale.t("certificates.popupBlocked"));
      }
    });
  }
  /** Downloads the certificate as a PNG image: for one student, or one image per student. */
  exportCertificateImages(student) {
    return __async(this, null, function* () {
      const sheet = this.sheet();
      if (!sheet || this.exporting())
        return;
      const students = student ? [student] : sheet.students;
      if (!students.length)
        return;
      this.clearFeedback();
      this.exporting.set(true);
      try {
        const brand = yield this.loadPrintBrand();
        const items = students.map((item) => ({
          model: this.printModel(sheet, item, brand),
          fileName: safeCertificateFileName(`${sheet.title} - ${item.studentName}`)
        }));
        yield exportCertificateModelImages(items, this.locale.lang() === "ar");
        this.message.set(this.locale.t("certificates.exported"));
      } catch (e) {
        this.error.set(this.locale.t("certificates.exportFailed"));
      } finally {
        this.exporting.set(false);
      }
    });
  }
  /** Resolves one student's certificate into the shared printable model. */
  printModel(sheet, student, brand) {
    const counted = this.totalSubjects(sheet).map((subject) => ({
      title: subject.courseTitle,
      maxDegree: subject.maxDegree,
      degree: this.degreeOf(student.studentId, subject.id)
    }));
    const extra = this.extraSubjects(sheet).map((subject) => ({
      title: subject.courseTitle,
      maxDegree: subject.maxDegree,
      degree: this.degreeOf(student.studentId, subject.id)
    }));
    const labels = {
      subject: this.locale.t("certificates.print.subject"),
      studentName: this.locale.t("certificates.print.studentName"),
      grade: this.locale.t("certificates.print.grade"),
      maxDegree: this.locale.t("certificates.print.maxDegree"),
      degree: this.locale.t("certificates.print.degree"),
      total: this.locale.t("certificates.total")
    };
    return {
      title: sheet.title,
      studentName: student.studentName,
      gradeLine: `${this.gradeLabel(sheet.grade)} \u2014 ${sheet.classroomName}`,
      brand,
      labels,
      counted,
      extra,
      maxTotal: certificateMaxTotal(counted),
      total: certificateTotal(counted)
    };
  }
  /**
  * Platform name and logo from the site settings, read fresh so the certificate never shows the
  * built-in default. The logo is inlined as a data URL so it is ready when the print dialog opens.
  */
  loadPrintBrand() {
    return __async(this, null, function* () {
      try {
        this.brand.apply(yield firstValueFrom(this.api.getSiteSettings()));
      } catch (e) {
      }
      const logoUrl = this.brand.logoUrl();
      let logo = null;
      if (logoUrl) {
        const absolute = new URL(logoUrl, window.location.origin).href;
        try {
          logo = yield blobToDataUrl(yield firstValueFrom(this.http.get(absolute, { responseType: "blob" })));
        } catch (e) {
          logo = absolute;
        }
      }
      return { name: this.brand.siteName(), logo };
    });
  }
  static {
    this.\u0275fac = function GradeCertificatesComponent_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _GradeCertificatesComponent)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _GradeCertificatesComponent, selectors: [["app-grade-certificates"]], decls: 45, vars: 35, consts: [[1, "panel-page"], [1, "meta"], [3, "ok", "error"], ["id", "certificate-form", 1, "block"], [1, "block"], [1, "sheet-head"], ["type", "button", 1, "ghost-btn", 3, "disabled"], [1, "table-wrap"], [1, "data-table"], [3, "is-open"], ["id", "certificate-sheet", 1, "block"], [1, "certificate-form", 3, "ngSubmit"], [1, "list-filters"], ["type", "text", "name", "formTitle", "maxlength", "200", 3, "ngModelChange", "ngModel", "placeholder"], ["name", "formClassroomId", 3, "ngModelChange", "ngModel", "disabled", "emptyLabel", "options"], [1, "form-actions"], ["type", "submit", 3, "disabled"], ["type", "button", 1, "ghost-btn"], [1, "data-table", "subject-table"], [1, "check"], ["type", "checkbox", 3, "ngModelChange", "ngModel", "name"], ["type", "number", "min", "1", "step", "0.5", 1, "degree-input", 3, "ngModelChange", "ngModel", "name", "disabled"], ["type", "checkbox", 3, "ngModelChange", "ngModel", "name", "disabled"], ["colspan", "3", 1, "meta"], ["type", "button", 1, "ghost-btn", 3, "click"], ["type", "button", 1, "ghost-btn", 3, "click", "disabled"], [1, "row-actions"], ["type", "button", 1, "ghost-btn", "small-btn", 3, "click"], [1, "approval-chip", "approved"], ["type", "button", 1, "ghost-btn", "small-btn", "approve-btn", 3, "disabled"], ["type", "button", 1, "ghost-btn", "small-btn", "approve-btn", 3, "click", "disabled"], ["type", "button", 1, "ghost-btn", "small-btn", 3, "disabled"], ["kind", "edit", 3, "action", "label"], ["kind", "delete", "variant", "danger", 3, "action", "label"], ["type", "button", 1, "ghost-btn", "small-btn", 3, "click", "disabled"], [1, "data-table", "sheet-table"], ["type", "button", 3, "click", "disabled"], [1, "max-chip"], [1, "total-cell"], ["type", "number", "min", "0", "step", "0.5", 1, "degree-input", 3, "ngModelChange", "max", "ngModel"]], template: function GradeCertificatesComponent_Template(rf, ctx) {
      if (rf & 1) {
        \u0275\u0275elementStart(0, "div", 0)(1, "h2");
        \u0275\u0275text(2);
        \u0275\u0275pipe(3, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(4, "p", 1);
        \u0275\u0275text(5);
        \u0275\u0275pipe(6, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275element(7, "app-page-feedback", 2);
        \u0275\u0275conditionalCreate(8, GradeCertificatesComponent_Conditional_8_Template, 22, 25, "section", 3);
        \u0275\u0275elementStart(9, "section", 4)(10, "div", 5)(11, "div")(12, "h3");
        \u0275\u0275text(13);
        \u0275\u0275pipe(14, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275conditionalCreate(15, GradeCertificatesComponent_Conditional_15_Template, 3, 7, "p", 1);
        \u0275\u0275elementEnd();
        \u0275\u0275conditionalCreate(16, GradeCertificatesComponent_Conditional_16_Template, 3, 4, "button", 6);
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(17, "div", 7)(18, "table", 8)(19, "thead")(20, "tr")(21, "th");
        \u0275\u0275text(22);
        \u0275\u0275pipe(23, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(24, "th");
        \u0275\u0275text(25);
        \u0275\u0275pipe(26, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(27, "th");
        \u0275\u0275text(28);
        \u0275\u0275pipe(29, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(30, "th");
        \u0275\u0275text(31);
        \u0275\u0275pipe(32, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275elementStart(33, "th");
        \u0275\u0275text(34);
        \u0275\u0275pipe(35, "t");
        \u0275\u0275elementEnd();
        \u0275\u0275conditionalCreate(36, GradeCertificatesComponent_Conditional_36_Template, 3, 3, "th");
        \u0275\u0275elementStart(37, "th");
        \u0275\u0275text(38);
        \u0275\u0275pipe(39, "t");
        \u0275\u0275elementEnd()()();
        \u0275\u0275elementStart(40, "tbody");
        \u0275\u0275repeaterCreate(41, GradeCertificatesComponent_For_42_Template, 19, 12, "tr", 9, _forTrack0, false, GradeCertificatesComponent_ForEmpty_43_Template, 4, 4, "tr");
        \u0275\u0275elementEnd()()()();
        \u0275\u0275conditionalCreate(44, GradeCertificatesComponent_Conditional_44_Template, 36, 18, "section", 10);
        \u0275\u0275elementEnd();
      }
      if (rf & 2) {
        let tmp_16_0;
        \u0275\u0275advance(2);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(3, 17, "certificates.title"));
        \u0275\u0275advance(3);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(6, 19, ctx.isAdmin() ? "certificates.subtitleAdmin" : "certificates.subtitleTeacher"));
        \u0275\u0275advance(2);
        \u0275\u0275property("ok", ctx.message())("error", ctx.error());
        \u0275\u0275advance();
        \u0275\u0275conditional(ctx.isAdmin() ? 8 : -1);
        \u0275\u0275advance(5);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(14, 21, "certificates.list"));
        \u0275\u0275advance(2);
        \u0275\u0275conditional(ctx.isAdmin() ? 15 : -1);
        \u0275\u0275advance();
        \u0275\u0275conditional(ctx.isAdmin() && ctx.pendingCount() > 0 ? 16 : -1);
        \u0275\u0275advance(6);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(23, 23, "certificates.name"));
        \u0275\u0275advance(3);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(26, 25, "common.classroom"));
        \u0275\u0275advance(3);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(29, 27, "common.grade"));
        \u0275\u0275advance(3);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(32, 29, "certificates.subjects"));
        \u0275\u0275advance(3);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(35, 31, "certificates.students"));
        \u0275\u0275advance(2);
        \u0275\u0275conditional(ctx.isAdmin() ? 36 : -1);
        \u0275\u0275advance(2);
        \u0275\u0275textInterpolate(\u0275\u0275pipeBind1(39, 33, "common.actions"));
        \u0275\u0275advance(3);
        \u0275\u0275repeater(ctx.certificates());
        \u0275\u0275advance(3);
        \u0275\u0275conditional((tmp_16_0 = ctx.sheet()) ? 44 : -1, tmp_16_0);
      }
    }, dependencies: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, \u0275NgNoValidate, DefaultValueAccessor, NumberValueAccessor, CheckboxControlValueAccessor, NgControlStatus, NgControlStatusGroup, MaxLengthValidator, MinValidator, MaxValidator, NgModel, NgForm, IconActionButtonComponent, TranslatePipe], styles: ['\n.page[_ngcontent-%COMP%] {\n  position: relative;\n  min-height: 100vh;\n  padding: var(--space-5) 6vw 4rem;\n  color: var(--text);\n  background:\n    radial-gradient(\n      circle at 88% 0%,\n      var(--page-glow-1),\n      transparent 28%),\n    radial-gradient(\n      circle at 8% 12%,\n      var(--page-glow-2),\n      transparent 22%),\n    var(--bg);\n}\n.page[_ngcontent-%COMP%]   p[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   span[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   small[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   label[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   li[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   td[_ngcontent-%COMP%], \n.page[_ngcontent-%COMP%]   th[_ngcontent-%COMP%] {\n  color: inherit;\n}\n.topbar[_ngcontent-%COMP%], \n.hero-strip[_ngcontent-%COMP%], \n.grid-two[_ngcontent-%COMP%], \n.grid-cards[_ngcontent-%COMP%], \n.chip-row[_ngcontent-%COMP%], \n.avatar-row[_ngcontent-%COMP%] {\n  display: flex;\n  gap: var(--space-3);\n}\n.topbar[_ngcontent-%COMP%] {\n  justify-content: space-between;\n  align-items: center;\n  margin-bottom: var(--space-5);\n}\n.brand[_ngcontent-%COMP%] {\n  margin: 0;\n  font-family: var(--font-display);\n  font-size: clamp(1.75rem, 3vw, 2.15rem);\n  font-weight: 800;\n  text-transform: uppercase;\n  color: var(--heading);\n  letter-spacing: 0.04em;\n}\nh1[_ngcontent-%COMP%], \nh2[_ngcontent-%COMP%], \nh3[_ngcontent-%COMP%], \nh4[_ngcontent-%COMP%] {\n  font-family: var(--font-display);\n  margin: 0.15rem 0;\n  color: var(--heading);\n  letter-spacing: 0.01em;\n  line-height: 1.15;\n}\nh1[_ngcontent-%COMP%] {\n  font-size: clamp(1.8rem, 3vw, 2.4rem);\n}\nh2[_ngcontent-%COMP%] {\n  font-size: clamp(1.4rem, 2.4vw, 1.85rem);\n}\nh3[_ngcontent-%COMP%] {\n  font-size: 1.2rem;\n}\n.hero-strip[_ngcontent-%COMP%] {\n  justify-content: space-between;\n  align-items: center;\n  gap: var(--space-4);\n  padding: 1.5rem 1.6rem;\n  border-radius: var(--radius-xl);\n  margin-bottom: var(--space-5);\n  background: var(--hero-bg);\n  border: 1px solid var(--hero-border);\n  box-shadow: var(--shadow-sm);\n  color: var(--hero-fg);\n}\n.hero-strip[_ngcontent-%COMP%]   p[_ngcontent-%COMP%], \n.hero-strip[_ngcontent-%COMP%]   h2[_ngcontent-%COMP%] {\n  color: var(--hero-fg);\n}\n.eyebrow[_ngcontent-%COMP%], \n.meta[_ngcontent-%COMP%], \n.back[_ngcontent-%COMP%] {\n  color: var(--text-muted);\n}\n.eyebrow[_ngcontent-%COMP%] {\n  margin: 0 0 0.35rem;\n  font-size: 0.78rem;\n  font-weight: 700;\n  letter-spacing: 0.08em;\n  text-transform: uppercase;\n  color: var(--teal);\n}\n.back[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.35rem;\n  margin-bottom: var(--space-3);\n  text-decoration: none;\n  font-weight: 600;\n  transition: color 0.15s ease;\n}\n.back[_ngcontent-%COMP%]:hover {\n  color: var(--heading);\n}\n.xp-pill[_ngcontent-%COMP%], \nbutton[_ngcontent-%COMP%], \n.chip[_ngcontent-%COMP%], \n.list-btn[_ngcontent-%COMP%], \n.avatar[_ngcontent-%COMP%], \n.badge[_ngcontent-%COMP%] {\n  border: none;\n  border-radius: var(--radius-pill);\n  font: inherit;\n}\n.xp-pill[_ngcontent-%COMP%], \nbutton[_ngcontent-%COMP%] {\n  padding: 0.8rem 1.15rem;\n  background:\n    linear-gradient(\n      135deg,\n      var(--accent),\n      var(--accent-hot));\n  color: var(--accent-ink);\n  font-weight: 800;\n  cursor: pointer;\n  transition:\n    transform 0.15s ease,\n    box-shadow 0.15s ease,\n    opacity 0.15s ease;\n  box-shadow: var(--btn-shadow);\n}\nbutton[_ngcontent-%COMP%]:hover:not(:disabled) {\n  transform: translateY(-1px);\n  box-shadow: var(--btn-shadow-hover);\n}\nbutton[_ngcontent-%COMP%]:active:not(:disabled) {\n  transform: translateY(0);\n}\nbutton[_ngcontent-%COMP%]:disabled {\n  opacity: 0.55;\n  cursor: not-allowed;\n  box-shadow: none;\n}\nbutton[_ngcontent-%COMP%]:focus-visible, \n.chip[_ngcontent-%COMP%]:focus-visible, \n.list-btn[_ngcontent-%COMP%]:focus-visible, \na[_ngcontent-%COMP%]:focus-visible, \ninput[_ngcontent-%COMP%]:focus-visible, \nselect[_ngcontent-%COMP%]:focus-visible, \ntextarea[_ngcontent-%COMP%]:focus-visible {\n  outline: none;\n  box-shadow: var(--focus-ring);\n}\nbutton.ghost[_ngcontent-%COMP%], \n.ghost-btn[_ngcontent-%COMP%] {\n  background: transparent;\n  color: var(--ghost-fg);\n  border: 1px solid var(--border-strong);\n  box-shadow: none;\n}\nbutton.ghost[_ngcontent-%COMP%]:hover:not(:disabled), \n.ghost-btn[_ngcontent-%COMP%]:hover:not(:disabled) {\n  background: var(--surface-strong);\n  box-shadow: none;\n}\n.grid-two[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: 1.3fr 0.9fr;\n  gap: var(--space-4);\n}\n.grid-cards[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));\n}\n.side-stack[_ngcontent-%COMP%] {\n  display: grid;\n  gap: var(--space-4);\n}\n.block[_ngcontent-%COMP%], \n.badge[_ngcontent-%COMP%], \n.avatar[_ngcontent-%COMP%] {\n  background: var(--surface);\n  border: 1px solid var(--border);\n  border-radius: var(--radius-lg);\n  padding: 1.25rem;\n  color: var(--text);\n}\n.block[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.85rem;\n  margin-bottom: var(--space-3);\n  box-shadow: var(--shadow-sm);\n  position: relative;\n  z-index: 0;\n}\n.block[_ngcontent-%COMP%]:has(app-searchable-select.ss--open), \n.block[_ngcontent-%COMP%]:has(app-searchable-multi-select.ms--open) {\n  z-index: 50;\n}\n.block[_ngcontent-%COMP%]    > h3[_ngcontent-%COMP%] {\n  padding-bottom: 0.55rem;\n  border-bottom: 1px solid var(--border);\n}\n.block[_ngcontent-%COMP%]   p[_ngcontent-%COMP%], \n.block[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.block[_ngcontent-%COMP%]   small[_ngcontent-%COMP%] {\n  color: var(--text);\n}\n.chip-row[_ngcontent-%COMP%], \n.avatar-row[_ngcontent-%COMP%] {\n  flex-wrap: wrap;\n}\n.chip[_ngcontent-%COMP%], \n.list-btn[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.4rem;\n  padding: 0.65rem 0.95rem;\n  background: var(--chip-bg);\n  border: 1px solid var(--chip-border);\n  color: var(--chip-fg);\n  text-decoration: none;\n  cursor: pointer;\n  transition:\n    background 0.15s ease,\n    border-color 0.15s ease,\n    transform 0.15s ease;\n}\n.chip[_ngcontent-%COMP%]:hover, \n.list-btn[_ngcontent-%COMP%]:hover {\n  background: var(--chip-bg);\n  border-color: var(--chip-border);\n  filter: brightness(0.97);\n  transform: translateY(-1px);\n}\n.chip.quiz[_ngcontent-%COMP%] {\n  background: rgba(95, 211, 188, 0.16);\n  border-color: rgba(95, 211, 188, 0.22);\n}\n.chip.video[_ngcontent-%COMP%] {\n  background: rgba(255, 214, 10, 0.16);\n  border-color: rgba(255, 214, 10, 0.28);\n}\n.list-btn[_ngcontent-%COMP%] {\n  width: 100%;\n  text-align: left;\n  margin-bottom: 0.45rem;\n  border-radius: var(--radius-md);\n}\n.list-btn.active[_ngcontent-%COMP%], \n.avatar.selected[_ngcontent-%COMP%], \n.badge.earned[_ngcontent-%COMP%] {\n  background:\n    linear-gradient(\n      135deg,\n      var(--accent),\n      var(--accent-hot));\n  border-color: transparent;\n  color: var(--accent-ink);\n}\n.avatar[_ngcontent-%COMP%] {\n  width: 9.5rem;\n  display: grid;\n  gap: 0.3rem;\n  text-align: left;\n  color: var(--text);\n  cursor: pointer;\n  transition: transform 0.15s ease, border-color 0.15s ease;\n}\n.avatar[_ngcontent-%COMP%]:hover:not(:disabled) {\n  transform: translateY(-2px);\n  border-color: var(--border-strong);\n}\n.avatar[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.avatar[_ngcontent-%COMP%]   small[_ngcontent-%COMP%], \n.badge[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.badge[_ngcontent-%COMP%]   small[_ngcontent-%COMP%] {\n  color: inherit;\n}\n.avatar[_ngcontent-%COMP%]:disabled {\n  opacity: 0.45;\n  cursor: not-allowed;\n}\n.emoji[_ngcontent-%COMP%] {\n  font-size: 2rem;\n}\ntextarea[_ngcontent-%COMP%], \ninput[type=radio][_ngcontent-%COMP%], \ninput[type=checkbox][_ngcontent-%COMP%] {\n  accent-color: var(--accent);\n}\ntextarea[_ngcontent-%COMP%], \ninput[type=text][_ngcontent-%COMP%], \ninput[type=email][_ngcontent-%COMP%], \ninput[type=password][_ngcontent-%COMP%], \ninput[type=number][_ngcontent-%COMP%], \ninput[type=datetime-local][_ngcontent-%COMP%], \ninput[type=file][_ngcontent-%COMP%], \nselect[_ngcontent-%COMP%] {\n  width: 100%;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.8rem 0.95rem;\n  font: inherit;\n  transition:\n    border-color 0.15s ease,\n    background 0.15s ease,\n    box-shadow 0.15s ease;\n}\ntextarea[_ngcontent-%COMP%] {\n  min-height: 9rem;\n  resize: vertical;\n  line-height: 1.45;\n}\ntextarea[_ngcontent-%COMP%]::placeholder, \ninput[_ngcontent-%COMP%]::placeholder {\n  color: var(--text-soft);\n}\ntextarea[_ngcontent-%COMP%]:hover, \ninput[_ngcontent-%COMP%]:hover, \nselect[_ngcontent-%COMP%]:hover {\n  border-color: var(--input-border-hover);\n}\ntextarea[_ngcontent-%COMP%]:focus, \ninput[_ngcontent-%COMP%]:focus, \nselect[_ngcontent-%COMP%]:focus {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  background: var(--input-bg-focus);\n  box-shadow: var(--focus-ring);\n}\nselect[_ngcontent-%COMP%]   option[_ngcontent-%COMP%] {\n  background: var(--bg-elevated);\n  color: var(--text);\n}\nlabel[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.4rem;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n  font-weight: 600;\n}\nlabel[_ngcontent-%COMP%]    > span[_ngcontent-%COMP%] {\n  color: var(--text-muted);\n}\n.feedback[_ngcontent-%COMP%] {\n  padding: 0.9rem 1rem;\n  border-radius: var(--radius-md);\n  background: var(--auth-error-bg);\n  border: 1px solid var(--auth-error-border);\n  color: var(--feedback-error-fg);\n}\n.feedback.ok[_ngcontent-%COMP%] {\n  background: rgba(81, 207, 102, 0.14);\n  border-color: rgba(125, 222, 160, 0.28);\n  color: var(--feedback-ok-fg);\n}\n[data-theme=light][_ngcontent-%COMP%]   .feedback.ok[_ngcontent-%COMP%] {\n  background: #f0fdf4;\n  border-color: #bbf7d0;\n}\n.question[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.5rem;\n  margin-bottom: var(--space-3);\n  padding: 1rem;\n  border-radius: var(--radius-md);\n  background: var(--elevated-bg);\n  border: 1px solid var(--border);\n  color: var(--text);\n}\n.prompt-html[_ngcontent-%COMP%] {\n  color: var(--prompt-fg);\n}\n.prompt-html[_ngcontent-%COMP%]   b[_ngcontent-%COMP%], \n.prompt-html[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%] {\n  font-weight: 800;\n}\n.table[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.35rem;\n}\n.table-row[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: 1.4fr repeat(4, 1fr);\n  gap: 0.5rem;\n  padding: 0.85rem 0.4rem;\n  border-bottom: 1px solid var(--border);\n  color: var(--text);\n  align-items: center;\n}\n.table-row.head[_ngcontent-%COMP%] {\n  color: var(--text-soft);\n  font-size: 0.82rem;\n  font-weight: 700;\n  letter-spacing: 0.04em;\n  text-transform: uppercase;\n  border-bottom-color: var(--border-strong);\n}\n@media (max-width: 900px) {\n  .grid-two[_ngcontent-%COMP%], \n   .table-row[_ngcontent-%COMP%] {\n    grid-template-columns: 1fr;\n  }\n}\n.panel-page[_ngcontent-%COMP%] {\n  display: grid;\n  gap: var(--space-4);\n  color: var(--text);\n  animation: _ngcontent-%COMP%_pageIn 0.35s ease;\n}\n.panel-page[_ngcontent-%COMP%]    > h2[_ngcontent-%COMP%] {\n  margin: 0;\n  color: var(--heading);\n}\n.panel-page[_ngcontent-%COMP%]    > .meta[_ngcontent-%COMP%] {\n  margin-top: -0.55rem;\n}\n.meeting-form[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));\n  gap: var(--space-3);\n  align-items: end;\n}\n.meeting-form[_ngcontent-%COMP%]   label[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   label[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.4rem;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n}\n.meeting-form[_ngcontent-%COMP%]   label.checkbox[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   label.checkbox[_ngcontent-%COMP%], \nlabel.checkbox[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 0.55rem;\n  padding: 0.7rem 0.85rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--surface);\n}\n.meeting-form[_ngcontent-%COMP%]   input[_ngcontent-%COMP%], \n.meeting-form[_ngcontent-%COMP%]   select[_ngcontent-%COMP%], \n.meeting-form[_ngcontent-%COMP%]   textarea[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   input[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   select[_ngcontent-%COMP%], \n.form-grid[_ngcontent-%COMP%]   textarea[_ngcontent-%COMP%] {\n  width: 100%;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.75rem 0.9rem;\n  font: inherit;\n}\n.meeting-row[_ngcontent-%COMP%] {\n  display: flex;\n  justify-content: space-between;\n  gap: var(--space-3);\n  align-items: center;\n  padding: 0.95rem 0.15rem;\n  border-bottom: 1px solid var(--border);\n  color: var(--text);\n}\n.meeting-row[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%], \n.meeting-row[_ngcontent-%COMP%]   .meta[_ngcontent-%COMP%] {\n  color: inherit;\n}\n.form-card[_ngcontent-%COMP%] {\n  display: grid;\n  gap: var(--space-3);\n  padding: 1.35rem;\n  border-radius: var(--radius-lg);\n  background: var(--surface);\n  border: 1px solid var(--border);\n  box-shadow: var(--shadow-sm);\n}\n.form-actions[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.75rem;\n  align-items: center;\n  padding-top: 0.35rem;\n}\n.stat-row[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));\n  gap: var(--space-3);\n}\n.stat-card[_ngcontent-%COMP%] {\n  padding: 1rem 1.1rem;\n  border-radius: var(--radius-md);\n  background: var(--surface-strong);\n  border: 1px solid var(--border);\n}\n.stat-card[_ngcontent-%COMP%]   strong[_ngcontent-%COMP%] {\n  display: block;\n  font-family: var(--font-display);\n  font-size: 1.55rem;\n  color: var(--stat-strong);\n}\n.stat-card[_ngcontent-%COMP%]   span[_ngcontent-%COMP%] {\n  color: var(--text-muted);\n  font-size: 0.85rem;\n}\nbutton.stat-card-link[_ngcontent-%COMP%] {\n  display: block;\n  width: 100%;\n  text-align: start;\n  font: inherit;\n  font-weight: inherit;\n  color: inherit;\n  cursor: pointer;\n  background: var(--surface-strong);\n  box-shadow: none;\n  transition:\n    transform 0.2s ease,\n    border-color 0.2s ease,\n    background 0.2s ease;\n}\nbutton.stat-card-link[_ngcontent-%COMP%]:hover, \nbutton.stat-card-link[_ngcontent-%COMP%]:focus-visible {\n  transform: translateY(-2px);\n  border-color: var(--border-strong);\n  box-shadow: none;\n  outline: none;\n}\nbutton.stat-card-link.active[_ngcontent-%COMP%] {\n  border-color: var(--border-strong);\n  box-shadow: 0 0 0 1px var(--border-strong);\n}\n@keyframes _ngcontent-%COMP%_pageIn {\n  from {\n    opacity: 0;\n    transform: translateY(6px);\n  }\n  to {\n    opacity: 1;\n    transform: translateY(0);\n  }\n}\n@media (max-width: 700px) {\n  .page[_ngcontent-%COMP%] {\n    padding: 1.35rem 1rem 3rem;\n  }\n  .meeting-row[_ngcontent-%COMP%] {\n    flex-direction: column;\n    align-items: flex-start;\n  }\n  .hero-strip[_ngcontent-%COMP%] {\n    flex-direction: column;\n    align-items: flex-start;\n  }\n}\n.student-side-tools[_ngcontent-%COMP%] {\n  position: fixed;\n  inset-inline-end: 0;\n  top: 38%;\n  z-index: 46;\n  display: grid;\n  gap: 0.55rem;\n  justify-items: end;\n}\n.student-side-tools[_ngcontent-%COMP%]   .side-tab[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.45rem;\n  padding: 0.7rem 0.75rem;\n  border: 1px solid var(--border-strong);\n  border-inline-end: none;\n  border-radius: var(--radius-md) 0 0 var(--radius-md);\n  background: var(--surface);\n  color: var(--heading);\n  text-decoration: none;\n  font-weight: 800;\n  box-shadow: var(--shadow-md);\n  max-width: 2.75rem;\n  overflow: hidden;\n  transition: max-width 0.2s ease;\n}\nhtml[dir=rtl][_ngcontent-%COMP%]   .student-side-tools[_ngcontent-%COMP%]   .side-tab[_ngcontent-%COMP%] {\n  border-radius: 0 var(--radius-md) var(--radius-md) 0;\n}\n.student-side-tools[_ngcontent-%COMP%]   .side-tab-icon[_ngcontent-%COMP%] {\n  flex-shrink: 0;\n  width: 1.2rem;\n  text-align: center;\n  font-size: 1.05rem;\n  line-height: 1;\n}\n.student-side-tools[_ngcontent-%COMP%]   .side-tab-label[_ngcontent-%COMP%] {\n  white-space: nowrap;\n  font-size: 0.88rem;\n}\n.student-side-tools[_ngcontent-%COMP%]   .side-tab[_ngcontent-%COMP%]:hover, \n.student-side-tools[_ngcontent-%COMP%]   .side-tab[_ngcontent-%COMP%]:focus-visible {\n  max-width: 12rem;\n}\n@media (max-width: 700px) {\n  .student-side-tools[_ngcontent-%COMP%] {\n    top: auto;\n    inset-block-end: 5.25rem;\n    z-index: 62;\n  }\n}\n\n\n.user-row[_ngcontent-%COMP%] {\n  grid-template-columns: 1.2fr 1.6fr 0.8fr 0.5fr 1fr;\n}\n.data-table[_ngcontent-%COMP%] {\n  width: 100%;\n  border-collapse: collapse;\n  font-size: 0.95rem;\n}\n.data-table[_ngcontent-%COMP%]   th[_ngcontent-%COMP%], \n.data-table[_ngcontent-%COMP%]   td[_ngcontent-%COMP%] {\n  padding: 0.8rem 0.7rem;\n  border-bottom: 1px solid var(--border);\n  text-align: left;\n  vertical-align: middle;\n}\n.data-table[_ngcontent-%COMP%]   td[_ngcontent-%COMP%]   .chip-row[_ngcontent-%COMP%] {\n  margin-top: 0.45rem;\n}\n.data-table[_ngcontent-%COMP%]   td[_ngcontent-%COMP%]   .name-cell[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 0.5rem;\n  min-width: 0;\n}\n.meeting-form[_ngcontent-%COMP%]   label[_ngcontent-%COMP%]   .chip-row[_ngcontent-%COMP%]   input[_ngcontent-%COMP%] {\n  flex: 1;\n  min-width: 0;\n}\n.wa-destination[_ngcontent-%COMP%] {\n  margin: 0;\n  padding: 0.85rem 1rem;\n  border: 1px solid var(--border);\n  border-radius: var(--radius-lg);\n  background: var(--surface);\n}\n.wa-destination[_ngcontent-%COMP%]   legend[_ngcontent-%COMP%] {\n  padding: 0 0.35rem;\n  font-weight: 800;\n  color: var(--heading);\n}\n.edit-panel[_ngcontent-%COMP%] {\n  border: 1px solid rgba(95, 211, 188, 0.28);\n  background:\n    linear-gradient(\n      180deg,\n      rgba(95, 211, 188, 0.08),\n      transparent 60%);\n}\n.classroom-edit-form[_ngcontent-%COMP%] {\n  grid-template-columns: repeat(3, minmax(0, 1fr));\n  align-items: start;\n}\n.student-edit-form[_ngcontent-%COMP%] {\n  grid-template-columns: repeat(4, minmax(0, 1fr));\n  align-items: start;\n}\n.student-edit-form[_ngcontent-%COMP%]   .form-actions[_ngcontent-%COMP%] {\n  grid-column: 1 / -1;\n  justify-content: flex-start;\n  padding-top: 0.25rem;\n}\n.classroom-edit-form[_ngcontent-%COMP%]   .span-3[_ngcontent-%COMP%] {\n  grid-column: 1 / -1;\n}\n.classroom-edit-form[_ngcontent-%COMP%]   .edit-form-hint[_ngcontent-%COMP%] {\n  margin: 0;\n  align-self: center;\n}\n.classroom-edit-form[_ngcontent-%COMP%]   .form-actions[_ngcontent-%COMP%] {\n  justify-content: flex-start;\n  padding-top: 0.25rem;\n}\n@media (max-width: 1100px) {\n  .student-edit-form[_ngcontent-%COMP%] {\n    grid-template-columns: repeat(2, minmax(0, 1fr));\n  }\n}\n@media (max-width: 960px) {\n  .classroom-edit-form[_ngcontent-%COMP%] {\n    grid-template-columns: 1fr;\n  }\n  .classroom-edit-form[_ngcontent-%COMP%]   .span-3[_ngcontent-%COMP%] {\n    grid-column: auto;\n  }\n  .student-edit-form[_ngcontent-%COMP%] {\n    grid-template-columns: 1fr;\n  }\n}\n.field-label[_ngcontent-%COMP%] {\n  margin: 0 0 0.25rem;\n  font-weight: 700;\n}\n.data-table[_ngcontent-%COMP%]   th[_ngcontent-%COMP%] {\n  color: var(--text-soft);\n  font-weight: 700;\n  font-size: 0.82rem;\n  letter-spacing: 0.04em;\n  text-transform: uppercase;\n  white-space: nowrap;\n}\n.data-table[_ngcontent-%COMP%]   th.sortable[_ngcontent-%COMP%] {\n  cursor: pointer;\n  -webkit-user-select: none;\n  user-select: none;\n}\n.data-table[_ngcontent-%COMP%]   th.sortable[_ngcontent-%COMP%]:hover {\n  color: var(--accent-fg);\n}\n.data-table[_ngcontent-%COMP%]   th[_ngcontent-%COMP%]   .sort-mark[_ngcontent-%COMP%] {\n  margin-left: 0.35rem;\n  opacity: 0.7;\n}\n.data-table[_ngcontent-%COMP%]   tr[_ngcontent-%COMP%]:hover   td[_ngcontent-%COMP%] {\n  background: var(--table-row-hover);\n}\n.data-table[_ngcontent-%COMP%]   tr.is-inactive[_ngcontent-%COMP%]   td[_ngcontent-%COMP%] {\n  opacity: 0.58;\n}\n.data-table[_ngcontent-%COMP%]   tr.total-row[_ngcontent-%COMP%]   td[_ngcontent-%COMP%] {\n  border-top: 2px solid var(--border);\n  background: var(--table-row-total);\n}\n.data-table[_ngcontent-%COMP%]   input[_ngcontent-%COMP%], \n.data-table[_ngcontent-%COMP%]   select[_ngcontent-%COMP%] {\n  width: 100%;\n  min-width: 7rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.5rem 0.6rem;\n  font: inherit;\n}\n.data-table[_ngcontent-%COMP%]   input[_ngcontent-%COMP%]:focus, \n.data-table[_ngcontent-%COMP%]   select[_ngcontent-%COMP%]:focus {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  box-shadow: var(--focus-ring);\n}\n.row-actions[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.4rem;\n}\n.row-actions[_ngcontent-%COMP%]   button[_ngcontent-%COMP%] {\n  padding: 0.45rem 0.75rem;\n  border-radius: var(--radius-pill);\n  font-size: 0.85rem;\n}\n.row-actions[_ngcontent-%COMP%]   app-icon-action-button[_ngcontent-%COMP%] {\n  display: inline-flex;\n}\n.row-actions[_ngcontent-%COMP%]   button.danger[_ngcontent-%COMP%] {\n  background: var(--danger-soft-bg);\n  color: var(--danger-soft-fg);\n  box-shadow: none;\n}\n.row-actions[_ngcontent-%COMP%]   button.ghost-btn[_ngcontent-%COMP%] {\n  background: transparent;\n  color: var(--ghost-fg);\n  border: 1px solid var(--border-strong);\n  box-shadow: none;\n}\n.table-wrap[_ngcontent-%COMP%] {\n  overflow-x: auto;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--surface);\n}\n.brand-preview[_ngcontent-%COMP%] {\n  display: block;\n  margin: 0.75rem 0 1rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--elevated-bg);\n  object-fit: contain;\n}\n.brand-preview.logo[_ngcontent-%COMP%] {\n  width: 96px;\n  height: 96px;\n  padding: 0.5rem;\n}\n.brand-preview.banner[_ngcontent-%COMP%] {\n  width: min(100%, 520px);\n  height: 160px;\n  object-fit: cover;\n}\n.login-preview[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: minmax(0, 0.9fr) minmax(0, 1.1fr);\n  gap: 0;\n  margin: 0.85rem 0 1rem;\n  min-height: 180px;\n  border-radius: var(--radius-md);\n  overflow: hidden;\n  border: 1px solid var(--border);\n  background: var(--surface);\n}\n.login-preview-form[_ngcontent-%COMP%], \n.login-preview-visual[_ngcontent-%COMP%] {\n  display: grid;\n  place-items: center;\n  padding: 1rem;\n  text-align: center;\n  font-size: 0.86rem;\n  color: var(--text-soft);\n}\n.login-preview-form[_ngcontent-%COMP%] {\n  border-right: 1px solid var(--border);\n  background: var(--elevated-bg);\n}\n.login-preview-visual[_ngcontent-%COMP%] {\n  position: relative;\n  padding: 0;\n  min-height: 180px;\n  background:\n    radial-gradient(\n      circle at 70% 30%,\n      rgba(255, 214, 10, 0.18),\n      transparent 40%),\n    linear-gradient(\n      145deg,\n      #07111f,\n      #145a8f);\n}\n.login-preview-visual[_ngcontent-%COMP%]   img[_ngcontent-%COMP%] {\n  position: absolute;\n  inset: 0;\n  width: 100%;\n  height: 100%;\n  object-fit: cover;\n}\n@media (max-width: 700px) {\n  .login-preview[_ngcontent-%COMP%] {\n    grid-template-columns: 1fr;\n  }\n  .login-preview-form[_ngcontent-%COMP%] {\n    border-right: none;\n    border-bottom: 1px solid var(--border);\n    min-height: 72px;\n  }\n}\n.form-actions[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.6rem;\n  align-items: center;\n}\n.file-btn[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n  padding: 0.55rem 1rem;\n  border-radius: var(--radius-pill);\n  border: 1px solid var(--border-strong);\n  background: var(--badge-ok-bg);\n  color: var(--badge-ok-fg);\n  font: inherit;\n  font-weight: 650;\n  cursor: pointer;\n}\n.file-btn[_ngcontent-%COMP%]:hover {\n  background: rgba(95, 211, 188, 0.26);\n}\n.ghost-btn[_ngcontent-%COMP%] {\n  background: transparent;\n  color: var(--ghost-fg);\n  border: 1px solid var(--border-strong);\n  box-shadow: none;\n}\n.ghost-btn[_ngcontent-%COMP%]:hover:not(:disabled) {\n  background: var(--surface-strong);\n  box-shadow: none;\n}\n.search-btn[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.45rem;\n  padding: 0.55rem 0.9rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid rgba(255, 214, 10, 0.55);\n  background: rgba(255, 214, 10, 0.22);\n  color: var(--text);\n  font: inherit;\n  font-weight: 700;\n  cursor: pointer;\n  box-shadow: none;\n}\n.search-btn[_ngcontent-%COMP%]   i[_ngcontent-%COMP%] {\n  font-size: 0.95em;\n  line-height: 1;\n}\n.search-btn[_ngcontent-%COMP%]:hover:not(:disabled) {\n  background: rgba(255, 214, 10, 0.32);\n  box-shadow: none;\n  transform: none;\n}\n.list-filters[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: end;\n  gap: 0.75rem 1rem;\n  margin: 0.75rem 0 1rem;\n  position: relative;\n  z-index: 2;\n}\n.list-filters[_ngcontent-%COMP%]   label[_ngcontent-%COMP%] {\n  display: flex;\n  flex-direction: column;\n  gap: 0.35rem;\n  min-width: 10rem;\n  color: var(--text-soft);\n  font-size: 0.85rem;\n  font-weight: 650;\n}\n.list-filters[_ngcontent-%COMP%]   select[_ngcontent-%COMP%], \n.list-filters[_ngcontent-%COMP%]   input[_ngcontent-%COMP%] {\n  min-width: 12rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.5rem 0.65rem;\n  font: inherit;\n}\n.list-filters[_ngcontent-%COMP%]   select[_ngcontent-%COMP%]:focus, \n.list-filters[_ngcontent-%COMP%]   input[_ngcontent-%COMP%]:focus {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  box-shadow: var(--focus-ring);\n}\n.list-filters[_ngcontent-%COMP%]   .meta[_ngcontent-%COMP%] {\n  margin: 0 0 0.35rem;\n  align-self: center;\n}\n.list-pager[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 0.75rem;\n  margin-top: 0.85rem;\n}\n.list-pager[_ngcontent-%COMP%]   .meta[_ngcontent-%COMP%] {\n  margin: 0;\n}\n.list-pager[_ngcontent-%COMP%]   button[_ngcontent-%COMP%]:disabled {\n  opacity: 0.45;\n  cursor: not-allowed;\n}\n.data-table[_ngcontent-%COMP%]   td[_ngcontent-%COMP%]   input[_ngcontent-%COMP%]    + input[_ngcontent-%COMP%] {\n  margin-top: 0.4rem;\n}\n.grade-multi[_ngcontent-%COMP%], \n.stage-multi[_ngcontent-%COMP%], \n.teachers-multi[_ngcontent-%COMP%] {\n  grid-column: 1 / -1;\n}\n.grade-multi[_ngcontent-%COMP%]   .meta[_ngcontent-%COMP%], \n.stage-multi[_ngcontent-%COMP%]   .meta[_ngcontent-%COMP%], \n.teachers-multi[_ngcontent-%COMP%]   .meta[_ngcontent-%COMP%] {\n  margin-top: 0.35rem;\n}\n.data-table[_ngcontent-%COMP%]   app-searchable-multi-select[_ngcontent-%COMP%] {\n  display: block;\n  min-width: 14rem;\n  max-width: 22rem;\n}\n.stage-multi[_ngcontent-%COMP%]   app-searchable-multi-select[_ngcontent-%COMP%] {\n  max-width: 36rem;\n}\n.course-teacher-block[_ngcontent-%COMP%] {\n  grid-column: 1 / -1;\n  display: flex;\n  flex-direction: column;\n  gap: 0.65rem;\n  padding: 0.85rem 0.9rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--elevated-bg);\n}\n.course-teacher-block.compact[_ngcontent-%COMP%] {\n  padding: 0.5rem;\n  gap: 0.5rem;\n}\n.course-teacher-head[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  justify-content: space-between;\n  gap: 0.5rem;\n}\n.course-teacher-row[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: 1.4fr 1fr auto;\n  gap: 0.6rem;\n  align-items: end;\n}\n.course-teacher-row[_ngcontent-%COMP%]   label[_ngcontent-%COMP%] {\n  display: flex;\n  flex-direction: column;\n  gap: 0.3rem;\n  min-width: 0;\n}\n.course-teacher-row[_ngcontent-%COMP%]   select[_ngcontent-%COMP%] {\n  width: 100%;\n  border-radius: var(--radius-sm);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.5rem 0.6rem;\n  font: inherit;\n}\n.course-teacher-block[_ngcontent-%COMP%]   .meta[_ngcontent-%COMP%] {\n  margin: 0;\n}\n@media (max-width: 700px) {\n  .course-teacher-row[_ngcontent-%COMP%] {\n    grid-template-columns: 1fr;\n  }\n}\n@media (max-width: 900px) {\n  .user-row[_ngcontent-%COMP%] {\n    grid-template-columns: 1fr;\n  }\n}\n.calendar-toolbar[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  justify-content: space-between;\n  gap: 0.75rem;\n  margin-bottom: 0.85rem;\n}\n.calendar-toolbar[_ngcontent-%COMP%]   h3[_ngcontent-%COMP%] {\n  margin: 0;\n}\n.calendar-nav[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 0.5rem;\n}\n.calendar-nav[_ngcontent-%COMP%]   .meta[_ngcontent-%COMP%] {\n  margin: 0;\n  margin-inline-start: 0.35rem;\n}\n.week-day-tabs[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.45rem;\n  margin-bottom: 0.85rem;\n}\n.week-day-tab[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.4rem;\n  padding: 0.45rem 0.75rem;\n  border-radius: var(--radius-pill);\n  border: 1px solid var(--border-strong);\n  background: var(--elevated-bg);\n  color: var(--text-soft);\n  font: inherit;\n  font-size: 0.85rem;\n  cursor: pointer;\n}\n.week-day-tab.active[_ngcontent-%COMP%] {\n  color: #07111f;\n  background: var(--accent);\n  border-color: transparent;\n  font-weight: 700;\n}\n.week-day-tab[_ngcontent-%COMP%]   .day-count[_ngcontent-%COMP%] {\n  min-width: 1.25rem;\n  height: 1.25rem;\n  padding: 0 0.35rem;\n  border-radius: 999px;\n  background: rgba(7, 17, 31, 0.18);\n  color: inherit;\n  font-size: 0.75rem;\n  display: inline-grid;\n  place-items: center;\n}\n.day-calendar[_ngcontent-%COMP%] {\n  display: grid;\n  grid-template-columns: 4rem minmax(0, 1fr);\n  gap: 0.35rem;\n  border: 1px solid var(--border);\n  border-radius: var(--radius-md);\n  background: var(--surface);\n  overflow: auto;\n  max-height: min(70vh, 42rem);\n}\n.day-hours[_ngcontent-%COMP%] {\n  border-right: 1px solid var(--border);\n  background: var(--elevated-bg);\n}\n.day-hour[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: flex-start;\n  justify-content: flex-end;\n  padding: 0.2rem 0.45rem 0 0;\n  color: var(--text-soft);\n  font-size: 0.75rem;\n  box-sizing: border-box;\n}\n.day-grid[_ngcontent-%COMP%] {\n  position: relative;\n  min-height: 12rem;\n}\n.day-grid-line[_ngcontent-%COMP%] {\n  position: absolute;\n  left: 0;\n  right: 0;\n  border-top: 1px dashed var(--border);\n  pointer-events: none;\n}\n.calendar-event[_ngcontent-%COMP%] {\n  position: absolute;\n  box-sizing: border-box;\n  display: flex;\n  flex-direction: column;\n  gap: 0.2rem;\n  padding: 0.45rem 0.5rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid rgba(255, 214, 10, 0.35);\n  background:\n    linear-gradient(\n      145deg,\n      rgba(255, 214, 10, 0.22),\n      rgba(20, 90, 143, 0.45));\n  color: #fff;\n  overflow: hidden;\n  z-index: 1;\n}\n.calendar-event-label[_ngcontent-%COMP%] {\n  font-weight: 700;\n  font-size: 0.92rem;\n  line-height: 1.3;\n  white-space: normal;\n  overflow: hidden;\n  display: -webkit-box;\n  -webkit-line-clamp: 3;\n  -webkit-box-orient: vertical;\n  word-break: break-word;\n}\n.calendar-event-time[_ngcontent-%COMP%] {\n  font-size: 0.78rem;\n  color: rgba(255, 255, 255, 0.82);\n  white-space: nowrap;\n}\n.calendar-event[_ngcontent-%COMP%]   .row-actions[_ngcontent-%COMP%] {\n  margin-top: auto;\n}\n.calendar-empty[_ngcontent-%COMP%] {\n  position: absolute;\n  inset: 0;\n  display: grid;\n  place-items: center;\n  margin: 0;\n  color: var(--text-soft);\n  font-size: 0.95rem;\n  pointer-events: none;\n}\n@media (max-width: 700px) {\n  .day-calendar[_ngcontent-%COMP%] {\n    grid-template-columns: 3rem minmax(0, 1fr);\n  }\n}\n.timetable-wrap[_ngcontent-%COMP%] {\n  overflow: auto;\n  max-height: min(80vh, 56rem);\n  border: 3px solid #e8c46a;\n  border-radius: 28px;\n  background:\n    radial-gradient(\n      circle at 8% 12%,\n      rgba(255, 214, 120, 0.35),\n      transparent 22%),\n    radial-gradient(\n      circle at 92% 10%,\n      rgba(168, 216, 255, 0.28),\n      transparent 24%),\n    radial-gradient(\n      circle at 10% 90%,\n      rgba(255, 182, 193, 0.25),\n      transparent 22%),\n    radial-gradient(\n      circle at 90% 88%,\n      rgba(186, 230, 180, 0.28),\n      transparent 24%),\n    linear-gradient(\n      180deg,\n      #fff8e8 0%,\n      #fffdf7 48%,\n      #f7fbff 100%);\n  direction: rtl;\n  position: relative;\n  z-index: 0;\n  isolation: isolate;\n  padding: 1.1rem 1rem 1.25rem;\n  box-shadow: 0 18px 40px rgba(120, 84, 20, 0.12);\n}\n.timetable-board[_ngcontent-%COMP%] {\n  font-family:\n    "Cairo",\n    "Baloo 2",\n    "Segoe UI",\n    Tahoma,\n    sans-serif;\n}\n.timetable-deco[_ngcontent-%COMP%] {\n  position: absolute;\n  font-size: 1.55rem;\n  line-height: 1;\n  opacity: 0.88;\n  pointer-events: none;\n  z-index: 1;\n  filter: drop-shadow(0 2px 2px rgba(0, 0, 0, 0.08));\n}\n.timetable-deco-tl[_ngcontent-%COMP%] {\n  top: 0.55rem;\n  inset-inline-start: 0.7rem;\n}\n.timetable-deco-tr[_ngcontent-%COMP%] {\n  top: 0.55rem;\n  inset-inline-end: 0.7rem;\n}\n.timetable-deco-bl[_ngcontent-%COMP%] {\n  bottom: 0.55rem;\n  inset-inline-start: 0.7rem;\n}\n.timetable-deco-br[_ngcontent-%COMP%] {\n  bottom: 0.55rem;\n  inset-inline-end: 0.7rem;\n}\n.timetable-banner[_ngcontent-%COMP%] {\n  position: relative;\n  z-index: 2;\n  display: grid;\n  place-items: center;\n  margin: 0.15rem auto 1rem;\n  width: min(28rem, 92%);\n  padding: 0.75rem 1.4rem 0.85rem;\n  border-radius: 18px;\n  border: 3px solid #d4a017;\n  background:\n    linear-gradient(\n      180deg,\n      #ffe9a8 0%,\n      #f6d06a 100%);\n  box-shadow: 0 8px 0 #c8961a, 0 14px 24px rgba(180, 120, 20, 0.18);\n  text-align: center;\n}\n.timetable-banner-crown[_ngcontent-%COMP%] {\n  position: absolute;\n  top: -0.85rem;\n  font-size: 1.35rem;\n  line-height: 1;\n}\n.timetable-banner-title[_ngcontent-%COMP%] {\n  margin: 0;\n  color: #1d3a75;\n  font-size: clamp(1.15rem, 2.4vw, 1.65rem);\n  font-weight: 800;\n  letter-spacing: 0.01em;\n}\n.timetable-motto[_ngcontent-%COMP%] {\n  position: relative;\n  z-index: 2;\n  display: flex;\n  align-items: center;\n  justify-content: center;\n  gap: 0.55rem;\n  margin: 1rem auto 0.15rem;\n  width: min(30rem, 94%);\n  padding: 0.55rem 1.1rem;\n  border-radius: 16px;\n  border: 2px solid #d4a017;\n  background:\n    linear-gradient(\n      180deg,\n      #fff1c2,\n      #f8d978);\n  color: #1d3a75;\n  font-weight: 800;\n  font-size: 1rem;\n  box-shadow: 0 4px 0 #c8961a;\n}\n.timetable-page[_ngcontent-%COMP%] {\n  direction: rtl;\n  text-align: right;\n}\n.timetable[_ngcontent-%COMP%] {\n  width: max-content;\n  border-collapse: separate;\n  border-spacing: 0.28rem;\n  font-size: 0.84rem;\n  font-family:\n    "Cairo",\n    "Baloo 2",\n    "Segoe UI",\n    Tahoma,\n    sans-serif;\n  color: #1f2a44;\n  direction: rtl;\n  position: relative;\n  z-index: 2;\n}\n.timetable[_ngcontent-%COMP%]   th[_ngcontent-%COMP%], \n.timetable[_ngcontent-%COMP%]   td[_ngcontent-%COMP%] {\n  border: none;\n  vertical-align: middle;\n}\n.timetable-day-col[_ngcontent-%COMP%], \n.timetable-day[_ngcontent-%COMP%] {\n  position: sticky;\n  right: 0;\n  left: auto;\n  inset-inline-start: auto;\n  inset-inline-end: 0;\n  z-index: 2;\n  min-width: 0;\n  width: 1%;\n  white-space: nowrap;\n  background:\n    linear-gradient(\n      180deg,\n      #ffe082,\n      #ffd54f) !important;\n  color: #1d3a75 !important;\n  text-align: center;\n  padding: 0.35rem 0.4rem;\n  font-weight: 800;\n  border-radius: 18px;\n  border: 2px solid #e0b13a !important;\n  box-shadow: 0 3px 0 rgba(196, 145, 30, 0.35);\n}\n.timetable-day[_ngcontent-%COMP%] {\n  text-align: center;\n}\n.timetable-day-icon[_ngcontent-%COMP%], \n.timetable-day-col-icon[_ngcontent-%COMP%] {\n  display: block;\n  font-size: 1.05rem;\n  line-height: 1;\n  margin-bottom: 0.2rem;\n}\n.timetable[_ngcontent-%COMP%]   thead[_ngcontent-%COMP%]   th[_ngcontent-%COMP%] {\n  position: sticky;\n  top: 0;\n  z-index: 3;\n  background:\n    linear-gradient(\n      180deg,\n      #ffe9a8,\n      #f0c75a) !important;\n  color: #1d3a75 !important;\n  font-size: 0.78rem;\n  letter-spacing: 0;\n  text-transform: none;\n  padding: 0.28rem 0.32rem;\n  text-align: center;\n  white-space: nowrap;\n  border-radius: 16px;\n  border: 2px solid #e0b13a !important;\n  box-shadow: 0 3px 0 rgba(196, 145, 30, 0.28);\n}\n.timetable[_ngcontent-%COMP%]   thead[_ngcontent-%COMP%]   .timetable-day-col[_ngcontent-%COMP%] {\n  z-index: 4;\n  top: 0;\n}\n.timetable-shift[_ngcontent-%COMP%] {\n  background:\n    linear-gradient(\n      180deg,\n      #fff3c4,\n      #f5d36a) !important;\n  color: #1d3a75 !important;\n  font-weight: 800;\n}\n.timetable-shift.pm[_ngcontent-%COMP%] {\n  background:\n    linear-gradient(\n      180deg,\n      #d8f5e8,\n      #9ed9bf) !important;\n  color: #14553a !important;\n  border-color: #7cbc9a !important;\n}\n.timetable-wrap.exporting[_ngcontent-%COMP%] {\n  overflow: visible !important;\n  max-height: none !important;\n  height: auto !important;\n}\n.timetable-wrap.exporting[_ngcontent-%COMP%]   .timetable[_ngcontent-%COMP%]   thead[_ngcontent-%COMP%]   th[_ngcontent-%COMP%], \n.timetable-wrap.exporting[_ngcontent-%COMP%]   .timetable-day-col[_ngcontent-%COMP%], \n.timetable-wrap.exporting[_ngcontent-%COMP%]   .timetable-day[_ngcontent-%COMP%] {\n  position: static !important;\n}\n.timetable-wrap.exporting[_ngcontent-%COMP%]   .row-actions[_ngcontent-%COMP%] {\n  display: none !important;\n}\n.timetable-export-bar[_ngcontent-%COMP%] {\n  display: flex;\n  justify-content: flex-start;\n  margin-bottom: 0.75rem;\n}\n.timetable-period[_ngcontent-%COMP%] {\n  min-width: 0;\n  width: 1%;\n  white-space: nowrap;\n}\n.timetable-session-id[_ngcontent-%COMP%] {\n  font-weight: 800;\n  font-size: 0.92rem;\n  color: #1d3a75;\n}\n.timetable-session-time[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n  gap: 0.25rem;\n  font-size: 0.78rem;\n  font-weight: 700;\n  letter-spacing: 0.01em;\n  text-transform: none;\n  color: #4a3b12;\n  margin-top: 0.25rem;\n  font-variant-numeric: tabular-nums;\n  direction: ltr;\n  unicode-bidi: isolate;\n}\n.timetable-clock[_ngcontent-%COMP%] {\n  font-size: 0.85rem;\n  line-height: 1;\n}\n.timetable-cell[_ngcontent-%COMP%] {\n  width: 1%;\n  min-width: 0;\n  max-width: none;\n  height: auto;\n  min-height: 0;\n  padding: 0.15rem;\n  background: rgba(255, 255, 255, 0.55);\n  vertical-align: middle;\n  overflow: visible;\n  cursor: cell;\n  transition:\n    transform 0.15s ease,\n    box-shadow 0.15s ease,\n    outline 0.15s ease;\n  border-radius: 12px;\n  border: 2px dashed rgba(180, 150, 90, 0.35) !important;\n}\n.timetable-cell.pm[_ngcontent-%COMP%] {\n  background: rgba(232, 255, 246, 0.55);\n}\n.timetable-cell.empty[_ngcontent-%COMP%] {\n  background: rgba(255, 255, 255, 0.42);\n}\n.timetable-empty[_ngcontent-%COMP%] {\n  display: grid;\n  place-items: center;\n  min-height: 0;\n  padding: 0.1rem 0.2rem;\n  line-height: 1;\n  color: #9aa3b5;\n  font-size: 0.9rem;\n  font-weight: 700;\n}\n.timetable-cell.drag-over[_ngcontent-%COMP%] {\n  outline: 3px dashed #1d3a75;\n  background: rgba(255, 214, 10, 0.22);\n  transform: scale(1.01);\n}\n.timetable-entry[_ngcontent-%COMP%] {\n  display: flex;\n  flex-direction: column;\n  align-items: center;\n  width: max-content;\n  min-width: max-content;\n  max-width: none;\n  gap: 0.08rem;\n  padding: 0.28rem 0.35rem;\n  border-radius: 10px;\n  border: 2px solid transparent;\n  margin: 0 auto;\n  overflow: visible;\n  cursor: grab;\n  text-align: center;\n  box-shadow: 0 3px 0 rgba(0, 0, 0, 0.06);\n  transition: transform 0.15s ease, box-shadow 0.15s ease;\n}\n.timetable-entry[_ngcontent-%COMP%]    + .timetable-entry[_ngcontent-%COMP%] {\n  margin-top: 0.18rem;\n}\n.timetable-entry[_ngcontent-%COMP%]:hover {\n  transform: translateY(-1px);\n  box-shadow: 0 6px 0 rgba(0, 0, 0, 0.08);\n}\n.timetable-entry[_ngcontent-%COMP%]:active {\n  cursor: grabbing;\n}\n.timetable-entry[_ngcontent-%COMP%]   .row-actions[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: nowrap;\n  flex-shrink: 0;\n  gap: 0.25rem;\n  margin-top: 0.2rem;\n  cursor: default;\n  justify-content: center;\n}\n.timetable-entry[_ngcontent-%COMP%]   .row-actions[_ngcontent-%COMP%]   app-icon-action-button[_ngcontent-%COMP%] {\n  display: inline-flex;\n  flex-shrink: 0;\n}\n.timetable-subject-icon[_ngcontent-%COMP%] {\n  font-size: 1.05rem;\n  line-height: 1;\n}\n.timetable-course[_ngcontent-%COMP%] {\n  font-weight: 800;\n  line-height: 1.2;\n  white-space: nowrap;\n  overflow: visible;\n  color: #1f2a44;\n  font-size: 0.8rem;\n}\n.timetable-teacher[_ngcontent-%COMP%] {\n  font-size: 0.7rem;\n  color: #4b5568;\n  white-space: nowrap;\n  overflow: visible;\n  font-weight: 600;\n}\n.timetable-entry[data-tone="0"][_ngcontent-%COMP%] {\n  background: #ffe0e8;\n  border-color: #f5a3b8;\n}\n.timetable-entry[data-tone="1"][_ngcontent-%COMP%] {\n  background: #e8d8ff;\n  border-color: #c4a6f5;\n}\n.timetable-entry[data-tone="2"][_ngcontent-%COMP%] {\n  background: #d8ecff;\n  border-color: #9ec4f0;\n}\n.timetable-entry[data-tone="3"][_ngcontent-%COMP%] {\n  background: #fff0b8;\n  border-color: #e6c85a;\n}\n.timetable-entry[data-tone="4"][_ngcontent-%COMP%] {\n  background: #d8f5e8;\n  border-color: #8fd0b0;\n}\n.timetable-entry[data-tone="5"][_ngcontent-%COMP%] {\n  background: #ffe6cc;\n  border-color: #f0b878;\n}\n.timetable-entry[data-tone="6"][_ngcontent-%COMP%] {\n  background: #e0f0ff;\n  border-color: #9ebfe8;\n}\n.timetable-entry[data-tone="7"][_ngcontent-%COMP%] {\n  background: #f3e0ff;\n  border-color: #d0a8ef;\n}\n.modal-backdrop[_ngcontent-%COMP%] {\n  position: fixed;\n  inset: 0;\n  z-index: 80;\n  display: grid;\n  place-items: center;\n  padding: 1rem;\n  background: rgba(4, 10, 22, 0.72);\n  -webkit-backdrop-filter: blur(2px);\n  backdrop-filter: blur(2px);\n}\n.modal-card[_ngcontent-%COMP%] {\n  width: min(32rem, 100%);\n  max-height: min(90vh, 44rem);\n  overflow: auto;\n  padding: 1.1rem 1.2rem 1.25rem;\n  border-radius: var(--radius);\n  border: 1px solid var(--border-strong);\n  background: var(--modal-bg);\n  box-shadow: var(--modal-shadow);\n}\n.modal-card.modal-card--wide[_ngcontent-%COMP%] {\n  width: min(60rem, 100%);\n}\n.page-actions[_ngcontent-%COMP%] {\n  display: flex;\n  justify-content: flex-end;\n  margin-bottom: 0.75rem;\n}\n.modal-head[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  justify-content: space-between;\n  gap: 0.75rem;\n  margin-bottom: 0.35rem;\n}\n.modal-head[_ngcontent-%COMP%]   h3[_ngcontent-%COMP%] {\n  margin: 0;\n  font-size: 1.15rem;\n}\n.course-tree[_ngcontent-%COMP%] {\n  display: flex;\n  flex-direction: column;\n  gap: 0.85rem;\n}\n.tree-unit[_ngcontent-%COMP%] {\n  border: 1px solid var(--border);\n  border-radius: 12px;\n  padding: 0.75rem 0.9rem;\n  background: var(--surface);\n}\n.tree-unit-head[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 0.6rem;\n  flex-wrap: wrap;\n}\n.tree-toggle[_ngcontent-%COMP%] {\n  min-width: 2rem;\n  padding: 0.2rem 0.4rem;\n}\n.tree-unit-title[_ngcontent-%COMP%], \n.tree-lesson-title[_ngcontent-%COMP%] {\n  flex: 1;\n  min-width: 12rem;\n}\n.ask-toggle[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.35rem;\n  font-size: 0.82rem;\n  white-space: nowrap;\n  color: var(--text-soft);\n}\n.tree-unit-body[_ngcontent-%COMP%] {\n  margin-top: 0.75rem;\n  margin-inline-start: 1.6rem;\n  display: flex;\n  flex-direction: column;\n  gap: 0.55rem;\n}\n.tree-lesson[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 0.6rem;\n  flex-wrap: wrap;\n  padding: 0.45rem 0.55rem;\n  border-radius: 8px;\n  background: var(--elevated-bg);\n}\n.tree-inline-form[_ngcontent-%COMP%], \n.tree-add-lesson[_ngcontent-%COMP%] {\n  width: 100%;\n}\n.course-tree-ai[_ngcontent-%COMP%] {\n  border: 1px solid var(--border);\n  border-radius: var(--radius-md);\n  padding: 1rem 1.1rem;\n  background: var(--surface);\n}\n.course-tree-prompt-field[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.45rem;\n  margin: 0.75rem 0 0.35rem;\n  width: 100%;\n  max-width: 42rem;\n}\n.course-tree-prompt-field[_ngcontent-%COMP%]    > span[_ngcontent-%COMP%]:first-child {\n  font-weight: 650;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n}\n.course-tree-prompt-field[_ngcontent-%COMP%]   textarea[_ngcontent-%COMP%] {\n  width: 100%;\n  min-height: 5.5rem;\n  resize: vertical;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.75rem 0.9rem;\n  font: inherit;\n  line-height: 1.45;\n}\n.course-tree-ai-actions[_ngcontent-%COMP%] {\n  margin-top: 0.35rem;\n}\n.course-tree-ai-preview[_ngcontent-%COMP%] {\n  margin-top: 0.85rem;\n  padding-top: 0.85rem;\n  border-top: 1px dashed var(--border);\n}\n.login-stat[_ngcontent-%COMP%] {\n  border-top: 3px solid transparent;\n}\n.login-stat-teachers[_ngcontent-%COMP%] {\n  border-top-color: #5fd3bc;\n}\n.login-stat-parents[_ngcontent-%COMP%] {\n  border-top-color: #ffd60a;\n}\n.login-stat-students[_ngcontent-%COMP%] {\n  border-top-color: #4ea8de;\n}\n.login-chart-legend[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.85rem 1.25rem;\n  margin: 0.75rem 0 1rem;\n}\n.login-legend-item[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.45rem;\n  color: var(--text-soft);\n  font-size: 0.88rem;\n}\n.login-swatch[_ngcontent-%COMP%] {\n  display: inline-block;\n  width: 0.75rem;\n  height: 0.75rem;\n  border-radius: 3px;\n}\n.login-swatch-teachers[_ngcontent-%COMP%], \n.login-bar-teachers[_ngcontent-%COMP%] {\n  background: #5fd3bc;\n}\n.login-swatch-parents[_ngcontent-%COMP%], \n.login-bar-parents[_ngcontent-%COMP%] {\n  background: #ffd60a;\n}\n.login-swatch-students[_ngcontent-%COMP%], \n.login-bar-students[_ngcontent-%COMP%] {\n  background: #4ea8de;\n}\n.login-chart[_ngcontent-%COMP%] {\n  padding: 1rem 0.75rem 0.5rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--surface);\n  overflow-x: auto;\n}\n.login-chart-bars[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: flex-end;\n  gap: 0.35rem;\n  min-width: 100%;\n  min-height: 220px;\n}\n.login-chart-day[_ngcontent-%COMP%] {\n  flex: 1 1 0;\n  min-width: 1.35rem;\n  display: flex;\n  flex-direction: column;\n  align-items: center;\n  gap: 0.4rem;\n}\n.login-chart-group[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: flex-end;\n  justify-content: center;\n  gap: 2px;\n  width: 100%;\n  height: 180px;\n}\n.login-chart-bar[_ngcontent-%COMP%] {\n  flex: 1 1 0;\n  min-width: 3px;\n  border-radius: 4px 4px 0 0;\n}\n.login-chart-label[_ngcontent-%COMP%] {\n  color: var(--text-muted);\n  font-size: 0.72rem;\n  line-height: 1;\n}\n.login-chart-label-spacer[_ngcontent-%COMP%] {\n  visibility: hidden;\n}\n.status-badge[_ngcontent-%COMP%] {\n  display: inline-flex;\n  align-items: center;\n  padding: 0.15rem 0.55rem;\n  border-radius: var(--radius-pill);\n  font-size: 0.78rem;\n  font-weight: 700;\n  line-height: 1.2;\n  border: 1px solid transparent;\n}\n.status-badge.published[_ngcontent-%COMP%] {\n  color: var(--badge-ok-fg);\n  background: var(--badge-ok-bg);\n  border-color: var(--badge-ok-border);\n}\n.status-badge.draft[_ngcontent-%COMP%] {\n  color: var(--badge-warn-fg);\n  background: var(--badge-warn-bg);\n  border-color: var(--badge-warn-border);\n}\nlabel.checkbox[_ngcontent-%COMP%] {\n  flex-direction: row;\n  align-items: center;\n  gap: 0.45rem;\n}\n.delete-options[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.6rem;\n  margin: 0.75rem 0 1rem;\n}\n.delete-option[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: flex-start;\n  gap: 0.65rem;\n  padding: 0.7rem 0.85rem;\n  border: 1px solid var(--border);\n  border-radius: var(--radius-md);\n  cursor: pointer;\n}\n.delete-option[_ngcontent-%COMP%]:has(input:checked) {\n  border-color: var(--border-strong);\n  background: var(--surface);\n}\n.delete-option[_ngcontent-%COMP%]   input[_ngcontent-%COMP%] {\n  margin-top: 0.25rem;\n  flex-shrink: 0;\n}\n.delete-option[_ngcontent-%COMP%]    > span[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 0.2rem;\n}\n/*# sourceMappingURL=admin-panel.css.map */', "\n.certificate-form[_ngcontent-%COMP%] {\n  display: grid;\n  gap: 1rem;\n}\n.check[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n  gap: 0.5rem;\n}\n.degree-input[_ngcontent-%COMP%] {\n  width: 5.5rem;\n  text-align: center;\n}\n.approval-chip[_ngcontent-%COMP%] {\n  display: inline-block;\n  padding: 0.15rem 0.6rem;\n  border-radius: 999px;\n  font-size: 0.85rem;\n  font-weight: 700;\n  white-space: nowrap;\n}\n.approval-chip.approved[_ngcontent-%COMP%] {\n  background: #e7f5df;\n  color: #3a7d1e;\n  border: 1px solid #b5d99c;\n}\n.approve-btn[_ngcontent-%COMP%] {\n  border-color: #3a7d1e;\n  color: #3a7d1e;\n}\n.sheet-head[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: flex-start;\n  justify-content: space-between;\n  gap: 0.75rem;\n  margin-bottom: 0.5rem;\n}\n.sheet-head[_ngcontent-%COMP%]   h3[_ngcontent-%COMP%] {\n  margin: 0;\n}\n.row-actions[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 0.4rem;\n}\n.form-actions[_ngcontent-%COMP%] {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.6rem;\n  margin-top: 0.9rem;\n}\n.max-chip[_ngcontent-%COMP%] {\n  display: inline-block;\n  margin-inline-start: 0.35rem;\n  padding: 0.05rem 0.45rem;\n  border-radius: 999px;\n  font-size: 0.78rem;\n  border: 1px solid var(--border-strong);\n  color: var(--text-muted);\n}\n.total-cell[_ngcontent-%COMP%] {\n  font-weight: 800;\n}\ntr.is-open[_ngcontent-%COMP%]   td[_ngcontent-%COMP%] {\n  background: var(--surface-strong);\n}\n/*# sourceMappingURL=grade-certificates.component.css.map */"] });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(GradeCertificatesComponent, [{
    type: Component,
    args: [{ selector: "app-grade-certificates", imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, TranslatePipe, IconActionButtonComponent], template: `<div class="panel-page">\r
  <h2>{{ 'certificates.title' | t }}</h2>\r
  <p class="meta">{{ (isAdmin() ? 'certificates.subtitleAdmin' : 'certificates.subtitleTeacher') | t }}</p>\r
\r
  <app-page-feedback [ok]="message()" [error]="error()" />\r
\r
  @if (isAdmin()) {\r
    <section class="block" id="certificate-form">\r
      <h3>{{ (editingId ? 'certificates.editTitle' : 'certificates.createTitle') | t }}</h3>\r
      <form class="certificate-form" (ngSubmit)="saveCertificate()">\r
        <div class="list-filters">\r
          <label>\r
            {{ 'certificates.name' | t }}\r
            <input\r
              type="text"\r
              name="formTitle"\r
              maxlength="200"\r
              [(ngModel)]="formTitle"\r
              [placeholder]="'certificates.namePlaceholder' | t"\r
            />\r
          </label>\r
          <label>\r
            {{ 'common.classroom' | t }}\r
            <app-searchable-select\r
              name="formClassroomId"\r
              [ngModel]="formClassroomId"\r
              (ngModelChange)="onFormClassroomChange($event)"\r
              [disabled]="!!editingId"\r
              [emptyLabel]="'certificates.chooseClassroom' | t"\r
              [options]="classroomOptions()"\r
            />\r
          </label>\r
        </div>\r
\r
        @if (formClassroomId) {\r
          <div class="table-wrap">\r
            <table class="data-table subject-table">\r
              <thead>\r
                <tr>\r
                  <th>{{ 'certificates.subject' | t }}</th>\r
                  <th>{{ 'certificates.maxDegree' | t }}</th>\r
                  <th>{{ 'certificates.includedInTotal' | t }}</th>\r
                </tr>\r
              </thead>\r
              <tbody>\r
                @for (draft of subjectDrafts; track draft.courseId; let i = $index) {\r
                  <tr>\r
                    <td>\r
                      <label class="check">\r
                        <input type="checkbox" [(ngModel)]="draft.selected" [name]="'subject-selected-' + i" />\r
                        <span>{{ draft.courseTitle }}</span>\r
                      </label>\r
                    </td>\r
                    <td>\r
                      <input\r
                        type="number"\r
                        min="1"\r
                        step="0.5"\r
                        class="degree-input"\r
                        [(ngModel)]="draft.maxDegree"\r
                        [name]="'subject-max-' + i"\r
                        [disabled]="!draft.selected"\r
                      />\r
                    </td>\r
                    <td>\r
                      <input\r
                        type="checkbox"\r
                        [(ngModel)]="draft.includedInTotal"\r
                        [name]="'subject-total-' + i"\r
                        [disabled]="!draft.selected"\r
                      />\r
                    </td>\r
                  </tr>\r
                } @empty {\r
                  <tr>\r
                    <td colspan="3" class="meta">{{ 'certificates.noSubjects' | t }}</td>\r
                  </tr>\r
                }\r
              </tbody>\r
            </table>\r
          </div>\r
        }\r
\r
        <div class="form-actions">\r
          <button type="submit" [disabled]="saving()">\r
            {{ (editingId ? 'common.save' : 'certificates.create') | t }}\r
          </button>\r
          @if (editingId) {\r
            <button type="button" class="ghost-btn" (click)="cancelEdit()">{{ 'common.cancel' | t }}</button>\r
          }\r
        </div>\r
      </form>\r
    </section>\r
  }\r
\r
  <section class="block">\r
    <div class="sheet-head">\r
      <div>\r
        <h3>{{ 'certificates.list' | t }}</h3>\r
        @if (isAdmin()) {\r
          <p class="meta">\r
            {{ 'certificates.approvalSummary' | t:{ approved: approvedCount(), pending: pendingCount() } }}\r
          </p>\r
        }\r
      </div>\r
      @if (isAdmin() && pendingCount() > 0) {\r
        <button type="button" class="ghost-btn" [disabled]="approving()" (click)="approveAllCertificates()">\r
          {{ (approving() ? 'certificates.approving' : 'certificates.approveAll') | t }}\r
        </button>\r
      }\r
    </div>\r
    <div class="table-wrap">\r
      <table class="data-table">\r
        <thead>\r
          <tr>\r
            <th>{{ 'certificates.name' | t }}</th>\r
            <th>{{ 'common.classroom' | t }}</th>\r
            <th>{{ 'common.grade' | t }}</th>\r
            <th>{{ 'certificates.subjects' | t }}</th>\r
            <th>{{ 'certificates.students' | t }}</th>\r
            @if (isAdmin()) {\r
              <th>{{ 'certificates.approval' | t }}</th>\r
            }\r
            <th>{{ 'common.actions' | t }}</th>\r
          </tr>\r
        </thead>\r
        <tbody>\r
          @for (certificate of certificates(); track certificate.id) {\r
            <tr [class.is-open]="sheet()?.id === certificate.id">\r
              <td>\r
                <strong>{{ certificate.title }}</strong>\r
              </td>\r
              <td>{{ certificate.classroomName }}</td>\r
              <td>{{ gradeLabel(certificate.grade) }}</td>\r
              <td>{{ certificate.subjectCount }}</td>\r
              <td>{{ certificate.studentCount }}</td>\r
              @if (isAdmin()) {\r
                <td>\r
                  @if (certificate.isApproved) {\r
                    <span class="approval-chip approved">\u2713 {{ 'certificates.approvedChip' | t }}</span>\r
                  } @else {\r
                    <button\r
                      type="button"\r
                      class="ghost-btn small-btn approve-btn"\r
                      [disabled]="approving()"\r
                      (click)="approveCertificate(certificate)"\r
                    >\r
                      {{ 'certificates.approve' | t }}\r
                    </button>\r
                  }\r
                </td>\r
              }\r
              <td>\r
                <div class="row-actions">\r
                  <button type="button" class="ghost-btn small-btn" (click)="openSheet(certificate)">\r
                    {{ 'certificates.enterDegrees' | t }}\r
                  </button>\r
                  @if (isAdmin()) {\r
                    @if (certificate.isApproved) {\r
                      <button\r
                        type="button"\r
                        class="ghost-btn small-btn"\r
                        [disabled]="approving()"\r
                        (click)="revokeApproval(certificate)"\r
                      >\r
                        {{ 'certificates.revokeApproval' | t }}\r
                      </button>\r
                    }\r
                    <app-icon-action-button kind="edit" [label]="'common.edit' | t" (action)="startEdit(certificate)" />\r
                    <app-icon-action-button\r
                      kind="delete"\r
                      variant="danger"\r
                      [label]="'common.delete' | t"\r
                      (action)="deleteCertificate(certificate)"\r
                    />\r
                  }\r
                </div>\r
              </td>\r
            </tr>\r
          } @empty {\r
            <tr>\r
              <td [attr.colspan]="isAdmin() ? 7 : 6" class="meta">\r
                {{ (isAdmin() ? 'certificates.emptyAdmin' : 'certificates.emptyTeacher') | t }}\r
              </td>\r
            </tr>\r
          }\r
        </tbody>\r
      </table>\r
    </div>\r
  </section>\r
\r
  @if (sheet(); as current) {\r
    <section class="block" id="certificate-sheet">\r
      <div class="sheet-head">\r
        <div>\r
          <h3>{{ current.title }}</h3>\r
          <p class="meta">{{ current.classroomName }} \xB7 {{ gradeLabel(current.grade) }}</p>\r
        </div>\r
        <div class="row-actions">\r
          @if (isAdmin()) {\r
            <button type="button" class="ghost-btn" [disabled]="!current.students.length" (click)="printCertificates()">\r
              {{ 'certificates.printAll' | t }}\r
            </button>\r
            <button\r
              type="button"\r
              class="ghost-btn"\r
              [disabled]="!current.students.length || exporting()"\r
              (click)="exportCertificateImages()"\r
            >\r
              {{ (exporting() ? 'certificates.exporting' : 'certificates.imageAll') | t }}\r
            </button>\r
          }\r
          <button type="button" class="ghost-btn" (click)="closeSheet()">{{ 'common.close' | t }}</button>\r
        </div>\r
      </div>\r
\r
      @if (!isAdmin()) {\r
        <p class="meta">{{ 'certificates.teacherHint' | t }}</p>\r
      } @else if (current.isApproved) {\r
        <p class="meta">\u2713 {{ 'certificates.sheetApprovedHint' | t }}</p>\r
      } @else {\r
        <p class="meta">{{ 'certificates.sheetPendingHint' | t }}</p>\r
      }\r
\r
      <div class="table-wrap">\r
        <table class="data-table sheet-table">\r
          <thead>\r
            <tr>\r
              <th>{{ 'common.student' | t }}</th>\r
              @for (subject of totalSubjects(current); track subject.id) {\r
                <th>\r
                  {{ subject.courseTitle }}\r
                  <span class="max-chip">{{ subject.maxDegree }}</span>\r
                </th>\r
              }\r
              @if (isAdmin()) {\r
                <th>\r
                  {{ 'certificates.total' | t }}\r
                  <span class="max-chip">{{ maxTotal(current) }}</span>\r
                </th>\r
              }\r
              @for (subject of extraSubjects(current); track subject.id) {\r
                <th>\r
                  {{ subject.courseTitle }}\r
                  <span class="max-chip">{{ subject.maxDegree }}</span>\r
                </th>\r
              }\r
              @if (isAdmin()) {\r
                <th>{{ 'common.actions' | t }}</th>\r
              }\r
            </tr>\r
          </thead>\r
          <tbody>\r
            @for (student of current.students; track student.studentId) {\r
              <tr>\r
                <td>{{ student.studentName }}</td>\r
                @for (subject of totalSubjects(current); track subject.id) {\r
                  <td>\r
                    <input\r
                      type="number"\r
                      class="degree-input"\r
                      min="0"\r
                      step="0.5"\r
                      [max]="subject.maxDegree"\r
                      [(ngModel)]="marks[student.studentId][subject.id]"\r
                      [attr.aria-label]="subject.courseTitle + ' \u2014 ' + student.studentName"\r
                    />\r
                  </td>\r
                }\r
                @if (isAdmin()) {\r
                  <td class="total-cell">{{ studentTotal(current, student.studentId) ?? ('common.emDash' | t) }}</td>\r
                }\r
                @for (subject of extraSubjects(current); track subject.id) {\r
                  <td>\r
                    <input\r
                      type="number"\r
                      class="degree-input"\r
                      min="0"\r
                      step="0.5"\r
                      [max]="subject.maxDegree"\r
                      [(ngModel)]="marks[student.studentId][subject.id]"\r
                      [attr.aria-label]="subject.courseTitle + ' \u2014 ' + student.studentName"\r
                    />\r
                  </td>\r
                }\r
                @if (isAdmin()) {\r
                  <td>\r
                    <div class="row-actions">\r
                      <button type="button" class="ghost-btn small-btn" (click)="printCertificates(student)">\r
                        {{ 'certificates.print' | t }}\r
                      </button>\r
                      <button\r
                        type="button"\r
                        class="ghost-btn small-btn"\r
                        [disabled]="exporting()"\r
                        (click)="exportCertificateImages(student)"\r
                      >\r
                        {{ 'certificates.image' | t }}\r
                      </button>\r
                    </div>\r
                  </td>\r
                }\r
              </tr>\r
            } @empty {\r
              <tr>\r
                <td [attr.colspan]="current.subjects.length + 3" class="meta">{{ 'certificates.noStudents' | t }}</td>\r
              </tr>\r
            }\r
          </tbody>\r
        </table>\r
      </div>\r
\r
      <div class="form-actions">\r
        <button type="button" [disabled]="saving() || !current.students.length" (click)="saveMarks()">\r
          {{ 'certificates.saveDegrees' | t }}\r
        </button>\r
      </div>\r
    </section>\r
  }\r
</div>\r
`, styles: ['/* src/app/styles/dashboard-shared.css */\n.page {\n  position: relative;\n  min-height: 100vh;\n  padding: var(--space-5) 6vw 4rem;\n  color: var(--text);\n  background:\n    radial-gradient(\n      circle at 88% 0%,\n      var(--page-glow-1),\n      transparent 28%),\n    radial-gradient(\n      circle at 8% 12%,\n      var(--page-glow-2),\n      transparent 22%),\n    var(--bg);\n}\n.page p,\n.page span,\n.page strong,\n.page small,\n.page label,\n.page li,\n.page td,\n.page th {\n  color: inherit;\n}\n.topbar,\n.hero-strip,\n.grid-two,\n.grid-cards,\n.chip-row,\n.avatar-row {\n  display: flex;\n  gap: var(--space-3);\n}\n.topbar {\n  justify-content: space-between;\n  align-items: center;\n  margin-bottom: var(--space-5);\n}\n.brand {\n  margin: 0;\n  font-family: var(--font-display);\n  font-size: clamp(1.75rem, 3vw, 2.15rem);\n  font-weight: 800;\n  text-transform: uppercase;\n  color: var(--heading);\n  letter-spacing: 0.04em;\n}\nh1,\nh2,\nh3,\nh4 {\n  font-family: var(--font-display);\n  margin: 0.15rem 0;\n  color: var(--heading);\n  letter-spacing: 0.01em;\n  line-height: 1.15;\n}\nh1 {\n  font-size: clamp(1.8rem, 3vw, 2.4rem);\n}\nh2 {\n  font-size: clamp(1.4rem, 2.4vw, 1.85rem);\n}\nh3 {\n  font-size: 1.2rem;\n}\n.hero-strip {\n  justify-content: space-between;\n  align-items: center;\n  gap: var(--space-4);\n  padding: 1.5rem 1.6rem;\n  border-radius: var(--radius-xl);\n  margin-bottom: var(--space-5);\n  background: var(--hero-bg);\n  border: 1px solid var(--hero-border);\n  box-shadow: var(--shadow-sm);\n  color: var(--hero-fg);\n}\n.hero-strip p,\n.hero-strip h2 {\n  color: var(--hero-fg);\n}\n.eyebrow,\n.meta,\n.back {\n  color: var(--text-muted);\n}\n.eyebrow {\n  margin: 0 0 0.35rem;\n  font-size: 0.78rem;\n  font-weight: 700;\n  letter-spacing: 0.08em;\n  text-transform: uppercase;\n  color: var(--teal);\n}\n.back {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.35rem;\n  margin-bottom: var(--space-3);\n  text-decoration: none;\n  font-weight: 600;\n  transition: color 0.15s ease;\n}\n.back:hover {\n  color: var(--heading);\n}\n.xp-pill,\nbutton,\n.chip,\n.list-btn,\n.avatar,\n.badge {\n  border: none;\n  border-radius: var(--radius-pill);\n  font: inherit;\n}\n.xp-pill,\nbutton {\n  padding: 0.8rem 1.15rem;\n  background:\n    linear-gradient(\n      135deg,\n      var(--accent),\n      var(--accent-hot));\n  color: var(--accent-ink);\n  font-weight: 800;\n  cursor: pointer;\n  transition:\n    transform 0.15s ease,\n    box-shadow 0.15s ease,\n    opacity 0.15s ease;\n  box-shadow: var(--btn-shadow);\n}\nbutton:hover:not(:disabled) {\n  transform: translateY(-1px);\n  box-shadow: var(--btn-shadow-hover);\n}\nbutton:active:not(:disabled) {\n  transform: translateY(0);\n}\nbutton:disabled {\n  opacity: 0.55;\n  cursor: not-allowed;\n  box-shadow: none;\n}\nbutton:focus-visible,\n.chip:focus-visible,\n.list-btn:focus-visible,\na:focus-visible,\ninput:focus-visible,\nselect:focus-visible,\ntextarea:focus-visible {\n  outline: none;\n  box-shadow: var(--focus-ring);\n}\nbutton.ghost,\n.ghost-btn {\n  background: transparent;\n  color: var(--ghost-fg);\n  border: 1px solid var(--border-strong);\n  box-shadow: none;\n}\nbutton.ghost:hover:not(:disabled),\n.ghost-btn:hover:not(:disabled) {\n  background: var(--surface-strong);\n  box-shadow: none;\n}\n.grid-two {\n  display: grid;\n  grid-template-columns: 1.3fr 0.9fr;\n  gap: var(--space-4);\n}\n.grid-cards {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));\n}\n.side-stack {\n  display: grid;\n  gap: var(--space-4);\n}\n.block,\n.badge,\n.avatar {\n  background: var(--surface);\n  border: 1px solid var(--border);\n  border-radius: var(--radius-lg);\n  padding: 1.25rem;\n  color: var(--text);\n}\n.block {\n  display: grid;\n  gap: 0.85rem;\n  margin-bottom: var(--space-3);\n  box-shadow: var(--shadow-sm);\n  position: relative;\n  z-index: 0;\n}\n.block:has(app-searchable-select.ss--open),\n.block:has(app-searchable-multi-select.ms--open) {\n  z-index: 50;\n}\n.block > h3 {\n  padding-bottom: 0.55rem;\n  border-bottom: 1px solid var(--border);\n}\n.block p,\n.block strong,\n.block small {\n  color: var(--text);\n}\n.chip-row,\n.avatar-row {\n  flex-wrap: wrap;\n}\n.chip,\n.list-btn {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.4rem;\n  padding: 0.65rem 0.95rem;\n  background: var(--chip-bg);\n  border: 1px solid var(--chip-border);\n  color: var(--chip-fg);\n  text-decoration: none;\n  cursor: pointer;\n  transition:\n    background 0.15s ease,\n    border-color 0.15s ease,\n    transform 0.15s ease;\n}\n.chip:hover,\n.list-btn:hover {\n  background: var(--chip-bg);\n  border-color: var(--chip-border);\n  filter: brightness(0.97);\n  transform: translateY(-1px);\n}\n.chip.quiz {\n  background: rgba(95, 211, 188, 0.16);\n  border-color: rgba(95, 211, 188, 0.22);\n}\n.chip.video {\n  background: rgba(255, 214, 10, 0.16);\n  border-color: rgba(255, 214, 10, 0.28);\n}\n.list-btn {\n  width: 100%;\n  text-align: left;\n  margin-bottom: 0.45rem;\n  border-radius: var(--radius-md);\n}\n.list-btn.active,\n.avatar.selected,\n.badge.earned {\n  background:\n    linear-gradient(\n      135deg,\n      var(--accent),\n      var(--accent-hot));\n  border-color: transparent;\n  color: var(--accent-ink);\n}\n.avatar {\n  width: 9.5rem;\n  display: grid;\n  gap: 0.3rem;\n  text-align: left;\n  color: var(--text);\n  cursor: pointer;\n  transition: transform 0.15s ease, border-color 0.15s ease;\n}\n.avatar:hover:not(:disabled) {\n  transform: translateY(-2px);\n  border-color: var(--border-strong);\n}\n.avatar strong,\n.avatar small,\n.badge strong,\n.badge small {\n  color: inherit;\n}\n.avatar:disabled {\n  opacity: 0.45;\n  cursor: not-allowed;\n}\n.emoji {\n  font-size: 2rem;\n}\ntextarea,\ninput[type=radio],\ninput[type=checkbox] {\n  accent-color: var(--accent);\n}\ntextarea,\ninput[type=text],\ninput[type=email],\ninput[type=password],\ninput[type=number],\ninput[type=datetime-local],\ninput[type=file],\nselect {\n  width: 100%;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.8rem 0.95rem;\n  font: inherit;\n  transition:\n    border-color 0.15s ease,\n    background 0.15s ease,\n    box-shadow 0.15s ease;\n}\ntextarea {\n  min-height: 9rem;\n  resize: vertical;\n  line-height: 1.45;\n}\ntextarea::placeholder,\ninput::placeholder {\n  color: var(--text-soft);\n}\ntextarea:hover,\ninput:hover,\nselect:hover {\n  border-color: var(--input-border-hover);\n}\ntextarea:focus,\ninput:focus,\nselect:focus {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  background: var(--input-bg-focus);\n  box-shadow: var(--focus-ring);\n}\nselect option {\n  background: var(--bg-elevated);\n  color: var(--text);\n}\nlabel {\n  display: grid;\n  gap: 0.4rem;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n  font-weight: 600;\n}\nlabel > span {\n  color: var(--text-muted);\n}\n.feedback {\n  padding: 0.9rem 1rem;\n  border-radius: var(--radius-md);\n  background: var(--auth-error-bg);\n  border: 1px solid var(--auth-error-border);\n  color: var(--feedback-error-fg);\n}\n.feedback.ok {\n  background: rgba(81, 207, 102, 0.14);\n  border-color: rgba(125, 222, 160, 0.28);\n  color: var(--feedback-ok-fg);\n}\n[data-theme=light] .feedback.ok {\n  background: #f0fdf4;\n  border-color: #bbf7d0;\n}\n.question {\n  display: grid;\n  gap: 0.5rem;\n  margin-bottom: var(--space-3);\n  padding: 1rem;\n  border-radius: var(--radius-md);\n  background: var(--elevated-bg);\n  border: 1px solid var(--border);\n  color: var(--text);\n}\n.prompt-html {\n  color: var(--prompt-fg);\n}\n.prompt-html b,\n.prompt-html strong {\n  font-weight: 800;\n}\n.table {\n  display: grid;\n  gap: 0.35rem;\n}\n.table-row {\n  display: grid;\n  grid-template-columns: 1.4fr repeat(4, 1fr);\n  gap: 0.5rem;\n  padding: 0.85rem 0.4rem;\n  border-bottom: 1px solid var(--border);\n  color: var(--text);\n  align-items: center;\n}\n.table-row.head {\n  color: var(--text-soft);\n  font-size: 0.82rem;\n  font-weight: 700;\n  letter-spacing: 0.04em;\n  text-transform: uppercase;\n  border-bottom-color: var(--border-strong);\n}\n@media (max-width: 900px) {\n  .grid-two,\n  .table-row {\n    grid-template-columns: 1fr;\n  }\n}\n.panel-page {\n  display: grid;\n  gap: var(--space-4);\n  color: var(--text);\n  animation: pageIn 0.35s ease;\n}\n.panel-page > h2 {\n  margin: 0;\n  color: var(--heading);\n}\n.panel-page > .meta {\n  margin-top: -0.55rem;\n}\n.meeting-form,\n.form-grid {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));\n  gap: var(--space-3);\n  align-items: end;\n}\n.meeting-form label,\n.form-grid label {\n  display: grid;\n  gap: 0.4rem;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n}\n.meeting-form label.checkbox,\n.form-grid label.checkbox,\nlabel.checkbox {\n  display: flex;\n  align-items: center;\n  gap: 0.55rem;\n  padding: 0.7rem 0.85rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--surface);\n}\n.meeting-form input,\n.meeting-form select,\n.meeting-form textarea,\n.form-grid input,\n.form-grid select,\n.form-grid textarea {\n  width: 100%;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.75rem 0.9rem;\n  font: inherit;\n}\n.meeting-row {\n  display: flex;\n  justify-content: space-between;\n  gap: var(--space-3);\n  align-items: center;\n  padding: 0.95rem 0.15rem;\n  border-bottom: 1px solid var(--border);\n  color: var(--text);\n}\n.meeting-row strong,\n.meeting-row .meta {\n  color: inherit;\n}\n.form-card {\n  display: grid;\n  gap: var(--space-3);\n  padding: 1.35rem;\n  border-radius: var(--radius-lg);\n  background: var(--surface);\n  border: 1px solid var(--border);\n  box-shadow: var(--shadow-sm);\n}\n.form-actions {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.75rem;\n  align-items: center;\n  padding-top: 0.35rem;\n}\n.stat-row {\n  display: grid;\n  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));\n  gap: var(--space-3);\n}\n.stat-card {\n  padding: 1rem 1.1rem;\n  border-radius: var(--radius-md);\n  background: var(--surface-strong);\n  border: 1px solid var(--border);\n}\n.stat-card strong {\n  display: block;\n  font-family: var(--font-display);\n  font-size: 1.55rem;\n  color: var(--stat-strong);\n}\n.stat-card span {\n  color: var(--text-muted);\n  font-size: 0.85rem;\n}\nbutton.stat-card-link {\n  display: block;\n  width: 100%;\n  text-align: start;\n  font: inherit;\n  font-weight: inherit;\n  color: inherit;\n  cursor: pointer;\n  background: var(--surface-strong);\n  box-shadow: none;\n  transition:\n    transform 0.2s ease,\n    border-color 0.2s ease,\n    background 0.2s ease;\n}\nbutton.stat-card-link:hover,\nbutton.stat-card-link:focus-visible {\n  transform: translateY(-2px);\n  border-color: var(--border-strong);\n  box-shadow: none;\n  outline: none;\n}\nbutton.stat-card-link.active {\n  border-color: var(--border-strong);\n  box-shadow: 0 0 0 1px var(--border-strong);\n}\n@keyframes pageIn {\n  from {\n    opacity: 0;\n    transform: translateY(6px);\n  }\n  to {\n    opacity: 1;\n    transform: translateY(0);\n  }\n}\n@media (max-width: 700px) {\n  .page {\n    padding: 1.35rem 1rem 3rem;\n  }\n  .meeting-row {\n    flex-direction: column;\n    align-items: flex-start;\n  }\n  .hero-strip {\n    flex-direction: column;\n    align-items: flex-start;\n  }\n}\n.student-side-tools {\n  position: fixed;\n  inset-inline-end: 0;\n  top: 38%;\n  z-index: 46;\n  display: grid;\n  gap: 0.55rem;\n  justify-items: end;\n}\n.student-side-tools .side-tab {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.45rem;\n  padding: 0.7rem 0.75rem;\n  border: 1px solid var(--border-strong);\n  border-inline-end: none;\n  border-radius: var(--radius-md) 0 0 var(--radius-md);\n  background: var(--surface);\n  color: var(--heading);\n  text-decoration: none;\n  font-weight: 800;\n  box-shadow: var(--shadow-md);\n  max-width: 2.75rem;\n  overflow: hidden;\n  transition: max-width 0.2s ease;\n}\nhtml[dir=rtl] .student-side-tools .side-tab {\n  border-radius: 0 var(--radius-md) var(--radius-md) 0;\n}\n.student-side-tools .side-tab-icon {\n  flex-shrink: 0;\n  width: 1.2rem;\n  text-align: center;\n  font-size: 1.05rem;\n  line-height: 1;\n}\n.student-side-tools .side-tab-label {\n  white-space: nowrap;\n  font-size: 0.88rem;\n}\n.student-side-tools .side-tab:hover,\n.student-side-tools .side-tab:focus-visible {\n  max-width: 12rem;\n}\n@media (max-width: 700px) {\n  .student-side-tools {\n    top: auto;\n    inset-block-end: 5.25rem;\n    z-index: 62;\n  }\n}\n\n/* src/app/pages/admin/admin-panel.css */\n.user-row {\n  grid-template-columns: 1.2fr 1.6fr 0.8fr 0.5fr 1fr;\n}\n.data-table {\n  width: 100%;\n  border-collapse: collapse;\n  font-size: 0.95rem;\n}\n.data-table th,\n.data-table td {\n  padding: 0.8rem 0.7rem;\n  border-bottom: 1px solid var(--border);\n  text-align: left;\n  vertical-align: middle;\n}\n.data-table td .chip-row {\n  margin-top: 0.45rem;\n}\n.data-table td .name-cell {\n  display: flex;\n  align-items: center;\n  gap: 0.5rem;\n  min-width: 0;\n}\n.meeting-form label .chip-row input {\n  flex: 1;\n  min-width: 0;\n}\n.wa-destination {\n  margin: 0;\n  padding: 0.85rem 1rem;\n  border: 1px solid var(--border);\n  border-radius: var(--radius-lg);\n  background: var(--surface);\n}\n.wa-destination legend {\n  padding: 0 0.35rem;\n  font-weight: 800;\n  color: var(--heading);\n}\n.edit-panel {\n  border: 1px solid rgba(95, 211, 188, 0.28);\n  background:\n    linear-gradient(\n      180deg,\n      rgba(95, 211, 188, 0.08),\n      transparent 60%);\n}\n.classroom-edit-form {\n  grid-template-columns: repeat(3, minmax(0, 1fr));\n  align-items: start;\n}\n.student-edit-form {\n  grid-template-columns: repeat(4, minmax(0, 1fr));\n  align-items: start;\n}\n.student-edit-form .form-actions {\n  grid-column: 1 / -1;\n  justify-content: flex-start;\n  padding-top: 0.25rem;\n}\n.classroom-edit-form .span-3 {\n  grid-column: 1 / -1;\n}\n.classroom-edit-form .edit-form-hint {\n  margin: 0;\n  align-self: center;\n}\n.classroom-edit-form .form-actions {\n  justify-content: flex-start;\n  padding-top: 0.25rem;\n}\n@media (max-width: 1100px) {\n  .student-edit-form {\n    grid-template-columns: repeat(2, minmax(0, 1fr));\n  }\n}\n@media (max-width: 960px) {\n  .classroom-edit-form {\n    grid-template-columns: 1fr;\n  }\n  .classroom-edit-form .span-3 {\n    grid-column: auto;\n  }\n  .student-edit-form {\n    grid-template-columns: 1fr;\n  }\n}\n.field-label {\n  margin: 0 0 0.25rem;\n  font-weight: 700;\n}\n.data-table th {\n  color: var(--text-soft);\n  font-weight: 700;\n  font-size: 0.82rem;\n  letter-spacing: 0.04em;\n  text-transform: uppercase;\n  white-space: nowrap;\n}\n.data-table th.sortable {\n  cursor: pointer;\n  -webkit-user-select: none;\n  user-select: none;\n}\n.data-table th.sortable:hover {\n  color: var(--accent-fg);\n}\n.data-table th .sort-mark {\n  margin-left: 0.35rem;\n  opacity: 0.7;\n}\n.data-table tr:hover td {\n  background: var(--table-row-hover);\n}\n.data-table tr.is-inactive td {\n  opacity: 0.58;\n}\n.data-table tr.total-row td {\n  border-top: 2px solid var(--border);\n  background: var(--table-row-total);\n}\n.data-table input,\n.data-table select {\n  width: 100%;\n  min-width: 7rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.5rem 0.6rem;\n  font: inherit;\n}\n.data-table input:focus,\n.data-table select:focus {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  box-shadow: var(--focus-ring);\n}\n.row-actions {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.4rem;\n}\n.row-actions button {\n  padding: 0.45rem 0.75rem;\n  border-radius: var(--radius-pill);\n  font-size: 0.85rem;\n}\n.row-actions app-icon-action-button {\n  display: inline-flex;\n}\n.row-actions button.danger {\n  background: var(--danger-soft-bg);\n  color: var(--danger-soft-fg);\n  box-shadow: none;\n}\n.row-actions button.ghost-btn {\n  background: transparent;\n  color: var(--ghost-fg);\n  border: 1px solid var(--border-strong);\n  box-shadow: none;\n}\n.table-wrap {\n  overflow-x: auto;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--surface);\n}\n.brand-preview {\n  display: block;\n  margin: 0.75rem 0 1rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--elevated-bg);\n  object-fit: contain;\n}\n.brand-preview.logo {\n  width: 96px;\n  height: 96px;\n  padding: 0.5rem;\n}\n.brand-preview.banner {\n  width: min(100%, 520px);\n  height: 160px;\n  object-fit: cover;\n}\n.login-preview {\n  display: grid;\n  grid-template-columns: minmax(0, 0.9fr) minmax(0, 1.1fr);\n  gap: 0;\n  margin: 0.85rem 0 1rem;\n  min-height: 180px;\n  border-radius: var(--radius-md);\n  overflow: hidden;\n  border: 1px solid var(--border);\n  background: var(--surface);\n}\n.login-preview-form,\n.login-preview-visual {\n  display: grid;\n  place-items: center;\n  padding: 1rem;\n  text-align: center;\n  font-size: 0.86rem;\n  color: var(--text-soft);\n}\n.login-preview-form {\n  border-right: 1px solid var(--border);\n  background: var(--elevated-bg);\n}\n.login-preview-visual {\n  position: relative;\n  padding: 0;\n  min-height: 180px;\n  background:\n    radial-gradient(\n      circle at 70% 30%,\n      rgba(255, 214, 10, 0.18),\n      transparent 40%),\n    linear-gradient(\n      145deg,\n      #07111f,\n      #145a8f);\n}\n.login-preview-visual img {\n  position: absolute;\n  inset: 0;\n  width: 100%;\n  height: 100%;\n  object-fit: cover;\n}\n@media (max-width: 700px) {\n  .login-preview {\n    grid-template-columns: 1fr;\n  }\n  .login-preview-form {\n    border-right: none;\n    border-bottom: 1px solid var(--border);\n    min-height: 72px;\n  }\n}\n.form-actions {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.6rem;\n  align-items: center;\n}\n.file-btn {\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n  padding: 0.55rem 1rem;\n  border-radius: var(--radius-pill);\n  border: 1px solid var(--border-strong);\n  background: var(--badge-ok-bg);\n  color: var(--badge-ok-fg);\n  font: inherit;\n  font-weight: 650;\n  cursor: pointer;\n}\n.file-btn:hover {\n  background: rgba(95, 211, 188, 0.26);\n}\n.ghost-btn {\n  background: transparent;\n  color: var(--ghost-fg);\n  border: 1px solid var(--border-strong);\n  box-shadow: none;\n}\n.ghost-btn:hover:not(:disabled) {\n  background: var(--surface-strong);\n  box-shadow: none;\n}\n.search-btn {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.45rem;\n  padding: 0.55rem 0.9rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid rgba(255, 214, 10, 0.55);\n  background: rgba(255, 214, 10, 0.22);\n  color: var(--text);\n  font: inherit;\n  font-weight: 700;\n  cursor: pointer;\n  box-shadow: none;\n}\n.search-btn i {\n  font-size: 0.95em;\n  line-height: 1;\n}\n.search-btn:hover:not(:disabled) {\n  background: rgba(255, 214, 10, 0.32);\n  box-shadow: none;\n  transform: none;\n}\n.list-filters {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: end;\n  gap: 0.75rem 1rem;\n  margin: 0.75rem 0 1rem;\n  position: relative;\n  z-index: 2;\n}\n.list-filters label {\n  display: flex;\n  flex-direction: column;\n  gap: 0.35rem;\n  min-width: 10rem;\n  color: var(--text-soft);\n  font-size: 0.85rem;\n  font-weight: 650;\n}\n.list-filters select,\n.list-filters input {\n  min-width: 12rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.5rem 0.65rem;\n  font: inherit;\n}\n.list-filters select:focus,\n.list-filters input:focus {\n  outline: none;\n  border-color: rgba(255, 214, 10, 0.65);\n  box-shadow: var(--focus-ring);\n}\n.list-filters .meta {\n  margin: 0 0 0.35rem;\n  align-self: center;\n}\n.list-pager {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 0.75rem;\n  margin-top: 0.85rem;\n}\n.list-pager .meta {\n  margin: 0;\n}\n.list-pager button:disabled {\n  opacity: 0.45;\n  cursor: not-allowed;\n}\n.data-table td input + input {\n  margin-top: 0.4rem;\n}\n.grade-multi,\n.stage-multi,\n.teachers-multi {\n  grid-column: 1 / -1;\n}\n.grade-multi .meta,\n.stage-multi .meta,\n.teachers-multi .meta {\n  margin-top: 0.35rem;\n}\n.data-table app-searchable-multi-select {\n  display: block;\n  min-width: 14rem;\n  max-width: 22rem;\n}\n.stage-multi app-searchable-multi-select {\n  max-width: 36rem;\n}\n.course-teacher-block {\n  grid-column: 1 / -1;\n  display: flex;\n  flex-direction: column;\n  gap: 0.65rem;\n  padding: 0.85rem 0.9rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--elevated-bg);\n}\n.course-teacher-block.compact {\n  padding: 0.5rem;\n  gap: 0.5rem;\n}\n.course-teacher-head {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  justify-content: space-between;\n  gap: 0.5rem;\n}\n.course-teacher-row {\n  display: grid;\n  grid-template-columns: 1.4fr 1fr auto;\n  gap: 0.6rem;\n  align-items: end;\n}\n.course-teacher-row label {\n  display: flex;\n  flex-direction: column;\n  gap: 0.3rem;\n  min-width: 0;\n}\n.course-teacher-row select {\n  width: 100%;\n  border-radius: var(--radius-sm);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.5rem 0.6rem;\n  font: inherit;\n}\n.course-teacher-block .meta {\n  margin: 0;\n}\n@media (max-width: 700px) {\n  .course-teacher-row {\n    grid-template-columns: 1fr;\n  }\n}\n@media (max-width: 900px) {\n  .user-row {\n    grid-template-columns: 1fr;\n  }\n}\n.calendar-toolbar {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  justify-content: space-between;\n  gap: 0.75rem;\n  margin-bottom: 0.85rem;\n}\n.calendar-toolbar h3 {\n  margin: 0;\n}\n.calendar-nav {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 0.5rem;\n}\n.calendar-nav .meta {\n  margin: 0;\n  margin-inline-start: 0.35rem;\n}\n.week-day-tabs {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.45rem;\n  margin-bottom: 0.85rem;\n}\n.week-day-tab {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.4rem;\n  padding: 0.45rem 0.75rem;\n  border-radius: var(--radius-pill);\n  border: 1px solid var(--border-strong);\n  background: var(--elevated-bg);\n  color: var(--text-soft);\n  font: inherit;\n  font-size: 0.85rem;\n  cursor: pointer;\n}\n.week-day-tab.active {\n  color: #07111f;\n  background: var(--accent);\n  border-color: transparent;\n  font-weight: 700;\n}\n.week-day-tab .day-count {\n  min-width: 1.25rem;\n  height: 1.25rem;\n  padding: 0 0.35rem;\n  border-radius: 999px;\n  background: rgba(7, 17, 31, 0.18);\n  color: inherit;\n  font-size: 0.75rem;\n  display: inline-grid;\n  place-items: center;\n}\n.day-calendar {\n  display: grid;\n  grid-template-columns: 4rem minmax(0, 1fr);\n  gap: 0.35rem;\n  border: 1px solid var(--border);\n  border-radius: var(--radius-md);\n  background: var(--surface);\n  overflow: auto;\n  max-height: min(70vh, 42rem);\n}\n.day-hours {\n  border-right: 1px solid var(--border);\n  background: var(--elevated-bg);\n}\n.day-hour {\n  display: flex;\n  align-items: flex-start;\n  justify-content: flex-end;\n  padding: 0.2rem 0.45rem 0 0;\n  color: var(--text-soft);\n  font-size: 0.75rem;\n  box-sizing: border-box;\n}\n.day-grid {\n  position: relative;\n  min-height: 12rem;\n}\n.day-grid-line {\n  position: absolute;\n  left: 0;\n  right: 0;\n  border-top: 1px dashed var(--border);\n  pointer-events: none;\n}\n.calendar-event {\n  position: absolute;\n  box-sizing: border-box;\n  display: flex;\n  flex-direction: column;\n  gap: 0.2rem;\n  padding: 0.45rem 0.5rem;\n  border-radius: var(--radius-sm);\n  border: 1px solid rgba(255, 214, 10, 0.35);\n  background:\n    linear-gradient(\n      145deg,\n      rgba(255, 214, 10, 0.22),\n      rgba(20, 90, 143, 0.45));\n  color: #fff;\n  overflow: hidden;\n  z-index: 1;\n}\n.calendar-event-label {\n  font-weight: 700;\n  font-size: 0.92rem;\n  line-height: 1.3;\n  white-space: normal;\n  overflow: hidden;\n  display: -webkit-box;\n  -webkit-line-clamp: 3;\n  -webkit-box-orient: vertical;\n  word-break: break-word;\n}\n.calendar-event-time {\n  font-size: 0.78rem;\n  color: rgba(255, 255, 255, 0.82);\n  white-space: nowrap;\n}\n.calendar-event .row-actions {\n  margin-top: auto;\n}\n.calendar-empty {\n  position: absolute;\n  inset: 0;\n  display: grid;\n  place-items: center;\n  margin: 0;\n  color: var(--text-soft);\n  font-size: 0.95rem;\n  pointer-events: none;\n}\n@media (max-width: 700px) {\n  .day-calendar {\n    grid-template-columns: 3rem minmax(0, 1fr);\n  }\n}\n.timetable-wrap {\n  overflow: auto;\n  max-height: min(80vh, 56rem);\n  border: 3px solid #e8c46a;\n  border-radius: 28px;\n  background:\n    radial-gradient(\n      circle at 8% 12%,\n      rgba(255, 214, 120, 0.35),\n      transparent 22%),\n    radial-gradient(\n      circle at 92% 10%,\n      rgba(168, 216, 255, 0.28),\n      transparent 24%),\n    radial-gradient(\n      circle at 10% 90%,\n      rgba(255, 182, 193, 0.25),\n      transparent 22%),\n    radial-gradient(\n      circle at 90% 88%,\n      rgba(186, 230, 180, 0.28),\n      transparent 24%),\n    linear-gradient(\n      180deg,\n      #fff8e8 0%,\n      #fffdf7 48%,\n      #f7fbff 100%);\n  direction: rtl;\n  position: relative;\n  z-index: 0;\n  isolation: isolate;\n  padding: 1.1rem 1rem 1.25rem;\n  box-shadow: 0 18px 40px rgba(120, 84, 20, 0.12);\n}\n.timetable-board {\n  font-family:\n    "Cairo",\n    "Baloo 2",\n    "Segoe UI",\n    Tahoma,\n    sans-serif;\n}\n.timetable-deco {\n  position: absolute;\n  font-size: 1.55rem;\n  line-height: 1;\n  opacity: 0.88;\n  pointer-events: none;\n  z-index: 1;\n  filter: drop-shadow(0 2px 2px rgba(0, 0, 0, 0.08));\n}\n.timetable-deco-tl {\n  top: 0.55rem;\n  inset-inline-start: 0.7rem;\n}\n.timetable-deco-tr {\n  top: 0.55rem;\n  inset-inline-end: 0.7rem;\n}\n.timetable-deco-bl {\n  bottom: 0.55rem;\n  inset-inline-start: 0.7rem;\n}\n.timetable-deco-br {\n  bottom: 0.55rem;\n  inset-inline-end: 0.7rem;\n}\n.timetable-banner {\n  position: relative;\n  z-index: 2;\n  display: grid;\n  place-items: center;\n  margin: 0.15rem auto 1rem;\n  width: min(28rem, 92%);\n  padding: 0.75rem 1.4rem 0.85rem;\n  border-radius: 18px;\n  border: 3px solid #d4a017;\n  background:\n    linear-gradient(\n      180deg,\n      #ffe9a8 0%,\n      #f6d06a 100%);\n  box-shadow: 0 8px 0 #c8961a, 0 14px 24px rgba(180, 120, 20, 0.18);\n  text-align: center;\n}\n.timetable-banner-crown {\n  position: absolute;\n  top: -0.85rem;\n  font-size: 1.35rem;\n  line-height: 1;\n}\n.timetable-banner-title {\n  margin: 0;\n  color: #1d3a75;\n  font-size: clamp(1.15rem, 2.4vw, 1.65rem);\n  font-weight: 800;\n  letter-spacing: 0.01em;\n}\n.timetable-motto {\n  position: relative;\n  z-index: 2;\n  display: flex;\n  align-items: center;\n  justify-content: center;\n  gap: 0.55rem;\n  margin: 1rem auto 0.15rem;\n  width: min(30rem, 94%);\n  padding: 0.55rem 1.1rem;\n  border-radius: 16px;\n  border: 2px solid #d4a017;\n  background:\n    linear-gradient(\n      180deg,\n      #fff1c2,\n      #f8d978);\n  color: #1d3a75;\n  font-weight: 800;\n  font-size: 1rem;\n  box-shadow: 0 4px 0 #c8961a;\n}\n.timetable-page {\n  direction: rtl;\n  text-align: right;\n}\n.timetable {\n  width: max-content;\n  border-collapse: separate;\n  border-spacing: 0.28rem;\n  font-size: 0.84rem;\n  font-family:\n    "Cairo",\n    "Baloo 2",\n    "Segoe UI",\n    Tahoma,\n    sans-serif;\n  color: #1f2a44;\n  direction: rtl;\n  position: relative;\n  z-index: 2;\n}\n.timetable th,\n.timetable td {\n  border: none;\n  vertical-align: middle;\n}\n.timetable-day-col,\n.timetable-day {\n  position: sticky;\n  right: 0;\n  left: auto;\n  inset-inline-start: auto;\n  inset-inline-end: 0;\n  z-index: 2;\n  min-width: 0;\n  width: 1%;\n  white-space: nowrap;\n  background:\n    linear-gradient(\n      180deg,\n      #ffe082,\n      #ffd54f) !important;\n  color: #1d3a75 !important;\n  text-align: center;\n  padding: 0.35rem 0.4rem;\n  font-weight: 800;\n  border-radius: 18px;\n  border: 2px solid #e0b13a !important;\n  box-shadow: 0 3px 0 rgba(196, 145, 30, 0.35);\n}\n.timetable-day {\n  text-align: center;\n}\n.timetable-day-icon,\n.timetable-day-col-icon {\n  display: block;\n  font-size: 1.05rem;\n  line-height: 1;\n  margin-bottom: 0.2rem;\n}\n.timetable thead th {\n  position: sticky;\n  top: 0;\n  z-index: 3;\n  background:\n    linear-gradient(\n      180deg,\n      #ffe9a8,\n      #f0c75a) !important;\n  color: #1d3a75 !important;\n  font-size: 0.78rem;\n  letter-spacing: 0;\n  text-transform: none;\n  padding: 0.28rem 0.32rem;\n  text-align: center;\n  white-space: nowrap;\n  border-radius: 16px;\n  border: 2px solid #e0b13a !important;\n  box-shadow: 0 3px 0 rgba(196, 145, 30, 0.28);\n}\n.timetable thead .timetable-day-col {\n  z-index: 4;\n  top: 0;\n}\n.timetable-shift {\n  background:\n    linear-gradient(\n      180deg,\n      #fff3c4,\n      #f5d36a) !important;\n  color: #1d3a75 !important;\n  font-weight: 800;\n}\n.timetable-shift.pm {\n  background:\n    linear-gradient(\n      180deg,\n      #d8f5e8,\n      #9ed9bf) !important;\n  color: #14553a !important;\n  border-color: #7cbc9a !important;\n}\n.timetable-wrap.exporting {\n  overflow: visible !important;\n  max-height: none !important;\n  height: auto !important;\n}\n.timetable-wrap.exporting .timetable thead th,\n.timetable-wrap.exporting .timetable-day-col,\n.timetable-wrap.exporting .timetable-day {\n  position: static !important;\n}\n.timetable-wrap.exporting .row-actions {\n  display: none !important;\n}\n.timetable-export-bar {\n  display: flex;\n  justify-content: flex-start;\n  margin-bottom: 0.75rem;\n}\n.timetable-period {\n  min-width: 0;\n  width: 1%;\n  white-space: nowrap;\n}\n.timetable-session-id {\n  font-weight: 800;\n  font-size: 0.92rem;\n  color: #1d3a75;\n}\n.timetable-session-time {\n  display: inline-flex;\n  align-items: center;\n  justify-content: center;\n  gap: 0.25rem;\n  font-size: 0.78rem;\n  font-weight: 700;\n  letter-spacing: 0.01em;\n  text-transform: none;\n  color: #4a3b12;\n  margin-top: 0.25rem;\n  font-variant-numeric: tabular-nums;\n  direction: ltr;\n  unicode-bidi: isolate;\n}\n.timetable-clock {\n  font-size: 0.85rem;\n  line-height: 1;\n}\n.timetable-cell {\n  width: 1%;\n  min-width: 0;\n  max-width: none;\n  height: auto;\n  min-height: 0;\n  padding: 0.15rem;\n  background: rgba(255, 255, 255, 0.55);\n  vertical-align: middle;\n  overflow: visible;\n  cursor: cell;\n  transition:\n    transform 0.15s ease,\n    box-shadow 0.15s ease,\n    outline 0.15s ease;\n  border-radius: 12px;\n  border: 2px dashed rgba(180, 150, 90, 0.35) !important;\n}\n.timetable-cell.pm {\n  background: rgba(232, 255, 246, 0.55);\n}\n.timetable-cell.empty {\n  background: rgba(255, 255, 255, 0.42);\n}\n.timetable-empty {\n  display: grid;\n  place-items: center;\n  min-height: 0;\n  padding: 0.1rem 0.2rem;\n  line-height: 1;\n  color: #9aa3b5;\n  font-size: 0.9rem;\n  font-weight: 700;\n}\n.timetable-cell.drag-over {\n  outline: 3px dashed #1d3a75;\n  background: rgba(255, 214, 10, 0.22);\n  transform: scale(1.01);\n}\n.timetable-entry {\n  display: flex;\n  flex-direction: column;\n  align-items: center;\n  width: max-content;\n  min-width: max-content;\n  max-width: none;\n  gap: 0.08rem;\n  padding: 0.28rem 0.35rem;\n  border-radius: 10px;\n  border: 2px solid transparent;\n  margin: 0 auto;\n  overflow: visible;\n  cursor: grab;\n  text-align: center;\n  box-shadow: 0 3px 0 rgba(0, 0, 0, 0.06);\n  transition: transform 0.15s ease, box-shadow 0.15s ease;\n}\n.timetable-entry + .timetable-entry {\n  margin-top: 0.18rem;\n}\n.timetable-entry:hover {\n  transform: translateY(-1px);\n  box-shadow: 0 6px 0 rgba(0, 0, 0, 0.08);\n}\n.timetable-entry:active {\n  cursor: grabbing;\n}\n.timetable-entry .row-actions {\n  display: flex;\n  flex-wrap: nowrap;\n  flex-shrink: 0;\n  gap: 0.25rem;\n  margin-top: 0.2rem;\n  cursor: default;\n  justify-content: center;\n}\n.timetable-entry .row-actions app-icon-action-button {\n  display: inline-flex;\n  flex-shrink: 0;\n}\n.timetable-subject-icon {\n  font-size: 1.05rem;\n  line-height: 1;\n}\n.timetable-course {\n  font-weight: 800;\n  line-height: 1.2;\n  white-space: nowrap;\n  overflow: visible;\n  color: #1f2a44;\n  font-size: 0.8rem;\n}\n.timetable-teacher {\n  font-size: 0.7rem;\n  color: #4b5568;\n  white-space: nowrap;\n  overflow: visible;\n  font-weight: 600;\n}\n.timetable-entry[data-tone="0"] {\n  background: #ffe0e8;\n  border-color: #f5a3b8;\n}\n.timetable-entry[data-tone="1"] {\n  background: #e8d8ff;\n  border-color: #c4a6f5;\n}\n.timetable-entry[data-tone="2"] {\n  background: #d8ecff;\n  border-color: #9ec4f0;\n}\n.timetable-entry[data-tone="3"] {\n  background: #fff0b8;\n  border-color: #e6c85a;\n}\n.timetable-entry[data-tone="4"] {\n  background: #d8f5e8;\n  border-color: #8fd0b0;\n}\n.timetable-entry[data-tone="5"] {\n  background: #ffe6cc;\n  border-color: #f0b878;\n}\n.timetable-entry[data-tone="6"] {\n  background: #e0f0ff;\n  border-color: #9ebfe8;\n}\n.timetable-entry[data-tone="7"] {\n  background: #f3e0ff;\n  border-color: #d0a8ef;\n}\n.modal-backdrop {\n  position: fixed;\n  inset: 0;\n  z-index: 80;\n  display: grid;\n  place-items: center;\n  padding: 1rem;\n  background: rgba(4, 10, 22, 0.72);\n  -webkit-backdrop-filter: blur(2px);\n  backdrop-filter: blur(2px);\n}\n.modal-card {\n  width: min(32rem, 100%);\n  max-height: min(90vh, 44rem);\n  overflow: auto;\n  padding: 1.1rem 1.2rem 1.25rem;\n  border-radius: var(--radius);\n  border: 1px solid var(--border-strong);\n  background: var(--modal-bg);\n  box-shadow: var(--modal-shadow);\n}\n.modal-card.modal-card--wide {\n  width: min(60rem, 100%);\n}\n.page-actions {\n  display: flex;\n  justify-content: flex-end;\n  margin-bottom: 0.75rem;\n}\n.modal-head {\n  display: flex;\n  align-items: center;\n  justify-content: space-between;\n  gap: 0.75rem;\n  margin-bottom: 0.35rem;\n}\n.modal-head h3 {\n  margin: 0;\n  font-size: 1.15rem;\n}\n.course-tree {\n  display: flex;\n  flex-direction: column;\n  gap: 0.85rem;\n}\n.tree-unit {\n  border: 1px solid var(--border);\n  border-radius: 12px;\n  padding: 0.75rem 0.9rem;\n  background: var(--surface);\n}\n.tree-unit-head {\n  display: flex;\n  align-items: center;\n  gap: 0.6rem;\n  flex-wrap: wrap;\n}\n.tree-toggle {\n  min-width: 2rem;\n  padding: 0.2rem 0.4rem;\n}\n.tree-unit-title,\n.tree-lesson-title {\n  flex: 1;\n  min-width: 12rem;\n}\n.ask-toggle {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.35rem;\n  font-size: 0.82rem;\n  white-space: nowrap;\n  color: var(--text-soft);\n}\n.tree-unit-body {\n  margin-top: 0.75rem;\n  margin-inline-start: 1.6rem;\n  display: flex;\n  flex-direction: column;\n  gap: 0.55rem;\n}\n.tree-lesson {\n  display: flex;\n  align-items: center;\n  gap: 0.6rem;\n  flex-wrap: wrap;\n  padding: 0.45rem 0.55rem;\n  border-radius: 8px;\n  background: var(--elevated-bg);\n}\n.tree-inline-form,\n.tree-add-lesson {\n  width: 100%;\n}\n.course-tree-ai {\n  border: 1px solid var(--border);\n  border-radius: var(--radius-md);\n  padding: 1rem 1.1rem;\n  background: var(--surface);\n}\n.course-tree-prompt-field {\n  display: grid;\n  gap: 0.45rem;\n  margin: 0.75rem 0 0.35rem;\n  width: 100%;\n  max-width: 42rem;\n}\n.course-tree-prompt-field > span:first-child {\n  font-weight: 650;\n  color: var(--text-muted);\n  font-size: 0.9rem;\n}\n.course-tree-prompt-field textarea {\n  width: 100%;\n  min-height: 5.5rem;\n  resize: vertical;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border-strong);\n  background: var(--input-bg);\n  color: var(--text);\n  padding: 0.75rem 0.9rem;\n  font: inherit;\n  line-height: 1.45;\n}\n.course-tree-ai-actions {\n  margin-top: 0.35rem;\n}\n.course-tree-ai-preview {\n  margin-top: 0.85rem;\n  padding-top: 0.85rem;\n  border-top: 1px dashed var(--border);\n}\n.login-stat {\n  border-top: 3px solid transparent;\n}\n.login-stat-teachers {\n  border-top-color: #5fd3bc;\n}\n.login-stat-parents {\n  border-top-color: #ffd60a;\n}\n.login-stat-students {\n  border-top-color: #4ea8de;\n}\n.login-chart-legend {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.85rem 1.25rem;\n  margin: 0.75rem 0 1rem;\n}\n.login-legend-item {\n  display: inline-flex;\n  align-items: center;\n  gap: 0.45rem;\n  color: var(--text-soft);\n  font-size: 0.88rem;\n}\n.login-swatch {\n  display: inline-block;\n  width: 0.75rem;\n  height: 0.75rem;\n  border-radius: 3px;\n}\n.login-swatch-teachers,\n.login-bar-teachers {\n  background: #5fd3bc;\n}\n.login-swatch-parents,\n.login-bar-parents {\n  background: #ffd60a;\n}\n.login-swatch-students,\n.login-bar-students {\n  background: #4ea8de;\n}\n.login-chart {\n  padding: 1rem 0.75rem 0.5rem;\n  border-radius: var(--radius-md);\n  border: 1px solid var(--border);\n  background: var(--surface);\n  overflow-x: auto;\n}\n.login-chart-bars {\n  display: flex;\n  align-items: flex-end;\n  gap: 0.35rem;\n  min-width: 100%;\n  min-height: 220px;\n}\n.login-chart-day {\n  flex: 1 1 0;\n  min-width: 1.35rem;\n  display: flex;\n  flex-direction: column;\n  align-items: center;\n  gap: 0.4rem;\n}\n.login-chart-group {\n  display: flex;\n  align-items: flex-end;\n  justify-content: center;\n  gap: 2px;\n  width: 100%;\n  height: 180px;\n}\n.login-chart-bar {\n  flex: 1 1 0;\n  min-width: 3px;\n  border-radius: 4px 4px 0 0;\n}\n.login-chart-label {\n  color: var(--text-muted);\n  font-size: 0.72rem;\n  line-height: 1;\n}\n.login-chart-label-spacer {\n  visibility: hidden;\n}\n.status-badge {\n  display: inline-flex;\n  align-items: center;\n  padding: 0.15rem 0.55rem;\n  border-radius: var(--radius-pill);\n  font-size: 0.78rem;\n  font-weight: 700;\n  line-height: 1.2;\n  border: 1px solid transparent;\n}\n.status-badge.published {\n  color: var(--badge-ok-fg);\n  background: var(--badge-ok-bg);\n  border-color: var(--badge-ok-border);\n}\n.status-badge.draft {\n  color: var(--badge-warn-fg);\n  background: var(--badge-warn-bg);\n  border-color: var(--badge-warn-border);\n}\nlabel.checkbox {\n  flex-direction: row;\n  align-items: center;\n  gap: 0.45rem;\n}\n.delete-options {\n  display: grid;\n  gap: 0.6rem;\n  margin: 0.75rem 0 1rem;\n}\n.delete-option {\n  display: flex;\n  align-items: flex-start;\n  gap: 0.65rem;\n  padding: 0.7rem 0.85rem;\n  border: 1px solid var(--border);\n  border-radius: var(--radius-md);\n  cursor: pointer;\n}\n.delete-option:has(input:checked) {\n  border-color: var(--border-strong);\n  background: var(--surface);\n}\n.delete-option input {\n  margin-top: 0.25rem;\n  flex-shrink: 0;\n}\n.delete-option > span {\n  display: grid;\n  gap: 0.2rem;\n}\n/*# sourceMappingURL=admin-panel.css.map */\n', "/* src/app/pages/admin/grade-certificates.component.css */\n.certificate-form {\n  display: grid;\n  gap: 1rem;\n}\n.check {\n  display: flex;\n  align-items: center;\n  gap: 0.5rem;\n}\n.degree-input {\n  width: 5.5rem;\n  text-align: center;\n}\n.approval-chip {\n  display: inline-block;\n  padding: 0.15rem 0.6rem;\n  border-radius: 999px;\n  font-size: 0.85rem;\n  font-weight: 700;\n  white-space: nowrap;\n}\n.approval-chip.approved {\n  background: #e7f5df;\n  color: #3a7d1e;\n  border: 1px solid #b5d99c;\n}\n.approve-btn {\n  border-color: #3a7d1e;\n  color: #3a7d1e;\n}\n.sheet-head {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: flex-start;\n  justify-content: space-between;\n  gap: 0.75rem;\n  margin-bottom: 0.5rem;\n}\n.sheet-head h3 {\n  margin: 0;\n}\n.row-actions {\n  display: flex;\n  flex-wrap: wrap;\n  align-items: center;\n  gap: 0.4rem;\n}\n.form-actions {\n  display: flex;\n  flex-wrap: wrap;\n  gap: 0.6rem;\n  margin-top: 0.9rem;\n}\n.max-chip {\n  display: inline-block;\n  margin-inline-start: 0.35rem;\n  padding: 0.05rem 0.45rem;\n  border-radius: 999px;\n  font-size: 0.78rem;\n  border: 1px solid var(--border-strong);\n  color: var(--text-muted);\n}\n.total-cell {\n  font-weight: 800;\n}\ntr.is-open td {\n  background: var(--surface-strong);\n}\n/*# sourceMappingURL=grade-certificates.component.css.map */\n"] }]
  }], () => [], null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(GradeCertificatesComponent, { className: "GradeCertificatesComponent", filePath: "src/app/pages/admin/grade-certificates.component.ts", lineNumber: 49 });
})();
function blobToDataUrl(blob) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result ?? ""));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(blob);
  });
}
export {
  GradeCertificatesComponent
};
//# sourceMappingURL=chunk-A7VP2EVQ.js.map
