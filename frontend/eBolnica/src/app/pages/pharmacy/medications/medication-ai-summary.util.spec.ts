import { getMedicationAiSummaryErrorMessage, resolveMedicationAiSummaryState } from './medication-ai-summary.util';
import { MedicationAiSummaryDto } from '../../../models/medication-ai-summary.dto';

describe('medication-ai-summary.util', () => {
  const summary: MedicationAiSummaryDto = {
    overview: 'Overview text',
    usageNotes: 'Usage notes',
    stockExpiryAlert: 'Stock alert',
    prescriptionRequirement: 'Prescription required'
  };

  it('resolves loading state while summary is generating', () => {
    expect(resolveMedicationAiSummaryState(true, null, null)).toBe('loading');
    expect(resolveMedicationAiSummaryState(true, 'Error', summary)).toBe('loading');
  });

  it('resolves error state when generation failed', () => {
    expect(resolveMedicationAiSummaryState(false, 'Service unavailable', null)).toBe('error');
  });

  it('resolves success state when summary is available', () => {
    expect(resolveMedicationAiSummaryState(false, null, summary)).toBe('success');
  });

  it('resolves idle state before first generation', () => {
    expect(resolveMedicationAiSummaryState(false, null, null)).toBe('idle');
  });

  it('builds friendly unavailable message for AI service errors', () => {
    expect(getMedicationAiSummaryErrorMessage({ status: 503 }))
      .toContain('temporarily unavailable');
  });

  it('builds non-blocking fallback message for unknown errors', () => {
    expect(getMedicationAiSummaryErrorMessage({ status: 500 }))
      .toContain('rest of this page is still available');
  });
});
