import { Component, inject } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { TranslatePipe } from '../translate.pipe';

@Component({
  selector: 'app-language-switcher',
  imports: [TranslatePipe],
  templateUrl: './language-switcher.component.html',
  styleUrl: './language-switcher.component.css'
})
export class LanguageSwitcherComponent {
  readonly locale = inject(LocaleService);

  setLang(lang: 'en' | 'ar'): void {
    this.locale.setLang(lang);
  }
}
