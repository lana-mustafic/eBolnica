# 📈 Arhitekturalna Analiza - Vizuelni Pregled

## Struktura aplikacije

```
Rs1Frontend202526
│
├─ 🔐 Authentication Module (auth/)
│  ├─ LoginComponent
│  ├─ LogoutComponent
│  ├─ RegisterComponent
│  └─ TwoFactorComponent
│
├─ 🛒 Client Module (client/)
│  ├─ ClientOrdersComponent
│  ├─ ClientOrderDetailsComponent
│  └─ ReservationComponent
│
├─ ⚙️ Admin Module (admin/)
│  ├─ ProductManagement
│  │  ├─ ProductsComponent (CRUD)
│  │  └─ CategoriesComponent (CRUD)
│  ├─ OrderManagement
│  │  ├─ AdminOrdersComponent
│  │  └─ OrderEditComponent
│  ├─ FaktureComponent
│  ├─ DostavljaciComponent
│  └─ AdminSettingsComponent
│
├─ 📱 Public Module (public/)
│  └─ HomePage
│
├─ 🔧 Core Services
│  ├─ AuthFacadeService (Glavni auth servis)
│  │  ├─ login(credentials)
│  │  ├─ logout()
│  │  ├─ refresh(token)
│  │  └─ Signals: currentUser, isAuthenticated, isAdmin
│  ├─ AuthStorageService (Token storage)
│  ├─ CurrentUserService (User state)
│  └─ LoadingBarService
│
├─ 🌐 API Services
│  ├─ AuthApiService
│  ├─ ProductsApiService
│  ├─ OrdersApiService
│  ├─ FaktureApiService
│  └─ ProductCategoriesApiService
│
├─ 🛡️ Guards & Interceptors
│  ├─ myAuthGuard (Route protection)
│  ├─ authInterceptor (Token + 401 handling)
│  ├─ loadingBarInterceptor (UX feedback)
│  └─ errorLoggingInterceptor (Error tracking)
│
├─ 🌍 Internationalization
│  ├─ TranslateService
│  └─ i18n/ (bs.json, en.json)
│
└─ 📦 Shared Module
   ├─ Components
   ├─ Directives
   ├─ Pipes
   └─ Material UI
```

---

## 🔄 Authentication Flow

```
┌─────────────────────────────────────────────────────────┐
│           USER PRIJAVLJIVANJA (Login Flow)              │
└─────────────────────────────────────────────────────────┘

    1. Korisnik unese email + password
                    ↓
    2. LoginComponent → AuthFacadeService.login()
                    ↓
    3. AuthApiService.login() → Backend (HTTP POST)
                    ↓
    4. Backend vraća: { accessToken, refreshToken, expiresAtUtc }
                    ↓
    5. AuthStorageService.saveLogin() → localStorage
                    ↓
    6. Dekodira JWT → popuni _currentUser signal
                    ↓
    7. scheduleTokenRefresh() → postavi timer
                    ↓
    8. Router → /admin ili /client (zavisno od role)


┌─────────────────────────────────────────────────────────┐
│           HTTP ZAHTJEV SA AUTENTIFIKACIJOM              │
└─────────────────────────────────────────────────────────┘

    HTTP Request
           ↓
    authInterceptor
      │
      ├─ 1) Skip ako je /Auth endpoint
      │
      ├─ 2) Dodaj Authorization header
      │    Authorization: Bearer <access_token>
      │
      └─ 3) Send → Backend
                   ↓
              ┌─────────────┐
              │ Response    │
              └─────────────┘
                   ↓
         ┌─────────────────┐
         │ Status 200-299? │─── Yes ──→ Return response
         └────────┬────────┘
                  │ No (401)
                  ↓
          authInterceptor catch 401
                  ↓
         ┌──────────────────────────┐
         │ Token refresh in progress?│
         └────┬──────────────────────┘
              ├─ Yes → Wait + Retry with new token
              │
              └─ No → Perform refresh
                     ↓
                 AuthFacadeService.refresh()
                     ↓
                 Get new tokens
                     ↓
                 scheduleTokenRefresh()
                     ↓
                 Retry original request with new token
                     ↓
                 Return response


┌─────────────────────────────────────────────────────────┐
│        TOKEN REFRESH (Preventivni + Na 401)             │
└─────────────────────────────────────────────────────────┘

    1. Token se sprema sa expiresAtUtc vremenom
                    ↓
    2. scheduleTokenRefresh() se poziva nakon login/refresh
                    ↓
    3. Računamo: delayUntilRefresh = (expiresAt - now - 60000ms)
                    ↓
    4. setTimeout() čeka taj period
                    ↓
    5. performTokenRefresh() → refresh token
                    ↓
    6. Nove tokens se čuvaju
                    ↓
    7. Novi refresh se raspoređuje (iterativno)
```

