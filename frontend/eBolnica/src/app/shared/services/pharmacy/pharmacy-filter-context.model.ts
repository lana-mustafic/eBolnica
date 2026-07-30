/** Isolated filter state key for each pharmacy list page. */
export type PharmacyFilterContext = 'medications' | 'inventory' | 'prescriptions';

export const PHARMACY_FILTER_CONTEXTS: readonly PharmacyFilterContext[] = [
  'medications',
  'inventory',
  'prescriptions'
] as const;
