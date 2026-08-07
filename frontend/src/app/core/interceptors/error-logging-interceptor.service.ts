import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { getApiErrorMessage } from '../utils/api-error.util';

/**
 * HTTP interceptor that logs errors.
 *
 * IMPORTANT: This interceptor only LOGS errors, it does NOT show toasters.
 * Toaster notifications should be handled in individual components where you
 * can provide context-specific error messages.
 */
export const errorLoggingInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      console.error('HTTP Error:', {
        url: req.url,
        method: req.method,
        status: error.status,
        statusText: error.statusText,
        message: error.message,
        code: error.error?.code,
        error: error.error,
        timestamp: new Date().toISOString(),
      });

      return throwError(() => error);
    })
  );
};

/** @deprecated Prefer getApiErrorMessage from api-error.util.ts */
export function getErrorMessage(error: HttpErrorResponse): string {
  return getApiErrorMessage(error);
}
