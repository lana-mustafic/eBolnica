import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { authInterceptor} from './shared/services/auth-interceptor.service';
import { I18nService } from './shared/services/i18n.service';
import { filter, take } from 'rxjs/operators';
import { firstValueFrom } from 'rxjs';

function initializeI18n(i18nService: I18nService, http: HttpClient) {
  return () => {
    // Initialize i18n service with HttpClient
    i18nService.initialize(http);
    
    // Wait for translations to be loaded
    return firstValueFrom(
      i18nService.getTranslationsLoaded().pipe(
        filter(loaded => loaded === true),
        take(1)
      )
    );
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }), 
    provideRouter(routes), 
    provideHttpClient(
      withInterceptors([authInterceptor])
    ),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeI18n,
      deps: [I18nService, HttpClient],
      multi: true
    }
  ]
};
