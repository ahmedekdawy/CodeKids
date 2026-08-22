import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { setCurrentTenantId } from '../../tenant';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent, SiteBrandComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly locale = inject(LocaleService);
  readonly brand = inject(SiteBrandService);

  readonly form = this.fb.nonNullable.group({
    login: ['', Validators.required],
    password: ['', Validators.required]
  });
  readonly loading = signal(false);
  readonly error = signal('');

  ngOnInit(): void {
    const tenant = (this.route.snapshot.queryParamMap.get('tenant') ?? '').trim();
    if (tenant) {
      setCurrentTenantId(tenant);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { login, password } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.auth.login(login.trim(), password).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl(this.auth.roleHome());
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'auth.signInFailed'));
      }
    });
  }
}
