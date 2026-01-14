export interface MedicationCreateDto {
  name: string;
  genericName?: string;
  description?: string;
  manufacturer?: string;
  price: number;
  stockQuantity: number;
  minimumStockLevel: number;
  expiryDate: string;
  batchNumber?: string;
  isActive: boolean;
  requiresPrescription: boolean;
  category: string;
  dosageForm?: string;
  strength?: string;
}
