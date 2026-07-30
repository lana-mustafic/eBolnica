import { MedicationImageDto } from '../../../models/medication-image.dto';
import { moveMedicationImageInGallery } from './medication-image-gallery-reorder.util';

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
});
