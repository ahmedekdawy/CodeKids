import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';

@Component({
  selector: 'app-assessment-link-entry',
  standalone: true,
  imports: [
    RouterLink,
    TranslatePipe,
    LanguageSwitcherComponent,
    ThemeSwitcherComponent,
    SiteBrandComponent,
    ApiBusyIndicatorComponent
  ],
  templateUrl: './assessment-link-entry.component.html',
  styleUrl: '../forgot-password/forgot-password.component.css'
})
export class AssessmentLinkEntryComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly locale = inject(LocaleService);
  readonly brand = inject(SiteBrandService);

  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    const key = (this.route.snapshot.paramMap.get('key') ?? '').trim();
    if (!key) {
      this.loading.set(false);
      this.error.set(this.locale.t('auth.assessmentLink.missingKey'));
      return;
    }

    this.auth.redeemAssessmentLink(key).subscribe({
      next: (result) => {
        const path = (result.redirectPath || '').trim() || this.auth.roleHome();
        void this.router.navigateByUrl(path);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'auth.assessmentLink.redeemFailed'));
      }
    });
  }
}
