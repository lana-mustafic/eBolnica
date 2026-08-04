# 🎨 Visual Reference Card - Quick Lookup

## 🔐 Authentication Flow - Quick Reference

```
LOGIN FLOW:
┌─────────────┐      ┌──────────────┐      ┌─────────────┐
│   Browser   │─────▶│  Backend     │─────▶│ Database    │
│             │      │  /Auth/login │      │             │
└─────────────┘      └──────────────┘      └─────────────┘
      │                    │
      │ POST email+pass    │ Returns:
      │                    │ - accessToken (JWT)
      │                    │ - refreshToken
      │                    │ - expiresAtUtc
      │◀───────────────────│
      │
      │ Store in localStorage
      │ Decode JWT → set currentUser signal
      │ scheduleTokenRefresh()
      │
      ▼
    Navigate to /admin or /client


TOKEN REFRESH ON 401:
┌──────────┐       ┌──────────┐       ┌─────────────┐
│Component │──────▶│Backend   │       │ Refreshed?  │
│Request   │       │Error 401 │       │             │
└──────────┘       └──────────┘       └─────────────┘
      │
      │ authInterceptor catches 401
      │
      ▼
┌─────────────────────────┐
│ Refresh token in cache? │
└────┬────────────────────┘
     │
     ├─ Yes → Wait for result
     │
     └─ No → Call /Auth/refresh
            ├─ Get new accessToken
            ├─ Store in localStorage
            ├─ scheduleTokenRefresh()
            │
            ▼
            Retry original request
            with new token
            │
            ▼
            Return response
```

---

## 🛡️ Route Protection - Quick Reference

```
User navigates to /admin
           ↓
myAuthGuard runs
           ↓
┌─────────────────────────────────┐
│ Is route public (no requireAuth)?│
└────┬─────────────────────────────┘
     │
     ├─ YES  → Allow ✅
     │
     └─ NO (requires auth)
            ├─ Is user logged in?
            │
            ├─ NO   → /auth/login 🚫
            │
            └─ YES  → Check roles
                     ├─ requireAdmin?
                     │  ├─ YES + isAdmin → Allow ✅
                     │  ├─ YES + NOT admin → /client 🚫
                     │
                     ├─ requireManager?
                     │  ├─ YES + isManager → Allow ✅
                     │  ├─ YES + NOT manager → /client 🚫
                     │
                     └─ Otherwise → Allow ✅
```

---

## 🌐 HTTP Request Pipeline

```
Component makes HTTP request
           ↓
┌────────────────────────────────────┐
│ 1. loadingBarInterceptor           │
│    ├─ Show loading spinner         │
│    └─ Continue                     │
└────────────────────────────────────┘
           ↓
┌────────────────────────────────────┐
│ 2. authInterceptor                 │
│    ├─ Add Authorization header     │
│    ├─ Skip auth endpoints          │
│    └─ Continue                     │
└────────────────────────────────────┘
           ↓
┌────────────────────────────────────┐
│ Backend responds                   │
│    ├─ 200-299 OK  → Continue       │
│    └─ 401        → Refresh + retry │
└────────────────────────────────────┘
           ↓
┌────────────────────────────────────┐
│ 3. errorLoggingInterceptor         │
│    ├─ Log error (if error)         │
│    └─ Continue                     │
└────────────────────────────────────┘
           ↓
Component receives response
```

---

## 📊 Signal Updates Flow

```
User action → Component method
           ↓
Update signal:
  products.set(newValue)
           ↓
Computed signals auto-update:
  filteredProducts = computed(() => 
    products().filter(...)
  )
           ↓
Template auto-refreshes:
  {{ products() | length }}
           ↓
Browser DOM updates
```

---

## 🗂️ Module Load Order

```
AppModule initializes
  ├─ BrowserModule
  ├─ HttpClientModule with interceptors
  │  ├─ loadingBarInterceptor
  │  ├─ authInterceptor
  │  └─ errorLoggingInterceptor
  ├─ TranslateModule
  ├─ SharedModule
  ├─ MaterialModules
  └─ AppRoutingModule
         ↓
User navigates
         ↓
Feature module lazy-loads:
  ├─ AdminModule
  ├─ AuthModule
  ├─ ClientModule
  └─ PublicModule
```

---

## 🔄 Service Dependency Tree

```
                    AppModule
                       │
        ┌──────────────┼──────────────┐
        │              │              │
    AuthFacadeService  ProductsApiService  LoadingBarService
        │
        ├─ AuthApiService
        │   └─ HttpClient
        │
        ├─ AuthStorageService
        │   └─ localStorage
        │
        └─ Router
           └─ Angular Router

CurrentUserService
    └─ AuthFacadeService
        └─ [see above]
```

---

## 🎯 Token Timeline Example

```
Login time:        00:00
Access expires:    01:00 (1 hour)
Refresh scheduled: 00:59 (1 minute before)

Timeline:
00:00 ─ User logs in
        ├─ Save tokens
        ├─ Set refresh timer for 00:59
        └─ Show dashboard

00:55 ─ [User still active]
        └─ Timer waiting...

00:59 ─ Refresh scheduled time
        ├─ Call /Auth/refresh
        ├─ Get new tokens
        ├─ Set new timer for 01:59
        └─ User doesn't notice ✅

01:59 ─ Next refresh

...continues silently in background...

User logs out → Clear timer + tokens + localStorage
```

---

## 📱 Component Types in This Project

```
CONTAINER COMPONENTS (Smart):
├─ AdminOrdersComponent
│  └─ Handles data fetching, updates
│
├─ ProductsComponent
│  └─ CRUD operations
│
└─ LoginComponent
   └─ Auth logic

PRESENTATIONAL COMPONENTS (Dumb):
├─ OrderDetailsDialogComponent
│  └─ Just displays data
│
├─ ChangeStatusDialogComponent
│  └─ Just displays form
│
└─ Shared UI components
   └─ Buttons, inputs, etc.
```

