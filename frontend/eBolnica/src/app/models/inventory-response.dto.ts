import { MedicationDto } from './medication.dto';
import { PagedResponse } from './paged-response.dto';

export interface InventoryResponse extends PagedResponse<MedicationDto> {
  lowStockAlerts: MedicationDto[];
  expiryAlerts: MedicationDto[];
}
