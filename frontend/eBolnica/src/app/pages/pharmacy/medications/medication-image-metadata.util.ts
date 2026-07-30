import { MedicationImageDto } from '../../../models/medication-image.dto';

export function formatMedicationImageFileSize(bytes?: number | null): string | null {
  if (bytes == null || !Number.isFinite(bytes) || bytes < 0) {
    return null;
  }

  if (bytes === 0) {
    return '0 B';
  }

  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1048576) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / 1048576).toFixed(1)} MB`;
}

export function formatMedicationImageDimensions(
  width?: number | null,
  height?: number | null
): string | null {
  if (
    width == null ||
    height == null ||
    !Number.isFinite(width) ||
    !Number.isFinite(height) ||
    width <= 0 ||
    height <= 0
  ) {
    return null;
  }

  return `${Math.round(width)} × ${Math.round(height)} px`;
}

export function hasMedicationImageMetadata(
  image: Pick<MedicationImageDto, 'fileSizeBytes' | 'width' | 'height'>
): boolean {
  return formatMedicationImageFileSize(image.fileSizeBytes) !== null
    || formatMedicationImageDimensions(image.width, image.height) !== null;
}

export function buildMedicationImageStoredMetadataSummary(
  image: Pick<MedicationImageDto, 'fileSizeBytes' | 'width' | 'height'>
): string | null {
  const parts = [
    formatMedicationImageDimensions(image.width, image.height),
    formatMedicationImageFileSize(image.fileSizeBytes)
  ].filter((part): part is string => part !== null);

  return parts.length > 0 ? parts.join(', ') : null;
}

export function buildMedicationImageUploadSuccessMessage(
  uploaded: Pick<MedicationImageDto, 'fileSizeBytes' | 'width' | 'height'>[]
): string {
  if (uploaded.length === 0) {
    return '';
  }

  if (uploaded.length === 1) {
    const summary = buildMedicationImageStoredMetadataSummary(uploaded[0]);
    return summary
      ? `1 image uploaded successfully. Stored as ${summary}.`
      : '1 image uploaded successfully.';
  }

  const lastSummary = buildMedicationImageStoredMetadataSummary(uploaded[uploaded.length - 1]);
  return lastSummary
    ? `${uploaded.length} images uploaded successfully. Latest stored as ${lastSummary}.`
    : `${uploaded.length} images uploaded successfully.`;
}
