# UTTN Dashboard

Comprehensive Strategic Dashboard for Universidad Tecnológica de Tamaulipas Norte (UTTN). Centralizes institutional data from 7 departments into real-time KPIs, charts, and early intervention alerts.

## Tech Stack

- **Backend:** ASP.NET Core 8 MVC (C#)
- **Data Access:** Dapper (micro-ORM)
- **Database:** SQL Server
- **Charts:** Chart.js + chartjs-plugin-datalabels
- **Frontend:** Razor Views + Plus Jakarta Sans + Custom CSS

## Modules

| Module | Status | Description |
|--------|--------|-------------|
| Rectoría | ✅ Done | Global KPIs, students by career, gender, status breakdown |
| Inscripciones | ✅ Done | Admission funnel, geographic origin, escuelas, promedios |
| Trámites | ✅ Done | Service efficiency, bottlenecks, document validation |
| Servicios Médicos | 🔜 Next | Vital signs, diagnoses, BMI |
| Diagnóstico BD | ✅ Done | Database health check, row counts, sample data |

## Database Setup

This project currently uses **SQL Server LocalDB** for development:
```
Server: (localdb)\UTTN-Dev
Database: TITESTUTTN2026_DEV
```

### To set up locally:

1. Create LocalDB instance:
```powershell
   sqllocaldb create UTTN-Dev
   sqllocaldb start UTTN-Dev
```

2. Run the DB creation script in SSMS:
   - Connect to `(localdb)\UTTN-Dev`
   - Execute `UTTN_CreateDB_DEV.sql`
   - Execute `UTTN_SeedData.sql` + `UTTN_SeedData_FIX.sql`

3. Update `appsettings.json` connection string if needed

### To switch to remote/production DB:

Update `appsettings.json`:
```json
"DefaultConnection": "Server=YOUR_SERVER;Database=TITESTUTTN2026;User Id=UTTN;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

## Seed Data

The local dev database includes realistic test data:

| Table | Records |
|-------|---------|
| Students | 200 |
| Teachers | 25 |
| Careers | 12 |
| Groups | 20 |
| Preinscripciones | 80 |
| Inscripciones | 50 |
| Trámites | 40 |
| Visitas Médicas | 60 |

## Running

1. Open `UTTN.Dashboard.sln` in Visual Studio 2022
2. Ensure LocalDB is running: `sqllocaldb start UTTN-Dev`
3. Press F5

## Authors

- Efren — Dashboard System Lead