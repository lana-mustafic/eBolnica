import { MedicationImageDto } from '../../../models/medication-image.dto';

export interface MedicationImageGalleryReorderResult {
  images: MedicationImageDto[];
  selectedIndex: number;
}

/** Moves a gallery image and reassigns sequential sortOrder values from 0. */
export function moveMedicationImageInGallery(
  images: MedicationImageDto[],
  fromIndex: number,
  toIndex: number,
  selectedIndex: number
): MedicationImageGalleryReorderResult {
  if (
    fromIndex === toIndex
    || fromIndex < 0
    || toIndex < 0
    || fromIndex >= images.length
    || toIndex >= images.length
  ) {
    return {
      images: images.map((image, index) => ({ ...image, sortOrder: index })),
      selectedIndex
    };
  }

  const reordered = [...images];
  const [moved] = reordered.splice(fromIndex, 1);
  reordered.splice(toIndex, 0, moved);

  return {
    images: reordered.map((image, index) => ({
      ...image,
      sortOrder: index
    })),
    selectedIndex: adjustSelectedIndexAfterMove(selectedIndex, fromIndex, toIndex)
  };
}

function adjustSelectedIndexAfterMove(
  selectedIndex: number,
  fromIndex: number,
  toIndex: number
): number {
  if (selectedIndex === fromIndex) {
    return toIndex;
  }

  if (fromIndex < selectedIndex && toIndex >= selectedIndex) {
    return selectedIndex - 1;
  }

  if (fromIndex > selectedIndex && toIndex <= selectedIndex) {
    return selectedIndex + 1;
  }

  return selectedIndex;
}
