import { MedicationImageDto } from '../../../models/medication-image.dto';

export interface MedicationImageGalleryReorderResult {
  images: MedicationImageDto[];
  selectedIndex: number;
}

export interface MedicationImageGalleryReorderSnapshot {
  images: MedicationImageDto[];
  selectedIndex: number;
}

/** Captures gallery state so optimistic reorder can be reverted on API failure. */
export function createMedicationImageGalleryReorderSnapshot(
  images: MedicationImageDto[],
  selectedIndex: number
): MedicationImageGalleryReorderSnapshot {
  return {
    images: images.map(image => ({ ...image })),
    selectedIndex
  };
}

/** Builds the ordered image id payload expected by the reorder API. */
export function buildMedicationImageReorderPayload(images: MedicationImageDto[]): number[] {
  return images.map(image => image.id);
}

export function getMedicationImageReorderErrorMessage(
  error: { status?: number; error?: { message?: string } | string }
): string {
  if (error?.status === 400) {
    const message = typeof error.error === 'string' ? error.error : error.error?.message;
    if (message) {
      return `${message} The gallery has been restored to its previous order.`;
    }
  }

  if (error?.status === 404) {
    return 'One or more images no longer exist. The gallery has been restored to its previous order.';
  }

  return 'Failed to save image order. The gallery has been restored to its previous order.';
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
