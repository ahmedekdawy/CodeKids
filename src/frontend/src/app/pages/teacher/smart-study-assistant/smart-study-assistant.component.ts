import { Component, ElementRef, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { LearningApiService } from '../../../learning-api.service';
import {
  SmartStudyAction,
  SmartStudyAssistantService,
  SmartStudyQuestion,
  SmartStudyResult,
  SmartStudyTarget,
  SmartStudyUnit
} from '../../../smart-study-assistant.service';
import { renderMarkdown } from '../../../shared/markdown-render';
import { SafeHtmlPipe } from '../../../shared/safe-html.pipe';
import { PageFeedbackComponent } from '../../../shared/page-feedback/page-feedback.component';
import { Course, CourseLesson, CourseUnit } from '../../../models';

type CourseOption = { id: string; title: string };
type UnitOption = { id: string; title: string };
type LessonOption = { id: string; title: string; unitId?: string | null };
type FileEntry = { file: File; preview: string };

@Component({
  selector: 'app-smart-study-assistant',
  imports: [FormsModule, SafeHtmlPipe, PageFeedbackComponent],
  templateUrl: './smart-study-assistant.component.html',
  styleUrls: ['./smart-study-assistant.component.css']
})
export class SmartStudyAssistantComponent {
  private readonly api = inject(LearningApiService);
  private readonly ai = inject(SmartStudyAssistantService);

  readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  readonly courses = signal<CourseOption[]>([]);
  readonly units = signal<UnitOption[]>([]);
  readonly lessons = signal<LessonOption[]>([]);

  readonly loading = signal(false);
  readonly applying = signal<SmartStudyTarget | null>(null);
  readonly error = signal('');
  readonly message = signal('');

  readonly courseId = signal('');
  readonly unitId = signal('');
  readonly lessonId = signal('');

  selectedCourseId = '';
  selectedUnitId = '';
  selectedLessonId = '';

  readonly files = signal<FileEntry[]>([]);
  prompt = '';
  questionCount = 5;

  readonly activeAction = signal<SmartStudyAction | null>(null);

  readonly result = signal<SmartStudyResult | null>(null);
  readonly resultTitle = signal('');
  readonly resultMarkdown = signal('');
  readonly renderedHtml = computed(() => renderMarkdown(this.resultMarkdown()));

  // Editable copy of the generated questions; this is what gets saved on apply.
  readonly questions = signal<SmartStudyQuestion[]>([]);
  readonly hasQuestions = computed(() => this.questions().length > 0);
  readonly hasUnits = computed(() => (this.result()?.units?.length ?? 0) > 0);

  readonly actions: { key: SmartStudyAction; label: string; icon: string; hint: string }[] = [
    { key: 'Outline', label: 'إنشاء فهرس للمادة', icon: '🗂️', hint: 'استخراج العناوين الرئيسية والفرعية في قائمة شجرية' },
    { key: 'Summary', label: 'تلخيص المحتوى', icon: '📝', hint: 'ملخص دراسي منظم بأهم النقاط' },
    { key: 'Assignment', label: 'توليد واجب / أسئلة مقالية', icon: '📚', hint: 'أسئلة تطبيقية تقيس فهم الدرس مع الإجابات' },
    { key: 'Quiz', label: 'إنشاء كويز / اختيار من متعدد', icon: '❓', hint: 'أسئلة اختيار من متعدد مع مفتاح الإجابات' }
  ];

  constructor() {
    void this.loadCourses();
  }

  private async loadCourses(): Promise<void> {
    this.error.set('');
    try {
      const courses = await firstValueFrom(this.api.getCourses(false));
      this.courses.set((courses as Course[]).map((c) => ({ id: c.id, title: c.title })));
    } catch {
      this.error.set('تعذر تحميل الكورسات. تحقق من الاتصال وأعد المحاولة.');
    }
  }

  async onCourseChange(courseId: string): Promise<void> {
    this.courseId.set(courseId);
    this.unitId.set('');
    this.lessonId.set('');
    this.selectedUnitId = '';
    this.selectedLessonId = '';
    this.units.set([]);
    this.lessons.set([]);
    if (!courseId) return;

    try {
      const course = await firstValueFrom(this.api.getCourse(courseId));
      this.units.set(((course.units ?? []) as CourseUnit[]).map((u) => ({ id: u.id, title: u.title })));
    } catch {
      this.error.set('تعذر تحميل وحدات هذا الكورس.');
    }
  }

  async onUnitChange(unitId: string): Promise<void> {
    this.unitId.set(unitId);
    this.lessonId.set('');
    this.selectedLessonId = '';
    this.lessons.set([]);
    if (!unitId) return;

    try {
      const course = await firstValueFrom(this.api.getCourse(this.courseId()));
      const unit = ((course.units ?? []) as CourseUnit[]).find((u) => u.id === unitId);
      this.lessons.set(
        ((unit?.lessons ?? []) as CourseLesson[]).map((l) => ({ id: l.id, title: l.title, unitId: unitId }))
      );
    } catch {
      this.error.set('تعذر تحميل دروس هذه الوحدة.');
    }
  }

  onFilesChosen(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.addFiles(Array.from(input.files ?? []));
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.addFiles(Array.from(event.dataTransfer?.files ?? []));
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  openFilePicker(): void {
    this.fileInput()?.nativeElement.click();
  }

  private addFiles(incoming: File[]): void {
    this.error.set('');
    if (incoming.length === 0) return;

    const accepted: FileEntry[] = [...this.files()];
    for (const file of incoming) {
      const ok = /\.(pdf|jpe?g|png)$/i.test(file.name)
        || file.type === 'application/pdf'
        || file.type.startsWith('image/');
      if (!ok) {
        this.error.set(`الملف "${file.name}" غير مدعوم. ارفع صور (JPG / PNG) أو ملفات PDF فقط.`);
        continue;
      }
      if (file.size > 25 * 1024 * 1024) {
        this.error.set(`الملف "${file.name}" كبير جداً. الحد الأقصى 25 ميجابايت.`);
        continue;
      }
      if (accepted.length >= 10) {
        this.error.set('الحد الأقصى 10 ملفات في المرة الواحدة.');
        break;
      }

      const entry: FileEntry = { file, preview: '' };
      if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => {
          entry.preview = String(reader.result ?? '');
          this.files.set([...this.files()]);
        };
        reader.readAsDataURL(file);
      }
      accepted.push(entry);
    }

    this.files.set(accepted);
  }

  removeFile(index: number): void {
    this.files.set(this.files().filter((_, i) => i !== index));
    const input = this.fileInput()?.nativeElement;
    if (input) input.value = '';
  }

  clearFiles(): void {
    this.files.set([]);
    const input = this.fileInput()?.nativeElement;
    if (input) input.value = '';
  }

  onCourseModelChange(value: string): void {
    this.selectedCourseId = value;
    void this.onCourseChange(value);
  }

  onUnitModelChange(value: string): void {
    this.selectedUnitId = value;
    void this.onUnitChange(value);
  }

  onLessonModelChange(value: string): void {
    this.selectedLessonId = value;
    this.lessonId.set(value);
  }

  async runAction(action: SmartStudyAction): Promise<void> {
    if (!this.courseId()) {
      this.error.set('اختر الكورس أولاً.');
      return;
    }
    if (!this.files().length && !this.prompt.trim()) {
      this.error.set('ارفع ملفاً أو اكتب وصفاً نصياً للمحتوى المطلوب.');
      return;
    }
    if (this.loading()) return;

    const wantsQuestions = action === 'Assignment' || action === 'Quiz';
    const questionCount = Math.trunc(Number(this.questionCount));
    if (wantsQuestions && !(questionCount >= 1 && questionCount <= 50)) {
      this.error.set('عدد الأسئلة يجب أن يكون بين 1 و 50.');
      return;
    }

    this.error.set('');
    this.message.set('');
    this.activeAction.set(action);
    this.loading.set(true);
    this.result.set(null);
    this.questions.set([]);
    this.resultTitle.set('');
    this.resultMarkdown.set('');

    try {
      const result = await firstValueFrom(this.ai.generate({
        action,
        courseId: this.courseId(),
        unitId: this.unitId() || null,
        lessonId: this.lessonId() || null,
        language: 'ar',
        prompt: this.prompt.trim() || null,
        questionCount: wantsQuestions ? questionCount : null,
        files: this.files().map((f) => f.file)
      }));
      this.result.set(result);
      this.questions.set((result.questions ?? []).map((q) => ({ ...q, options: [...(q.options ?? [])] })));
      this.resultTitle.set(result.title);
      this.resultMarkdown.set(result.markdown);
      this.message.set('تم إنشاء المحتوى بنجاح ✨');
    } catch (ex) {
      this.error.set(this.describeError(ex));
    } finally {
      this.loading.set(false);
      this.activeAction.set(null);
    }
  }

  async applyTo(target: SmartStudyTarget): Promise<void> {
    const result = this.result();
    if (!result || !this.courseId() || this.applying()) return;

    const questions = this.questions().map((q, i) => ({
      ...q,
      prompt: q.prompt.trim(),
      options: q.options.map((o) => o.trim()),
      sortOrder: i + 1
    }));
    if (target !== 'tree') {
      const invalid = questions.findIndex((q) => !q.prompt || q.options.some((o) => !o));
      if (invalid >= 0) {
        this.error.set(`السؤال رقم ${invalid + 1} يحتوي على نص سؤال أو اختيار فارغ. أكمله أو احذفه قبل الحفظ.`);
        return;
      }
    }

    this.error.set('');
    this.applying.set(target);
    try {
      const response = await firstValueFrom(this.ai.apply({
        courseId: this.courseId(),
        target,
        title: result.title || 'المساعد الذكي للدراسة',
        description: null,
        unitId: this.unitId() || null,
        lessonId: this.lessonId() || null,
        mode: 'update',
        questions: questions.length ? questions : null,
        units: result.units?.length ? result.units : null
      }));
      this.message.set(response.message);
    } catch (ex) {
      this.error.set(this.describeError(ex));
    } finally {
      this.applying.set(null);
    }
  }

  optionLetter(index: number): string {
    return String.fromCharCode(65 + index);
  }

  isCorrectOption(question: SmartStudyQuestion, index: number): boolean {
    const key = (question.correctOption ?? '').trim().toUpperCase();
    if (/^[A-Z]$/.test(key)) return key === this.optionLetter(index);
    const answer = (question.correctAnswer ?? '').trim();
    return answer.length > 0 && answer === (question.options[index] ?? '').trim();
  }

  updateQuestion(index: number, patch: Partial<SmartStudyQuestion>): void {
    this.questions.update((list) => list.map((q, i) => (i === index ? { ...q, ...patch } : q)));
  }

  updateOption(index: number, optionIndex: number, value: string): void {
    const question = this.questions()[index];
    if (!question) return;
    const wasCorrect = this.isCorrectOption(question, optionIndex);
    const options = question.options.map((o, i) => (i === optionIndex ? value : o));
    // Keep the stored answer text in step with the option it points at.
    this.updateQuestion(index, wasCorrect
      ? { options, correctOption: this.optionLetter(optionIndex), correctAnswer: value }
      : { options });
  }

  setCorrectOption(index: number, optionIndex: number): void {
    const question = this.questions()[index];
    if (!question) return;
    this.updateQuestion(index, {
      correctOption: this.optionLetter(optionIndex),
      correctAnswer: question.options[optionIndex] ?? ''
    });
  }

  updatePoints(index: number, value: number | string): void {
    const points = Math.trunc(Number(value));
    this.updateQuestion(index, { points: points >= 1 ? points : 1 });
  }

  removeQuestion(index: number): void {
    this.questions.update((list) => list.filter((_, i) => i !== index));
  }

  async copyResult(): Promise<void> {
    const text = this.resultMarkdown();
    if (!text) return;
    try {
      await navigator.clipboard.writeText(text);
      this.message.set('تم نسخ النص 📋');
    } catch {
      this.error.set('تعذر النسخ إلى الحافظة.');
    }
  }

  downloadPdf(): void {
    const title = this.resultTitle() || 'المساعد الذكي للدراسة';
    const html = this.renderedHtml();
    if (!html) return;

    const win = window.open('', '_blank');
    if (!win) {
      this.error.set('من فضلك اسمح بالنوافذ المنبثقة لتحميل الملف.');
      return;
    }

    win.document.write(`<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
  <meta charset="utf-8">
  <title>${title.replace(/[<>&]/g, '')}</title>
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
  <h1>${title.replace(/[<>&]/g, '')}</h1>
  ${html}
</body>
</html>`);
    win.document.close();
    win.focus();
    setTimeout(() => win.print(), 400);
  }

  private describeError(ex: unknown): string {
    if (typeof ex === 'object' && ex !== null && 'error' in ex) {
      const problem = (ex as { error?: { detail?: string; title?: string } }).error;
      const detail = problem?.detail || problem?.title;
      if (detail) return detail;
    }

    if (typeof ex === 'object' && ex !== null && 'status' in ex) {
      const status = (ex as { status?: number }).status;
      if (status === 0) return 'انقطع الاتصال بالخادم. تحقق من الشبكة وأعد المحاولة.';
      if (status === 401) return 'انتهت صلاحية الجلسة. من فضلك سجّل الدخول مرة أخرى.';
      if (status === 403) return 'لا تملك صلاحية تنفيذ هذا الإجراء على هذا الكورس.';
      if (status === 404) return 'العنصر المطلوب غير موجود.';
      return `حدث خطأ أثناء الاتصال بالخادم (رمز ${status}).`;
    }

    return 'حدث خطأ غير متوقع. أعد المحاولة.';
  }
}
