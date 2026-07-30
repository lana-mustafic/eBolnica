/** Max upload size in bytes (matches backend MedicationImageUploadSettings). */
export const MEDICATION_IMAGE_MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024;

export const MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL = '5MB';

export const MEDICATION_IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp';

export const MEDICATION_IMAGE_ACCEPTED_MIME_TYPES = [
  'image/jpeg',
  'image/png',
  'image/webp'
] as const;

const MEDICATION_IMAGE_ALLOWED_EXTENSIONS = [
  '.jpg',
  '.jpeg',
  '.png',
  '.webp'
] as const;

export interface MedicationImageValidationError {
  fileName: string;
  message: string;
}

export interface MedicationImageValidationResult {
  validFiles: File[];
  errors: MedicationImageValidationError[];
}

export function getMedicationImageExtension(fileName: string): string {
  const dotIndex = fileName.lastIndexOf('.');
  if (dotIndex < 0) return '';
  return fileName.slice(dotIndex).toLowerCase();
}

export function isAllowedMedicationImageType(file: File): boolean {
  const mimeType = file.type.toLowerCase();
  if (
    mimeType &&
    (MEDICATION_IMAGE_ACCEPTED_MIME_TYPES as readonly string[]).includes(mimeType)
  ) {
    return true;
  }

  const extension = getMedicationImageExtension(file.name);
  return (MEDICATION_IMAGE_ALLOWED_EXTENSIONS as readonly string[]).includes(extension);
}

export function validateMedicationImageFile(
  file: File,
  maxBytes: number = MEDICATION_IMAGE_MAX_FILE_SIZE_BYTES
): string | null {
  if (file.size === 0) {
    return 'File is empty.';
  }

  if (!isAllowedMedicationImageType(file)) {
    return 'Invalid file type. Allowed formats: JPG, PNG, WEBP.';
  }

  if (file.size > maxBytes) {
    return `File is too large. Maximum size is ${MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL}.`;
  }

  return null;
}

export function partitionMedicationImageFiles(files: File[]): MedicationImageValidationResult {
  const validFiles: File[] = [];
  const errors: MedicationImageValidationError[] = [];

  for (const file of files) {
    const message = validateMedicationImageFile(file);
    if (message) {
      errors.push({ fileName: file.name, message });
    } else {
      validFiles.push(file);
    }
  }

  return { validFiles, errors };
}
