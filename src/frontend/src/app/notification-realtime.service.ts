import { Injectable, effect, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { AppNotification } from './models';
import { currentTenantId } from './tenant';
import { resolveApiOrigin } from './api-base-url';

@Injectable({ providedIn: 'root' })
export class NotificationRealtimeService {
  private readonly auth = inject(AuthService);
  private connection: signalR.HubConnection | null = null;
  private connecting: Promise<signalR.HubConnection> | null = null;
  private readonly notificationSubject = new Subject<AppNotification>();

  readonly notifications$ = this.notificationSubject.asObservable();

  constructor() {
    effect(() => {
      const token = this.auth.token();
      const role = this.auth.user()?.role;
      const canNotify = role === 'Student' || role === 'Parent' || role === 'Teacher' || role === 'SuperAdmin';
      if (token && canNotify) {
        void this.ensureConnected();
      } else {
        void this.disconnect();
      }
    });
  }

  hubUrl(): string {
    return `${resolveApiOrigin()}/hubs/notifications`;
  }

  async ensureConnected(): Promise<signalR.HubConnection> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }
    if (this.connecting) {
      return this.connecting;
    }

    this.connecting = this.startConnection();
    try {
      return await this.connecting;
    } finally {
      this.connecting = null;
    }
  }

  private async startConnection(): Promise<signalR.HubConnection> {
    const token = this.auth.token();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl(), {
        accessTokenFactory: () => token,
        headers: { 'X-Tenant-Id': currentTenantId() ?? '' }
      })
      .withAutomaticReconnect()
      .build();

    connection.on('notification', (payload: AppNotification) => {
      if (payload?.id) {
        this.notificationSubject.next(payload);
      }
    });

    await connection.start();
    this.connection = connection;
    return connection;
  }

  private async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}
