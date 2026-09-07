import { Component, inject } from '@angular/core';
import { AppTheme, ThemeService } from '../../theme/theme.service';
import { TranslatePipe } from '../translate.pipe';

@Component({
  selector: 'app-theme-switcher',
  imports: [TranslatePipe],
  templateUrl: './theme-switcher.component.html',
  styleUrl: './theme-switcher.component.css'
})
export class ThemeSwitcherComponent {
  readonly theme = inject(ThemeService);

  setTheme(mode: AppTheme): void {
    this.theme.setTheme(mode);
  }
}
