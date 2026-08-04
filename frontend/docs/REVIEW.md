# 📋 Code Review: RS1 Frontend 2025-26

**Datum:** 31. januar 2026  
**Verzija:** Angular 21.0.0  
**TypeScript:** 5.9.2

---

## ✅ Pozitivne karakteristike

### 1. **Excellent Architecture & Separation of Concerns**
- **Jasna struktura modula**: Admin, Auth, Client, Public - svaki sa svojom routing i logikom
- **API Services layer**: Odvajanje HTTP komunikacije od poslovne logike
  ```
  api-services/
  ├── auth/
  ├── catalog/
  ├── products/
  ├── orders/
  └── fakture/
  ```
- **Core Services**: Centralizovana autentifikacija, interceptori i guardovi

### 2. **Authentication & Security Implementation** ⭐
**Jako dobro:**
- ✅ **JWT Token Management**: AuthFacadeService + AuthStorageService 
- ✅ **Token Refresh Logic**: Implementiran u auth-interceptor sa queue-ovanjem zahtjeva
- ✅ **HTTP Interceptors**: Tri strategički interceptora:
  - `authInterceptor` - dodaje Authorization header, handla 401
  - `loadingBarInterceptor` - UX feedback
  - `errorLoggingInterceptor` - centralizovano logovanje
- ✅ **Route Guards**: `myAuthGuard` sa role-based access control (Admin, Manager, Employee)
- ✅ **Safe token storage**: localStorage sa jasnim ključevima

### 3. **Modern Angular Patterns**
- ✅ **Signals API**: Korišteni za reactive state (`_currentUser`, `isAuthenticated`, itd.)
- ✅ **Computed signals**: `isAdmin()`, `isManager()`, `isEmployee()` - efikasno
- ✅ **Functional interceptors**: Novi Angular 15+ stil umjesto klasa
- ✅ **Lazy loading**: Svi feature moduli se učitavaju na zahtjev
- ✅ **Standalone components**: Priprema za budućnost

### 4. **Multi-language Support** 🌍
- ✅ **ngx-translate integration**: Podupirani jezici: Bosanski (bs), English (en)
- ✅ **i18n fajlovi**: Strukturirani `/public/i18n/` direktorij
- ✅ **localStorage persistence**: Jezik se čuva između sesija
- ✅ **Custom Translate Loader**: Prilagođeni loader za specifične potrebe

### 5. **API Communication** 📡
- ✅ **Type-safe DTOs**: Svaki API endpoint ima definirane request/response tipove
- ✅ **Consistent naming**: Commands (write), Queries (read) pattern
- ✅ **Error handling**: Centralizovan kroz interceptore
- ✅ **HTTP parameters builder**: `buildHttpParams()` za kompleksne filteriranja

### 6. **Modular Organization**
- ✅ **Shared Module**: Reusable komponente, direktivе, pipe-ovi
- ✅ **Material UI**: Angular Material integriranι za profesionalan izgled
- ✅ **Dialog components**: OrderDetailsDialog, ChangeStatusDialog - za modal operacije
- ✅ **Feature-based structure**: Lakše proširivanje sa novim modulima

---

## ⚠️ Preporuke za poboljšanja

### 1. **Authentication & Security**

#### 1.1 Token Expiration Handling
**Problem:** Token expiration vremena se čuvaju u localStorage, ali se ne koriste za preventivni refresh.
```typescript
// TRENUTNO: Čeka 401 grešku pa tek onda refresha
// PREPORUKA: Provjeravati prije isteka tokena
```

**Rješenje:**
Dodajte preventivni refresh prije nego što token istekne:
```typescript
export class AuthFacadeService {
  private tokenRefreshTimer: any;
  
  private scheduleTokenRefresh(): void {
    const expiresAt = this.storage.getAccessTokenExpiresAt();
    if (!expiresAt) return;
    
    const now = Date.now();
    const expirationTime = new Date(expiresAt).getTime();
    // Refresh 1 minutu prije isteka
    const refreshTime = expirationTime - now - 60000;
    
    if (refreshTime > 0) {
      this.tokenRefreshTimer = setTimeout(() => {
        this.refresh({ refreshToken: this.getRefreshToken(), fingerprint: null })
          .subscribe();
      }, refreshTime);
    }
  }
  
  logout(): Observable<void> {
    if (this.tokenRefreshTimer) clearTimeout(this.tokenRefreshTimer);
    // ... ostatak logout logike
  }
}
```

