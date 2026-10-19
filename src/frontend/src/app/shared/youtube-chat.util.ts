/** The YouTube video key a video URL and its live chat URL share, e.g. V9mTgsEvkt4. */
export function youtubeVideoKey(url: string | null | undefined): string | null {
  const raw = (url ?? '').trim();
  if (!raw) return null;

  let parsed: URL;
  try {
    parsed = new URL(raw);
  } catch {
    return null;
  }

  const host = parsed.hostname.replace(/^www\./, '').toLowerCase();
  // Covers youtube.com plus m./music./studio. subdomains, youtu.be and youtube-nocookie.com.
  const isYouTube =
    host === 'youtube.com' ||
    host.endsWith('.youtube.com') ||
    host === 'youtu.be' ||
    host.endsWith('.youtu.be') ||
    host === 'youtube-nocookie.com' ||
    host.endsWith('.youtube-nocookie.com');
  if (!isYouTube) return null;

  const segments = parsed.pathname.split('/').filter(Boolean);
  const first = (segments[0] ?? '').toLowerCase();

  let candidate: string | null;
  if (host === 'youtu.be') {
    candidate = segments[0] ?? null;
  } else if (first === 'watch') {
    candidate = parsed.searchParams.get('v');
  } else if (first === 'embed' || first === 'live' || first === 'shorts' || first === 'v' || first === 'video') {
    // Covers studio.youtube.com/video/<key>/livestreaming as well as /embed/, /live/, /shorts/.
    candidate = segments[1] ?? parsed.searchParams.get('v');
  } else {
    candidate = parsed.searchParams.get('v');
  }

  return candidate && /^[A-Za-z0-9_-]{6,20}$/.test(candidate) ? candidate : null;
}

/**
 * Chat panel for students, built from the same key as the video.
 * YouTube only renders live chat inside an iframe when `embed_domain` matches the page's domain.
 */
export function youtubeChatUrl(key: string): string {
  const params = new URLSearchParams({ is_popout: '1', v: key });
  const domain = typeof location !== 'undefined' ? location.hostname : '';
  if (domain) params.set('embed_domain', domain);
  return `https://www.youtube.com/live_chat?${params.toString()}`;
}
