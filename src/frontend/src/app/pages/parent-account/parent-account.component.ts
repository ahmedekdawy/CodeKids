import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';

/**
 * The signed-in parent's own account page: email, mobile, and password.
 * Split out of the parent dashboard so it has its own link and URL (/parent/account).
 */
@Component({
  selector: 'app-parent-account',
  imports: [
    FormsModule,
    RouterLink,
    TranslatePipe,
    SiteBrandComponent,
    ThemeSwitcherComponent,
    LanguageSwitcherComponent,
    ApiBusyIndicatorComponent
  ],
  templateUrl: './parent-account.component.html',
  styleUrl: './parent-account.component.css'
})
export class ParentAccountComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly saving = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  parentEmail = '';
  parentMobile = '';
  parentPassword = '';
  parentPasswordConfirm = '';

  constructor() {
    this.api.getParentDashboard().subscribe({
      next: (dashboard) => {
        this.parentEmail = dashboard.parentEmail ?? '';
        this.parentMobile = dashboard.parentMobilePhone ?? '';
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'parent.loadChildFailed'))
    });
  }

  saveAccount(): void {
    this.message.set('');
    this.error.set('');

    const email = this.parentEmail.trim();
    const mobilePhone = this.parentMobile.trim();
    if (!email && !mobilePhone) {
      this.error.set(this.locale.t('admin.users.emailOrMobileRequired'));
      return;
    }
    if (this.parentPassword || this.parentPasswordConfirm) {
      if (this.parentPassword !== this.parentPasswordConfirm) {
        this.error.set(this.locale.t('auth.reset.mismatch'));
        return;
      }
      if (this.parentPassword.trim().length < 6) {
        this.error.set(this.locale.t('api.errors.auth.passwordTooShort'));
        return;
      }
    }

    const userId = this.auth.user()?.id;
    if (!userId) return;

    this.saving.set(true);
    this.api
      .updateParentManagedAccount(userId, {
        email: email || null,
        mobilePhone: mobilePhone || null,
        password: this.parentPassword.trim() || null
      })
      .subscribe({
        next: (account) => {
          this.saving.set(false);
          this.parentPassword = '';
          this.parentPasswordConfirm = '';
          this.auth.patchUser({ email: account.email, mobilePhone: account.mobilePhone });
          this.message.set(this.locale.t('parent.accountSaved'));
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(this.locale.fromApiError(err, 'parent.accountSaveFailed'));
        }
      });
  }
}
