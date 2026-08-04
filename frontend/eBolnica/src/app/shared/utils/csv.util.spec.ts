import {
  buildCsvContent,
  downloadCsv,
  escapeCsvValue,
  formatIsoDateForCsv,
  getDatedExportFilename
} from './csv.util';

describe('csv.util', () => {
  it('should escape CSV values containing commas and quotes', () => {
    expect(escapeCsvValue('Pain, relief')).toBe('"Pain, relief"');
    expect(escapeCsvValue('Say "hello"')).toBe('"Say ""hello"""');
  });

  it('should build CSV content with headers and rows', () => {
    const csv = buildCsvContent(['Name', 'Price'], [['Paracetamol', '9.99']]);
    expect(csv).toBe('Name,Price\nParacetamol,9.99');
  });

  it('should format ISO dates for CSV export', () => {
    expect(formatIsoDateForCsv('2026-12-31T00:00:00Z')).toBe('2026-12-31');
  });

  it('should build dated export filenames', () => {
    expect(getDatedExportFilename('pharmacy-inventory', new Date('2026-03-15T12:00:00Z')))
      .toBe('pharmacy-inventory-2026-03-15.csv');
  });

  it('should trigger CSV download', () => {
    const appendChildSpy = spyOn(document.body, 'appendChild').and.callThrough();
    const removeChildSpy = spyOn(document.body, 'removeChild').and.callThrough();

    downloadCsv('Name\nTest', 'sample.csv');

    expect(appendChildSpy).toHaveBeenCalled();
    expect(removeChildSpy).toHaveBeenCalled();
  });
});
