import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { AskedQuestionsBoardComponent } from '../../shared/asked-questions-board/asked-questions-board.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';

@Component({
  selector: 'app-student-asked-questions',
  imports: [
    RouterLink,
    TranslatePipe,
    LanguageSwitcherComponent,
    SiteBrandComponent,
    AskedQuestionsBoardComponent,
    ApiBusyIndicatorComponent
  ],
  template: `
    <div class="page study-plan-page">
      <app-api-busy-indicator />
      <header class="topbar">
        <div>
          <app-site-brand />
          <h1>{{ 'askedQuestions.studentTitle' | t }}</h1>
        </div>
        <div class="topbar-actions">
          <app-language-switcher />
          <button type="button" class="ghost" (click)="auth.logout()">{{ 'common.signOut' | t }}</button>
        </div>
      </header>
      <a class="back" routerLink="/student">{{ 'common.backMissions' | t }}</a>
      <section class="block">
        <p class="section-hint">{{ 'askedQuestions.studentSubtitle' | t }}</p>
        <app-asked-questions-board />
      </section>
    </div>
  `,
  styleUrl: '../study-plan-view/study-plan-view.component.css'
})
export class StudentAskedQuestionsComponent {
  readonly auth = inject(AuthService);
}
