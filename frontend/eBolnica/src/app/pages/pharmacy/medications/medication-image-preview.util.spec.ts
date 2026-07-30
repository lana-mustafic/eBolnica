import {
  createMedicationImagePreviewUrl,
  revokeMedicationImagePreviewUrl
} from './medication-image-preview.util';

describe('medication-image-preview.util', () => {
  function createFile(name: string): File {
    return new File([new Uint8Array(11)], name, { type: 'image/jpeg' });
  }

  it('creates a blob preview URL via URL.createObjectURL', () => {
    spyOn(URL, 'createObjectURL').and.returnValue('blob:preview-url');
    const file = createFile('preview.jpg');

    const previewUrl = createMedicationImagePreviewUrl(file);

    expect(URL.createObjectURL).toHaveBeenCalledWith(file);
    expect(previewUrl).toBe('blob:preview-url');
  });

  it('revokes preview URLs via URL.revokeObjectURL', () => {
    const revokeSpy = spyOn(URL, 'revokeObjectURL');

    revokeMedicationImagePreviewUrl('blob:preview-url');

    expect(revokeSpy).toHaveBeenCalledWith('blob:preview-url');
  });

  it('ignores null or empty preview URLs when revoking', () => {
    const revokeSpy = spyOn(URL, 'revokeObjectURL');

    revokeMedicationImagePreviewUrl(null);
    revokeMedicationImagePreviewUrl(undefined);

    expect(revokeSpy).not.toHaveBeenCalled();
  });
});
