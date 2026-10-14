import { Subject } from 'rxjs';

/** Fired by the API calls that mark things read, so the app icon's badge drops without waiting for the next event. */
export const unreadChanged = new Subject<void>();
