# RS1 :: PRIJAVA PREGLEDA SEMINARSKOG RADA
## Akademska godina 2025/26

> **Kako koristiti ovaj fajl:** otvori Word predložak i prepisuj sekciju po sekciju.
> Ovo je samo tvoj dio (**Član 1**, IB210240). Osnovne stavke obuhvataju apoteku (CRUD/wizard lijekova)
> plus login/logout i landing page, koji već postoje u kodu ali nisu bili navedeni u prvoj prijavi.
> Tekst u `[uglastim zagradama]` dopuni sama.
> Na svako mjesto **[OVDJE UMETNI SCREENSHOT]** ubaci sliku iz aplikacije (ne kod).

---

## ŠTA MORAŠ DOPUNITI PRIJE SLANJA

1. **Broj indeksa** (`IB210240`).
2. **Broj članova tima** u Word tabeli — ostale redove popuni sama (indeks, NE). U ovom fajlu je samo tvoj dio.
3. **Vrsta pregleda** — ostavi samo jednu rečenicu.
4. **Screenshotovi** iz aplikacije: landing `/`, login/logout, plus apoteka (ne kod).
5. **Unit testovi** — imaš 2 testa u istom spec fajlu (async validator). To pokriva minimum za tebe.
6. **PasswordStrengthMeter (1b)** — nije u apoteci. Ne upisuj ga.

Test nalog za sve tvoje ekrane: `pharmacist@ebolnica.local` / `Pharmacist123!` → `/pharmacy`

---

# 1. OSNOVNI PODACI

**Naziv projekta:** eBolnica

**Link na DevOps/GitHub repo:** https://github.com/lana-mustafic/eBolnica.git

Pregledavamo samo defaultni branch: **main**

**Napomena:** Repozitorij ne smije sadržavati fajlove/foldere koji se tipično ignorišu: .git, .vs, .code, obj, bin, node_modules, dist, …

**Broj članova tima:** [upisati 2 ili 3]

**Članovi tima:**

| Oznaka člana (bez imena) | Broj indeksa | Da li se njegov dio funkcionalnosti pregleda u ovom dokumentu | Status, historija pregleda |
|--------------------------|--------------|----------------------------------------------------------------|----------------------------|
| Član 1 | IB210240 | DA | Drugi pregled |

**Vrsta pregleda — ostavi SAMO ovu rečenicu, obriši ostale:**

Pregled radi pristupa odbrani (min 10 bodova za osnove + 30 bodova za napredne funkcionalnosti po članu grupe)

---

# 2. LOGIN PODACI ZA TESTIRANJE

| Uloga | Email/Username | Lozinka |
|-------|----------------|---------|
| Farmaceut | pharmacist@ebolnica.local | Pharmacist123! |

Nakon logina otvara se `/pharmacy` (dashboard apoteke). Sve moje forme su pod `/pharmacy/...`.

---

# 3. OSNOVNE FUNKCIONALNOSTI

Član 1 (IB210240) — Osnovne funkcionalnosti

Osnove: CRUD lijekova sa 5 filtera + FE/BE validacija, backend paging/filter, wizard za unos lijeka,
login i logout forma, te vlastiti dizajn landing page-a.

**Ispravka nakon prvog pregleda:** profesor je priznao CRUD+paging/filter kao 5b i wizard kao 3b (ukupno 8b).
Login/logout (2b) i landing page (3b) već postoje u kodu, ali nisu bili navedeni u prvoj prijavi — sada su
navedeni sa FE/BE evidence-om. Ukupno osnovnih: **13b** (min 10b, max 15b).

---

### 3.1.1 CRUD forma lijekova sa 5 FE filtera + FE i BE validacijom

**Bodovi:** 3b (bodovna tabela); uz 3.1.2 profesor je u prvom pregledu priznao 5b za CRUD+paging/filter

Klasična CRUD forma za lijekove (create, read, update, delete). Lista ima 5 filtera:

1. tekstualna pretraga (naziv / generički naziv / proizvođač)
2. kategorija (dropdown)
3. status zalihe (dropdown)
4. da li zahtijeva recept (dropdown)
5. prikaži neaktivne

FE validacija: Angular Reactive Forms + `Validators`. BE validacija: FluentValidation (`CreateMedicationCommandValidator`, `UpdateMedicationCommandValidator`).

