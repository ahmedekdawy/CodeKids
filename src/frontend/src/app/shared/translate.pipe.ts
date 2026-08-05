import { Pipe, PipeTransform, inject } from '@angular/core';
import { LocaleService } from '../i18n/locale.service';

@Pipe({
  name: 't',
  pure: false,
  standalone: true
})
export class TranslatePipe implements PipeTransform {
  private readonly locale = inject(LocaleService);

  transform(key: string, params?: Record<string, string | number>): string {
    this.locale.lang();
    return this.locale.t(key, params);
  }
}
