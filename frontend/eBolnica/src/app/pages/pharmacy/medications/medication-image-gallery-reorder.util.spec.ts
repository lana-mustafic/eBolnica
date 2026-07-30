import { MedicationImageDto } from '../../../models/medication-image.dto';
import {
  buildMedicationImageReorderPayload,
  createMedicationImageGalleryReorderSnapshot,
  getMedicationImageReorderErrorMessage,
  moveMedicationImageInGallery
} from './medication-image-gallery-reorder.util';

describe('medication-image-gallery-reorder.util', () => {
  function createImage(id: number, sortOrder: number, isPrimary = false): MedicationImageDto {
    return {
      id,
      medicationId: 10,
      fileName: `image-${id}.jpg`,
      imageUrl: `/uploads/medications/10/${id}.jpg`,
      isPrimary,
      sortOrder,
      uploadedAt: '2026-07-30T10:00:00.000Z'
    };
  }

  it('moves an image and reassigns sortOrder sequentially from 0', () => {
    const images = [createImage(1, 0, true), createImage(2, 1), createImage(3, 2)];
    const result = moveMedicationImageInGallery(images, 2, 0, 1);

    expect(result.images.map(image => image.id)).toEqual([3, 1, 2]);
    expect(result.images.map(image => image.sortOrder)).toEqual([0, 1, 2]);
  });

  it('keeps primary flag on the same image after reorder', () => {
    const images = [createImage(1, 0, true), createImage(2, 1), createImage(3, 2)];
    const result = moveMedicationImageInGallery(images, 0, 2, 0);

    expect(result.images.find(image => image.id === 1)?.isPrimary).toBeTrue();
    expect(result.images.filter(image => image.isPrimary)).toHaveSize(1);
  });

  it('updates selectedIndex when the selected thumbnail is moved', () => {
    const images = [createImage(1, 0), createImage(2, 1), createImage(3, 2)];
    const result = moveMedicationImageInGallery(images, 1, 0, 1);

    expect(result.selectedIndex).toBe(0);
  });

  it('updates selectedIndex when another thumbnail is moved past the selection', () => {
    const images = [createImage(1, 0), createImage(2, 1), createImage(3, 2)];
    const result = moveMedicationImageInGallery(images, 2, 0, 1);

    expect(result.selectedIndex).toBe(2);
  });

  it('returns unchanged order for invalid indices', () => {
    const images = [createImage(1, 0), createImage(2, 1)];
    const result = moveMedicationImageInGallery(images, 0, 0, 0);

    expect(result.images.map(image => image.id)).toEqual([1, 2]);
    expect(result.selectedIndex).toBe(0);
  });

  it('creates a deep snapshot for optimistic reorder rollback', () => {
    const images = [createImage(1, 0, true), createImage(2, 1)];
    const snapshot = createMedicationImageGalleryReorderSnapshot(images, 1);

    images[0].sortOrder = 99;

    expect(snapshot.images[0].sortOrder).toBe(0);
    expect(snapshot.selectedIndex).toBe(1);
  });

  it('builds ordered image id payload for reorder API', () => {
    const images = [createImage(3, 0), createImage(1, 1), createImage(2, 2)];
    expect(buildMedicationImageReorderPayload(images)).toEqual([3, 1, 2]);
  });

  it('builds reorder error messages and mentions rollback', () => {
    expect(getMedicationImageReorderErrorMessage({ status: 400, error: 'Invalid image order.' }))
      .toBe('Invalid image order. The gallery has been restored to its previous order.');
    expect(getMedicationImageReorderErrorMessage({ status: 500 }))
      .toBe('Failed to save image order. The gallery has been restored to its previous order.');
  });
});
