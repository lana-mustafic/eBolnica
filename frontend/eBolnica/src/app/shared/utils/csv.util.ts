/**
 * Shared CSV helpers for pharmacy export/import features.
 */

export function escapeCsvValue(value: string): string {
  if (!value) {
    return '';
  }

  if (value.includes(',') || value.includes('"') || value.includes('\n')) {
    return `"${value.replace(/"/g, '""')}"`;
  }

  return value;
}

export function buildCsvContent(headers: readonly string[], rows: string[][]): string {
  return [headers.join(','), ...rows.map(row => row.join(','))].join('\n');
}

export function downloadCsv(content: string, filename: string): void {
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.style.display = 'none';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
}

export function formatIsoDateForCsv(dateString: string | undefined): string {
  if (!dateString) {
    return '';
  }

  const date = new Date(dateString);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return date.toISOString().split('T')[0];
}

export function formatLocaleDateForCsv(dateString: string | undefined): string {
  if (!dateString) {
    return '';
  }

  const date = new Date(dateString);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  });
}

export function getDatedExportFilename(prefix: string, date: Date = new Date()): string {
  return `${prefix}-${date.toISOString().split('T')[0]}.csv`;
}
