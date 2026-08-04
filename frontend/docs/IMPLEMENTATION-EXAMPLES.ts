/**
 * RECOMMENDED IMPROVEMENTS - Praktični primjeri koda
 *
 * Ovi primjeri pokazuju kako implementirati preporuke iz REVIEW.md
 */

// ============================================================================
// 1. PREVENTIVNI TOKEN REFRESH
// ============================================================================

// Datoteka: src/app/core/services/auth/auth-facade-improved.service.ts
/*
  Dodajte ovu metodu u AuthFacadeService:
*/

private tokenRefreshTimer: any;

/**
 * Schedule automatic token refresh prije nego što istekne.
 * Refresha token 1 minutu prije isteka.
 */
private scheduleTokenRefresh(): void {
  // Očisti stari timer ako postoji
  if (this.tokenRefreshTimer) {
    clearTimeout(this.tokenRefreshTimer);
  }

  const expiresAtUtc = this.storage.getAccessTokenExpiresAt();
  if (!expiresAtUtc) {
    console.warn('Token expiration time not available');
    return;
  }

  try {
    const now = Date.now();
    const expirationTime = new Date(expiresAtUtc).getTime();

    if (isNaN(expirationTime)) {
      console.warn('Invalid token expiration format:', expiresAtUtc);
      return;
    }

    // Refresh 1 minutu prije nego što istekne
    const REFRESH_BUFFER_MS = 60000; // 1 minut
    const timeUntilExpiry = expirationTime - now;
    const delayUntilRefresh = timeUntilExpiry - REFRESH_BUFFER_MS;

    if (delayUntilRefresh > 0) {
      console.log(`Token refresh scheduled in ${Math.round(delayUntilRefresh / 1000)}s`);

      this.tokenRefreshTimer = setTimeout(() => {
        this.performTokenRefresh();
      }, delayUntilRefresh);
    } else if (timeUntilExpiry > 0) {
      // Token će uskoro istjeći, refresh odmah
      this.performTokenRefresh();
    } else {
      // Token je već istekao
      this.logout().subscribe();
    }
  } catch (error) {
    console.error('Error scheduling token refresh:', error);
  }
}

/**
 * Izvrši token refresh
 */
private performTokenRefresh(): void {
  const refreshToken = this.storage.getRefreshToken();
  if (!refreshToken) {
    console.warn('No refresh token available');
    this.logout().subscribe();
    return;
  }

  this.refresh({ refreshToken, fingerprint: null }).pipe(
    tap(() => {
      console.log('Token refreshed successfully');
      // Rasporedi novi refresh
      this.scheduleTokenRefresh();
    }),
    catchError((error) => {
      console.error('Token refresh failed:', error);
      this.logout().subscribe();
      return throwError(() => error);
    })
  ).subscribe();
}

// Pozovi ovo u login i refresh metodama:
login(payload: LoginCommand): Observable<void> {
  return this.api.login(payload).pipe(
    tap((response: LoginCommandDto) => {
      this.storage.saveLogin(response);
      this.decodeAndSetUser(response.accessToken);
      this.scheduleTokenRefresh(); // ← DODAJTE OVO
    }),
    map(() => void 0)
  );
}

// Također u logout-u očisti timer:
logout(): Observable<void> {
  if (this.tokenRefreshTimer) {
    clearTimeout(this.tokenRefreshTimer);
  }
  // ... ostatak logout logike
}


// ============================================================================
// 2. POBOLJŠAN ERROR HANDLING SA USER-FRIENDLY PORUKAMA
// ============================================================================

// Datoteka: src/app/core/services/error-message.service.ts

