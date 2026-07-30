import { validateMedicationImageFile } from './medication-image-validation.util';
import {
  createMedicationImagePreviewUrl,
  revokeMedicationImagePreviewUrl
} from './medication-image-preview.util';

export type PendingMedicationImageStatus = 'valid' | 'invalid';

export interface PendingMedicationImage {
  id: string;
  file: File;
  fileName: string;
  status: PendingMedicationImageStatus;
  errorMessage?: string;
  previewUrl: string | null;
}

export interface AddToPendingQueueResult {
  queue: PendingMedicationImage[];
  added: PendingMedicationImage[];
  wasLimited: boolean;
  totalProvided: number;
}

export interface RemovePendingMedicationImageResult {
  queue: PendingMedicationImage[];
  removed: PendingMedicationImage | null;
}

let pendingIdCounter = 0;

export function createPendingMedicationImageId(): string {
  pendingIdCounter += 1;
  return `pending-${pendingIdCounter}`;
}

export function createPendingMedicationImage(file: File): PendingMedicationImage {
  const errorMessage = validateMedicationImageFile(file);
  const status: PendingMedicationImageStatus = errorMessage ? 'invalid' : 'valid';

  return {
    id: createPendingMedicationImageId(),
    file,
    fileName: file.name,
    status,
    errorMessage: errorMessage ?? undefined,
    previewUrl: status === 'valid' ? createMedicationImagePreviewUrl(file) : null
  };
}

export function addFilesToPendingQueue(
  currentQueue: PendingMedicationImage[],
  files: File[],
  maxFiles: number
): AddToPendingQueueResult {
  const availableSlots = Math.max(0, maxFiles - currentQueue.length);
  const filesToAdd = files.slice(0, availableSlots);
  const added = filesToAdd.map(createPendingMedicationImage);

  return {
    queue: [...currentQueue, ...added],
    added,
    wasLimited: files.length > availableSlots,
    totalProvided: files.length
  };
}

export function removePendingMedicationImage(
  queue: PendingMedicationImage[],
  id: string
): RemovePendingMedicationImageResult {
  const removed = queue.find(item => item.id === id) ?? null;

  if (removed) {
    revokePendingMedicationImagePreview(removed);
  }

  return {
    queue: queue.filter(item => item.id !== id),
    removed
  };
}

export function clearPendingMedicationImageQueue(): PendingMedicationImage[] {
  return [];
}

export function revokePendingMedicationImagePreview(item: PendingMedicationImage): void {
  revokeMedicationImagePreviewUrl(item.previewUrl);
}

export function revokePendingMedicationImagePreviews(items: PendingMedicationImage[]): void {
  for (const item of items) {
    revokePendingMedicationImagePreview(item);
  }
}

export function getUploadablePendingFiles(queue: PendingMedicationImage[]): File[] {
  return queue.filter(item => item.status === 'valid').map(item => item.file);
}

export function hasUploadablePendingFiles(queue: PendingMedicationImage[]): boolean {
  return queue.some(item => item.status === 'valid');
}

/** Clears pending previews locally. Does not call upload APIs. */
export function cancelPendingMedicationImageQueue(
  queue: PendingMedicationImage[]
): PendingMedicationImage[] {
  revokePendingMedicationImagePreviews(queue);
  return clearPendingMedicationImageQueue();
}

export function buildPendingQueueCancelMessage(count: number): string {
  const label = count === 1 ? 'image' : 'images';
  return `Discard ${count} selected ${label} from the upload queue? No files will be uploaded.`;
}
