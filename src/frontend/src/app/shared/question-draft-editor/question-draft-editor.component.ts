import { Component, Input, inject } from '@angular/core';
import { ControlContainer, FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { IconActionButtonComponent } from '../icon-action-button/icon-action-button.component';
import { MathPromptEditorComponent } from '../math-prompt-editor/math-prompt-editor.component';
import { QuestionImageUploadComponent } from '../question-image-upload/question-image-upload.component';
import { SearchableSelectComponent } from '../searchable-select/searchable-select.component';
import { TranslatePipe } from '../translate.pipe';
import { QuestionDraft } from '../question-draft/question-draft.model';
import {
  applyTypeDefaults,
  childQuestionTypes,
  editorTypes,
  filledOptions,
  isFreeText,
  isMulti,
  isParagraph,
  isShortAnswer,
  isTeacherGradedText,
  needsOptions,
  optionLabel,
  questionTypeLabelKey
} from '../question-draft/question-draft.util';

@Component({
  selector: 'app-question-draft-editor',
  imports: [
    FormsModule,
    MathPromptEditorComponent,
    SearchableSelectComponent,
    TranslatePipe,
    QuestionImageUploadComponent,
    IconActionButtonComponent
  ],
  templateUrl: './question-draft-editor.component.html',
  styleUrl: './question-draft-editor.component.css',
  viewProviders: [{ provide: ControlContainer, useValue: null }]
})
export class QuestionDraftEditorComponent {
  private readonly locale = inject(LocaleService);

  @Input({ required: true }) draft!: QuestionDraft;
  @Input() namePrefix = 'q';
  @Input() allowShortAnswer = false;
  @Input() allowComposite = true;
  @Input() allowFreeText = true;

  types(): ReturnType<typeof editorTypes> {
    return editorTypes(this.allowShortAnswer, this.allowFreeText).filter(
      (type) => this.allowComposite || (type !== 'Paragraph' && type !== 'Underline')
    );
  }

  childTypes(): ReturnType<typeof childQuestionTypes> {
    return childQuestionTypes(this.allowShortAnswer).filter(
      (type) => this.allowFreeText || type !== 'FreeText'
    );
  }

  typeLabel(type: string): string {
    return this.locale.t(questionTypeLabelKey(type));
  }

  isParagraph(type: string = this.draft.questionType): boolean {
    return isParagraph(type);
  }

  isShortAnswer(type: string = this.draft.questionType): boolean {
    return isShortAnswer(type);
  }

  isFreeText(type: string = this.draft.questionType): boolean {
    return isFreeText(type);
  }

  isTeacherGradedText(type: string = this.draft.questionType): boolean {
    return isTeacherGradedText(type);
  }

  needsOptions(type: string = this.draft.questionType): boolean {
    return needsOptions(type);
  }

  isMulti(type: string = this.draft.questionType): boolean {
    return isMulti(type);
  }

  optionLabel(index: number): string {
    return optionLabel(index);
  }

  filled(list = this.draft.options) {
    return filledOptions(list);
  }

  onTypeChange(): void {
    applyTypeDefaults(this.draft);
  }

  applyTypeDefaults = applyTypeDefaults;

  addOption(): void {
    if (this.draft.options.length >= 26) return;
    this.draft.options.push({ text: '' });
  }

  removeOption(index: number): void {
    if (this.draft.options.length <= 2) return;
    this.draft.options.splice(index, 1);
    this.syncCorrect();
  }

  syncCorrect(): void {
    const keys = new Set(this.filled().map((option) => option.key));
    if (this.isMulti()) {
      this.draft.correctKeys = this.draft.correctKeys.filter((key) => keys.has(key));
      this.draft.correctAnswer = this.draft.correctKeys.join(',');
    } else if (this.draft.correctAnswer && !keys.has(this.draft.correctAnswer)) {
      this.draft.correctAnswer = '';
    }
  }

  toggleCorrectKey(key: string): void {
    const set = new Set(this.draft.correctKeys);
    if (set.has(key)) set.delete(key);
    else set.add(key);
    this.draft.correctKeys = [...set].sort();
    this.draft.correctAnswer = this.draft.correctKeys.join(',');
  }

  addChild(): void {
    this.draft.children.push({
      prompt: '',
      questionType: 'SingleChoice',
      passageText: '',
      options: [{ text: '' }, { text: '' }],
      correctAnswer: '',
      correctKeys: [],
      points: 1,
      children: []
    });
  }

  removeChild(index: number): void {
    this.draft.children.splice(index, 1);
  }

  addChildOption(child: QuestionDraft): void {
    if (child.options.length >= 26) return;
    child.options.push({ text: '' });
  }

  removeChildOption(child: QuestionDraft, index: number): void {
    if (child.options.length <= 2) return;
    child.options.splice(index, 1);
    const keys = new Set(filledOptions(child.options).map((option) => option.key));
    if (child.questionType === 'MultiChoice') {
      child.correctKeys = child.correctKeys.filter((key) => keys.has(key));
      child.correctAnswer = child.correctKeys.join(',');
    } else if (child.correctAnswer && !keys.has(child.correctAnswer)) {
      child.correctAnswer = '';
    }
  }

  toggleChildCorrectKey(child: QuestionDraft, key: string): void {
    const set = new Set(child.correctKeys);
    if (set.has(key)) set.delete(key);
    else set.add(key);
    child.correctKeys = [...set].sort();
    child.correctAnswer = child.correctKeys.join(',');
  }
}
