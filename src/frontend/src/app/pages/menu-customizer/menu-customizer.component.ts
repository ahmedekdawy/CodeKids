import { Component, inject, signal } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { MenuPage } from '../../models';
import { MenuService } from '../../menu.service';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { TranslatePipe } from '../../shared/translate.pipe';

/**
 * Lets any user reorder the pages their roles expose and hide the ones they don't use. The page set
 * itself comes from the roles, so it cannot be widened here.
 */
@Component({
  selector: 'app-menu-customizer',
  imports: [PageFeedbackComponent, TranslatePipe],
  templateUrl: './menu-customizer.component.html',
  styleUrl: './menu-customizer.component.css'
})
export class MenuCustomizerComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly menu = inject(MenuService);

  readonly items = signal<MenuPage[]>([]);
  readonly roles = signal<string[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  constructor() {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.getMyMenu().subscribe({
      next: (menu) => {
        this.items.set(menu.items ?? []);
        this.roles.set(menu.roles ?? []);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'menu.loadFailed'));
      }
    });
  }

  move(index: number, delta: number): void {
    const list = [...this.items()];
    const target = index + delta;
    if (target < 0 || target >= list.length) {
      return;
    }

    [list[index], list[target]] = [list[target], list[index]];
    this.items.set(list);
  }

  toggleHidden(index: number): void {
    this.items.set(
      this.items().map((item, i) => (i === index ? { ...item, isHidden: !item.isHidden } : item))
    );
  }

  save(): void {
    this.error.set('');
    this.api
      .saveMyMenu(this.items().map((item) => ({ key: item.key, isHidden: item.isHidden })))
      .subscribe({
        next: (menu) => {
          this.items.set(menu.items ?? []);
          this.roles.set(menu.roles ?? []);
          this.menu.apply(menu);
          this.message.set(this.locale.t('menu.saved'));
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'menu.saveFailed'))
      });
  }

  reset(): void {
    this.error.set('');
    this.api.resetMyMenu().subscribe({
      next: (menu) => {
        this.items.set(menu.items ?? []);
        this.roles.set(menu.roles ?? []);
        this.menu.apply(menu);
        this.message.set(this.locale.t('menu.reset'));
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'menu.resetFailed'))
    });
  }
}
