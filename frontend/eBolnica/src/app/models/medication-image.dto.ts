export interface MedicationImageDto {
  id: number;
  medicationId: number;
  fileName?: string;
  imageUrl: string;
  isPrimary: boolean;
  sortOrder: number;
  uploadedAt: string;
}