#### 1.2 Secure Token Storage
**Problem:** JWT se čuva u localStorage (dostupan XSS napadima).
**Preporuka:** Za produkciju razmislite o:
- HttpOnly cookies (zahtjeva backend promjenu)
- In-memory storage + refresh token rotation
- Content Security Policy headers

#### 1.3 CSRF Protection
**Problem:** Nema vidljive CSRF zaštite.
**Preporuka:** Dodajte CSRF token ako backend zahtjeva:
```typescript
// U auth-interceptor.service.ts
const csrfToken = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content');
if (csrfToken) {
  authReq = authReq.clone({
    setHeaders: { 'X-CSRF-Token': csrfToken }
  });
}
```

---

### 2. **Error Handling & Logging**

#### 2.1 Greške nisu dovoljno detalje loggirane
**Problem:** `errorLoggingInterceptor` verovatno samo logira u console.
**Preporuka:** 
```typescript
// Dodajte structured error logging sa:
// - Timestamp
// - Error code
// - Request details (bezbjedno - bez sensitive data)
// - Stack trace u dev modu

export const errorLoggingInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const errorLog = {
        timestamp: new Date().toISOString(),
        status: error.status,
        statusText: error.statusText,
        url: req.url,
        method: req.method,
        // user: auth.currentUser()?.email // ako je sigurno
      };
      
      // Pošalji na logging service / backend
      console.error('HTTP Error:', errorLog);
      
      return throwError(() => error);
    })
  );
};
```

#### 2.2 User-friendly error messages
**Preporuka:** 
```typescript
// U components, mapujte HTTP greške na user-friendly poruke:
login(credentials) {
  this.authFacade.login(credentials).pipe(
    catchError(error => {
      const message = this.getErrorMessage(error);
      this.toaster.error(message);
      return throwError(() => error);
    })
  ).subscribe();
}

private getErrorMessage(error: any): string {
  if (error.status === 401) return 'Pogrešna email ili lozinka';
  if (error.status === 403) return 'Niste autorizirani za ovu akciju';
  if (error.status === 500) return 'Server greška. Pokušajte kasnije';
  return 'Nepoznata greška';
}
```

---

### 3. **Code Quality & Testing**

#### 3.1 Nema vidljivih unit testova
**Problem:** `test` folder nije viđen u strukturi.
**Preporuka:** Dodajte testove za ključne servise:

```typescript
// auth-facade.service.spec.ts
describe('AuthFacadeService', () => {
  let service: AuthFacadeService;
  let apiService: jasmine.SpyObj<AuthApiService>;
  let storageService: jasmine.SpyObj<AuthStorageService>;

  beforeEach(() => {
    const apiSpy = jasmine.createSpyObj('AuthApiService', ['login', 'logout', 'refresh']);
    const storageSpy = jasmine.createSpyObj('AuthStorageService', 
      ['saveLogin', 'getAccessToken', 'clear']);

    TestBed.configureTestingModule({
      providers: [
        AuthFacadeService,
        { provide: AuthApiService, useValue: apiSpy },
        { provide: AuthStorageService, useValue: storageSpy }
      ]
    });

    service = TestBed.inject(AuthFacadeService);
    apiService = TestBed.inject(AuthApiService) as jasmine.SpyObj<AuthApiService>;
    storageService = TestBed.inject(AuthStorageService) as jasmine.SpyObj<AuthStorageService>;
  });

  it('should save tokens on successful login', (done) => {
    const loginResponse: LoginCommandDto = {
      accessToken: 'mock-token',
      refreshToken: 'mock-refresh',
      expiresAtUtc: new Date().toISOString()
    };

    apiService.login.and.returnValue(of(loginResponse));
    storageSpy.saveLogin.and.returnValue(undefined);

    service.login({ email: 'test@example.com', password: 'password' })
      .subscribe(() => {
        expect(storageSpy.saveLogin).toHaveBeenCalledWith(loginResponse);
        expect(service.isAuthenticated()).toBe(true);
        done();
      });
  });
});
```

