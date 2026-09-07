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

  choiceOptions(question: PlayableQuestion): ChoiceOption[] {
    if (question.options?.length) return question.options;
    const legacy: ChoiceOption[] = [];
    if (question.optionA) legacy.push({ key: 'A', text: question.optionA });
    if (question.optionB) legacy.push({ key: 'B', text: question.optionB });
    if (question.optionC) legacy.push({ key: 'C', text: question.optionC });
    if (question.optionD) legacy.push({ key: 'D', text: question.optionD });
    return legacy;
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
}