---

## 🌍 i18n Flow

```
App starts
    ├─ Check localStorage.getItem('language')
    │  ├─ If exists → use it
    │  └─ If not → use 'bs' (default)
    │
    ├─ TranslateService.use('bs')
    │
    ├─ Load /i18n/bs.json
    │  └─ { "PRODUCTS.TITLE": "Proizvodi" }
    │
    ├─ Template: {{ 'PRODUCTS.TITLE' | translate }}
    │  └─ Renders: "Proizvodi"
    │
    └─ User clicks language button
       ├─ TranslateService.use('en')
       ├─ Load /i18n/en.json
       ├─ Update UI
       └─ localStorage.setItem('language', 'en')
```

---

## ✅ Error Handling Pattern

```
try:
  HTTP Request
    │
    ├─ 200 OK
    │  └─ Return data
    │
    ├─ 400 Bad Request
    │  └─ Error message: "Invalid input"
    │
    ├─ 401 Unauthorized
    │  └─ authInterceptor: Refresh + retry
    │
    ├─ 403 Forbidden
    │  └─ Error message: "Access denied"
    │
    ├─ 500 Server Error
    │  └─ Error message: "Server error"
    │
    └─ Network error
       └─ Error message: "Connection failed"

catch:
  errorLoggingInterceptor
    ├─ Log to console (dev)
    ├─ Log to server (prod)
    └─ Pass to component

Component:
  ├─ Disable loading state
  ├─ Show toaster with message
  └─ Allow user to retry
```

---

## 🎛️ Configuration Layers

```
environment.ts (Dev)
├─ apiUrl: localhost:7001
└─ production: false

environment.staging.ts
├─ apiUrl: staging-api.example.com
└─ production: false

environment.prod.ts
├─ apiUrl: https://api.example.com
└─ production: true

Import:
  import { environment } from '@env'
  `${environment.apiUrl}/Products`
```

---

## 🧪 Test Structure (Recommended)

```
src/
  app/
    core/
      services/
        auth/
          auth-facade.service.ts
          auth-facade.service.spec.ts  ← Test file
    api-services/
      products/
        products-api.service.ts
        products-api.service.spec.ts  ← Test file
    modules/
      admin/
        catalogs/
          products/
            products.component.ts
            products.component.spec.ts  ← Test file
```

---

## 📈 Bundle Size Tips

```
To analyze:
  ng build --stats-json
  webpack-bundle-analyzer dist/*/stats.json

Common issues:
  ├─ Large libraries not tree-shaken
  ├─ Unused Material modules
  ├─ Images not optimized
  └─ Missing lazy loading

Fix with:
  ├─ Import only needed Material modules
  ├─ Use lazy loading for features
  ├─ Compress images
  └─ Remove unused dependencies
```

---

## 🔐 Security Checklist

```
DONE:
  ✅ JWT tokens
  ✅ Authorization header
  ✅ Token refresh on 401
  ✅ Route guards
  ✅ Role-based access

TODO:
  ⚠️ Preventive token refresh
  ⚠️ CSRF protection
  ⚠️ Input validation
  ⚠️ XSS prevention
  ⚠️ Secure headers
```

---

## 🎨 Styling Architecture

```
Global:
  src/styles.scss
    └─ Global styles

Component-scoped:
  components/
    product-list/
      product-list.component.scss
        └─ Scoped to component only

Material theming:
  ├─ Material color palette
  ├─ Custom theme colors
  └─ Scss variables
```

---

## 🚀 Performance Quick Tips

```
✅ Already doing:
  ├─ Lazy loading modules
  ├─ Tree-shaking (production build)
  └─ Bundling optimization

⚠️ Can improve:
  ├─ Add ChangeDetectionStrategy.OnPush
  ├─ Use trackBy in *ngFor
  ├─ Implement virtual scroll for lists
  ├─ Cache HTTP responses
  ├─ Image lazy loading
  └─ Code splitting
```

---

## 💾 localStorage Keys Used

```
accessToken          ← JWT token
refreshToken         ← Refresh token
accessTokenExpiresAtUtc  ← Token expiry
refreshTokenExpiresAtUtc ← Refresh expiry
language             ← Current language (bs/en)
```

---

## 🎯 Common Tasks Reference

```
ADD NEW API ENDPOINT:
  1. Create model in api-services/resource/resource-api.models.ts
  2. Create service in api-services/resource/resource-api.service.ts
  3. Inject in component
  4. Use in template/logic

ADD NEW ROUTE:
  1. Add to app-routing-module.ts or feature routing
  2. Create component
  3. Import in module
  4. Add guard if protected

ADD NEW LANGUAGE:
  1. Add to i18n/ (new .json file)
  2. Add to TranslateService.addLangs()
  3. Add UI toggle button

UPDATE STYLES:
  1. Global: src/styles.scss
  2. Component: component.component.scss
  3. Use Material variables: $primary-color, etc.
```

---

## 🎓 Learning Path for This Architecture

```
1. Understand HTTP & REST
   └─ API services layer

2. Learn Angular basics
   └─ Components, modules, directives

3. Master RxJS
   └─ Observables, Subjects, operators

4. Learn Angular advanced
   └─ Guards, interceptors, lazy loading

5. Understand state management
   └─ Signals, services as state

6. Security concepts
   └─ JWT, authentication, authorization
```

---

**Last Updated:** 31. januar 2026  
**For:** RS1 Frontend 2025-26  
**Quick Reference:** Yes ✓
