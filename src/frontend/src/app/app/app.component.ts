import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ChatNotifyService } from '../chat-notify.service';
import { SiteBrandService } from '../site-brand.service';
import { ToastHostComponent } from '../shared/toast/toast-host.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastHostComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  private readonly brand = inject(SiteBrandService);
  private readonly _chatNotify = inject(ChatNotifyService);

  ngOnInit(): void {
    this.brand.load();
  }
}
