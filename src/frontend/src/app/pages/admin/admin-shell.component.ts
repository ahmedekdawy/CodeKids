import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PanelShellComponent } from '../../layouts/panel-shell/panel-shell.component';
import { SiteBrandService } from '../../site-brand.service';
import { AuthService } from '../../auth.service';
import { MenuService } from '../../menu.service';
import { panelNavItems } from '../../panel-nav';

@Component({
  selector: 'app-admin-shell',
  imports: [PanelShellComponent, RouterOutlet],
  template: `
    <app-panel-shell
      titleKey="shell.admin.title"
      subtitleKey="shell.admin.subtitle"
      [navItems]="navItems()">
      <router-outlet />
    </app-panel-shell>
  `
})
export class AdminShellComponent {
  private readonly brand = inject(SiteBrandService);
  private readonly auth = inject(AuthService);
  private readonly menu = inject(MenuService);

  readonly navItems = computed(() => {
    const items = this.menu.items();
    return this.menu.loaded()
      ? items
      : panelNavItems('admin', this.brand.tenantStyle(), this.auth.user()?.isTenantOwner === true);
  });

  constructor() {
    this.menu.load();
  }
}
