import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { TranslatePipe } from '../../shared/translate.pipe';

export interface PanelNavItem {
  labelKey: string;
  path: string;
  icon?: string;
}

const COLLAPSED_KEY = 'codekids_sidebar_collapsed';

@Component({
  selector: 'app-panel-shell',
  imports: [RouterLink, RouterLinkActive, TranslatePipe, LanguageSwitcherComponent],
  templateUrl: './panel-shell.component.html',
  styleUrl: './panel-shell.component.css'
})
export class PanelShellComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);
  readonly brand = inject(SiteBrandService);
  readonly collapsed = signal(false);

  @Input({ required: true }) titleKey = '';
  @Input({ required: true }) subtitleKey = '';
  @Input({ required: true }) navItems: PanelNavItem[] = [];

  ngOnInit(): void {
    this.collapsed.set(localStorage.getItem(COLLAPSED_KEY) === '1');
  }

  toggle(): void {
    const next = !this.collapsed();
    this.collapsed.set(next);
    localStorage.setItem(COLLAPSED_KEY, next ? '1' : '0');
  }

  iconFor(item: PanelNavItem): string {
    return item.icon || item.labelKey.slice(-1).toUpperCase();
  }

  collapseTitle(): string {
    return this.locale.t(this.collapsed() ? 'common.expandMenu' : 'common.collapseMenu');
  }
}
