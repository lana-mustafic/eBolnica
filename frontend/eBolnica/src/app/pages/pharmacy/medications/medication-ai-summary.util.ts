import { MedicationAiSummaryDto } from '../../../models/medication-ai-summary.dto';

export type MedicationAiSummaryState = 'idle' | 'loading' | 'success' | 'error';

export function resolveMedicationAiSummaryState(
  isGenerating: boolean,
  errorMessage: string | null,
  summary: MedicationAiSummaryDto | null
): MedicationAiSummaryState {
  if (isGenerating) {
    return 'loading';
  }

  if (errorMessage) {
    return 'error';
  }

  if (summary) {
    return 'success';
  }

  return 'idle';
}

export function getMedicationAiSummaryErrorMessage(
  error: { status?: number; error?: { message?: string } | string }
): string {
  if (error?.status === 404) {
    return 'This medication could not be found. Refresh the page and try again.';
  }

  if (error?.status === 503 || error?.status === 502 || error?.status === 504) {
    return 'The AI summary service is temporarily unavailable. Please try again later.';
  }

  if (error?.status === 400) {
    const message = typeof error.error === 'string' ? error.error : error.error?.message;
    if (message) {
      return message;
    }
  }

  if (typeof error.error === 'object' && error.error?.message) {
    return error.error.message;
  }

  if (typeof error.error === 'string' && error.error.trim()) {
    return error.error;
  }

  return 'Unable to generate an AI summary right now. The rest of this page is still available.';
}
