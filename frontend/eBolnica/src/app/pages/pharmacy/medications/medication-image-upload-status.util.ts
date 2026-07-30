export type MedicationImageUploadFileStatusState = 'pending' | 'uploading' | 'done' | 'error';

export interface MedicationImageUploadFileStatus {
  fileName: string;
  status: MedicationImageUploadFileStatusState;
  message?: string;
}

export function createUploadFileStatuses(files: File[]): MedicationImageUploadFileStatus[] {
  return files.map(file => ({
    fileName: file.name,
    status: 'pending'
  }));
}

export function markUploadFileStatus(
  statuses: MedicationImageUploadFileStatus[],
  fileName: string,
  status: MedicationImageUploadFileStatusState,
  message?: string
): MedicationImageUploadFileStatus[] {
  return statuses.map(item =>
    item.fileName === fileName
      ? { ...item, status, message: message ?? item.message }
      : item
  );
}

export function getActiveUploadFileName(
  statuses: MedicationImageUploadFileStatus[]
): string | null {
  return statuses.find(item => item.status === 'uploading')?.fileName ?? null;
}
