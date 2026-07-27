import { MedicationImageDto } from './medication-image.dto';

export interface MedicationDto {
  id: number;
  name: string;
  genericName?: string;
  description?: string;
  manufacturer?: string;
  price: number;
  stockQuantity: number;
  minimumStockLevel: number;
  expiryDate?: string;
  batchNumber?: string;
  isActive: boolean;
  requiresPrescription: boolean;
  category?: string;
  dosageForm?: string;
  strength?: string;
  createdAt: string;
  updatedAt?: string;
  isLowStock?: boolean;
  isExpired?: boolean;
  primaryImageUrl?: string;
  images?: MedicationImageDto[];
}
