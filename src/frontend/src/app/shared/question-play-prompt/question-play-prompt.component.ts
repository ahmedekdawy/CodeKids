import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChoiceOption } from '../../models';
import { QuestionImageDisplayComponent } from '../question-image-display/question-image-display.component';
import { SafeHtmlPipe } from '../safe-html.pipe';
import { StudentAnswerUploadComponent } from '../student-answer-upload/student-answer-upload.component';
import { TranslatePipe } from '../translate.pipe';
import { AnswerImageDraft, PlayableQuestion } from './playable-question';

@Component({
  selector: 'app-question-play-prompt',
  imports: [
    FormsModule,
    SafeHtmlPipe,
    TranslatePipe,
    QuestionImageDisplayComponent,
    StudentAnswerUploadComponent
  ],
  templateUrl: './question-play-prompt.component.html',
  styleUrl: './question-play-prompt.component.css'
})
export class QuestionPlayPromptComponent {
  @Input({ required: true }) question!: PlayableQuestion;
  @Input() answers: Record<string, string> = {};
  @Input() multiAnswers: Record<string, Set<string>> = {};
  @Input() answerImages: Record<string, AnswerImageDraft> = {};
  @Input() answerUpload: 'all' | 'text' | 'none' = 'none';

  @Output() readonly answerChange = new EventEmitter<{ questionId: string; value: string }>();
  @Output() readonly multiToggle = new EventEmitter<{ questionId: string; key: string }>();
  @Output() readonly answerImageChange = new EventEmitter<{
    questionId: string;
    mediaAssetId: string | null;
    imageUrl: string | null;
  }>();

  private readonly orderLists = new Map<string, ChoiceOption[]>();
  private orderDragFrom: number | null = null;
  private orderDragQuestionId: string | null = null;

  choiceOptions(question: PlayableQuestion): ChoiceOption[] {
    if (question.options?.length) return question.options;
    const legacy: ChoiceOption[] = [];
    if (question.optionA) legacy.push({ key: 'A', text: question.optionA });
    if (question.optionB) legacy.push({ key: 'B', text: question.optionB });
    if (question.optionC) legacy.push({ key: 'C', text: question.optionC });
    if (question.optionD) legacy.push({ key: 'D', text: question.optionD });
    return legacy;
  }

  orderedOptions(question: PlayableQuestion): ChoiceOption[] {
    let list = this.orderLists.get(question.id);
    if (!list) {
      list = this.shuffle(this.choiceOptions(question));
      this.orderLists.set(question.id, list);
      const value = list.map((option) => option.key).join(',');
      if ((this.answers[question.id] || '') !== value) {
        queueMicrotask(() => this.setAnswer(question.id, value));
      }
    }
    return list;
  }

  isMultiChecked(questionId: string, key: string): boolean {
    return this.multiAnswers[questionId]?.has(key) === true;
  }

  showUpload(type: string): boolean {
    if (type === 'Paragraph') return false;
    if (this.answerUpload === 'all') return true;
    if (this.answerUpload === 'text') return type === 'ShortAnswer' || type === 'FreeText';
    return false;
  }

  setAnswer(questionId: string, value: string): void {
    this.answerChange.emit({ questionId, value });
  }

  toggleMulti(questionId: string, key: string): void {
    this.multiToggle.emit({ questionId, key });
  }

  setAnswerImage(questionId: string, mediaAssetId: string | null, imageUrl: string | null): void {
    this.answerImageChange.emit({ questionId, mediaAssetId, imageUrl });
  }

  onOrderDragStart(event: DragEvent, questionId: string, index: number): void {
    this.orderDragFrom = index;
    this.orderDragQuestionId = questionId;
    event.dataTransfer?.setData('text/plain', String(index));
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  onOrderDragOver(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
  }

  onOrderDrop(event: DragEvent, questionId: string, targetIndex: number): void {
    event.preventDefault();
    if (this.orderDragFrom == null || this.orderDragQuestionId !== questionId) return;
    const list = this.orderLists.get(questionId);
    if (!list) return;
    const from = this.orderDragFrom;
    if (from === targetIndex) {
      this.orderDragFrom = null;
      this.orderDragQuestionId = null;
      return;
    }
    const [item] = list.splice(from, 1);
    list.splice(targetIndex, 0, item);
    this.orderLists.set(questionId, [...list]);
    this.setAnswer(questionId, list.map((option) => option.key).join(','));
    this.orderDragFrom = null;
    this.orderDragQuestionId = null;
  }

  onOrderDragEnd(): void {
    this.orderDragFrom = null;
    this.orderDragQuestionId = null;
  }

  moveOrderItem(questionId: string, index: number, delta: number): void {
    const list = this.orderLists.get(questionId);
    if (!list) return;
    const next = index + delta;
    if (next < 0 || next >= list.length) return;
    const [item] = list.splice(index, 1);
    list.splice(next, 0, item);
    this.orderLists.set(questionId, [...list]);
    this.setAnswer(questionId, list.map((option) => option.key).join(','));
  }

  private shuffle(items: ChoiceOption[]): ChoiceOption[] {
    const copy = [...items];
    for (let i = copy.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [copy[i], copy[j]] = [copy[j], copy[i]];
    }
    // Avoid leaving the already-correct order when possible.
    if (copy.length > 1 && copy.every((item, index) => item.key === items[index]?.key)) {
      [copy[0], copy[1]] = [copy[1], copy[0]];
    }
    return copy;
  }
}
