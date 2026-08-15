import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-register-tenant',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent, SiteBrandComponent],
  templateUrl: './register-tenant.component.html',
  styleUrl: '../forgot-password/forgot-password.component.css'
})
export class RegisterTenantComponent {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly locale = inject(LocaleService);
  readonly brand = inject(SiteBrandService);

  readonly form = this.fb.nonNullable.group({
    tenantName: ['', Validators.required],
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });
  readonly loading = signal(false);
  readonly error = signal('');
  readonly success = signal('');

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.success.set('');
    this.auth
      .registerTenant({
        tenantName: value.tenantName.trim(),
        displayName: value.displayName.trim(),
        email: value.email.trim(),
        password: value.password
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.success.set(this.locale.t('auth.tenant.checkEmail'));
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(this.locale.fromApiError(err, 'auth.tenant.registerFailed'));
        }
      });
  }
}
