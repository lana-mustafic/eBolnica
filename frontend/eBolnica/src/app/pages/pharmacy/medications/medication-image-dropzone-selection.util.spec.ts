import {
  MEDICATION_IMAGE_MAX_FILES,
  normalizeSelectedFiles
} from './medication-image-dropzone-selection.util';

describe('medication-image-dropzone-selection.util', () => {
  function createFiles(count: number): File[] {
    return Array.from({ length: count }, (_, index) =>
      new File(['image'], `photo-${index + 1}.jpg`, { type: 'image/jpeg' })
    );
  }

  it('returns all files when within the multiple selection limit', () => {
    const files = createFiles(3);

    expect(
      normalizeSelectedFiles(files as unknown as FileList, {
        multiple: true,
        maxFiles: MEDICATION_IMAGE_MAX_FILES
      })
    ).toEqual({
      files,
      totalProvided: 3,
      wasLimited: false
    });
  });

  it('limits multiple selection to maxFiles', () => {
    const files = createFiles(7);
    const result = normalizeSelectedFiles(files as unknown as FileList, {
      multiple: true,
      maxFiles: MEDICATION_IMAGE_MAX_FILES
    });

    expect(result.files).toEqual(files.slice(0, MEDICATION_IMAGE_MAX_FILES));
    expect(result.totalProvided).toBe(7);
    expect(result.wasLimited).toBeTrue();
  });

  it('selects only the first file when multiple is disabled', () => {
    const files = createFiles(3);
    const result = normalizeSelectedFiles(files as unknown as FileList, {
      multiple: false,
      maxFiles: MEDICATION_IMAGE_MAX_FILES
    });

    expect(result.files).toEqual([files[0]]);
    expect(result.wasLimited).toBeTrue();
  });
});
