import { ClassroomZoomLink } from '../../models';

export type ClassroomZoomLinkDraft = {
  name: string;
  url: string;
};

export function cloneZoomLinks(links?: ClassroomZoomLink[] | null): ClassroomZoomLinkDraft[] {
  return (links ?? []).map((link) => ({ name: link.name || '', url: link.url || '' }));
}

export function normalizeZoomLinks(links: ClassroomZoomLinkDraft[]): ClassroomZoomLink[] {
  return links
    .map((link) => ({ name: link.name.trim(), url: link.url.trim() }))
    .filter((link) => link.name.length > 0 && link.url.length > 0);
}

export function classroomHasZoomLinks(room: { zoomLinks?: ClassroomZoomLink[] | null }): boolean {
  return normalizeZoomLinks(cloneZoomLinks(room.zoomLinks)).length > 0;
}

export function emptyZoomLinkDraft(): ClassroomZoomLinkDraft {
  return { name: '', url: '' };
}
