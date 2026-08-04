/** Creates a blob URL thumbnail preview for a local image file. */
export function createMedicationImagePreviewUrl(file: File): string {
  return URL.createObjectURL(file);
}

/** Revokes a blob URL created for a local image preview. */
export function revokeMedicationImagePreviewUrl(previewUrl: string | null | undefined): void {
  if (!previewUrl) return;
  URL.revokeObjectURL(previewUrl);
}

/** Revokes multiple blob preview URLs. Ignores null entries. */
export function revokeMedicationImagePreviewUrls(
  previewUrls: ReadonlyArray<string | null | undefined>
): void {
  for (const previewUrl of previewUrls) {
    revokeMedicationImagePreviewUrl(previewUrl);
  }
}
