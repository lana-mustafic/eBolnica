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
  error: { status?: number; name?: string; error?: { message?: string } | string; message?: string }
): string {
  if (error?.name === 'TimeoutError') {
    return 'The AI summary request timed out. Please try again later.';
  }

  if (error?.status === 404) {
    return 'This medication could not be found. Refresh the page and try again.';
  }

  if (error?.status === 504 || error?.status === 408) {
    return 'The AI summary request timed out. Please try again later.';
  }

  if (error?.status === 503 || error?.status === 502) {
    return 'The AI summary service is temporarily unavailable. Please try again later.';
  }

  if (error?.status === 400) {
    const message = typeof error.error === 'string' ? error.error : error.error?.message;
    if (message) {
      return message;
    }
  }

  if (error?.status === 0) {
    return 'Unable to reach the AI summary service. The rest of this page is still available.';
  }

  if (typeof error.error === 'object' && error.error?.message) {
    return error.error.message;
  }

  if (typeof error.error === 'string' && error.error.trim()) {
    return error.error;
  }

  if (typeof error.message === 'string' && error.message.trim()) {
    return error.message;
  }

  return 'Unable to generate an AI summary right now. The rest of this page is still available.';
}
