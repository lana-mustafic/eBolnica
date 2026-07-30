import {
  calculateBatchUploadProgress,
  calculateSequentialBatchProgress,
  canRetryUploadFile,
  createUploadFileStatuses,
  deriveBatchUploadProgress,
  finalizeUploadFileStatusesAfterBatch,
  formatBatchUploadProgressLabel,
  getActiveUploadFileName,
  getUploadFileStatusSummary,
  hasUploadFileErrors,
  markUploadFileStatus,
  shouldShowBatchUploadProgress,
  updateUploadFileProgress
} from './medication-image-upload-status.util';

describe('medication-image-upload-status.util', () => {
  it('creates pending statuses for each selected file', () => {
    const files = [
      new File(['a'], 'a.jpg', { type: 'image/jpeg' }),
      new File(['b'], 'b.jpg', { type: 'image/jpeg' })
    ];

    expect(createUploadFileStatuses(files)).toEqual([
      { fileName: 'a.jpg', status: 'pending' },
      { fileName: 'b.jpg', status: 'pending' }
    ]);
  });

  it('updates a single file status immutably', () => {
    const initial = createUploadFileStatuses([
      new File(['a'], 'a.jpg', { type: 'image/jpeg' }),
      new File(['b'], 'b.jpg', { type: 'image/jpeg' })
    ]);

    const uploading = markUploadFileStatus(initial, 'b.jpg', 'uploading');
    expect(getActiveUploadFileName(uploading)).toBe('b.jpg');
    expect(uploading).toEqual([
      { fileName: 'a.jpg', status: 'pending' },
      { fileName: 'b.jpg', status: 'uploading', progressPercent: 0 }
    ]);
  });

  it('tracks per-file and batch upload progress', () => {
    let statuses = markUploadFileStatus(
      createUploadFileStatuses([
        new File(['a'], 'a.jpg', { type: 'image/jpeg' }),
        new File(['b'], 'b.jpg', { type: 'image/jpeg' })
      ]),
      'a.jpg',
      'uploading'
    );

    statuses = updateUploadFileProgress(statuses, 'a.jpg', 50);
    expect(statuses[0].status).toBe('uploading');
    expect(calculateBatchUploadProgress(statuses)).toBe(25);

    statuses = markUploadFileStatus(statuses, 'a.jpg', 'done');
    statuses = markUploadFileStatus(statuses, 'b.jpg', 'uploading');
    statuses = updateUploadFileProgress(statuses, 'b.jpg', 40);
    expect(calculateBatchUploadProgress(statuses)).toBe(70);
  });

  it('clears error message when status moves out of error', () => {
    const initial = markUploadFileStatus(
      createUploadFileStatuses([new File(['a'], 'a.jpg', { type: 'image/jpeg' })]),
      'a.jpg',
      'error',
      'Upload failed'
    );

    const pending = markUploadFileStatus(initial, 'a.jpg', 'pending');
    expect(pending[0].message).toBeUndefined();
  });

  it('derives batch progress for a three-file upload', () => {
    let statuses = createUploadFileStatuses([
      new File(['a'], 'a.jpg', { type: 'image/jpeg' }),
      new File(['b'], 'b.jpg', { type: 'image/jpeg' }),
      new File(['c'], 'c.jpg', { type: 'image/jpeg' })
    ]);

    statuses = markUploadFileStatus(statuses, 'a.jpg', 'uploading');
    statuses = updateUploadFileProgress(statuses, 'a.jpg', 60);

    expect(deriveBatchUploadProgress(statuses)).toEqual({
      overallPercent: 20,
      totalFiles: 3,
      completedFiles: 0,
      activeFileName: 'a.jpg'
    });
    expect(formatBatchUploadProgressLabel(deriveBatchUploadProgress(statuses)))
      .toBe('Uploading 1 of 3 files');
    expect(shouldShowBatchUploadProgress(3)).toBeTrue();
    expect(shouldShowBatchUploadProgress(1)).toBeFalse();
  });

  it('calculates sequential batch progress across multiple files', () => {
    expect(calculateSequentialBatchProgress(0, 3, 90)).toBe(30);
    expect(calculateSequentialBatchProgress(1, 3, 50)).toBe(50);
    expect(calculateSequentialBatchProgress(2, 3, 100)).toBe(100);
  });

  it('supports per-file error and retry state helpers', () => {
    const statuses = finalizeUploadFileStatusesAfterBatch([
      { fileName: 'a.jpg', status: 'done', progressPercent: 100 },
      { fileName: 'b.jpg', status: 'error', progressPercent: 40, message: 'Network error' },
      { fileName: 'c.jpg', status: 'pending' }
    ]);

    expect(statuses).toEqual([
      { fileName: 'a.jpg', status: 'done', progressPercent: 100 },
      { fileName: 'b.jpg', status: 'error', progressPercent: 40, message: 'Network error' }
    ]);
    expect(hasUploadFileErrors(statuses)).toBeTrue();
    expect(canRetryUploadFile(statuses[1], false)).toBeTrue();
    expect(canRetryUploadFile(statuses[1], true)).toBeFalse();
    expect(getUploadFileStatusSummary(statuses[1])).toBe('Network error');
  });
});
