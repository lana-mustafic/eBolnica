export const MEDICATION_CATEGORIES = [
  'Analgetici',
  'Antibiotici',
  'Antiviralni',
  'Kardiovaskularni',
  'Dijabetes',
  'Gastrointestinalni',
  'Respiratorni',
  'Vitamini',
  'Ostalo',
] as const;

export type MedicationCategory = (typeof MEDICATION_CATEGORIES)[number];

const LEGACY_ENGLISH_CATEGORIES: Record<string, MedicationCategory> = {
  Analgesics: 'Analgetici',
  Antibiotics: 'Antibiotici',
  Antivirals: 'Antiviralni',
  Cardiovascular: 'Kardiovaskularni',
  Diabetes: 'Dijabetes',
  Gastrointestinal: 'Gastrointestinalni',
  Respiratory: 'Respiratorni',
  Vitamins: 'Vitamini',
  Other: 'Ostalo',
};

/** Maps legacy English values from DB/CSV to Bosnian labels. */
export function normalizeMedicationCategory(value: string | null | undefined): string {
  if (!value) return '';
  const trimmed = value.trim();
  return LEGACY_ENGLISH_CATEGORIES[trimmed] ?? trimmed;
}

/** Read-only display label for medication category. */
export function getMedicationCategoryLabel(value: string | null | undefined): string {
  const normalized = normalizeMedicationCategory(value);
  return normalized || '-';
}
