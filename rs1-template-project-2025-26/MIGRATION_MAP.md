# eBolnica → RS1 Template (Clean Architecture + CQRS) — Feature Map

## Putanje

| Uloga | Zadana putanja | Stvarna putanja |
|-------|----------------|-----------------|
| OLD | `c:\tmp\moj-rad-2024-25` | **NE POSTOJI** → koristiti `c:\projects\eBolnica` |
| NEW | `c:\tmp\rs1-template-project-2025-26` | Kopirano iz `Desktop\github\rs1_*-2025-26` |

## Arhitektura — before / after

| Aspekt | Stari (eBolnica) | Novi (RS1 template) |
|--------|------------------|---------------------|
| Backend | Monolith API, Controllers → Services → DbContext | Domain / Application / Infrastructure / API |
| Patterns | Anemic services | CQRS + MediatR + FluentValidation pipeline |
| Auth | Identity + custom JWT, `UserType` claim | JWT + refresh token, role policies |
| FE | Standalone components, flat routes | NgModules, lazy loading, api-services layer |
| DB | EF migrations u API projektu | Migrations u Infrastructure, seeder pattern |

---

## Feature map

### 1. Auth & Registration

| Stari | Novi (CQRS modul) |
|-------|-------------------|
| **BE** `AccountsController`: patient/doctor registration, login | `Modules/Auth/Commands/RegisterPatient`, `RegisterDoctor`, `Login` |
| **Entity** `AppUser` (+ Doctor/Patient/Pharmacist 1:1) | `MarketUserEntity` → prilagoditi na `AppUser` + profile entitete |
| **DTO** `UserLoginDto`, `PatientRegistrationDto`, `DoctorRegistrationDto` | Command/Query DTOs + Validators |
| **FE** `login/`, `patient-registration/`, `doctor-registration/` | `modules/auth/` lazy, `api-services/auth/` |
| **JWT** localStorage `jwtToken`, role iz `UserType` | Auth interceptor + refresh, policies po roli |

### 2. Admin — User Management

| Stari | Novi |
|-------|------|
| **BE** `AdminController`: list-users, create/update/delete, approve registration | `Modules/Admin/Users/Commands/*`, `Queries/ListUsers` |
| **Entity** AppUser, Doctor, Patient, Pharmacist | Isti domen, soft-delete preko BaseEntity |
| **FE** `admin/admin-dashboard` | `modules/admin/users/` lazy routes |

### 3. Doctor — Profile & Dashboard

| Stari | Novi |
|-------|------|
| **BE** `DoctorController`: doctor-data, edit-doctor, doctor-stats | `Modules/Doctor/Profile/*`, `Modules/Doctor/Dashboard/Queries/GetDoctorStats` |
| **FE** doctor-dashboard, doctor-profile, doctor-profile-edit | `modules/doctor/` lazy modul |

### 4. Doctor — Patients List

| Stari | Novi |
|-------|------|
| **BE** `GET /api/doctor/list-patients` (paginated, filters) | `Modules/Doctor/Patients/Queries/ListPatients` |
| **FE** `doctor-patients` | `modules/doctor/patients/` |

### 5. Medical Records & Reports

| Stari | Novi |
|-------|------|
| **BE** `MedicalRecordController`: get records, new report | `Modules/MedicalRecords/Queries/*`, `Commands/CreateMedicalReport` |
| **Entity** MedicalRecord, MedicalReport | Domain entities + EF configs |
| **FE** `medical-record/:patientId` | `modules/doctor/medical-records/` |

### 6. Medical Record PDF

| Stari | Novi |
|-------|------|
| **BE** `PdfReportController`: QuestPDF | `Modules/MedicalRecords/Queries/GenerateMedicalRecordPdf` (infrastructure service) |
| **FE** PDF download u medical-record | api-service + download u komponenti |

### 7. Patient Portal

| Stari | Novi |
|-------|------|
| **BE** `PatientController`: patient-data | `Modules/Patient/Queries/GetPatientProfile` |
| **FE** `patient-dashboard` | `modules/patient/` lazy |

### 8. Patient Files

| Stari | Novi |
|-------|------|
| **BE** `FileController`: upload/download/delete/list | `Modules/Files/Commands/*`, `Queries/ListPatientFiles` |
| **Entity** FileEntity | Domain + file storage abstraction |
| **FE** file upload u medical-record | `modules/doctor/files/` ili shared |

### 9–17. Pharmacy (najveći modul)

| Pod-feature | Stari endpointi (prefix `/api/pharmacy`) | Novi modul |
|-------------|----------------------------------------|------------|
| Medications CRUD | medications GET/POST/PUT/DELETE | `Modules/Pharmacy/Medications/` |
| CSV import/export | medications/export, import | Commands/ExportCsv, ImportCsv |
| Autocomplete/check-name | medications/autocomplete, check-name | Queries |
| Images | medications/{id}/images/* | Commands + file storage |
| AI summary | medications/{id}/ai-summary | Command (OpenAI infra) |
| Prescriptions | prescriptions CRUD | `Modules/Pharmacy/Prescriptions/` |
| Dispense | prescriptions/{id}/dispense | Command (transaction) |
| Inventory | inventory GET | Query |
| Analytics | analytics/* | Queries (cache) |
| PDF reports | reports/*/pdf | Queries |
| Pharmacist profile | pharmacist-data | Query |

