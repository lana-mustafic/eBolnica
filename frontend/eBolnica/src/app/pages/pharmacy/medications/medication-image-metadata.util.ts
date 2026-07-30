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
