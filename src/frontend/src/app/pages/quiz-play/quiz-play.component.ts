import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Quiz, SubmitQuizResponse } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { QuestionPlayPromptComponent } from '../../shared/question-play-prompt/question-play-prompt.component';
import { answerableQuestions, flattenQuestions } from '../../shared/question-draft/question-draft.util';

@Component({
  selector: 'app-quiz-play',
  imports: [RouterLink, TranslatePipe, ApiBusyIndicatorComponent, QuestionPlayPromptComponent],
  templateUrl: './quiz-play.component.html',
  styleUrl: './quiz-play.component.css'
})
export class QuizPlayComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly locale = inject(LocaleService);

  readonly quiz = signal<Quiz | null>(null);
  readonly result = signal<SubmitQuizResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly answers = signal<Record<string, string>>({});
  readonly multiAnswers = signal<Record<string, Set<string>>>({});

  constructor() {
    const quizId = this.route.snapshot.paramMap.get('quizId')!;
    this.api.getQuiz(quizId).subscribe((quiz) => {
      this.quiz.set(quiz);
      const seed: Record<string, string> = {};
      const multi: Record<string, Set<string>> = {};
      for (const question of flattenQuestions(quiz.questions)) {
        seed[question.id] = '';
        if (question.questionType === 'MultiChoice') multi[question.id] = new Set();
      }
      this.answers.set(seed);
      this.multiAnswers.set(multi);
    });
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

  submit(): void {
    const quiz = this.quiz();
    if (!quiz || this.loading()) return;

    this.loading.set(true);
    this.error.set('');
    this.api.submitQuiz({
      quizId: quiz.id,
      answers: answerableQuestions(quiz.questions).map((question) => ({
        questionId: question.id,
        selectedOption: this.answers()[question.id] || ''
      }))
    }).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.result.set(response);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'play.submitQuizFailed'));
      }
    });
  }

  feedbackText(response: SubmitQuizResponse): string {
    return this.locale.fromApiFeedback(response);
  }
}
