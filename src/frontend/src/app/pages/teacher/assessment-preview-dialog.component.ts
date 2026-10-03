import { Component, EventEmitter, Input, OnChanges, Output, signal } from '@angular/core';
import { TranslatePipe } from '../../shared/translate.pipe';
import { QuestionPlayPromptComponent } from '../../shared/question-play-prompt/question-play-prompt.component';
import { PlayableQuestion } from '../../shared/question-play-prompt/playable-question';
import { QuestionDraft } from '../../shared/question-draft/question-draft.model';
import {
  filledOptions,
  isMap,
  isParagraph,
  needsOptions,
  normalizeType
} from '../../shared/question-draft/question-draft.util';

/** What the preview dialog shows: the assessment as a student would open it. */
export interface AssessmentPreview {
  kind: 'exam' | 'quiz' | 'assignment';
  /** Set for a saved assessment; absent when previewing the unsaved form. */
  id?: string;
  title: string;
  description?: string | null;
  classroomName?: string | null;
  xpReward?: number | null;
  durationMinutes?: number | null;
  isPublished: boolean;
  questions: PlayableQuestion[];
}

/** Questions still being edited in a form, in the shape the student player renders. */
export function previewQuestionsFromDrafts(drafts: QuestionDraft[], prefix = 'preview'): PlayableQuestion[] {
  return drafts.map((draft, index) => {
    const id = draft.id || `${prefix}-${index + 1}`;
    const type = normalizeType(draft.questionType);
    return {
      id,
      prompt: draft.prompt || '',
      questionType: type,
      passageText: draft.passageText || '',
      options: needsOptions(type) ? filledOptions(draft.options) : [],
      promptImageUrl: draft.promptImageUrl ?? null,
      mapMarkers: isMap(type) ? draft.mapMarkers ?? [] : null,
      children: isParagraph(type) ? previewQuestionsFromDrafts(draft.children ?? [], `${id}-c`) : []
    };
  });
}

@Component({
  selector: 'app-assessment-preview-dialog',
  imports: [TranslatePipe, QuestionPlayPromptComponent],
  templateUrl: './assessment-preview-dialog.component.html',
  styleUrl: './assessment-preview-dialog.component.css'
})
export class AssessmentPreviewDialogComponent implements OnChanges {
  @Input({ required: true }) preview!: AssessmentPreview;

  @Output() readonly closed = new EventEmitter<void>();
  @Output() readonly publish = new EventEmitter<void>();

  // Answers typed in the preview stay local; nothing is submitted.
  readonly answers = signal<Record<string, string>>({});
  readonly multiAnswers = signal<Record<string, Set<string>>>({});

  ngOnChanges(): void {
    this.answers.set({});
    this.multiAnswers.set({});
  }

  questionCount(): number {
    return this.countAnswerable(this.preview.questions);
  }

  canPublish(): boolean {
    return !!this.preview.id && !this.preview.isPublished;
  }

  setAnswer(questionId: string, value: string): void {
    this.answers.update((current) => ({ ...current, [questionId]: value }));
  }

  toggleMulti(questionId: string, key: string): void {
    const current = this.multiAnswers();
    const set = new Set(current[questionId] || []);
    if (set.has(key)) set.delete(key);
    else set.add(key);
    this.multiAnswers.set({ ...current, [questionId]: set });
    this.setAnswer(questionId, [...set].sort().join(','));
  }

  close(): void {
    this.closed.emit();
  }

  private countAnswerable(questions: PlayableQuestion[]): number {
    return questions.reduce(
      (total, question) =>
        total + (isParagraph(question.questionType) ? this.countAnswerable(question.children ?? []) : 1),
      0
    );
  }
}
