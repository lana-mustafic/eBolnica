import {
  createDropzoneFileInputId,
  filesFromInput,
  resetNativeFileInput
} from './medication-image-dropzone-file-picker.util';

describe('medication-image-dropzone-file-picker.util', () => {
  it('creates unique file input ids', () => {
    const first = createDropzoneFileInputId();
    const second = createDropzoneFileInputId();

    expect(first).not.toBe(second);
    expect(first.startsWith('medication-image-file-input-')).toBeTrue();
  });

  it('converts FileList to array', () => {
    const file = new File(['x'], 'a.jpg', { type: 'image/jpeg' });
    expect(filesFromInput([file] as unknown as FileList)).toEqual([file]);
    expect(filesFromInput(null)).toEqual([]);
  });

  it('resets native file input value', () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.value = 'C:\\fakepath\\photo.jpg';

    resetNativeFileInput(input);

    expect(input.value).toBe('');
  });
});