Rute: `/pharmacy/medications`, `/pharmacy/medications/new`, `/pharmacy/medications/:id/edit`

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/CreateMedication/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/UpdateMedication/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/DeleteMedication/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Queries/ListMedications/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/MedicationQueryFilters.cs ; backend/eBolnica.API/Controllers/PharmacyController.cs

**FE kod:** frontend/src/app/modules/pharmacy/medications/pharmacy-medications.component.ts ; frontend/src/app/modules/pharmacy/medications/medication-form/ ; frontend/src/app/modules/pharmacy/shared/utils/medication-list-query.util.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — `/pharmacy/medications` sa svih 5 filtera + forma za unos/izmjenu lijeka

Ne treba screenshot source code-a

---

### 3.1.2 Backend paging i backend filter (lijekovi)

**Bodovi:** 2b (dodatak na CRUD prema bodovnoj tabeli)

Dodatak na CRUD: `PageNumber` / `PageSize` na API-ju, EF `Skip`/`Take`, total count. Svih 5 filtera se primjenjuje na backendu (`MedicationQueryFilters`). FE paginator šalje novi request, ne filtrira samo lokalno.

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Queries/ListMedications/ListMedicationsQueryHandler.cs ; backend/eBolnica.Application/Modules/Pharmacy/Medications/MedicationQueryFilters.cs

**FE kod:** frontend/src/app/modules/pharmacy/medications/pharmacy-medications.component.ts ; frontend/src/app/modules/shared/components/fit-paginator-bar/

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — tabela lijekova sa paginatorom i aktivnim BE filterom

---

### 3.1.3 Wizard forma (unos lijeka u koracima)

**Bodovi:** 3b

Wizard za kreiranje lijeka (`mat-stepper`), 3 koraka: Osnovno → Zaliha → Slike. Validacija po koraku. Na zadnjem koraku upload slika nakon kreiranja.

Ruta: `/pharmacy/medications/wizard`

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/CreateMedication/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/UploadMedicationImage/

**FE kod:** frontend/src/app/modules/pharmacy/medications/medication-wizard/medication-wizard.component.ts ; frontend/src/app/modules/pharmacy/medications/medication-wizard/medication-wizard.component.html

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — wizard sa vidljivim stepperom (po mogućnosti 2–3 koraka)

---

### 3.1.4 Login i logout forma

**Bodovi:** 2b

Login forma (`/auth/login`): Reactive Form sa email + lozinka, FE validacija (`Validators.required`, `Validators.email`),
poziv `POST /api/auth/login`. Nakon uspjeha JWT se sprema i korisnik se preusmjerava na default rutu uloge
(farmaceut → `/pharmacy`).

Logout (`/auth/logout`, link i u pharmacy layoutu): FE čisti lokalno stanje i zove `POST /api/auth/logout`,
koji na backendu opoziva (revoke) refresh token. Zatim redirect na login.

BE validacija logina: FluentValidation (`LoginCommandValidator` — email i lozinka obavezni).

Rute: `/auth/login`, `/auth/logout`

**BE kod:** backend/eBolnica.API/Controllers/AuthController.cs ; backend/eBolnica.Application/Modules/Auth/Commands/Login/LoginCommand.cs ; backend/eBolnica.Application/Modules/Auth/Commands/Login/LoginCommandHandler.cs ; backend/eBolnica.Application/Modules/Auth/Commands/Login/LoginCommandValidator.cs ; backend/eBolnica.Application/Modules/Auth/Commands/Logout/LogoutCommand.cs ; backend/eBolnica.Application/Modules/Auth/Commands/Logout/LogoutCommandHandler.cs

**FE kod:** frontend/src/app/modules/auth/login/login.component.ts ; frontend/src/app/modules/auth/login/login.component.html ; frontend/src/app/modules/auth/logout/logout.component.ts ; frontend/src/app/modules/auth/logout/logout.component.html ; frontend/src/app/core/services/auth/auth-facade.service.ts ; frontend/src/app/api-services/auth/auth-api.service.ts ; frontend/src/app/modules/pharmacy/pharmacy-layout/pharmacy-layout.component.html (logout link)

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — `/auth/login` forma (email, lozinka, dugme Prijava)
[OVDJE UMETNI SCREENSHOT] — `/auth/logout` ili klik Odjava iz apoteke pa ekran odjave / povratak na login

Ne treba screenshot source code-a

---

### 3.1.5 Vlastiti dizajn landing page-a

**Bodovi:** 3b

