import {
  formatMedicationImageDimensions,
  formatMedicationImageFileSize,
  hasMedicationImageMetadata
} from './medication-image-metadata.util';

describe('medication-image-metadata.util', () => {
  it('formats file size bytes for display', () => {
    expect(formatMedicationImageFileSize(512)).toBe('512 B');
    expect(formatMedicationImageFileSize(2048)).toBe('2.0 KB');
    expect(formatMedicationImageFileSize(2621440)).toBe('2.5 MB');
    expect(formatMedicationImageFileSize(undefined)).toBeNull();
  });

  it('formats image dimensions for display', () => {
    expect(formatMedicationImageDimensions(1920, 1080)).toBe('1920 × 1080 px');
    expect(formatMedicationImageDimensions(0, 1080)).toBeNull();
    expect(formatMedicationImageDimensions(null, 1080)).toBeNull();
  });

  it('detects when image metadata is available', () => {
    expect(hasMedicationImageMetadata({ fileSizeBytes: 1024, width: 1920, height: 1080 })).toBeTrue();
    expect(hasMedicationImageMetadata({ fileSizeBytes: undefined, width: 1920, height: 1080 })).toBeTrue();
    expect(hasMedicationImageMetadata({ fileSizeBytes: 1024, width: undefined, height: undefined })).toBeTrue();
    expect(hasMedicationImageMetadata({})).toBeFalse();
  });
});