import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ErrorMessageService {
  /**
   * Mapira HTTP greške na korisničke poruke
   */
  getUserFriendlyMessage(error: any): string {
    // Ako je to HttpErrorResponse
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 0:
          return 'Greška pri konekciji. Provjerite internet konekciju.';

        case 400:
          return error.error?.message || 'Pogrešan format podataka.';

        case 401:
          // Razlikujte između login i drugih 401 grešaka
          if (error.url?.includes('/Auth/login')) {
            return 'Pogrešna email ili lozinka.';
          }
          return 'Sesija je istekla. Molimo prijavite se ponovo.';

        case 403:
          return 'Nemате dozvolu za ovu akciju.';

        case 404:
          return 'Resurs nije pronađen.';

        case 409:
          return 'Konflikt pri sačuvanju. Podatak već postoji.';

        case 422:
          return 'Validacijska greška. Provjerite upisane podatke.';

        case 500:
          return 'Greška na serveru. Pokušajte kasnije.';

        case 503:
          return 'Server je nedostupan. Pokušajte kasnije.';

        default:
          return `Greška: ${error.statusText || 'Nepoznata greška'}`;
      }
    }

    // Ako je to timeout
    if (error.name === 'TimeoutError') {
      return 'Zahtjev je trajao previše dugo. Pokušajte ponovo.';
    }

    // Ako je to custom error
    if (error.message) {
      return error.message;
    }

    return 'Došlo je do neočekivane greške. Pokušajte ponovo.';
  }

  /**
   * Detaljne greške za development mode
   */
  getDetailedMessage(error: any): string {
    if (error instanceof HttpErrorResponse) {
      return `${error.status} ${error.statusText}: ${error.error?.message || ''}`;
    }
    return error?.message || JSON.stringify(error);
  }
}


// Datoteka: src/app/modules/auth/login/login.component.ts

import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ErrorMessageService } from '../../../core/services/error-message.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  private authFacade = inject(AuthFacadeService);
  private errorMessages = inject(ErrorMessageService);
  private toaster = inject(ToasterService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  isLoading = false;
  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  onLogin(): void {
    if (this.loginForm.invalid) {
      this.toaster.warning('Molimo ispunite sve polje ispravno.');
      return;
    }

    this.isLoading = true;

    const credentials = {
      email: this.loginForm.get('email')!.value,
      password: this.loginForm.get('password')!.value
    };

    this.authFacade.login(credentials).subscribe({
      next: () => {
        this.toaster.success('Uspješna prijava!');
        // Redirektuj na default rutu (admin ili client)
        const defaultRoute = this.authFacade.currentUser()?.isAdmin
          ? '/admin'
          : '/client/orders';
        this.router.navigate([defaultRoute]);
      },
      error: (error) => {
        const userMessage = this.errorMessages.getUserFriendlyMessage(error);
        this.toaster.error(userMessage);

        // Log detaljnu grešku samo u dev modu
        if (!environment.production) {
          console.error('Login error details:', this.errorMessages.getDetailedMessage(error));
        }

        this.isLoading = false;
      }
    });
  }
}


// ============================================================================
// 3. POBOLJŠAN ERROR LOGGING INTERCEPTOR
// ============================================================================

// Datoteka: src/app/core/interceptors/error-logging-interceptor-improved.service.ts

import {
  HttpInterceptorFn,
  HttpErrorResponse
} from '@angular/common/http';
import { inject } from '@angular/core';
import { LoggingService } from '../services/logging.service';
import { environment } from '../../../environments/environment';

export const errorLoggingInterceptor: HttpInterceptorFn = (req, next) => {
  const logger = inject(LoggingService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const errorLog = {
        timestamp: new Date().toISOString(),
        status: error.status,
        statusText: error.statusText,
        url: req.url,
        method: req.method,
        message: error.message,
        // Safe fields - bez sensitive podataka
        userId: 'anonymous', // popunite ako je dostupno iz auth servisa
      };

      // U dev modu loguj sve detalje
      if (!environment.production) {
        console.group('❌ HTTP Error');
        console.error('Status:', error.status);
        console.error('Message:', error.message);
        console.error('URL:', req.url);
        console.error('Response:', error.error);
        console.groupEnd();
      }

      // Pošalji na remote logging service (ako postoji)
      logger.logError(errorLog);

      // Za kritične greške (500+) dodaj extra alert
      if (error.status >= 500) {
        logger.alertAdmins({
          severity: 'critical',
          message: `Server error: ${error.status} on ${req.url}`,
          timestamp: new Date().toISOString()
        });
      }

      return throwError(() => error);
    })
  );
};


// Datoteka: src/app/core/services/logging.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LoggingService {
  private http = inject(HttpClient);

  /**
   * Log error na server (ako backend podržava)
   */
  logError(errorLog: any): void {
    if (environment.production) {
      this.http.post(`${environment.apiUrl}/api/Logging/error`, errorLog)
        .subscribe({
          error: (err) => console.error('Failed to log error:', err)
        });
    } else {
      console.log('Error logged (dev mode):', errorLog);
    }
  }

  /**
   * Alert administratore o kritičnim greškama
   */
  alertAdmins(alert: any): void {
    if (environment.production) {
      this.http.post(`${environment.apiUrl}/api/Logging/alert-admins`, alert)
        .subscribe({
          error: (err) => console.error('Failed to alert admins:', err)
        });
    }
  }
}


