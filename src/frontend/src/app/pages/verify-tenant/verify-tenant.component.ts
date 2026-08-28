import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { setCurrentTenantId } from '../../tenant';

@Component({
  selector: 'app-verify-tenant',
  standalone: true,
  imports: [RouterLink, TranslatePipe, LanguageSwitcherComponent, SiteBrandComponent, ApiBusyIndicatorComponent],
  templateUrl: './verify-tenant.component.html',
  styleUrl: '../forgot-password/forgot-password.component.css'
})
export class VerifyTenantComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly locale = inject(LocaleService);
  readonly brand = inject(SiteBrandService);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly success = signal('');
  readonly tenantId = signal('');

  ngOnInit(): void {
    const token = (this.route.snapshot.queryParamMap.get('token') ?? '').trim();
    if (!token) {
      this.loading.set(false);
      this.error.set(this.locale.t('auth.tenant.missingToken'));
      return;
    }

    this.auth.verifyTenant(token).subscribe({
      next: (result) => {
        const tenant = (result.tenantId ?? '').trim();
        setCurrentTenantId(tenant);
        this.tenantId.set(tenant);
        this.loading.set(false);
        this.success.set(result.message || this.locale.t('auth.tenant.verified'));
        void this.router.navigate(['/login'], { queryParams: tenant ? { tenant } : {} });
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'auth.tenant.verifyFailed'));
      }
    });
  }
}
