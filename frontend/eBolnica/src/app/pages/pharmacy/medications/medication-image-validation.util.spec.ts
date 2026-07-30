import {
  MEDICATION_IMAGE_MAX_FILE_SIZE_BYTES,
  MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL,
  partitionMedicationImageFiles,
  validateMedicationImageFile
} from './medication-image-validation.util';

describe('medication-image-validation.util', () => {
  function createFile(
    name: string,
    options: { type?: string; size?: number } = {}
  ): File {
    const size = options.size ?? 4;
    return new File([new Uint8Array(size)], name, {
      type: options.type ?? 'image/jpeg'
    });
  }

  it('accepts jpg, png, and webp files within size limit', () => {
    expect(validateMedicationImageFile(createFile('photo.jpg'))).toBeNull();
    expect(validateMedicationImageFile(createFile('photo.png', { type: 'image/png' }))).toBeNull();
    expect(validateMedicationImageFile(createFile('photo.webp', { type: 'image/webp' }))).toBeNull();
  });

  it('rejects unsupported file types', () => {
    const message = validateMedicationImageFile(createFile('notes.pdf', { type: 'application/pdf' }));
    expect(message).toBe('Invalid file type. Allowed formats: JPG, PNG, WEBP.');
  });

  it('rejects files larger than 5MB', () => {
    const message = validateMedicationImageFile(
      createFile('large.jpg', { size: MEDICATION_IMAGE_MAX_FILE_SIZE_BYTES + 1 })
    );
    expect(message).toBe(`File is too large. Maximum size is ${MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL}.`);
  });

  it('rejects empty files', () => {
    expect(validateMedicationImageFile(createFile('empty.jpg', { size: 0 }))).toBe('File is empty.');
  });

  it('keeps valid files and reports invalid ones separately', () => {
    const valid = createFile('ok.jpg');
    const invalidType = createFile('bad.gif', { type: 'image/gif' });
    const tooLarge = createFile('big.jpg', { size: MEDICATION_IMAGE_MAX_FILE_SIZE_BYTES + 10 });

    const result = partitionMedicationImageFiles([valid, invalidType, tooLarge]);

    expect(result.validFiles).toEqual([valid]);
    expect(result.errors).toEqual([
      { fileName: 'bad.gif', message: 'Invalid file type. Allowed formats: JPG, PNG, WEBP.' },
      {
        fileName: 'big.jpg',
        message: `File is too large. Maximum size is ${MEDICATION_IMAGE_MAX_FILE_SIZE_LABEL}.`
      }
    ]);
  });
});
