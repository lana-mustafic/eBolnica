/**
 * Default sorting configuration for Pharmacy module tables
 * All tables default to showing newest items first
 */
export const DEFAULT_SORT_CONFIG = {
  COLUMN: 'createdAt',
  ORDER: 'desc' as 'asc' | 'desc',
  DISPLAY_NAME: 'Date Created'
};

/**
 * Table-specific default sort configurations
 * Allows different defaults per table if business rules require it
 */
export const TABLE_DEFAULT_SORTS = {
  MEDICATIONS: {
    column: 'createdAt',
    order: 'desc' as 'asc' | 'desc',
    description: 'Newest medications first'
  },
  PRESCRIPTIONS: {
    column: 'prescribedDate',
    order: 'desc' as 'asc' | 'desc',
    description: 'Most recent prescriptions first'
  },
  INVENTORY: {
    column: 'createdAt',
    order: 'desc' as 'asc' | 'desc',
    description: 'Newest items first'
  }
};

/**
 * Maps inventory table header / UI column keys to backend sortBy field names.
 * Backend supports: name, price, createdAt, stockQuantity, stock, category, expiryDate
 */
export const INVENTORY_SORT_COLUMN_MAP: Record<string, string> = {
  name: 'name',
  medicationName: 'name',
  category: 'category',
  stock: 'stockQuantity',
  stockQuantity: 'stockQuantity',
  quantity: 'stockQuantity',
  stockStatus: 'stockQuantity',
  expiryDate: 'expiryDate',
  expiry: 'expiryDate',
  createdAt: 'createdAt',
  dateCreated: 'createdAt',
  createdDate: 'createdAt'
};

export const INVENTORY_SORT_DISPLAY_NAMES: Record<string, string> = {
  name: 'Medication Name',
  category: 'Category',
  stockQuantity: 'Stock Quantity',
  expiryDate: 'Expiry Date',
  createdAt: 'Date Created'
};

export function mapInventorySortColumn(column: string): string {
  return INVENTORY_SORT_COLUMN_MAP[column] ?? column;
}

/** Maps prescriptions UI sort keys to backend sortBy field names. */
export const PRESCRIPTION_SORT_COLUMN_MAP: Record<string, string> = {
  date: 'prescribedDate',
  prescribedDate: 'prescribedDate',
  number: 'prescriptionNumber',
  prescriptionNumber: 'prescriptionNumber',
  amount: 'totalAmount',
  totalAmount: 'totalAmount',
  status: 'status',
  createdAt: 'createdAt'
};

export function mapPrescriptionSortColumn(column: string): string {
  return PRESCRIPTION_SORT_COLUMN_MAP[column] ?? column;
}