| Stari FE | Novi FE |
|----------|---------|
| `pages/pharmacy/*` (standalone) | `modules/pharmacy/` lazy: dashboard, medications, prescriptions, inventory |
| `PharmacyService` (fat) | `api-services/pharmacy/*` (thin) + feature services za forme |

### 18. Registration Approval Workflow

| Stari | Novi |
|-------|------|
| Admin approve doctor/patient | Part of Admin Users commands |
| Login blocks unapproved | LoginCommandHandler validation |

### 19. Settings / i18n

| Stari | Novi |
|-------|------|
| settings, en/bs | `modules/shared` + `public/i18n/` (template već ima) |

---

## Ukloniti iz template-a (Phase 0)

### Backend
- Entities: Product, ProductCategory, Order, OrderItem, Promotion, UserProductFavorite
- Modules: Catalog/Products, Catalog/ProductCategories, Sales/Orders, CatalogHome
- Controllers: Products, ProductCategories, Orders, Catalog
- DbContext DbSets, configurations, seeder references
- **Zadržati**: Auth, Fakture (opciono ukloniti ako nije potrebno), Identity

### Frontend
- api-services: products, product-categories, orders, catalog
- modules/admin/catalogs/products, product-categories*, orders
- modules/public: search-products, cart-page, public-home product refs
- modules/client: client-orders
- core/services/cart
- Admin routing + nav links

---

## Predloženi redoslijed migracije

1. **Phase 0** — Ukloni product/category/order (BE + FE), build
2. **Phase 1** — Auth (login, register patient/doctor, JWT, roles)
3. **Phase 2** — Admin (users, registration approval)
4. **Phase 3** — Doctor (profile, patients, medical records, files, PDF)
5. **Phase 4** — Patient portal
6. **Phase 5** — Pharmacy core (medications CRUD)
7. **Phase 6** — Pharmacy extended (images, CSV, wizard, inventory)
8. **Phase 7** — Prescriptions + dispense
9. **Phase 8** — Analytics + PDF reports
10. **Phase 9** — FE modules, routing, validators, E2E build ✅

---

## Status migracije (2026-08-04)

| Faza | Status |
|------|--------|
| 0–9 | ✅ Kompletno |

### Pokretanje (dev)

```bash
# Backend
cd backend/Market.API
export ASPNETCORE_ENVIRONMENT=Development   # Git Bash
dotnet run

# Frontend
cd frontend
npm start
```

**Lokalna baza:** `appsettings.Development.json` → `Server=.\SQLEXPRESS; Database=MarketDB`

### Production build

```bash
dotnet build -c Release          # backend
cd frontend && npm run build     # FE production (default config)
```

### Demo korisnici

| Uloga | Email | Lozinka | Ruta |
|-------|-------|---------|------|
| Admin | admin@market.local | Admin123! | /admin |
| Doktor | doctor@ebolnica.local | Doctor123! | /doctor |
| Pacijent | patient@ebolnica.local | Patient123! | /patient |
| Farmaceut | pharmacist@ebolnica.local | Pharmacist123! | /pharmacy |

### Odgođeno (van scope-a migracije)

- Medical record PDF (BE query postoji, FE download nije implementiran)
- Patient files upload
- AI medication summary
- Namespace rename Market.* → eBolnica.*
- MedicationStockHistory (historijski trend zaliha)

## Arhitektonske odluke (2026-08-02)

| Pitanje | Odluka | Obrazloženje |
|---------|--------|--------------|
| Namespace `Market.*` vs `eBolnica.*` | **Zadržati `Market.*`** | Mehanički rename 5+ projekata usred migracije = visok rizik, nula poslovne vrijednosti. RS1 template ostaje referenca. Branding je već eBolnica u UI. Rename kasnije u zasebnom PR-u ako treba. |
| Fakture modul | **Ukloniti** | Demo iz template-a, nije dio bolničkog domena. |
| Dostavljači modul | **Ukloniti** | Isti razlog — template demo bez backend logike. |
| Demo korisnici (manager/employee) | **Ukloniti iz seedera** | Ostaje samo admin + swagger/test dummy — manje konfuzije s eBolnica rolama. |

---

## Tehnički dug (očekivano)

- Rename `Market.*` → `eBolnica.*` (namespace) — **odgođeno**, nije prioritet
- Identity model razlike (UserType vs legacy role flags) — UserType je primarni
- PharmacyController ~2000 linija → ~15 CQRS slice-ova
- FE standalone → NgModule konverzija za sve komponente
- QuestPDF / OpenAI / file upload infrastruktura u Infrastructure layer
