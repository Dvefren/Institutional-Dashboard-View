# UTTN Dashboard

Comprehensive Strategic Dashboard for Universidad Tecnológica de Tamaulipas Norte (UTTN). Centralizes institutional data from 7 departments into real-time KPIs, charts, and early intervention alerts.

## Tech Stack

- **Backend:** ASP.NET Core 8 MVC (C#)
- **Data Access:** Dapper (micro-ORM)
- **Database:** SQL Server (LocalDB for dev, remote for production)
- **Charts:** Chart.js 4.4 + chartjs-plugin-datalabels
- **Frontend:** Razor Views + Plus Jakarta Sans + Custom CSS

## Modules

| Module | View | Tables | Status |
|--------|------|--------|--------|
| Inicio | `Home/Index` | `management_*` (13 tables) | ✅ Working |
| Inscripciones | `Admissions/Index` | `academiccontrol_*` (9 tables) with old-table fallback | ✅ Working |
| Trámites | `Tramites/Index` | `CE_Tramites*` (4 tables) | ✅ Working |
| Aspirantes | `Aspirantes/Index` | `Aspirantes` + 5 child tables | ⚠️ LocalDB only |
| Servicios Médicos | `Medical/Index` | `Visitas`, `VisitasPsicologicas` | ✅ Working |
| Vinculación | `Vinculacion/Index` | `operational_*` (4 tables) | ⚠️ Empty on remote |
| Calidad Académica | `AcademicQuality/Index` | `grades_*` (7 tables) | ⚠️ Empty on remote |

> Views marked ⚠️ have `TableExists()` guards — they show an empty state instead of crashing when tables don't exist on the remote DB.

## Features

- **Year + Cuatrimestre filters** on every view (Ene–Abr, May–Ago, Sep–Dic)
- **2 charts per view** (bar + donut) with data labels always visible
- **Data tables** with multi-column client-side filtering, record counts, and CSV export
- **KPI cards** with icons and progress bars
- **Table existence guards** for remote DB compatibility
- **Auth system** ready (cookie-based, currently bypassed for development)

## Architecture

```
Controllers/         7 dashboard + Auth + Management (future CRUD)
Services/            DashboardService (7 methods), AuthService, ManagementService
ViewModels/Dashboard/ RectorateVM, AdmissionsVM, TramitesVM, AspirantesVM,
                      MedicalVM, VinculacionVM, AcademicQualityVM
Views/               7 dashboard views + Auth views + shared Layout
Data/                DapperContext (connection factory)
Models/              Management models (Person, User, Role, Career, etc.)
```

## Database

### Remote (Production)

```
Server: 10.7.65.198
Database: TITESTUTTN2026
Tables: 39 (management, academiccontrol, grades, CE_Tramites, Visitas, operational)
```

### Local (Development)

```
Server: (localdb)\UTTN-Dev
Database: TITESTUTTN2026_DEV
Tables: 39 + old legacy tables (Preinscripciones, Aspirantes) with seed data
```

### Setup

1. Create LocalDB instance:
```powershell
sqllocaldb create UTTN-Dev
sqllocaldb start UTTN-Dev
```

2. Run SQL scripts in SSMS against `(localdb)\UTTN-Dev`:
   - `UTTN_CreateDB_DEV.sql`
   - `UTTN_SeedData.sql`
   - `UTTN_FinalDBUpdate.sql` (academiccontrol + grades tables)

3. Connection string in `appsettings.json`:
```json
"DefaultConnection": "Server=(localdb)\\UTTN-Dev;Database=TITESTUTTN2026_DEV;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

### Switching to Remote

Update `appsettings.json` — **do not commit credentials to git**.

## Seed Data (LocalDB)

| Module | Table | Records |
|--------|-------|---------|
| Management | Students | 200 |
| Management | Teachers | 25 |
| Management | Careers | 12 |
| Management | Groups | 20 |
| Academic Control | Preinscripciones | 80 |
| Academic Control | Inscripciones | 50 |
| Trámites | Solicitudes | 40 |
| Trámites | Categorías | 7 |
| Trámites | Requisitos | 47 |
| Medical | Visitas Médicas | 60 |
| Medical | Visitas Psicológicas | 30 |
| Aspirantes | Aspirantes | 100 |
| Vinculación | Organizations | 12 |
| Vinculación | Programs | 8 |
| Vinculación | Assignments | 40 |
| Grades | Subjects | 24 |
| Grades | Final Grades | 100 |

## Running

1. Open `UTTN.Dashboard.sln` in Visual Studio 2022
2. Ensure LocalDB is running: `sqllocaldb start UTTN-Dev`
3. Press F5
4. Default route is `Home/Index` (auth bypassed in dev)

## Authors

- Efren — Dashboard System Lead