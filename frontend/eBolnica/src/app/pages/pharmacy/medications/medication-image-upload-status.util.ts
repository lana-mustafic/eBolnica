export type MedicationImageUploadFileStatusState = 'pending' | 'uploading' | 'done' | 'error';

export interface MedicationImageUploadEntry {
  file: File;
  uploadKey: string;
}

export interface MedicationImageUploadFileStatus {
  uploadKey: string;
  fileName: string;
  status: MedicationImageUploadFileStatusState;
  message?: string;
  progressPercent?: number;
}

export function createUploadFileStatuses(
  entries: MedicationImageUploadEntry[]
): MedicationImageUploadFileStatus[] {
  return entries.map(entry => ({
    uploadKey: entry.uploadKey,
    fileName: entry.file.name,
    status: 'pending'
  }));
}

export function markUploadFileStatus(
  statuses: MedicationImageUploadFileStatus[],
  uploadKey: string,
  status: MedicationImageUploadFileStatusState,
  message?: string
): MedicationImageUploadFileStatus[] {
  return statuses.map(item => {
    if (item.uploadKey !== uploadKey) {
      return item;
    }

    const progressPercent =
      status === 'done'
        ? 100
        : status === 'uploading'
          ? 0
          : item.progressPercent;

    return {
      ...item,
      status,
      message: status === 'error' ? (message ?? item.message) : undefined,
      progressPercent
    };
  });
}

export function updateUploadFileProgress(
  statuses: MedicationImageUploadFileStatus[],
  uploadKey: string,
  progressPercent: number
): MedicationImageUploadFileStatus[] {
  const clamped = Math.min(100, Math.max(0, progressPercent));

  return statuses.map(item =>
    item.uploadKey === uploadKey
      ? { ...item, status: 'uploading', progressPercent: clamped }
      : item
  );
}

export function calculateBatchUploadProgress(
  statuses: MedicationImageUploadFileStatus[]
): number {
  if (statuses.length === 0) {
    return 0;
  }

  const total = statuses.reduce((sum, item) => sum + getUploadFileProgressContribution(item), 0);

  return Math.round(total / statuses.length);
}

export interface MedicationImageBatchUploadProgress {
  overallPercent: number;
  totalFiles: number;
  completedFiles: number;
  activeFileName: string | null;
}

export function getUploadFileProgressContribution(
  item: MedicationImageUploadFileStatus
): number {
  if (item.status === 'done') {
    return 100;
  }

  if (item.status === 'error') {
    return item.progressPercent ?? 0;
  }

  return item.progressPercent ?? 0;
}

export function deriveBatchUploadProgress(
  statuses: MedicationImageUploadFileStatus[]
): MedicationImageBatchUploadProgress {
  return {
    overallPercent: calculateBatchUploadProgress(statuses),
    totalFiles: statuses.length,
    completedFiles: statuses.filter(item => item.status === 'done').length,
    activeFileName: getActiveUploadFileName(statuses)
  };
}

export function calculateSequentialBatchProgress(
  fileIndex: number,
  totalFiles: number,
  currentFileProgressPercent: number
): number {
  if (totalFiles <= 0) {
    return 0;
  }

  const clamped = Math.min(100, Math.max(0, currentFileProgressPercent));
  return Math.round(((fileIndex * 100) + clamped) / totalFiles);
}

export function formatBatchUploadProgressLabel(
  progress: MedicationImageBatchUploadProgress
): string {
  if (progress.totalFiles <= 1) {
    return 'Uploading image...';
  }

  const activeCount = progress.completedFiles + (progress.activeFileName ? 1 : 0);
  return `Uploading ${Math.min(activeCount, progress.totalFiles)} of ${progress.totalFiles} files`;
}

export function formatCompletedBatchUploadProgressLabel(totalFiles: number): string {
  if (totalFiles <= 1) {
    return 'Upload complete';
  }

  return `Uploaded ${totalFiles} of ${totalFiles} files`;
}

export function shouldShowBatchUploadProgress(totalFiles: number): boolean {
  return totalFiles > 1;
}

export function getUploadStatusForUploadKey(
  statuses: MedicationImageUploadFileStatus[],
  uploadKey: string
): MedicationImageUploadFileStatus | undefined {
  return statuses.find(item => item.uploadKey === uploadKey);
}

/** @deprecated Prefer getUploadStatusForUploadKey — fileName is not unique within a batch. */
export function getUploadStatusForFileName(
  statuses: MedicationImageUploadFileStatus[],
  fileName: string
): MedicationImageUploadFileStatus | undefined {
  return statuses.find(item => item.fileName === fileName);
}

export function getActiveUploadFileName(
  statuses: MedicationImageUploadFileStatus[]
): string | null {
  return statuses.find(item => item.status === 'uploading')?.fileName ?? null;
}

export function getFailedUploadFileNames(
  statuses: MedicationImageUploadFileStatus[]
): string[] {
  return statuses
    .filter(item => item.status === 'error')
    .map(item => item.fileName);
}

export function hasUploadFileErrors(
  statuses: MedicationImageUploadFileStatus[]
): boolean {
  return statuses.some(item => item.status === 'error');
}

export function canRetryUploadFile(
  item: MedicationImageUploadFileStatus,
  isUploading: boolean,
  isDisabled = false
): boolean {
  return item.status === 'error' && !isUploading && !isDisabled;
}

export function finalizeUploadFileStatusesAfterBatch(
  statuses: MedicationImageUploadFileStatus[]
): MedicationImageUploadFileStatus[] {
  return statuses.filter(item => item.status === 'done' || item.status === 'error');
}

export const UPLOAD_PROGRESS_COMPLETE_DISPLAY_MS = 400;

export function markUploadFileStatusesComplete(
  statuses: MedicationImageUploadFileStatus[]
): MedicationImageUploadFileStatus[] {
  return statuses.map(item => ({
    ...item,
    status: 'done',
    progressPercent: 100,
    message: undefined
  }));
}

export function isSuccessfulUploadBatch(result: {
  uploaded: unknown[];
  errors: unknown[];
}): boolean {
  return result.errors.length === 0 && result.uploaded.length > 0;
}

export function getUploadFileStatusSummary(
  item: MedicationImageUploadFileStatus
): string {
  switch (item.status) {
    case 'uploading':
      return `Uploading ${item.fileName}`;
    case 'done':
      return `${item.fileName} uploaded successfully`;
    case 'error':
      return item.message ?? `Failed to upload ${item.fileName}`;
    default:
      return item.fileName;
  }
}