#### 3.2 E2E testovi
**Preporuka:** Dodajte osnovne E2E testove za kritične tokove:
- Login / Logout
- Role-based access (Admin može pristupiti /admin, Client ne može)
- Token refresh na 401

---

### 4. **Type Safety & Null Safety**

#### 4.1 Potencijalni null reference errors
**Problem:** U `myAuthGuard.ts`:
```typescript
const user = currentUser.snapshot; // može biti null
if (requireAdmin && !user.isAdmin) { // ⚠️ user može biti null!
```

**Rješenje:**
```typescript
// Već je pokriveno sa null check-om prije:
if (!user) {
  router.navigate(['/auth/login']);
  return false;
}
// Ali eksplicitno navedite kao:
if (requireAdmin && user && !user.isAdmin) {
```

#### 4.2 Type-safe HTTP parameters
**Preporuka:** Proširite `buildHttpParams` za type safety:
```typescript
// core/models/build-http-params.ts
export function buildHttpParams<T>(
  obj: T,
  options?: { excludeFields?: (keyof T)[] }
): HttpParams {
  let params = new HttpParams();
  
  for (const [key, value] of Object.entries(obj || {})) {
    if (options?.excludeFields?.includes(key as keyof T)) continue;
    if (value === null || value === undefined) continue;
    
    if (Array.isArray(value)) {
      value.forEach(v => params = params.append(key, String(v)));
    } else {
      params = params.set(key, String(value));
    }
  }
  
  return params;
}
```

---

### 5. **Performance Optimizations**

#### 5.1 Change Detection
**Preporuka:** Koristite `OnPush` strategy gdje je moguće:
```typescript
@Component({
  selector: 'app-products',
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush  // ← Dodajte ovo
})
export class ProductsComponent {
  // ...
}
```

#### 5.2 Unsubscribe iz RxJS streamova
**Preporuka:** Koristite `takeUntil` pattern:
```typescript
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

export class MyComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  ngOnInit() {
    this.authFacade.currentUser.pipe(
      takeUntil(this.destroy$)
    ).subscribe(user => {
      // ...
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
```

#### 5.3 Lazy loading images
**Preporuka:** Za produkcijske slike koristi loading="lazy":
```html
<img src="image.jpg" alt="..." loading="lazy" />
```

---

### 6. **Configuration & Environment**

#### 6.1 API URL management
**Problem:** Hardkodiran u `environment.ts`
**Preporuka:** Dodajte više environment fajlova:
```typescript
// environment.staging.ts - postoji ✅
// environment.prod.ts - postoji ✅
// environment.local.ts - preporuka

// Također: validators da API URL postoji
import { environment } from '../environments/environment';

if (!environment.apiUrl) {
  console.error('❌ API_URL nije konfiguriran!');
}
```

#### 6.2 Feature flags
**Preporuka:** Dodajte feature flags za kontrolu funkcionalnosti:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:7001',
  features: {
    enableChat: true,
    enableTwoFactor: true,
    enableAnalytics: false
  }
};
```

---

### 7. **Documentation & Code Comments**

#### 7.1 ✅ Dobro dokumentirani servisi
- AuthFacadeService ima javdoc
- API servisi imaju JSDoc za endpoints

#### 7.2 Preporuka: README za feature module
```typescript
// modules/admin/README.md
# Admin Module

## Feature Overview
- Product Management
- Category Management
- Order Management
- User Settings

## Routes
- `/admin` - Admin Dashboard
- `/admin/catalogs/products` - Products CRUD
- `/admin/catalogs/categories` - Categories CRUD
- `/admin/orders` - Order Management
- `/admin/settings` - Admin Settings

## Key Services
- ProductsApiService
- OrdersApiService
- ProductCategoriesApiService

