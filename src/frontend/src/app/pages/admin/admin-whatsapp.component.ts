import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { AdminWhatsAppRecipient } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-admin-whatsapp',
  imports: [PageFeedbackComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './admin-whatsapp.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminWhatsAppComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly phones = signal<string[]>([]);
  readonly recipients = signal<AdminWhatsAppRecipient[]>([]);
  readonly shareUrl = signal<string | null>(null);
  readonly sending = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  phoneInput = '';
  body = '';

  readonly canSend = computed(() => this.phones().length > 0 && !this.sending());

  addPhone(): void {
    const parsed = this.phoneInput
      .split(/[,;\n]/)
      .map((p) => p.trim())
      .filter((p) => p.length > 0);

    if (!parsed.length) {
      this.error.set(this.locale.t('admin.whatsapp.enterPhone'));
      return;
    }

    this.clearStatus();
    this.phones.update((list) => {
      const next = [...list];
      for (const phone of parsed) {
        if (!next.includes(phone)) next.push(phone);
      }
      return next;
    });
    this.phoneInput = '';
  }

  removePhone(phone: string): void {
    this.phones.update((list) => list.filter((p) => p !== phone));
  }

  clearPhones(): void {
    this.phones.set([]);
  }

  send(): void {
    this.clearStatus();
    this.recipients.set([]);
    this.shareUrl.set(null);

    // Let the admin send without pressing "add" first.
    if (this.phoneInput.trim()) {
      this.addPhone();
    }

    if (!this.phones().length) {
      this.error.set(this.locale.t('admin.whatsapp.enterPhone'));
      return;
    }
    if (!this.body.trim()) {
      this.error.set(this.locale.t('admin.whatsapp.enterMessage'));
      return;
    }

    this.sending.set(true);
    this.api.sendAdminWhatsApp({ phones: this.phones(), message: this.body.trim() }).subscribe({
      next: (result) => {
        this.sending.set(false);
        this.recipients.set(result.recipients);
        this.shareUrl.set(result.shareUrl || null);
        this.message.set(
          this.locale.t('admin.whatsapp.sendResult', {
            sent: result.sentCount,
            failed: result.failedCount
          })
        );
      },
      error: (err) => {
        this.sending.set(false);
        this.error.set(this.locale.fromApiError(err, 'admin.whatsapp.sendFailed'));
      }
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