---

## 🛡️ Role-Based Access Control (RBAC)

```
┌────────────────────────────────────────────────┐
│           ROUTE PROTECTION FLOW                 │
└────────────────────────────────────────────────┘

User navigira na /admin
           ↓
myAuthGuard aktivira
           ↓
┌──────────────────────────────────────────┐
│ route.data['auth'].requireAuth === true? │
└────┬─────────────────────────────────────┘
     │
     ├─ No → Dozvoli pristup (javna ruta)
     │
     └─ Yes → Provjeri authentication
              ↓
         ┌──────────────────────────┐
         │ Korisnik je login-ovan?  │
         └────┬───────────────────┬─┘
              │ No               │ Yes
              ↓                  ↓
         Go to /auth/login    Check roles
                               ↓
                    ┌──────────────────────┐
                    │ requireAdmin === true?│
                    └────┬───────────┬──────┘
                         │ No       │ Yes
                         ↓         ↓
                    Check      isAdmin()
                  Manager      === true?
                    role       ↓
                               ├─ Yes → Pusti ✅
                               │
                               └─ No → Go to /client/orders ❌


┌────────────────────────────────────────────────────┐
│         ROLE HIERARCHY (Kako funkcionira)          │
└────────────────────────────────────────────────────┘

ADMIN
  ├─ Pristup: /admin/** (sve)
  ├─ Pristup: /client/** (sve orders)
  ├─ Mogućnosti:
  │  ├─ Manage products & categories
  │  ├─ Manage all orders
  │  ├─ Manage users/roles
  │  └─ View analytics
  └─ Role check: isAdmin() === true

MANAGER
  ├─ Pristup: /admin/orders (samo orderi)
  ├─ Pristup: /client/** (samo njihovi orderi)
  ├─ Mogućnosti:
  │  ├─ View/manage assigned orders
  │  ├─ Update order status
  │  └─ View reports
  └─ Role check: isManager() === true

EMPLOYEE
  ├─ Pristup: /client/** (samo njihovi orderi)
  ├─ Mogućnosti:
  │  ├─ View their orders
  │  ├─ View order details
  │  └─ Submit updates
  └─ Role check: isEmployee() === true

UNAUTHENTICATED
  ├─ Pristup: /auth/**, / (public)
  ├─ Mogućnosti:
  │  ├─ Register
  │  ├─ Login
  │  └─ View public content
  └─ Role check: isAuthenticated() === false
```

---

## 📊 Dependency Injection & Service Injection Map

