import { PrescriptionDto } from '../../../../api-services/pharmacy/pharmacy-api.models';

export interface PrescriptionStockValidationResult {
  ok: boolean;
  message: string;
}

export function validatePrescriptionStock(
  prescription: PrescriptionDto | null | undefined
): PrescriptionStockValidationResult {
  if (!prescription) {
    return { ok: false, message: 'Recept nije učitan.' };
  }

  if (prescription.prescriptionItems.length === 0) {
    return { ok: false, message: 'Recept nema stavki za izdavanje.' };
  }

  const requiredByMedication = new Map<
    number,
    { name: string; required: number; stock: number | null | undefined }
  >();

  for (const item of prescription.prescriptionItems) {
    const existing = requiredByMedication.get(item.medicationId);
    if (existing) {
      existing.required += item.quantity;
      continue;
    }

    requiredByMedication.set(item.medicationId, {
      name: item.medicationName,
      required: item.quantity,
      stock: item.stockQuantity,
    });
  }

  for (const { name, required, stock } of requiredByMedication.values()) {
    if (stock == null || stock < required) {
      return { ok: false, message: `Nedovoljna zaliha za ${name}.` };
    }
  }

  return { ok: true, message: '' };
}