Javna početna stranica (`/`): vlastiti hero layout (brand eBolnica, CTA Prijava/Registracija), floating kartice
(doktori/pacijenti, apoteka, kartoni) i stats sekcija. Dizajn je u SCSS-u komponente (gradient, animacije, responsive grid),
nije default Angular template.

Landing je javni FE ekran (nema zasebnog API poziva). Backend auth se koristi nakon klika na Prijava.

Ruta: `/`

**BE kod:** (javna FE stranica; nakon CTA Prijava koristi se `POST /api/auth/login` iz 3.1.4)

**FE kod:** frontend/src/app/modules/public/public-home/public-home.component.html ; frontend/src/app/modules/public/public-home/public-home.component.scss ; frontend/src/app/modules/public/public-home/public-home.component.ts ; frontend/src/app/modules/public/public-routing-module.ts ; frontend/src/app/app-routing-module.ts (`path: ''` → PublicModule)

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — `/` landing page (hero + CTA Prijava/Registracija + kartice)

Ne treba screenshot source code-a

**UKUPNO Član 1 (osnovne): 13b / min 10b** (max 15b)

Računica po bodovnoj tabeli: CRUD 3b + BE paging/filter 2b + wizard 3b + login/logout 2b + landing 3b = **13b**.
(Prvi pregled je već priznao 5b + 3b = 8b; nedostajala su 2b koja pokriva login/logout. Landing je dodatni buffer.)

---

# 4. NAPREDNE FUNKCIONALNOSTI

Član 1 (IB210240) — Napredne funkcionalnosti

Sve stavke su iz apoteke (lijekovi, slike, recepti, inventar, dashboard).

**Ispravka nakon prvog pregleda:** samoprocjena je usklađena sa važećom bodovnom tabelom. Nisu claimane stavke koje
tabela ne priznaje kao zasebne: LiveFilter (dropdowni se primjenjuju preko „Primijeni filtere“, nije zasebnih 6b),
„Prikaz slika“ i „CRUD Operations with Angular Reactive Forms“ (to je već u osnovnom CRUD-u / galeriji / file storage).
Bodovi za progress bar, async validator, zoom i compression svedeni su na standardne vrijednosti iz tabele.
Nove napredne funkcionalnosti nisu dodavane — prag od 30b je i dalje zadovoljen.

---

### 4.1.1 Drag-and-Drop File Upload with Preview

**Standardni bodovi:** 6b (drag-and-drop 3b + file upload with preview 3b)
**Moja procjena:** 6b

Na formi/wizardu lijeka: povlačenje slike u drop zonu, preview prije slanja, CDK drag-and-drop za redoslijed slika u galeriji.

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/UploadMedicationImage/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/ReorderMedicationImages/

**FE kod:** frontend/src/app/modules/pharmacy/medications/medication-form/medication-form.component.ts ; frontend/src/app/modules/pharmacy/medications/medication-wizard/medication-wizard.component.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — drop zona sa preview thumbnailima

---

### 4.1.2 File Upload with Progress Bar in Angular and .NET Core

**Standardni bodovi:** 8b
**Moja procjena:** 8b

Upload slike lijeka sa `reportProgress: true`, `observe: 'events'` i `mat-progress-bar` na FE.

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/UploadMedicationImage/UploadMedicationImageCommand.cs

**FE kod:** frontend/src/app/api-services/pharmacy/pharmacy-api.service.ts ; frontend/src/app/modules/pharmacy/medications/medication-image-upload-progress.util.ts ; frontend/src/app/modules/pharmacy/medications/medication-form/

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — upload u toku sa progress barom

---

### 4.1.3 Custom Angular Form Validation with Async Validators

**Standardni bodovi:** 3b
**Moja procjena:** 3b

Na formi i wizardu lijeka: async validator provjerava da li je naziv slobodan (`GET medications/check-name`), sa debounce i `excludeId` na editu.

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Queries/CheckMedicationName/

**FE kod:** frontend/src/app/modules/shared/validators/medication-name-async.validator.ts ; frontend/src/app/modules/pharmacy/medications/medication-form/medication-form.component.ts ; frontend/src/app/modules/pharmacy/medications/medication-wizard/medication-wizard.component.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — unos postojećeg naziva lijeka i greška da naziv već postoji

---

### 4.1.4 Galerija slika

**Standardni bodovi:** 5b
**Moja procjena:** 5b

Galerija slika lijeka: više slika, primary, reorder, delete. Na formi i detalju lijeka.

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/SetPrimaryMedicationImage/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/ReorderMedicationImages/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/DeleteMedicationImage/

