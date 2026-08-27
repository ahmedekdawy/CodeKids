import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { UserRole } from '../../models';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [SearchableSelectComponent, ReactiveFormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent, SiteBrandComponent, ApiBusyIndicatorComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly locale = inject(LocaleService);
  readonly brand = inject(SiteBrandService);

  readonly form = this.fb.nonNullable.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    role: ['Student' as UserRole, Validators.required],
    parentId: ['']
  });
  readonly loading = signal(false);
  readonly error = signal('');

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, displayName, password, role, parentId } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.auth.register({
      email,
      displayName,
      password,
      role,
      parentId: parentId || null
    }).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl(this.auth.roleHome());
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'auth.registerFailed'));
      }
    });
  }
}
