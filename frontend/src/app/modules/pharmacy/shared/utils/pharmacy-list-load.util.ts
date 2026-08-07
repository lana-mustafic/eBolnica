import { WritableSignal } from '@angular/core';
import { catchError, Observable, of, startWith, tap } from 'rxjs';
import { resolvePharmacyApiErrorMessage } from './pharmacy-api-error.util';

export interface ListLoadSignals {
  isLoading: WritableSignal<boolean>;
  loadError: WritableSignal<boolean>;
}

interface LoadableViewModel {
  loading: boolean;
  error: boolean;
}

export function pipeListLoad<T extends LoadableViewModel>(
  source$: Observable<T>,
  signals: ListLoadSignals,
  emptyViewModel: (state: { loading?: boolean; error?: boolean }) => T,
  fallbackMessage: string,
  onError: (message: string) => void
): Observable<T> {
  return source$.pipe(
    tap((vm) => {
      signals.isLoading.set(vm.loading);
      signals.loadError.set(vm.error);
    }),
    catchError((err: unknown) => {
      const message = resolvePharmacyApiErrorMessage(err, fallbackMessage);
      onError(message);
      const errorVm = emptyViewModel({ error: true });
      signals.isLoading.set(false);
      signals.loadError.set(true);
      return of(errorVm);
    }),
    startWith(emptyViewModel({ loading: true })),
    tap((vm) => {
      signals.isLoading.set(vm.loading);
      signals.loadError.set(vm.error);
    })
  );
}
