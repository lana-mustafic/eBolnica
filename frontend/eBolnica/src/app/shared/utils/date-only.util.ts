/**
 * Formats a Date as YYYY-MM-DD using local calendar components (no UTC shift).
 */
export function formatLocalDateParam(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/**
 * Converts an HTML date input value (YYYY-MM-DD) to an ISO string at UTC noon
 * to avoid timezone shifting the stored calendar day.
 */
export function dateInputToIsoString(dateInput: string | null | undefined): string {
  if (!dateInput) {
    return '';
  }

  return `${dateInput}T12:00:00.000Z`;
}
