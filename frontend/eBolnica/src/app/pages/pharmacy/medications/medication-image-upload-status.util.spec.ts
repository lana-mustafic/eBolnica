import {
  calculateBatchUploadProgress,
  calculateSequentialBatchProgress,
  canRetryUploadFile,
  createUploadFileStatuses,
  deriveBatchUploadProgress,
  finalizeUploadFileStatusesAfterBatch,
  formatBatchUploadProgressLabel,
  formatCompletedBatchUploadProgressLabel,
  getActiveUploadFileName,
  getUploadFileStatusSummary,
  getUploadStatusForUploadKey,
  hasUploadFileErrors,
  isSuccessfulUploadBatch,
  markUploadFileStatus,
  markUploadFileStatusesComplete,
  shouldShowBatchUploadProgress,
  UPLOAD_PROGRESS_COMPLETE_DISPLAY_MS,
  updateUploadFileProgress
} from './medication-image-upload-status.util';
import { createMedicationImageUploadEntry } from './medication-image-upload.util';

describe('medication-image-upload-status.util', () => {
  function createEntries(names: string[]) {
    return names.map(name => createMedicationImageUploadEntry(new File(['a'], name, { type: 'image/jpeg' })));
  }

  it('creates pending statuses for each selected file', () => {
    const entries = createEntries(['a.jpg', 'b.jpg']);

    const statuses = createUploadFileStatuses(entries);
    expect(statuses).toHaveSize(2);
    expect(statuses.every(item => item.status === 'pending')).toBeTrue();
    expect(statuses.map(item => item.fileName)).toEqual(['a.jpg', 'b.jpg']);
    expect(new Set(statuses.map(item => item.uploadKey)).size).toBe(2);
  });

  it('updates a single file status immutably by uploadKey', () => {
    const entries = createEntries(['a.jpg', 'b.jpg']);
    const initial = createUploadFileStatuses(entries);
    const targetKey = initial[1].uploadKey;

    const uploading = markUploadFileStatus(initial, targetKey, 'uploading');
    expect(getActiveUploadFileName(uploading)).toBe('b.jpg');
    expect(uploading[0].status).toBe('pending');
    expect(uploading[1].status).toBe('uploading');
    expect(uploading[1].progressPercent).toBe(0);
  });

  it('tracks per-file and batch upload progress by uploadKey', () => {
    const entries = createEntries(['a.jpg', 'b.jpg']);
    let statuses = createUploadFileStatuses(entries);
    const firstKey = statuses[0].uploadKey;

    statuses = markUploadFileStatus(statuses, firstKey, 'uploading');
    statuses = updateUploadFileProgress(statuses, firstKey, 50);
    expect(statuses[0].status).toBe('uploading');
    expect(calculateBatchUploadProgress(statuses)).toBe(25);

    statuses = markUploadFileStatus(statuses, firstKey, 'done');
    statuses = markUploadFileStatus(statuses, statuses[1].uploadKey, 'uploading');
    statuses = updateUploadFileProgress(statuses, statuses[1].uploadKey, 40);
    expect(calculateBatchUploadProgress(statuses)).toBe(70);
  });

  it('resolves upload status by uploadKey even when file names match', () => {
    const duplicateName = 'photo.jpg';
    const entries = [
      createMedicationImageUploadEntry(new File(['a'], duplicateName, { type: 'image/jpeg' })),
      createMedicationImageUploadEntry(new File(['b'], duplicateName, { type: 'image/jpeg' }))
    ];
    const statuses = markUploadFileStatus(
      createUploadFileStatuses(entries),
      entries[1].uploadKey,
      'error',
      'Upload failed'
    );

    expect(getUploadStatusForUploadKey(statuses, entries[0].uploadKey)?.status).toBe('pending');
    expect(getUploadStatusForUploadKey(statuses, entries[1].uploadKey)?.status).toBe('error');
  });

  it('clears error message when status moves out of error', () => {
    const entry = createEntries(['a.jpg'])[0];
    const initial = markUploadFileStatus(
      createUploadFileStatuses([entry]),
      entry.uploadKey,
      'error',
      'Upload failed'
    );

    const pending = markUploadFileStatus(initial, entry.uploadKey, 'pending');
    expect(pending[0].message).toBeUndefined();
  });

  it('derives batch progress for a three-file upload', () => {
    const entries = createEntries(['a.jpg', 'b.jpg', 'c.jpg']);
    let statuses = createUploadFileStatuses(entries);

    statuses = markUploadFileStatus(statuses, entries[0].uploadKey, 'done');
    statuses = markUploadFileStatus(statuses, entries[1].uploadKey, 'uploading');
    statuses = updateUploadFileProgress(statuses, entries[1].uploadKey, 60);

    const batch = deriveBatchUploadProgress(statuses);
    expect(batch.overallPercent).toBe(53);
    expect(batch.completedFiles).toBe(1);
    expect(batch.activeFileName).toBe('b.jpg');
  });

  it('calculates sequential batch progress', () => {
    expect(calculateSequentialBatchProgress(0, 2, 50)).toBe(25);
    expect(calculateSequentialBatchProgress(1, 2, 100)).toBe(100);
  });

  it('formats batch upload labels', () => {
    expect(formatBatchUploadProgressLabel({
      overallPercent: 50,
      totalFiles: 1,
      completedFiles: 0,
      activeFileName: 'a.jpg'
    })).toBe('Uploading image...');

    expect(formatCompletedBatchUploadProgressLabel(3)).toBe('Uploaded 3 of 3 files');
  });

  it('detects upload errors and retry eligibility', () => {
    const entry = createEntries(['a.jpg'])[0];
    const statuses = markUploadFileStatus(
      createUploadFileStatuses([entry]),
      entry.uploadKey,
      'error',
      'Failed'
    );

    expect(hasUploadFileErrors(statuses)).toBeTrue();
    expect(canRetryUploadFile(statuses[0], false)).toBeTrue();
    expect(canRetryUploadFile(statuses[0], true)).toBeFalse();
  });

  it('finalizes and completes batch statuses', () => {
    const entries = createEntries(['a.jpg', 'b.jpg']);
    const initial = createUploadFileStatuses(entries);
    const finalized = finalizeUploadFileStatusesAfterBatch(
      markUploadFileStatus(initial, entries[0].uploadKey, 'done')
    );

    expect(finalized).toHaveSize(1);
    expect(markUploadFileStatusesComplete(initial).every(item => item.status === 'done')).toBeTrue();
  });

  it('detects successful upload batches', () => {
    expect(isSuccessfulUploadBatch({ uploaded: [{}], errors: [] })).toBeTrue();
    expect(isSuccessfulUploadBatch({ uploaded: [], errors: [{}] })).toBeFalse();
  });

  it('summarizes upload file status text', () => {
    const entry = createEntries(['a.jpg'])[0];
    const statuses = createUploadFileStatuses([entry]);

    expect(getUploadFileStatusSummary(statuses[0])).toBe('a.jpg');
    expect(getUploadFileStatusSummary(
      markUploadFileStatus(statuses, entry.uploadKey, 'uploading')[0]
    )).toBe('Uploading a.jpg');
  });

  it('exposes upload progress display constants', () => {
    expect(shouldShowBatchUploadProgress(2)).toBeTrue();
    expect(shouldShowBatchUploadProgress(1)).toBeFalse();
    expect(UPLOAD_PROGRESS_COMPLETE_DISPLAY_MS).toBeGreaterThan(0);
  });
});
