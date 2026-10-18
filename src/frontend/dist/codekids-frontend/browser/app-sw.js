// Angular's service worker shows push notifications but never touches the app icon's badge, so this
// wraps it: everything else is ngsw-worker.js, and each push's appBadge count goes onto the icon.
importScripts('./ngsw-worker.js');

self.addEventListener('push', (event) => {
  let count;
  try {
    count = event.data?.json()?.appBadge;
  } catch {
    return;
  }
  if (typeof count !== 'number' || !self.navigator.setAppBadge) return;
  event.waitUntil((count > 0 ? self.navigator.setAppBadge(count) : self.navigator.clearAppBadge()).catch(() => undefined));
});
