import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

/**
 * The signed-in teacher's own account page: email, mobile, and password.
 * Split out of the overview page so it has its own route and nav link (/teacher/account).
 */
@Component({
  selector: 'app-teacher-account',
  imports: [FormsModule, TranslatePipe, PageFeedbackComponent],
  templateUrl: './teacher-account.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherAccountComponent {
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);

  readonly saving = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  email = this.auth.user()?.email ?? '';
  mobilePhone = this.auth.user()?.mobilePhone ?? '';
  password = '';
  passwordConfirm = '';

  saveAccount(): void {
    this.error.set('');
    this.message.set('');
    if (!this.email.trim() && !this.mobilePhone.trim()) {
      this.error.set(this.locale.t('admin.users.emailOrMobileRequired'));
      return;
    }
    if (this.password || this.passwordConfirm) {
      if (this.password !== this.passwordConfirm) {
        this.error.set(this.locale.t('auth.reset.mismatch'));
        return;
      }
      if (this.password.trim().length < 6) {
        this.error.set(this.locale.t('api.errors.auth.passwordTooShort'));
        return;
      }
    }

    this.saving.set(true);
    this.auth
      .updateAccount({
        email: this.email.trim() || null,
        mobilePhone: this.mobilePhone.trim() || null,
        password: this.password.trim() || null
      })
      .subscribe({
        next: (user) => {
          this.saving.set(false);
          this.email = user.email ?? '';
          this.mobilePhone = user.mobilePhone ?? '';
          this.password = '';
          this.passwordConfirm = '';
          this.message.set(this.locale.t('teacher.account.saved'));
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.account.saveFailed'));
        }
      });
  }
}
