import {
  beginMedicationImageUploadBatch,
  canStartMedicationImageUploadBatch,
  MEDICATION_IMAGE_UPLOAD_BATCH_IN_PROGRESS_MESSAGE
} from './medication-image-upload-batch.util';

describe('medication-image-upload-batch.util', () => {
  it('allows starting when no upload is active', () => {
    expect(canStartMedicationImageUploadBatch(false)).toBeTrue();
    expect(beginMedicationImageUploadBatch(false)).toEqual({ started: true });
  });

  it('blocks duplicate concurrent batches', () => {
    expect(canStartMedicationImageUploadBatch(true)).toBeFalse();
    expect(beginMedicationImageUploadBatch(true)).toEqual({
      started: false,
      message: MEDICATION_IMAGE_UPLOAD_BATCH_IN_PROGRESS_MESSAGE
    });
  });
});
