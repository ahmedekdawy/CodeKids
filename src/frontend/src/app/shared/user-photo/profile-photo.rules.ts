/** Mirrors ProfilePhotoUploadRules on the API so bad files are rejected before uploading. */
export const PROFILE_PHOTO_MAX_BYTES = 3 * 1024 * 1024;

export const PROFILE_PHOTO_TYPES = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp'];
