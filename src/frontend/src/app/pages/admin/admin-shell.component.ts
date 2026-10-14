import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PanelShellComponent } from '../../layouts/panel-shell/panel-shell.component';
import { SiteBrandService } from '../../site-brand.service';
import { AuthService } from '../../auth.service';
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

  readonly navItems = computed(() =>
    panelNavItems('admin', this.brand.tenantStyle(), this.auth.user()?.isTenantOwner === true)
  );
}
