import { Component } from '@angular/core';
import { TranslatePipe } from '../../shared/translate.pipe';
import { AskedQuestionsBoardComponent } from '../../shared/asked-questions-board/asked-questions-board.component';

@Component({
  selector: 'app-teacher-asked-questions',
  imports: [TranslatePipe, AskedQuestionsBoardComponent],
  template: `
    <div class="panel-page">
      <h2>{{ 'askedQuestions.teacherTitle' | t }}</h2>
      <p class="meta">{{ 'askedQuestions.teacherSubtitle' | t }}</p>
      <app-asked-questions-board [canAnswer]="true" />
    </div>
  `,
  styleUrl: './teacher-panel.css'
})
export class TeacherAskedQuestionsComponent {}
