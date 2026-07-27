export interface MedicationImageDto {
  id: number;
  medicationId: number;
  imageUrl: string;
  isPrimary: boolean;
  sortOrder: number;
  uploadedAt: string;
}
