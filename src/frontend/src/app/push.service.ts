import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom } from 'rxjs';
import { resolveApiBaseUrl } from './api-base-url';
import { AuthService } from './auth.service';

/** Set when the user turns alerts off here, so the app does not quietly turn them back on. */
const OPTED_OUT_KEY = 'codekids.push.optedOut';
/** Until when "Not now" keeps the prompt hidden. */
const SNOOZE_KEY = 'codekids.push.askAfter';
const SNOOZE_MS = 7 * 24 * 60 * 60 * 1000;

/**
 * Push notifications for the installed app. A device has one subscription; it is tied to whoever is
 * signed in, and released on sign-out so the next person on the device does not get someone else's alerts.
 */
@Injectable({ providedIn: 'root' })
export class PushService {
  private readonly swPush = inject(SwPush);
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  /** False in development builds and in browsers without push support. */
  readonly supported = this.swPush.isEnabled && 'Notification' in globalThis && 'PushManager' in globalThis;
  /** True while this device has an active subscription. */
  readonly enabled = signal(false);
  readonly busy = signal(false);
  /**
   * iPhone and iPad only offer push to a web app opened from the Home Screen, so in Safari itself push
   * looks unsupported. Tell those users how to get it instead of hiding the option.
   */
  readonly needsInstall =
    this.swPush.isEnabled && !this.supported && isAppleMobile() && !isStandalone();
  /** True when the browser's notification permission was refused; only the browser's settings can undo that. */
  readonly blocked = signal(this.supported && Notification.permission === 'denied');

  /** False until the service worker has said whether this device is already subscribed. */
  private readonly checked = signal(false);
  private readonly snoozed = signal(readNumber(SNOOZE_KEY) > Date.now());
  private readonly signedIn = computed(() => !!this.auth.token() && !!this.auth.user()?.id);

  /**
   * Whether to offer alerts to the signed-in user. Only an offer: the browser's own permission dialog
   * has to come from a tap, so the banner's button is what calls enable().
   */
  readonly shouldAsk = computed(
    () =>
      this.supported &&
      this.checked() &&
      this.signedIn() &&
      !this.auth.impersonator() &&
      !this.enabled() &&
      !this.blocked() &&
      !this.snoozed() &&
      Notification.permission === 'default'
  );

  private subscription: PushSubscription | null = null;
  private syncedFor: string | null = null;
  private resubscribeTried = false;

  constructor() {
    if (!this.supported) return;

    this.swPush.subscription.subscribe((subscription) => {
      this.subscription = subscription;
      this.enabled.set(!!subscription);
      this.checked.set(true);
      this.sync();
      this.resubscribe();
    });

    effect(() => {
      const userId = this.auth.token() ? (this.auth.user()?.id ?? null) : null;
      if (userId) {
        untracked(() => {
          this.sync();
          this.resubscribe();
        });
      } else if (this.syncedFor) {
        this.release();
      }
    });
  }

  /** Must be called from a click: browsers only show the permission prompt in response to one. */
  async enable(): Promise<boolean> {
    if (!this.supported || this.busy()) return false;
    this.busy.set(true);
    try {
      const config = await firstValueFrom(this.http.get<{ publicKey: string }>(`${resolveApiBaseUrl()}/push/config`));
      const subscription = await this.swPush.requestSubscription({ serverPublicKey: config.publicKey });
      this.subscription = subscription;
      this.syncedFor = null;
      await this.save(subscription);
      this.enabled.set(true);
      writeStorage(OPTED_OUT_KEY, null);
      return true;
    } catch {
      this.blocked.set(Notification.permission === 'denied');
      return false;
    } finally {
      this.busy.set(false);
    }
  }

  async disable(): Promise<void> {
    if (!this.supported || this.busy()) return;
    this.busy.set(true);
    try {
      this.release();
      await this.swPush.unsubscribe().catch(() => undefined);
      this.enabled.set(false);
      writeStorage(OPTED_OUT_KEY, '1');
    } finally {
      this.busy.set(false);
    }
  }

  /** Hides the prompt for a week. */
  snooze(): void {
    writeStorage(SNOOZE_KEY, String(Date.now() + SNOOZE_MS));
    this.snoozed.set(true);
  }

  /**
   * Permission was granted before but the subscription is gone (the browser expired it, or the app was
   * reinstalled). Subscribing again needs no dialog then, so do it without asking.
   */
  private resubscribe(): void {
    if (this.resubscribeTried || !this.checked() || !this.signedIn() || this.enabled() || this.busy()) return;
    if (Notification.permission !== 'granted' || readStorage(OPTED_OUT_KEY)) return;
    this.resubscribeTried = true;
    void this.enable();
  }

  /** Makes sure the server has this device recorded under the signed-in user. */
  private sync(): void {
    const userId = this.auth.token() ? (this.auth.user()?.id ?? null) : null;
    if (!this.subscription || !userId || this.syncedFor === userId) return;
    this.syncedFor = userId;
    void this.save(this.subscription).catch(() => (this.syncedFor = null));
  }

  private save(subscription: PushSubscription): Promise<void> {
    const keys = subscription.toJSON().keys ?? {};
    return firstValueFrom(
      this.http.post<void>(`${resolveApiBaseUrl()}/push/subscriptions`, {
        endpoint: subscription.endpoint,
        p256dh: keys['p256dh'],
        auth: keys['auth']
      })
    );
  }

  /** Tells the server to stop sending to this device; the browser keeps its subscription for the next sign-in. */
  private release(): void {
    this.syncedFor = null;
    if (!this.subscription) return;
    this.http
      .post<void>(`${resolveApiBaseUrl()}/push/unsubscribe`, { endpoint: this.subscription.endpoint })
      .subscribe({ error: () => undefined });
  }
}

/** iPadOS reports itself as a Mac, so a touch-capable "Mac" is an iPad. */
function isAppleMobile(): boolean {
  const ua = navigator.userAgent;
  return /iPhone|iPad|iPod/.test(ua) || (/Macintosh/.test(ua) && navigator.maxTouchPoints > 1);
}

function isStandalone(): boolean {
  return (
    globalThis.matchMedia?.('(display-mode: standalone)').matches ||
    (navigator as Navigator & { standalone?: boolean }).standalone === true
  );
}

/** Storage can be unavailable (private windows, blocked site data); the prompt then just shows again. */
function readStorage(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function readNumber(key: string): number {
  return Number(readStorage(key)) || 0;
}

function writeStorage(key: string, value: string | null): void {
  try {
    if (value === null) localStorage.removeItem(key);
    else localStorage.setItem(key, value);
  } catch {
    // Not remembered; harmless.
  }
}