```
┌─────────────────────────────────────────────────────────────┐
│            INJECTION HIERARCHY                              │
└─────────────────────────────────────────────────────────────┘

AppModule (providedIn: 'root' servisi)
  │
  ├─ AuthFacadeService
  │  ├─ Injecta: AuthApiService
  │  ├─ Injecta: AuthStorageService
  │  └─ Injecta: Router
  │
  ├─ AuthApiService
  │  └─ Injecta: HttpClient
  │
  ├─ AuthStorageService
  │  └─ localStorage (window API)
  │
  ├─ CurrentUserService
  │  └─ Injecta: AuthFacadeService
  │
  ├─ ProductsApiService
  │  ├─ Injecta: HttpClient
  │  └─ Injecta: environment.apiUrl
  │
  ├─ OrdersApiService
  │  ├─ Injecta: HttpClient
  │  └─ Injecta: environment.apiUrl
  │
  ├─ LoadingBarService
  │  └─ Maintained: loading state
  │
  ├─ ToasterService
  │  └─ Maintained: toast messages
  │
  └─ TranslateService (@ngx-translate)
     └─ CustomTranslateLoader
        └─ Injecta: HttpClient


HTTP Pipeline (za svaki HTTP zahtjev):
  │
  └─ HttpClient.request()
     │
     ├─ loadingBarInterceptor
     │  └─ Start loading
     │
     ├─ authInterceptor
     │  ├─ Add Authorization header
     │  ├─ Handle 401 + refresh
     │  └─ Retry with new token
     │
     ├─ errorLoggingInterceptor
     │  └─ Log errors
     │
     └─ Backend
        └─ Response
           │
           └─ Back through interceptors
              └─ Return to component
```

---

## 📈 Data Flow: Product List Example

```
ProductsComponent (ngOnInit)
         ↓
     inject(ProductsApiService)
         ↓
     productService.list({ pageNumber: 1, pageSize: 10 })
         ↓
     HTTP GET /Products?pageNumber=1&pageSize=10
         ↓
     [authInterceptor]
     ├─ Dodaj Bearer token
     └─ Send
         ↓
     Backend
         ↓
     [200 OK]
     { items: [...], total: 100 }
         ↓
     [errorLoggingInterceptor] - OK, pass through
         ↓
     Component receives ListProductsResponse
         ↓
     Popuni signal: products = response.items
         ↓
     Template *ngFor
         ↓
     Prikaži proizvode
```

---

## 🔌 Environment Configuration

```
Development (environment.ts)
├─ apiUrl: http://localhost:7001
└─ Logging: console

Staging (environment.staging.ts)
├─ apiUrl: https://staging-api.example.com
├─ Logging: remote
└─ Analytics: enabled

Production (environment.prod.ts)
├─ apiUrl: https://api.example.com
├─ Logging: remote + alerts
├─ Analytics: full
└─ Optimizations: enabled
```

---

## 🚀 Build & Deployment Pipeline

```
Source Code
    ↓
ng build --configuration production
    ↓
Angular Compiler
    ├─ Bundling
    ├─ Tree-shaking
    ├─ Minification
    └─ Optimization
    ↓
dist/ folder
    ├─ index.html
    ├─ main.xxxxx.js
    ├─ polyfills.xxxxx.js
    ├─ styles.xxxxx.css
    └─ assets/
    ↓
Web Server (nginx/apache)
    ↓
Served to browser
    ↓
User downloads & executes
```

---

## 📱 Component Lifecycle Example

```
ProductsAddComponent (Add New Product)

1. Constructor
   - Injecta servise (FormBuilder, ProductsApiService, etc.)

2. ngOnInit
   - Inicijalizira FormGroup
   - Učitaj categories (ako je potrebno)

3. onSubmit()
   - Validacija forme
   - Disable submit button (loading)
   - Pozovi productService.create(formData)

4. HTTP Request (sa interceptorima)
   - POST /Products { name, description, ... }
   - authInterceptor dodaj token
   - loadingBarInterceptor start
   - Čekaj odgovor

5. Response
   - 200 OK: Preusmjeri na /admin/catalogs/products
   - 400: Prikaži error sa detalji validacije
   - 401: authInterceptor refresh → retry
   - 500: Prikaži generic error message

6. ngOnDestroy
   - Očisti subscriptions
   - Cancel pending requests (ako je potrebno)
```

---

## 🌐 Multi-Language Flow

