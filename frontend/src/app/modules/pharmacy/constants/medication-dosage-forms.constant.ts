export const MEDICATION_DOSAGE_FORMS = [
  'Tableta',
  'Kapsula',
  'Tečnost',
  'Injekcija',
  'Krema',
  'Kapi',
  'Ostalo',
] as const;

export type MedicationDosageForm = (typeof MEDICATION_DOSAGE_FORMS)[number];

const LEGACY_ENGLISH_DOSAGE_FORMS: Record<string, MedicationDosageForm> = {
  Tablet: 'Tableta',
  Capsule: 'Kapsula',
  Liquid: 'Tečnost',
  Injection: 'Injekcija',
  Cream: 'Krema',
  Drops: 'Kapi',
  Other: 'Ostalo',
};

/** Maps legacy English values from DB/CSV to Bosnian labels. */
export function normalizeDosageForm(value: string | null | undefined): string {
  if (!value) return '';
  const trimmed = value.trim();
  return LEGACY_ENGLISH_DOSAGE_FORMS[trimmed] ?? trimmed;
}

/** Read-only display label for dosage form. */
export function getDosageFormLabel(value: string | null | undefined): string {
  const normalized = normalizeDosageForm(value);
  return normalized || '-';
}
