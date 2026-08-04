# eBolnica

Hospital information system migrated to the RS1 Clean Architecture template (CQRS, MediatR, lazy-loaded Angular modules).

## Tech stack

| Layer | Stack |
|-------|--------|
| Backend | .NET, ASP.NET Core, EF Core, MediatR, FluentValidation, JWT |
| Frontend | Angular 21, Angular Material, ngx-translate |
| Database | SQL Server (local dev: `MarketDB` on `.\SQLEXPRESS`) |

## Project structure

```
eBolnica/
├── backend/                 # Clean Architecture (.NET solution)
│   ├── Market.API/          # HTTP API, Swagger
│   ├── Market.Application/  # CQRS commands & queries
│   ├── Market.Domain/       # Entities
│   ├── Market.Infrastructure/
│   └── rs1_backend-2025-26.sln
├── frontend/                # Angular SPA
├── MIGRATION_MAP.md         # Migration notes (legacy → new architecture)
├── db-backups/              # Database backups
└── dokumenti/               # Domain / use-case diagrams
```

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version required by `backend/Market.API/Market.API.csproj`)
- [Node.js](https://nodejs.org/) + npm (see `frontend/package.json` → `packageManager`)
- SQL Server Express (or compatible instance)

## Getting started

### 1. Database

Connection string (Development): `backend/Market.API/appsettings.Development.json`

```
Server=.\SQLEXPRESS;Database=MarketDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Apply migrations and seed demo data:

```bash
cd backend/Market.API
export ASPNETCORE_ENVIRONMENT=Development   # Git Bash
dotnet ef database update --project ../Market.Infrastructure
```

On Windows PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project ../Market.Infrastructure
```

### 2. Backend API

```bash
cd backend/Market.API
export ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

- API: `http://localhost:7001`
- Swagger: `http://localhost:7001/swagger`

### 3. Frontend

```bash
cd frontend
npm install
npm start
```

- App: `http://localhost:4200`
- API URL: configured in `frontend/src/environments/environment.ts` → `http://localhost:7001`

## Demo users

Seeded on first run (Development). Passwords are case-sensitive.

| Role | Email | Password | Route after login |
|------|-------|----------|-------------------|
| Admin | admin@market.local | Admin123! | `/admin` |
| Doctor | doctor@ebolnica.local | Doctor123! | `/doctor` |
| Patient | patient@ebolnica.local | Patient123! | `/patient` |
| Pharmacist | pharmacist@ebolnica.local | Pharmacist123! | `/pharmacy` |

## Production build

```bash
# Backend
dotnet build backend/rs1_backend-2025-26.sln -c Release

# Frontend
cd frontend
npm run build
```

Output: `frontend/dist/`

## Modules (overview)

- **Auth** — login, patient/doctor registration, JWT + refresh token
- **Admin** — user management
- **Doctor** — profile, patients, medical records
- **Patient** — patient portal
- **Pharmacy** — medications, inventory, prescriptions, analytics, PDF reports

For migration details, deferred items, and architecture decisions, see [MIGRATION_MAP.md](./MIGRATION_MAP.md).