```
User lands on application
           ↓
app.component.ts ngOnInit
           ↓
Provjeri localStorage.getItem('language')
           ↓
If not exists → set 'bs' (default)
           ↓
TranslateService.use('bs')
           ↓
CustomTranslateLoader
  ├─ HTTP GET /i18n/bs.json
  │  └─ { "PRODUCTS.TITLE": "Proizvodi", ... }
  │
  └─ HTTP GET /i18n/en.json (ako trebalo)
     └─ { "PRODUCTS.TITLE": "Products", ... }
           ↓
Template
  ├─ <h1>{{ 'PRODUCTS.TITLE' | translate }}</h1>
  │  └─ Rendera: "Proizvodi"
  │
  └─ User promijeni jezik
     ↓
     TranslateService.use('en')
     ├─ Učitaj translations
     └─ Template automatski se update
        └─ Rendera: "Products"
           └─ localStorage.setItem('language', 'en')
```

---

## 🔐 JWT Token Structure

```
JWT Token primjer:
Header.Payload.Signature

Dekodira se na server:
{
  "sub": "user-id",
  "email": "user@example.com",
  "isAdmin": true,
  "isManager": false,
  "isEmployee": false,
  "iat": 1674000000,
  "exp": 1674003600
}

U AuthFacadeService:
  ├─ Dekodira se sa jwt-decode
  ├─ Provjeri exp vrijeme
  ├─ Popuni _currentUser signal
  └─ Koristi se za isAdmin(), isManager(), itd.
```

---

## 📞 API Response Handling

```
Component zahtjeva
         ↓
ProductsApiService.list()
         ↓
HttpClient.get()
         ↓
authInterceptor
  ├─ Skip ako je auth endpoint
  ├─ Dodaj token
  └─ Proslijedi
         ↓
Backend
         ↓
Response može biti:
  ├─ 200 OK { items: [...], total: 100 }
  │
  ├─ 400 Bad Request { message: "Invalid filter" }
  │
  ├─ 401 Unauthorized
  │  ├─ authInterceptor catch
  │  ├─ Refresh token
  │  └─ Retry sa novim tokenima
  │
  ├─ 403 Forbidden { message: "Access denied" }
  │
  ├─ 404 Not Found
  │
  ├─ 500 Server Error
  │
  └─ 503 Service Unavailable
         ↓
errorLoggingInterceptor
  ├─ Log error
  └─ Proslijedi
         ↓
Component
  ├─ Receive response
  ├─ Update signal/state
  ├─ Update UI
  └─ Show error message (ako je trebalo)
```

---

## 🎨 UI State Management Pattern

```
Koristi se Angular Signals:

products = signal<Product[]>([]);
isLoading = signal(false);
error = signal<string | null>(null);

loadProducts() {
  this.isLoading.set(true);
  this.error.set(null);
  
  this.productService.list().subscribe({
    next: (response) => {
      this.products.set(response.items);
      this.isLoading.set(false);
    },
    error: (error) => {
      this.error.set(error.message);
      this.isLoading.set(false);
    }
  });
}

Template koristi computed ili async:
{{ products() | length }} proizvoda
<div *ngIf="isLoading()">Loading...</div>
<div *ngIf="error()">{{ error() }}</div>
```

---

## ✨ Zaključak Arhitekture

**Strengths:**
- ✅ Clear separation of concerns
- ✅ Reactive state management sa signals
- ✅ Comprehensive auth system
- ✅ Scalable module structure
- ✅ Type-safe services

**Areas for improvement:**
- ⚠️ Add more granular error handling
- ⚠️ Implement unit & e2e tests
- ⚠️ Add preventive token refresh
- ⚠️ Optimize bundle size
- ⚠️ Add offline capabilities

---

**Generated:** 31. januar 2026  
**Angular Version:** 21.0.0  
**Architecture Pattern:** Feature-based Modular with Facade Services