**FE kod:** frontend/src/app/modules/pharmacy/medications/medication-form/medication-form.component.html ; frontend/src/app/modules/pharmacy/medications/medication-detail/

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — galerija na formi ili detalju lijeka

---

### 4.1.5 Image Zoom

**Standardni bodovi:** 2b
**Moja procjena:** 2b

Lightbox slike lijeka: zoom in/out, zoom kotačićem, pan kad je uvećano.

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Queries/GetMedicationImageFile/

**FE kod:** frontend/src/app/modules/pharmacy/medications/medication-image-lightbox/medication-image-lightbox.component.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — otvoren lightbox sa zumiranom slikom lijeka

---

### 4.1.6 Autosave funkcionalnost za formu (wizard lijeka)

**Standardni bodovi:** 2b
**Moja procjena:** 2b

Autosave drafta wizarda u `localStorage`, debounce 2s, TTL 7 dana, banner za restore. Jedna forma (max 4b po članu).

**BE kod:** (FE draft)

**FE kod:** frontend/src/app/modules/pharmacy/services/medication-wizard-draft.service.ts ; frontend/src/app/modules/pharmacy/medications/medication-wizard-autosave.util.ts ; frontend/src/app/modules/pharmacy/medications/medication-wizard/medication-wizard.component.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — wizard sa porukom da je draft sačuvan / restore banner

---

### 4.1.7 Spremanje/preuzimanje fajlova na file sistem (backend)

**Standardni bodovi:** 4b (3b file sistem + 1b rad sa slikama)
**Moja procjena:** 4b

Upload/download slika lijekova na lokalni disk: `uploads/medications/{id}/`. Nije cloud blob — ne tvrdim Blob storage dodatak.

**BE kod:** backend/eBolnica.Infrastructure/Pharmacy/MedicationImageStorageService.cs ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Queries/GetMedicationImageFile/

**FE kod:** frontend/src/app/modules/pharmacy/services/medication-image-url.service.ts ; frontend/src/app/api-services/pharmacy/pharmacy-api.service.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — upload slike + prikaz na lijeku

---

### 4.1.8 PDF report — recepti (datum početni / datum krajnji)

**Standardni bodovi:** 3b
**Moja procjena:** 3b

QuestPDF izvještaj recepata. Parametri: `PrescribedFrom`, `PrescribedTo`, plus status/search.

Ruta: `/pharmacy/prescriptions` → Export PDF

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Analytics/Queries/ExportPrescriptionsPdf/ExportPrescriptionsPdfQuery.cs ; backend/eBolnica.Infrastructure/Pharmacy/PharmacyPdfReportService.cs

**FE kod:** frontend/src/app/modules/pharmacy/prescriptions/pharmacy-prescriptions.component.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — filter datuma + otvoren PDF

---

### 4.1.9 PDF report — inventar

**Standardni bodovi:** 3b
**Moja procjena:** 3b

Drugi PDF (max 6b po članu). Inventar lijekova sa filterima (kategorija, zaliha, recept, search).

Ruta: `/pharmacy/inventory` (i lista lijekova) → Export PDF

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Analytics/Queries/ExportInventoryPdf/ ; backend/eBolnica.Infrastructure/Pharmacy/PharmacyPdfReportService.cs

**FE kod:** frontend/src/app/modules/pharmacy/inventory/pharmacy-inventory.component.ts ; frontend/src/app/modules/pharmacy/medications/pharmacy-medications.component.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — inventar + preuzeti PDF

---

### 4.1.10 JS-grafikoni za prikaz podataka

**Standardni bodovi:** 4b (max 4b)
**Moja procjena:** 4b

Chart.js na pharmacy dashboardu: prihod, kategorije (doughnut), trend zalihe.

Ruta: `/pharmacy/dashboard`

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Analytics/Queries/GetDashboardStats/ ; backend/eBolnica.Infrastructure/Pharmacy/PharmacyAnalyticsService.cs

**FE kod:** frontend/src/app/modules/pharmacy/dashboard/pharmacy-dashboard.component.ts ; frontend/src/app/modules/pharmacy/dashboard/charts/

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — `/pharmacy/dashboard` sa grafikonima

---

### 4.1.11 Data Import/Export (CSV)

**Standardni bodovi:** 6b
**Moja procjena:** 6b

CSV export lijekova, import, download template. Format CSV.

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Queries/ExportMedicationsCsv/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Commands/ImportMedicationsCsv/ ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Csv/MedicationCsvService.cs

