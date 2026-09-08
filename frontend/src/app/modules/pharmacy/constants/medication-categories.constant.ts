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
  if (LEGACY_ENGLISH_CATEGORIES[trimmed])
    return LEGACY_ENGLISH_CATEGORIES[trimmed];

  const match = Object.entries(LEGACY_ENGLISH_CATEGORIES).find(
    ([key]) => key.toLowerCase() === trimmed.toLowerCase(),
  );
  return match?.[1] ?? trimmed;
}

/** Read-only display label for medication category. */
export function getMedicationCategoryLabel(value: string | null | undefined): string {
  const normalized = normalizeMedicationCategory(value);
  return normalized || '-';
}
