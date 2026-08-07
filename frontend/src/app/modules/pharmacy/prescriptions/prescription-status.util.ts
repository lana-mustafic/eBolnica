export function getPrescriptionStatusLabel(status: string): string {
  switch (status) {
    case 'Pending':
      return 'Na čekanju';
    case 'Dispensed':
      return 'Izdan';
    case 'Cancelled':
      return 'Otkazan';
    default:
      return status;
  }
}

export function getPrescriptionStatusClass(status: string): string {
  switch (status) {
    case 'Pending':
      return 'status-pending';
    case 'Dispensed':
      return 'status-dispensed';
    case 'Cancelled':
      return 'status-cancelled';
    default:
      return 'status-unknown';
  }
}
