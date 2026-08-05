import { Component, inject, input } from '@angular/core';
import { SiteBrandService } from '../../site-brand.service';

@Component({
  selector: 'app-site-brand',
  standalone: true,
  template: `
    <div class="site-brand" [class.compact]="compact()">
      @if (brand.logoUrl(); as logo) {
        <img class="site-brand-logo" [src]="logo" [alt]="brand.siteName()" />
      }
      <p class="brand">{{ brand.siteName() }}</p>
    </div>
  `,
  styles: `
    .site-brand {
      display: flex;
      align-items: center;
      gap: 0.65rem;
      min-width: 0;
    }

    .site-brand-logo {
      width: 40px;
      height: 40px;
      object-fit: contain;
      border-radius: 10px;
      background: rgba(255, 255, 255, 0.08);
      padding: 0.15rem;
      flex-shrink: 0;
    }

    .compact .site-brand-logo {
      width: 32px;
      height: 32px;
    }

    .brand {
      margin: 0;
      font-family: var(--font-display);
      font-size: 1.35rem;
      font-weight: 800;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      line-height: 1.1;
    }

    .compact .brand {
      font-size: 1.1rem;
    }
  `
})
export class SiteBrandComponent {
  readonly brand = inject(SiteBrandService);
  readonly compact = input(false);
}