// ============================================================================
// 4. TYPE-SAFE HTTP PARAMETERS
// ============================================================================

// Datoteka: src/app/core/models/build-http-params-improved.ts

import { HttpParams } from '@angular/common/http';

export interface HttpParamsOptions {
  excludeFields?: string[];
  excludeNullish?: boolean;
  dateFormat?: 'iso' | 'timestamp';
}

/**
 * Type-safe HttpParams builder
 * Primjer:
 * ```
 * const params = buildHttpParams({
 *   pageNumber: 1,
 *   pageSize: 10,
 *   searchTerm: 'test',
 *   startDate: new Date('2025-01-01')
 * }, {
 *   dateFormat: 'iso',
 *   excludeNullish: true
 * });
 * ```
 */
export function buildHttpParams(
  obj: Record<string, any>,
  options: HttpParamsOptions = {}
): HttpParams {
  let params = new HttpParams();
  const { excludeFields = [], excludeNullish = true, dateFormat = 'iso' } = options;

  for (const [key, value] of Object.entries(obj || {})) {
    // Preskoči excluded fields
    if (excludeFields.includes(key)) continue;

    // Preskoči null/undefined ako je enabled
    if (excludeNullish && (value === null || value === undefined)) {
      continue;
    }

    // Poseban tretman za datume
    if (value instanceof Date) {
      const formattedDate = dateFormat === 'iso'
        ? value.toISOString()
        : value.getTime();
      params = params.set(key, String(formattedDate));
      continue;
    }

    // Array vrijednosti
    if (Array.isArray(value)) {
      value.forEach(v => {
        params = params.append(key, String(v));
      });
      continue;
    }

    // Boolean
    if (typeof value === 'boolean') {
      params = params.set(key, value ? 'true' : 'false');
      continue;
    }

    // Sve ostale vrijednosti
    params = params.set(key, String(value));
  }

  return params;
}


// ============================================================================
// 5. SETUP TESTA ZA AUTH FACADE SERVICE
// ============================================================================

// Datoteka: src/app/core/services/auth/auth-facade.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthFacadeService } from './auth-facade.service';
import { AuthApiService } from '../../../api-services/auth/auth-api.service';
import { AuthStorageService } from './auth-storage.service';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

