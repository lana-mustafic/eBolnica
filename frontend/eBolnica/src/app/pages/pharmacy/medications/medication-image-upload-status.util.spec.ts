import {
  createUploadFileStatuses,
  getActiveUploadFileName,
  markUploadFileStatus
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
      { fileName: 'b.jpg', status: 'uploading' }
    ]);
  });
});