**FE kod:** frontend/src/app/modules/pharmacy/medications/pharmacy-medications.component.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — Import/Export CSV na listi lijekova + rezultat importa

---

### 4.1.12 Sortiranje po koloni u tabelarnom prikazu

**Standardni bodovi:** 3b
**Moja procjena:** 3b

Klik na header tabele lijekova/receptata/inventara. `sortBy` / `sortOrder` ide na backend (`PharmacySortValidator`).

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/PharmacySortValidator.cs ; backend/eBolnica.Application/Modules/Pharmacy/Medications/Queries/ListMedications/

**FE kod:** frontend/src/app/modules/pharmacy/shared/utils/pharmacy-table.util.ts ; frontend/src/app/modules/pharmacy/medications/pharmacy-medications.component.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — tabela lijekova sortirana po koloni

---

### 4.1.13 Autocomplete u poljima za unos

**Standardni bodovi:** 2b
**Moja procjena:** 2b

Autocomplete naziva lijeka na listi i na formi recepta u apoteci.

**BE kod:** backend/eBolnica.Application/Modules/Pharmacy/Medications/Queries/GetMedicationAutocomplete/ ; backend/eBolnica.Application/Modules/Pharmacy/Prescriptions/Queries/SearchPrescriptionPatients/

**FE kod:** frontend/src/app/modules/pharmacy/medications/pharmacy-medications.component.ts ; frontend/src/app/modules/pharmacy/prescriptions/prescription-form/prescription-form.component.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — dropdown prijedloga dok kucaš naziv lijeka

---

### 4.1.14 Image/Video Compression Before Upload

**Standardni bodovi:** 3b
**Moja procjena:** 3b

Prije uploada slike lijeka: canvas resize (max 1280px), JPEG quality 0.85.

**BE kod:** (kompresija na FE)

**FE kod:** frontend/src/app/modules/pharmacy/medications/utils/medication-image-compress.util.ts

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — forma/wizard nakon dropa slike (preview); ako UI pokazuje original vs compressed size, to slikaj

---

### 4.1.15 Rate-Limiting API Requests

**Standardni bodovi:** 5b
**Moja procjena:** 5b

Policy `PharmacyUpload` (20 req/min) na CSV import i upload slika lijekova.

**BE kod:** PharmacyController — `EnableRateLimiting("PharmacyUpload")` na import/upload endpointima

**FE kod:** frontend/src/app/modules/pharmacy/medications/ (import/upload pozivi)

**Screenshot UI:**
[OVDJE UMETNI SCREENSHOT] — 429 nakon previše uploada, ili Network odgovor sa rate limitom

**UKUPNO Član 1 (napredne): 59b / min 30b**

(6+8+3+5+2+2+4+3+3+4+6+3+2+3+5 = 59)

Ako ti je lista preduga za Word, ostavi prvih 7 stavki (4.1.1–4.1.7 = **30b**) — to prolazi minimum. Ostalo je bonus ako profesor prima više.

Nije claimano (nije zasebna stavka u tabeli / već pokriveno drugdje):
- LiveFilter 6b — dropdown filteri nisu real-time; primjenjuju se preko „Primijeni filtere“. Tekstualna pretraga ima debounce, ali to nije zaseban claim.
- Prikaz slika 5b — pokriveno galerijom (4.1.4) i file storage + rad sa slikama (4.1.7).
- CRUD Operations with Angular Reactive Forms 7b — to je osnovni CRUD (sekcija 3), nije napredna stavka iz tabele.

---

# 5. APP-LEVEL FUNKCIONALNOSTI (dijele se)

Za ovaj pregled ne claimam ostale app-level stavke izvan navedenih osnova (JWT kao zasebna napredna stavka, i18n, lazy loading iz template-a). Login/logout i landing page su osnovne stavke iz bodovne tabele i sada su navedene u sekciji 3. Rate-limit i reactive forms u naprednim su vezani za pharmacy endpointе i formu lijekova.

---

# 6. POSLOVNI FLOW

**Naziv flow-a:** Upravljanje lijekovima u apoteci (od liste do izdavanja i izvještaja)

**Korak 1:** Javni landing `/` → Prijava `/auth/login` kao farmaceut → `/pharmacy/dashboard` (KPI + grafikoni)
[OVDJE UMETNI SCREENSHOT KORAKA 1]

**Korak 2:** `/pharmacy/medications` — 5 filtera, sort, paging, autocomplete
[OVDJE UMETNI SCREENSHOT KORAKA 2]

