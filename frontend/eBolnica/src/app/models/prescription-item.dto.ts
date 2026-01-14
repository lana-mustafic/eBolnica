export interface PrescriptionItemDto {
  id: number;
  prescriptionId: number;
  medicationId: number;
  medicationName: string;
  quantity: number;
  instructions?: string;
  unitPrice: number;
  totalPrice: number;
}
