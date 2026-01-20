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
