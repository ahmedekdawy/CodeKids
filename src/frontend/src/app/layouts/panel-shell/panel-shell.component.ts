import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../auth.service';

export interface PanelNavItem {
  label: string;
  path: string;
  icon?: string;
}

const COLLAPSED_KEY = 'codekids_sidebar_collapsed';

@Component({
  selector: 'app-panel-shell',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './panel-shell.component.html',
  styleUrl: './panel-shell.component.css'
})
export class PanelShellComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly collapsed = signal(false);

  @Input({ required: true }) title = '';
  @Input({ required: true }) subtitle = '';
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
    return item.icon || item.label.trim().charAt(0).toUpperCase();
  }
}
