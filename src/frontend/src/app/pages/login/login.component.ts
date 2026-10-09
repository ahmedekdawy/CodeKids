import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { safeReturnUrl } from '../../auth.guard';
import { AuthService } from '../../auth.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { TopStudentsBoardComponent } from '../../shared/top-students-board/top-students-board.component';
import { currentTenantId, normalizeTenantId, setCurrentTenantId } from '../../tenant';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent, ThemeSwitcherComponent, SiteBrandComponent, ApiBusyIndicatorComponent, TopStudentsBoardComponent],
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
    tenant: [''],
    login: ['', Validators.required],
    password: ['', Validators.required]
  });
  /** True when nothing identified the tenant, so the form asks for it. */
  readonly askTenant = signal(false);
  readonly loading = signal(false);
  readonly error = signal('');

  ngOnInit(): void {
    const tenant = (this.route.snapshot.queryParamMap.get('tenant') ?? '').trim();
    if (tenant) {
      setCurrentTenantId(tenant);
    }

    if (this.auth.isLoggedIn()) {
      void this.router.navigateByUrl(this.postLoginUrl());
      return;
    }

    if (!currentTenantId()) {
      this.askTenant.set(true);
      this.form.controls.tenant.addValidators(Validators.required);
      this.form.controls.tenant.updateValueAndValidity();
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { tenant, login, password } = this.form.getRawValue();
    this.error.set('');
    if (!this.askTenant()) {
      this.signIn(login, password);
      return;
    }

    const tenantId = normalizeTenantId(tenant);
    if (!tenantId) {
      this.error.set(this.locale.t('auth.tenant.invalid'));
      return;
    }

    // Requests read the tenant from the query string first, so update the URL before signing in.
    setCurrentTenantId(tenantId);
    void this.router
      .navigate([], { relativeTo: this.route, queryParams: { tenant: tenantId }, queryParamsHandling: 'merge', replaceUrl: true })
      .then(() => {
        this.brand.load();
        this.signIn(login, password);
      });
  }

  private signIn(login: string, password: string): void {
    this.loading.set(true);
    this.auth.login(login.trim(), password).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl(this.postLoginUrl());
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'auth.signInFailed'));
      }
    });
  }

  private postLoginUrl(): string {
    return safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')) ?? this.auth.roleHome();
  }
}
