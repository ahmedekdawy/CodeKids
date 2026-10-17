import { Injectable, inject, signal } from '@angular/core';
import { AuthService } from './auth.service';
import type { PanelNavItem } from './layouts/panel-shell/panel-shell.component';
import { LearningApiService } from './learning-api.service';
import { MyMenu } from './models';
import { ownerFilteredNavItems } from './panel-nav';
import { SiteBrandService } from './site-brand.service';
import { navItemsForStyle } from './tenant-style';

/**
 * Loads the current user's menu once and shares it with both panel shells. The menu is built from
 * the pages their roles expose; users can reorder and hide pages in their own copy.
 */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly api = inject(LearningApiService);
  private readonly brand = inject(SiteBrandService);
  private readonly auth = inject(AuthService);

  readonly items = signal<PanelNavItem[]>([]);
  readonly roles = signal<string[]>([]);
  /** False until the first load settles, so shells can show their static fallback meanwhile. */
  readonly loaded = signal(false);
  readonly error = signal('');

  private loading = false;

  load(force = false): void {
    if (this.loading || (this.loaded() && !force)) {
      return;
    }

    this.loading = true;
    this.api.getMyMenu().subscribe({
      next: (menu) => {
        this.apply(menu);
        this.loading = false;
      },
      error: () => {
        // The static menu remains a safe fallback while the API is unavailable.
        this.loading = false;
        this.loaded.set(true);
      }
    });
  }

  apply(menu: MyMenu): void {
    const visible = (menu.items ?? [])
      .filter((item) => !item.isHidden)
      .map<PanelNavItem>((item) => ({
        labelKey: item.labelKey,
        path: item.key.startsWith('/') ? item.key : `/${item.key}`,
        icon: item.icon,
        categoryKey: item.categoryKey || undefined
      }));

    const styled = navItemsForStyle(visible, this.brand.tenantStyle());
    this.items.set(this.auth.user()?.isTenantOwner ? ownerFilteredNavItems(styled) : styled);
    this.roles.set(menu.roles ?? []);
    this.error.set('');
    this.loaded.set(true);
  }
}
