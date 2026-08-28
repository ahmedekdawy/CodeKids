import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '../translate.pipe';
import { ToastItem, ToastService } from './toast.service';

@Component({
  selector: 'app-toast-host',
  imports: [TranslatePipe],
  templateUrl: './toast-host.component.html',
  styleUrl: './toast-host.component.css'
})
export class ToastHostComponent {
  readonly toasts = inject(ToastService);
  private readonly router = inject(Router);

  activate(item: ToastItem): void {
    const href = item.href;
    this.toasts.dismiss(item.id);
    if (href) {
      void this.router.navigateByUrl(href);
    }
  }
}