## Usage
```
Dodajte slične README fajlove za svaki feature modul.

---

### 8. **Build & Deployment**

#### 8.1 Bundle size
**Preporuka:** Analizirajte bundle veličinu:
```bash
ng build --stats-json
webpack-bundle-analyzer dist/*/stats.json
```

#### 8.2 Produksijsku optimizaciju
```json
{
  "scripts": {
    "build:prod": "ng build --configuration production --optimization --build-optimizer"
  }
}
```

#### 8.3 Service Worker
**Preporuka:** Dodajte offline podrške:
```bash
ng add @angular/service-worker
```

---

### 9. **Specifične tehnilčke preporuke**

#### 9.1 Interceptor ordering
⚠️ **Važno:** Redosljed interceptora je bitan!
```typescript
// app-module.ts - TRENUTNA KONFIGURACIJA
withInterceptors([
  loadingBarInterceptor,    // Trebao bi biti prvi (setup)
  authInterceptor,          // ✅ Dobar redosljed
  errorLoggingInterceptor   // ✅ Trebao bi biti posljednji (cleanup)
])
```

#### 9.2 Translation loading
**Problem:** Custom loader korištenje - svaki put se učitavaju .json fajlovi
**Preporuka:** Cache-ujte nakon učitavanja:
```typescript
export class CustomTranslateLoader implements TranslateLoader {
  private cache = new Map<string, Observable<any>>();

  getTranslation(lang: string): Observable<any> {
    if (this.cache.has(lang)) {
      return this.cache.get(lang)!;
    }

    const observable = this.http.get(`/i18n/${lang}.json`).pipe(
      shareReplay(1)
    );
    
    this.cache.set(lang, observable);
    return observable;
  }
}
```

#### 9.3 Role hierarchy
**Preoruka:** Dokumentujte jasno role hijerarhiju:
```typescript
/**
 * Role hierarchy (inclusive):
 * - ADMIN: pristup svemu
 * - MANAGER: pristup operacionoj logici
 * - EMPLOYEE: samo njihov dio
 * 
 * Primjer:
 * - Admin može vidjeti sve ordere
 * - Manager može vidjeti samo svoj region
 * - Employee može vidjeti samo svoj rad
 */
```

---

## 🎯 Priority Action Items

### VISOKI PRIORITET (Hajde odmah)
1. ✅ Dodajte unit testove za AuthFacadeService i auth-interceptor
2. ✅ Implementirajte preventivni token refresh
3. ✅ Dodajte CSRF protection ako backend zahtjeva
4. ✅ Poboljšajte error handling sa user-friendly porukama

### SREDNJI PRIORITET (Uskoro)
5. ✅ Dodajte ChangeDetectionStrategy.OnPush gdje je moguće
6. ✅ Dokumentujte role-based access control
7. ✅ Analizirajte bundle size
8. ✅ Dodajte feature flags za feature management

### NISKI PRIORITET (Dugoročno)
9. ✅ Dodajte Service Worker za offline podrške
10. ✅ Implementirajte analytics tracking
11. ✅ Dodajte E2E testove

---

## 📊 Ocjena po domenama

| Domena | Ocjena | Napomena |
|--------|--------|---------|
| **Arhitektura** | ⭐⭐⭐⭐⭐ | Odličan modularni dizajn |
| **Autentifikacija** | ⭐⭐⭐⭐½ | Dobra, ali nedostaje preventivni refresh |
| **Type Safety** | ⭐⭐⭐⭐ | Odličan, malo null-safetyja |
| **Testing** | ⭐⭐⭐ | Nedostaju testovi |
| **Performance** | ⭐⭐⭐⭐ | Dobar, mali prostor za optimizaciju |
| **Documentation** | ⭐⭐⭐⭐ | Solidna, mogla bi biti viša razina |
| **Security** | ⭐⭐⭐⭐ | Dobar, trebaju male dorade |

---

## 🚀 Zaključak

**Ovo je studentski projekat VISOKOG KVALITETA!**

Projekat pokazuje:
✅ Dobro razumijevanje Angular arhitekture  
✅ Primjena modernih Angular patterja (Signals, functional interceptors)  
✅ Čist i organizovan kod  
✅ Proaktivan pristup security-u  
✅ Skalabilna struktura  

Preporuke su uglavnom za "nice to have" optimizacije i best practices, a ne za kritične greške. Kod je spreman za produkciju sa minor poboljšanjima.

---

**Happy Coding! 🎉**