**Korak 3:** Unos lijeka wizardom (3 koraka) ili CRUD formom — async validacija naziva, drag-drop slike, preview, progress bar, autosave
[OVDJE UMETNI SCREENSHOT KORAKA 3]

**Korak 4:** Galerija + zoom slike; zatim `/pharmacy/prescriptions` izdavanje recepta i Export PDF (datumi); `/pharmacy/inventory` PDF inventara; CSV import/export
[OVDJE UMETNI SCREENSHOT KORAKA 4]

---

# 7. UNIT TESTOVI

Član 1 (IB210240):

**Test 1:** medicationNameAsyncValidator — vraća null kad je naziv lijeka slobodan
**Putanja:** frontend/src/app/modules/shared/validators/medication-name-async.validator.spec.ts

**Test 2:** PharmacySortValidator — dozvoljena kolona sortiranja ne baca grešku (`AllowedMedicationSortColumn_DoesNotThrow`); nepoznata kolona baca ValidationException
**Putanja:** backend/eBolnica.Tests/Pharmacy/PharmacySortValidatorTests.cs

---

# 8. SAŽETAK BODOVA

| Kategorija | Član 1 |
|------------|--------|
| Osnovne funkcionalnosti (min 10b, max 15b) | 13b |
| Napredne funkcionalnosti (min 30b) | 59b (ili 30b ako ostaviš samo 4.1.1–4.1.7) |
| **UKUPNO** | **72b** (ili **43b**) |

---

# 9. CHECKLIST PRIJE SLANJA

- [x] GIT sadrži frontend/ i backend/ foldere
- [x] Postoji db-backups/ folder sa backup-om baze (`db-backups/eBolnicaDB.zip`)
- [ ] Postoji dokumenti/ folder sa dijagramima — provjeri da su slike na default branchu
- [x] Login podaci su navedeni u ovom dokumentu (farmaceut)
- [x] Član 1 ima minimum 10b osnovnih funkcionalnosti (13b: CRUD+paging 5b + wizard 3b + login/logout 2b + landing 3b)
- [x] Član 1 ima minimum 30b naprednih funkcionalnosti (59b po važećoj tabeli; min 30b)
- [x] Član 1 ima minimum 2 unit testa
- [x] Dashboard ekran postoji i funkcionira — `/pharmacy/dashboard`
- [ ] Settings ekran — u apoteci nema posebnog settings; za checklist ostavi napomenu ili slikaj dashboard
- [x] CRUD handleri lijekova imaju FluentValidation klase
- [x] FE forme lijekova koriste Reactive Forms sa Validators
- [x] Kod i komentari su na engleskom jeziku
- [ ] Svi SCREENSHOTS su dodani. Ne treba screenshot source code-a

---

# 10. NAPOMENE (opcionalno)

Moj dio projekta je modul apoteke: lijekovi (CRUD, wizard, slike, CSV), inventar, recepti (izdavanje), analitika i PDF izvještaji.

U osnovnim bodovima su navedeni i login/logout te landing page — već postoje u kodu; u prvoj prijavi nisu bili navedeni pa nisu priznati.

U naprednim bodovima samoprocjena je usklađena sa tabelom (59b). LiveFilter, „Prikaz slika“ i Reactive Forms CRUD nisu zasebni claimovi.

Demo nalog: `pharmacist@ebolnica.local` / `Pharmacist123!`

Seed manje bitnih podataka (početni lijekovi/korisnici) je u `backend/eBolnica.Infrastructure/Database/Seeders/DynamicDataSeeder.cs`.

---

# BRZI SCENARIJ ZA SCREENSHOTE

1. Landing `/` — vlastiti dizajn (hero, CTA, kartice)
2. Login: `pharmacist@ebolnica.local` / `Pharmacist123!` na `/auth/login`
3. `/pharmacy/dashboard` — grafikoni
4. `/pharmacy/medications` — 5 filtera, sort, paging, prazno stanje, autocomplete, CSV
5. `/pharmacy/medications/new` ili edit — reactive forma, async validator, drop slike, preview, progress bar, galerija
6. Lightbox zoom na slici
7. `/pharmacy/medications/wizard` — 3 koraka + autosave
8. `/pharmacy/prescriptions` — izdavanje + Export PDF sa datumima
9. `/pharmacy/inventory` — Export PDF
10. Odjava iz apoteke (`/auth/logout`) — ekran odjave i povratak na login