describe('AuthFacadeService', () => {
  let service: AuthFacadeService;
  let apiServiceMock: jasmine.SpyObj<AuthApiService>;
  let storageServiceMock: jasmine.SpyObj<AuthStorageService>;
  let routerMock: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const apiSpy = jasmine.createSpyObj('AuthApiService', [
      'login',
      'logout',
      'refresh'
    ]);

    const storageSpy = jasmine.createSpyObj('AuthStorageService', [
      'saveLogin',
      'saveRefresh',
      'clear',
      'getAccessToken',
      'getRefreshToken',
      'getAccessTokenExpiresAt'
    ]);

    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        AuthFacadeService,
        { provide: AuthApiService, useValue: apiSpy },
        { provide: AuthStorageService, useValue: storageSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthFacadeService);
    apiServiceMock = TestBed.inject(AuthApiService) as jasmine.SpyObj<AuthApiService>;
    storageServiceMock = TestBed.inject(AuthStorageService) as jasmine.SpyObj<AuthStorageService>;
    routerMock = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  describe('login', () => {
    it('should save tokens and set current user on successful login', (done) => {
      const mockResponse = {
        accessToken: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c',
        refreshToken: 'mock-refresh-token',
        expiresAtUtc: new Date(Date.now() + 3600000).toISOString()
      };

      apiServiceMock.login.and.returnValue(of(mockResponse));
      storageServiceMock.saveLogin.and.returnValue(undefined);

      const credentials = { email: 'test@example.com', password: 'password' };

      service.login(credentials).subscribe(() => {
        expect(storageServiceMock.saveLogin).toHaveBeenCalledWith(mockResponse);
        expect(service.isAuthenticated()).toBe(true);
        expect(service.currentUser()).not.toBeNull();
        done();
      });
    });

    it('should handle login error', (done) => {
      const mockError = new HttpErrorResponse({
        error: { message: 'Invalid credentials' },
        status: 401,
        statusText: 'Unauthorized'
      });

      apiServiceMock.login.and.returnValue(throwError(() => mockError));

      const credentials = { email: 'test@example.com', password: 'wrong' };

      service.login(credentials).subscribe({
        error: (error) => {
          expect(service.isAuthenticated()).toBe(false);
          expect(error).toBe(mockError);
          done();
        }
      });
    });
  });

  describe('logout', () => {
    it('should clear tokens and user state', (done) => {
      // Prvo ga login-ujemo
      const mockResponse = {
        accessToken: 'token',
        refreshToken: 'refresh',
        expiresAtUtc: new Date().toISOString()
      };
      apiServiceMock.login.and.returnValue(of(mockResponse));
      storageServiceMock.saveLogin.and.returnValue(undefined);

      service.login({ email: 'test@example.com', password: 'password' })
        .subscribe(() => {
          // Sada ga logout-ujemo
          apiServiceMock.logout.and.returnValue(of(void 0));
          storageServiceMock.clear.and.returnValue(undefined);

          service.logout().subscribe(() => {
            expect(storageServiceMock.clear).toHaveBeenCalled();
            expect(service.isAuthenticated()).toBe(false);
            done();
          });
        });
    });
  });

  describe('isAdmin computed signal', () => {
    it('should return true if user is admin', (done) => {
      const mockResponse = {
        accessToken: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc0FkbWluIjp0cnVlfQ.xxx',
        refreshToken: 'refresh',
        expiresAtUtc: new Date().toISOString()
      };

      apiServiceMock.login.and.returnValue(of(mockResponse));
      storageServiceMock.saveLogin.and.returnValue(undefined);

      service.login({ email: 'admin@example.com', password: 'password' })
        .subscribe(() => {
          expect(service.isAdmin()).toBe(true);
          done();
        });
    });
  });
});


// ============================================================================
// 6. FEATURE FLAGS KONFIGURACIJA
// ============================================================================

// Datoteka: src/environments/environment.ts

export const environment = {
  production: false,
  apiUrl: 'http://localhost:7001',

  // Feature flags
  features: {
    enableChat: true,
    enableTwoFactor: false,
    enableAnalytics: false,
    enableOrderTracking: true,
    enableNotifications: true
  },

  // API timeouts
  api: {
    requestTimeout: 30000, // 30 sekundi
    retryAttempts: 2,
    retryDelay: 1000
  },

  // Logging
  logging: {
    enableConsoleLogging: true,
    enableRemoteLogging: false,
    logLevel: 'DEBUG' // DEBUG, INFO, WARN, ERROR
  }
};


// Datoteka: src/app/core/services/feature-flag.service.ts

import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class FeatureFlagService {
  /**
   * Check if feature je enabled
   *
   * Primjer:
   * ```
   * if (this.featureFlags.isEnabled('enableChat')) {
   *   // pokaži chat
   * }
   * ```
   */
  isEnabled(featureName: keyof typeof environment.features): boolean {
    return environment.features[featureName] ?? false;
  }

  /**
   * Get all features
   */
  getAll() {
    return environment.features;
  }
}


// ============================================================================
// 7. SETUP ZA SERVICE WORKER (Offline podrška)
// ============================================================================

/*
  Instalacija:
  ng add @angular/service-worker

  Datoteka: ngsw-config.json

  {
    "$schema": "./node_modules/@angular/service-worker/config/schema.json",
    "index": "/index.html",
    "assetGroups": [
      {
        "name": "app",
        "installMode": "prefetch",
        "resources": {
          "files": [
            "/favicon.ico",
            "/index.html",
            "/manifest.webmanifest",
            "/**/*.css",
            "/**/*.js"
          ]
        }
      },
      {
        "name": "assets",
        "installMode": "lazy",
        "updateMode": "prefetch",
        "resources": {
          "files": [
            "/assets/**",
            "/**/.*"
          ]
        }
      }
    ],
    "dataGroups": [
      {
        "name": "api",
        "urls": ["/api/**"],
        "cacheConfig": {
          "strategy": "freshness",
          "maxAge": "1h",
          "maxSize": 100
        }
      }
    ]
  }
*/

*/
