import {
  addFilesToPendingQueue,
  cancelPendingMedicationImageQueue,
  buildPendingQueueCancelMessage,
  clearPendingMedicationImageQueue,
  createPendingMedicationImage,
  getUploadablePendingFiles,
  hasUploadablePendingFiles,
  removePendingMedicationImage,
  revokePendingMedicationImagePreviews
} from './medication-image-pending-queue.util';
import { MEDICATION_IMAGE_MAX_FILE_SIZE_BYTES } from './medication-image-validation.util';

describe('medication-image-pending-queue.util', () => {
  function createFile(
    name: string,
    options: { type?: string; size?: number } = {}
  ): File {
    const size = options.size ?? 11;
    return new File([new Uint8Array(size)], name, {
      type: options.type ?? 'image/jpeg'
    });
  }

  afterEach(() => {
    pendingQueueRevokeSpy?.mockRestore();
  });

  let pendingQueueRevokeSpy: jasmine.Spy | undefined;

  function spyRevokeObjectUrl(): jasmine.Spy {
    pendingQueueRevokeSpy = spyOn(URL, 'revokeObjectURL');
    return pendingQueueRevokeSpy;
  }

  it('creates pending items with preview URLs for valid files', () => {
    spyOn(URL, 'createObjectURL').and.returnValue('blob:preview-1');
    const file = createFile('photo.jpg');

    const item = createPendingMedicationImage(file);

    expect(item.status).toBe('valid');
    expect(item.fileName).toBe('photo.jpg');
    expect(item.previewUrl).toBe('blob:preview-1');
    expect(item.errorMessage).toBeUndefined();
  });

  it('creates invalid pending items without preview URLs', () => {
    spyOn(URL, 'createObjectURL');
    const file = createFile('bad.pdf', { type: 'application/pdf' });

    const item = createPendingMedicationImage(file);

    expect(item.status).toBe('invalid');
    expect(item.previewUrl).toBeNull();
    expect(item.errorMessage).toContain('Invalid file type');
    expect(URL.createObjectURL).not.toHaveBeenCalled();
  });

  it('adds validated files to the pending queue and respects max size', () => {
    spyOn(URL, 'createObjectURL').and.callFake((blob: Blob) => `blob:${(blob as File).name}`);
    const files = Array.from({ length: 3 }, (_, index) => createFile(`photo-${index + 1}.jpg`));

    const result = addFilesToPendingQueue([], files, 5);

    expect(result.queue).toHaveSize(3);
    expect(result.added).toHaveSize(3);
    expect(result.wasLimited).toBeFalse();
    expect(result.queue.every(item => item.status === 'valid')).toBeTrue();
  });

  it('limits additions based on remaining queue capacity', () => {
    spyOn(URL, 'createObjectURL').and.returnValue('blob:preview');
    const existing = addFilesToPendingQueue([], [createFile('existing.jpg')], 5).queue;
    const incoming = Array.from({ length: 3 }, (_, index) => createFile(`new-${index + 1}.jpg`));

    const result = addFilesToPendingQueue(existing, incoming, 5);

    expect(result.queue).toHaveSize(5);
    expect(result.added).toHaveSize(4);
    expect(result.wasLimited).toBeTrue();
    expect(result.totalProvided).toBe(3);
  });

  it('keeps invalid files in the queue for preview error state', () => {
    spyOn(URL, 'createObjectURL').and.returnValue('blob:preview');
    const valid = createFile('ok.jpg');
    const invalid = createFile('large.jpg', { size: MEDICATION_IMAGE_MAX_FILE_SIZE_BYTES + 1 });

    const result = addFilesToPendingQueue([], [valid, invalid], 5);

    expect(result.queue).toHaveSize(2);
    expect(result.queue[0].status).toBe('valid');
    expect(result.queue[1].status).toBe('invalid');
    expect(getUploadablePendingFiles(result.queue)).toEqual([valid]);
    expect(hasUploadablePendingFiles(result.queue)).toBeTrue();
  });

  it('removes a pending item and revokes its preview URL', () => {
    spyOn(URL, 'createObjectURL').and.returnValue('blob:remove-me');
    const revokeSpy = spyRevokeObjectUrl();
    const queue = addFilesToPendingQueue([], [createFile('remove-me.jpg')], 5).queue;

    const result = removePendingMedicationImage(queue, queue[0].id);

    expect(result.queue).toEqual([]);
    expect(result.removed?.fileName).toBe('remove-me.jpg');
    expect(revokeSpy).toHaveBeenCalledWith('blob:remove-me');
  });

  it('revokes all preview URLs when clearing the queue', () => {
    spyOn(URL, 'createObjectURL').and.returnValues('blob:a', 'blob:b');
    const revokeSpy = spyRevokeObjectUrl();
    const queue = addFilesToPendingQueue(
      [],
      [createFile('a.jpg'), createFile('b.jpg')],
      5
    ).queue;

    revokePendingMedicationImagePreviews(queue);
    clearPendingMedicationImageQueue();

    expect(revokeSpy).toHaveBeenCalledWith('blob:a');
    expect(revokeSpy).toHaveBeenCalledWith('blob:b');
  });

  it('cancels pending queue locally without leaving stale previews', () => {
    spyOn(URL, 'createObjectURL').and.returnValues('blob:a', 'blob:b');
    const revokeSpy = spyRevokeObjectUrl();
    const queue = addFilesToPendingQueue(
      [],
      [createFile('a.jpg'), createFile('b.jpg')],
      5
    ).queue;

    const cancelled = cancelPendingMedicationImageQueue(queue);

    expect(cancelled).toEqual([]);
    expect(revokeSpy).toHaveBeenCalledWith('blob:a');
    expect(revokeSpy).toHaveBeenCalledWith('blob:b');
  });

  it('builds cancel confirmation message for one or many images', () => {
    expect(buildPendingQueueCancelMessage(1))
      .toContain('Discard 1 selected image');
    expect(buildPendingQueueCancelMessage(3))
      .toContain('Discard 3 selected images');
    expect(buildPendingQueueCancelMessage(3))
      .toContain('No files will be uploaded');
  });
});
