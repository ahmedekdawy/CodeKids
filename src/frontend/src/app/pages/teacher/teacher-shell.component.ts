import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PanelShellComponent } from '../../layouts/panel-shell/panel-shell.component';
import { SiteBrandService } from '../../site-brand.service';
import { AuthService } from '../../auth.service';
import { panelNavItems } from '../../panel-nav';

@Component({
  selector: 'app-teacher-shell',
  imports: [PanelShellComponent, RouterOutlet],
  template: `
    <app-panel-shell
      titleKey="shell.teacher.title"
      subtitleKey="shell.teacher.subtitle"
      [navItems]="navItems()">
      <router-outlet />
    </app-panel-shell>
  `
})
export class TeacherShellComponent {
  private readonly brand = inject(SiteBrandService);
  private readonly auth = inject(AuthService);

  readonly navItems = computed(() =>
    panelNavItems('teacher', this.brand.tenantStyle(), this.auth.user()?.isTenantOwner === true)
  );
}
