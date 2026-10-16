import { Component, DestroyRef, ElementRef, HostListener, Input, OnChanges, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { TranslatePipe } from '../../shared/translate.pipe';

export interface PanelNavItem {
  labelKey: string;
  path: string;
  icon?: string;
  /** Optional i18n key of the category this item belongs to; uncategorized items render before the groups. */
  categoryKey?: string;
}

/** One step of the navigation path above the page; the last step has no link. */
export interface PanelCrumb {
  labelKey: string;
  path?: string;
}

/** Trailing address segments that name a sub-page of a menu item. */
const SUB_PAGE_CRUMBS: Record<string, string> = {
  new: 'common.new',
  edit: 'common.edit'
};

export interface PanelNavCategory {
  /** i18n key of the category heading; empty string renders an unlabeled group. */
  labelKey: string;
  items: PanelNavItem[];
}

const COLLAPSED_KEY = 'codekids_sidebar_collapsed';
const NAV_COLLAPSED_KEY = 'codekids_nav_categories_collapsed';

@Component({
  selector: 'app-panel-shell',
  imports: [RouterLink, RouterLinkActive, TranslatePipe, LanguageSwitcherComponent, ThemeSwitcherComponent, ApiBusyIndicatorComponent],
  templateUrl: './panel-shell.component.html',
  styleUrl: './panel-shell.component.css'
})
export class PanelShellComponent implements OnInit, OnChanges {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);
  readonly brand = inject(SiteBrandService);
  readonly collapsed = signal(false);
  readonly menuOpen = signal(false);
  private readonly currentUrl = signal(this.router.url);
  private readonly navItemsVersion = signal(0);

  /** Home › category › page › sub-page, built from the menu so it always matches what the sidebar calls things. */
  readonly crumbs = computed<PanelCrumb[]>(() => {
    this.navItemsVersion();
    return this.buildCrumbs(this.currentUrl().split('?')[0].split('#')[0]);
  });

  @Input({ required: true }) titleKey = '';
  @Input({ required: true }) subtitleKey = '';
  @Input({ required: true }) navItems: PanelNavItem[] = [];

  /** Items without a category, in original order. */
  uncategorizedItems: PanelNavItem[] = [];

  /** Category groups, in the order the categories first appear in navItems. */
  categories: PanelNavCategory[] = [];

  /** Collapsed category keys (prefixed per shell), persisted to localStorage. */
  private readonly collapsedCategories = signal<ReadonlySet<string>>(new Set());

  constructor() {
    // Keep the category holding the active page expanded.
    this.router.events
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event) => {
        if (event instanceof NavigationEnd) {
          this.currentUrl.set(event.urlAfterRedirects);
          this.expandCategoryForUrl(event.urlAfterRedirects);
        }
      });
  }

  iconFor(item: PanelNavItem): string {
    return item.icon || item.labelKey.slice(-1).toUpperCase();
  }

  isCategoryCollapsed(categoryKey: string): boolean {
    return this.collapsedCategories().has(this.categoryStateKey(categoryKey));
  }

  toggleCategory(categoryKey: string): void {
    const key = this.categoryStateKey(categoryKey);
    this.updateCollapsedCategories((current) => {
      const next = new Set(current);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  }

  private categoryStateKey(categoryKey: string): string {
    return `${this.titleKey}|${categoryKey}`;
  }

  private updateCollapsedCategories(mutate: (current: ReadonlySet<string>) => ReadonlySet<string>): void {
    const next = mutate(this.collapsedCategories());
    this.collapsedCategories.set(next);
    this.persistCollapsedCategories(next);
  }

  private persistCollapsedCategories(value: ReadonlySet<string>): void {
    try {
      const map: Record<string, boolean> = {};
      for (const key of value) {
        map[key] = true;
      }
      localStorage.setItem(NAV_COLLAPSED_KEY, JSON.stringify(map));
    } catch {
      // Storage unavailable; collapse state simply won't persist.
    }
  }

  private loadCollapsedCategories(): void {
    try {
      const raw = localStorage.getItem(NAV_COLLAPSED_KEY);
      if (!raw) return;
      const parsed = JSON.parse(raw) as Record<string, unknown>;
      const restored = new Set(
        Object.entries(parsed)
          .filter(([, value]) => value === true)
          .map(([key]) => key)
      );
      this.collapsedCategories.set(restored);
    } catch {
      // Corrupt storage; start with all categories expanded.
    }
  }

  private expandCategoryForUrl(url: string): void {
    const path = url.split('?')[0].split('#')[0];
    const keysToExpand = this.categories
      .filter((category) => category.items.some((item) => path.startsWith(item.path)))
      .map((category) => this.categoryStateKey(category.labelKey));

    const current = this.collapsedCategories();
    if (!keysToExpand.some((key) => current.has(key))) {
      return;
    }

    this.updateCollapsedCategories((set) => {
      const next = new Set(set);
      for (const key of keysToExpand) {
        next.delete(key);
      }
      return next;
    });
  }

  private groupNavItems(): void {
    const categories: PanelNavCategory[] = [];
    const byKey = new Map<string, PanelNavCategory>();
    const uncategorized: PanelNavItem[] = [];

    for (const item of this.navItems) {
      const key = item.categoryKey ?? '';
      if (!key) {
        uncategorized.push(item);
        continue;
      }
      let category = byKey.get(key);
      if (!category) {
        category = { labelKey: key, items: [] };
        byKey.set(key, category);
        categories.push(category);
      }
      category.items.push(item);
    }

    this.uncategorizedItems = uncategorized;
    this.categories = categories;
  }

  /** The menu can change after load (it depends on the tenant style), so regroup whenever it does. */
  ngOnChanges(): void {
    this.groupNavItems();
    this.navItemsVersion.update((v) => v + 1);
  }

  private buildCrumbs(path: string): PanelCrumb[] {
    const home = this.navItems[0];
    const item = this.navItems
      .filter((x) => path === x.path || path.startsWith(x.path + '/'))
      .sort((a, b) => b.path.length - a.path.length)[0];

    const crumbs: PanelCrumb[] = [];
    if (home && home !== item) crumbs.push({ labelKey: home.labelKey, path: home.path });
    if (item) {
      if (item.categoryKey) crumbs.push({ labelKey: item.categoryKey });
      crumbs.push({ labelKey: item.labelKey, path: item.path });
      const subPage = path.slice(item.path.length).split('/').filter(Boolean).pop();
      const subKey = subPage ? SUB_PAGE_CRUMBS[subPage] : undefined;
      if (subKey) crumbs.push({ labelKey: subKey });
    }

    // A page outside the menu (a profile, say) names itself through its route's data.crumbKey.
    const routeKey = this.deepestCrumbKey(this.router.routerState.snapshot.root);
    if (routeKey) crumbs.push({ labelKey: routeKey });

    if (crumbs.length) delete crumbs[crumbs.length - 1].path;
    return crumbs;
  }

  private deepestCrumbKey(route: ActivatedRouteSnapshot): string | undefined {
    let key: string | undefined;
    for (let current: ActivatedRouteSnapshot | null = route; current; current = current.firstChild) {
      if (current.routeConfig?.data?.['crumbKey']) key = current.routeConfig.data['crumbKey'];
    }
    return key;
  }

  ngOnInit(): void {
    this.loadCollapsedCategories();
    this.expandCategoryForUrl(this.router.url);
    this.collapsed.set(localStorage.getItem(COLLAPSED_KEY) === '1');
  }

  toggle(): void {
    const next = !this.collapsed();
    this.collapsed.set(next);
    localStorage.setItem(COLLAPSED_KEY, next ? '1' : '0');
  }

  toggleMenu(event: Event): void {
    event.stopPropagation();
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  onNavClick(event: Event): void {
    if ((event.target as HTMLElement | null)?.closest('a')) {
      this.closeMenu();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.menuOpen()) return;
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.closeMenu();
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if (window.innerWidth > 900) {
      this.closeMenu();
    }
  }

  collapseTitle(): string {
    return this.locale.t(this.collapsed() ? 'common.expandMenu' : 'common.collapseMenu');
  }
}
