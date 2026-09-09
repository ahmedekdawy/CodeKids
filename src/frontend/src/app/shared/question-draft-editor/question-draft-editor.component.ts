import { Component, Input, inject } from '@angular/core';
import { ControlContainer, FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { IconActionButtonComponent } from '../icon-action-button/icon-action-button.component';
import { MapQuestionBoardComponent } from '../map-question-board/map-question-board.component';
import { MathPromptEditorComponent } from '../math-prompt-editor/math-prompt-editor.component';
import { QuestionImageUploadComponent } from '../question-image-upload/question-image-upload.component';
import { SearchableSelectComponent } from '../searchable-select/searchable-select.component';
import { TranslatePipe } from '../translate.pipe';
import { MapMarkerDraft, QuestionDraft } from '../question-draft/question-draft.model';
import {
  applyTypeDefaults,
  childQuestionTypes,
  editorTypes,
  encodeMapAnswers,
  filledOptions,
  isFreeText,
  isMap,
  isMulti,
  isOrder,
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
    IconActionButtonComponent,
    MapQuestionBoardComponent
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

  dragIndex: number | null = null;
  dragScope: string | null = null;

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

  isOrder(type: string = this.draft.questionType): boolean {
    return isOrder(type);
  }

  isMap(type: string = this.draft.questionType): boolean {
    return isMap(type);
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

  onMapMarkersChange(markers: MapMarkerDraft[]): void {
    this.draft.mapMarkers = markers;
    this.draft.correctAnswer = encodeMapAnswers(markers);
  }

  addOption(): void {
    if (this.draft.options.length >= 26) return;
    this.draft.options.push({ text: '' });
    this.syncCorrect();
  }

  removeOption(index: number): void {
    if (this.draft.options.length <= 2) return;
    this.draft.options.splice(index, 1);
    this.syncCorrect();
  }

  syncCorrect(): void {
    const keys = new Set(this.filled().map((option) => option.key));
    if (this.isOrder()) {
      this.draft.correctKeys = [];
      this.draft.correctAnswer = this.filled()
        .map((option) => option.key)
        .join(',');
      return;
    }
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

  onDragStart(event: DragEvent, index: number, scope: string): void {
    this.dragIndex = index;
    this.dragScope = scope;
    event.dataTransfer?.setData('text/plain', String(index));
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
  }

  onDragEnd(): void {
    this.dragIndex = null;
    this.dragScope = null;
  }

  onRootDrop(event: DragEvent, targetIndex: number): void {
    event.preventDefault();
    if (this.dragIndex == null || this.dragScope !== 'root') return;
    this.moveOption(this.draft.options, this.dragIndex, targetIndex);
    this.syncCorrect();
    this.onDragEnd();
  }

  onChildDrop(event: DragEvent, child: QuestionDraft, targetIndex: number): void {
    event.preventDefault();
    if (this.dragIndex == null || !this.dragScope?.startsWith('c')) return;
    this.moveOption(child.options, this.dragIndex, targetIndex);
    if (isOrder(child.questionType)) {
      child.correctKeys = [];
      child.correctAnswer = filledOptions(child.options)
        .map((option) => option.key)
        .join(',');
    }
    this.onDragEnd();
  }

  private moveOption(list: { text: string }[], from: number, to: number): void {
    if (from === to || from < 0 || to < 0 || from >= list.length || to >= list.length) return;
    const [item] = list.splice(from, 1);
    list.splice(to, 0, item);
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
      children: [],
      mapMarkers: []
    });
  }

  removeChild(index: number): void {
    this.draft.children.splice(index, 1);
  }

  addChildOption(child: QuestionDraft): void {
    if (child.options.length >= 26) return;
    child.options.push({ text: '' });
    if (isOrder(child.questionType)) {
      child.correctAnswer = filledOptions(child.options)
        .map((option) => option.key)
        .join(',');
    }
  }

  removeChildOption(child: QuestionDraft, index: number): void {
    if (child.options.length <= 2) return;
    child.options.splice(index, 1);
    const keys = new Set(filledOptions(child.options).map((option) => option.key));
    if (child.questionType === 'MultiChoice') {
      child.correctKeys = child.correctKeys.filter((key) => keys.has(key));
      child.correctAnswer = child.correctKeys.join(',');
    } else if (isOrder(child.questionType)) {
      child.correctKeys = [];
      child.correctAnswer = filledOptions(child.options)
        .map((option) => option.key)
        .join(',');
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
