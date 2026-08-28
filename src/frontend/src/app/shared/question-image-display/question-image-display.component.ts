import { Component, computed, inject, input } from '@angular/core';
import { LearningApiService } from '../../learning-api.service';

@Component({
  selector: 'app-question-image-display',
  template: `
    @if (src(); as imageSrc) {
      <img [src]="imageSrc" alt="" class="question-image" [class.compact]="compact()" />
    }
  `,
  styleUrl: './question-image-display.component.css'
})
export class QuestionImageDisplayComponent {
  private readonly api = inject(LearningApiService);

  readonly url = input<string | null | undefined>(null);
  readonly compact = input(false);

  readonly src = computed(() => this.api.siteAssetUrl(this.url() ?? null));
}
