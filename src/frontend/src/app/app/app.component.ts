import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../auth.service';
import { ChatNotifyService } from '../chat-notify.service';
import { NotificationNotifyService } from '../notification-notify.service';
import { SiteBrandService } from '../site-brand.service';
import { ThemeService } from '../theme/theme.service';
import { ToastHostComponent } from '../shared/toast/toast-host.component';
import { TranslatePipe } from '../shared/translate.pipe';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastHostComponent, TranslatePipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly brand = inject(SiteBrandService);
  private readonly _chatNotify = inject(ChatNotifyService);
  private readonly _notificationNotify = inject(NotificationNotifyService);
  private readonly _theme = inject(ThemeService);

  ngOnInit(): void {
    this.brand.load();
  }
}
