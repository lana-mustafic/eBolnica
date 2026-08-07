import { PharmacyActivityDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { PharmacyIconName } from './pharmacy-icon/pharmacy-icon.component';

export type PharmacyActivityTone = 'success' | 'warning' | 'info';

export function mapPharmacyActivityIcon(activity: PharmacyActivityDto): PharmacyIconName {
  switch (activity.eventType) {
    case 'prescription.created':
      return 'file-text';
    case 'prescription.dispensed':
      return 'check-circle';
    case 'prescription.cancelled':
      return 'triangle-alert';
    case 'medication.created':
    case 'medication.imported':
      return 'pill';
    case 'medication.deleted':
      return 'trash-2';
    case 'inventory.stock_adjusted':
      return 'package';
    default:
      return 'activity';
  }
}

export function mapPharmacyActivityTone(activity: PharmacyActivityDto): PharmacyActivityTone {
  switch (activity.severity) {
    case 'success':
      return 'success';
    case 'warning':
      return 'warning';
    default:
      return 'info';
  }
}

export function mapPrescriptionActivityType(
  activity: PharmacyActivityDto
): 'new' | 'success' | 'warning' {
  switch (activity.eventType) {
    case 'prescription.created':
      return 'new';
    case 'prescription.dispensed':
      return 'success';
    case 'prescription.cancelled':
      return 'warning';
    default:
      return mapPharmacyActivityTone(activity) === 'warning' ? 'warning' : 'success';
  }
}
