export interface MedicationImageDto {
  id: number;
  medicationId: number;
  fileName?: string;
  imageUrl: string;
  thumbnailUrl?: string;
  isPrimary: boolean;
  sortOrder: number;
  uploadedAt: string;
  fileSizeBytes?: number;
  width?: number;
  height?: number;
}

export interface MedicationImageReorderRequest {
  imageIds: number[];
}
