import { Injectable, effect, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { ChatMember, ChatMessage } from './models';
import { currentTenantId } from './tenant';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class ChatRealtimeService {
  private readonly auth = inject(AuthService);
  private connection: signalR.HubConnection | null = null;
  private joinedRoomId: string | null = null;
  private connecting: Promise<signalR.HubConnection> | null = null;
  private readonly messageSubject = new Subject<ChatMessage>();
  private readonly deletedSubject = new Subject<ChatMessage>();
  private readonly memberSubject = new Subject<ChatMember>();

  readonly messages$ = this.messageSubject.asObservable();
  readonly deleted$ = this.deletedSubject.asObservable();
  readonly members$ = this.memberSubject.asObservable();

  constructor() {
    effect(() => {
      const token = this.auth.token();
      const role = this.auth.user()?.role;
      const canChat = role === 'Student' || role === 'Teacher' || role === 'SuperAdmin';
      if (token && canChat) {
        void this.ensureConnected();
      } else {
        void this.disconnect();
      }
    });
  }

  hubUrl(): string {
    return `${environment.apiBaseUrl.replace(/\/api\/?$/, '')}/hubs/chat`;
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

  async joinRoom(roomId: string): Promise<void> {
    const connection = await this.ensureConnected();
    if (this.joinedRoomId && this.joinedRoomId !== roomId) {
      await connection.invoke('LeaveRoom', this.joinedRoomId);
    }
    await connection.invoke('JoinRoom', roomId);
    this.joinedRoomId = roomId;
  }

  async sendMessage(roomId: string, body: string): Promise<void> {
    const connection = await this.ensureConnected();
    await connection.invoke('SendMessage', roomId, body);
  }

  async disconnect(): Promise<void> {
    this.joinedRoomId = null;
    if (this.connection) {
      const current = this.connection;
      this.connection = null;
      await current.stop();
    }
  }

  private async startConnection(): Promise<signalR.HubConnection> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl(), {
        accessTokenFactory: () => this.auth.token() ?? '',
        headers: { 'X-Tenant-Id': currentTenantId() }
      })
      .withAutomaticReconnect()
      .build();

    connection.on('message', (message: ChatMessage) => this.messageSubject.next(message));
    connection.on('messageDeleted', (message: ChatMessage) => this.deletedSubject.next(message));
    connection.on('memberUpdated', (member: ChatMember) => this.memberSubject.next(member));
    this.connection = connection;
    await connection.start();
    return connection;
  }
}
