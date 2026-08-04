export const MEDICATION_IMAGE_UPLOAD_BATCH_IN_PROGRESS_MESSAGE =
  'An upload is already in progress. Please wait for it to finish.';

export function canStartMedicationImageUploadBatch(isUploading: boolean): boolean {
  return !isUploading;
}

export function beginMedicationImageUploadBatch(isUploading: boolean): {
  started: boolean;
  message?: string;
} {
  if (isUploading) {
    return {
      started: false,
      message: MEDICATION_IMAGE_UPLOAD_BATCH_IN_PROGRESS_MESSAGE
    };
  }

  return { started: true };
}
