using Dapper;
using System.Data;
using UTTN.Dashboard.Data;
using UTTN.Dashboard.Services.Interfaces;
using UTTN.Dashboard.ViewModels.Dashboard;

namespace UTTN.Dashboard.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly DapperContext _context;

        public DashboardService(DapperContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════
        // HELPER: Check if table exists
        // ═══════════════════════════════════════
        private async Task<bool> TableExists(IDbConnection connection, string tableName)
        {
            return await connection.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.tables WHERE name = @Name) THEN 1 ELSE 0 END",
                new { Name = tableName }) == 1;
        }

        // ═══════════════════════════════════════════════════════════════
        // 1. INICIO (RECTORATE)
        // ═══════════════════════════════════════════════════════════════
        public async Task<RectorateViewModel> GetRectorateDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new RectorateViewModel();

            // Available years
            var years = await connection.QueryAsync<int>(@"
                SELECT DISTINCT YEAR(management_person_createdDate) FROM management_person_table
                ORDER BY 1 DESC");
            vm.AvailableYears = years.ToList();
            if (!vm.AvailableYears.Any()) vm.AvailableYears.Add(DateTime.Now.Year);
            vm.SelectedYear = year ?? DateTime.Now.Year;
            vm.SelectedCuatrimestre = cuatrimestre ?? 0;

            int startMonth = 1, endMonth = 12;
            if (vm.SelectedCuatrimestre > 0)
            {
                startMonth = vm.SelectedCuatrimestre switch { 1 => 1, 2 => 5, 3 => 9, _ => 1 };
                endMonth = vm.SelectedCuatrimestre switch { 1 => 4, 2 => 8, 3 => 12, _ => 12 };
            }
            var fp = new { Year = vm.SelectedYear, StartMonth = startMonth, EndMonth = endMonth };

            // KPIs
            vm.TotalStudents = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_student_table WHERE management_student_status = 1");
            vm.TotalTeachers = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_teacher_table WHERE management_teacher_status = 1");
            vm.TotalCareers = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_career_table WHERE management_career_status = 1");
            vm.TotalGroups = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_group_table WHERE management_group_status = 1");

            // Students by career
            var byCareer = await connection.QueryAsync<dynamic>(@"
                SELECT c.management_career_Name AS CareerName, COUNT(*) AS Count
                FROM management_student_table s
                INNER JOIN management_career_table c ON s.management_student_CareerID = c.management_career_ID
                WHERE s.management_student_status = 1
                GROUP BY c.management_career_Name ORDER BY Count DESC");
            var totalCr = byCareer.Sum(x => (int)x.Count);
            vm.StudentsByCareer = byCareer.Select(x => new CareerStatItem
            {
                CareerName = (string)x.CareerName,
                Count = (int)x.Count,
                Percentage = totalCr > 0 ? Math.Round((decimal)(int)x.Count / totalCr * 100, 1) : 0
            }).ToList();

            // Students by status
            var byStatus = await connection.QueryAsync<dynamic>(@"
                SELECT ISNULL(management_student_StatusCode,'Activo') AS Status, COUNT(*) AS Count
                FROM management_student_table WHERE management_student_status = 1
                GROUP BY management_student_StatusCode ORDER BY Count DESC");
            var totalSt = byStatus.Sum(x => (int)x.Count);
            vm.StudentsByStatus = byStatus.Select(x => new StatusStatItem
            {
                Status = (string)x.Status,
                Count = (int)x.Count,
                Percentage = totalSt > 0 ? Math.Round((decimal)(int)x.Count / totalSt * 100, 1) : 0
            }).ToList();

            // Gender
            var gender = await connection.QueryAsync<dynamic>(@"
                SELECT p.management_person_Gender AS Gender, COUNT(*) AS Count
                FROM management_student_table s
                INNER JOIN management_person_table p ON s.management_student_PersonID = p.management_person_ID
                WHERE s.management_student_status = 1
                GROUP BY p.management_person_Gender");
            foreach (var g in gender)
            {
                string gen = ((string)(g.Gender ?? "")).ToLower();
                int count = (int)g.Count;
                if (gen.Contains("masculino") || gen == "m") vm.MaleCount += count;
                else if (gen.Contains("femenino") || gen == "f") vm.FemaleCount += count;
            }

            // Groups by career (with cuatrimestre filter)
            vm.GroupsByCareer = (await connection.QueryAsync<GroupStatItem>(@"
                SELECT c.management_career_Name AS CareerName, g.management_group_Code AS GroupCode,
                    g.management_group_Name AS GroupName, ISNULL(g.management_group_Shift,'—') AS Shift,
                    COUNT(s.management_student_ID) AS StudentCount
                FROM management_group_table g
                INNER JOIN management_career_table c ON g.management_group_CareerID = c.management_career_ID
                LEFT JOIN management_student_table s ON g.management_group_ID = s.management_student_GroupID AND s.management_student_status = 1
                WHERE g.management_group_status = 1
                GROUP BY c.management_career_Name, g.management_group_Code, g.management_group_Name, g.management_group_Shift
                ORDER BY c.management_career_Name, g.management_group_Code")).ToList();

            // Preinscripciones count (from academiccontrol or old tables)
            try
            {
                if (await TableExists(connection, "academiccontrol_preinscription_table"))
                {
                    vm.TotalPreinscripciones = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM academiccontrol_preinscription_table WHERE academiccontrol_preinscription_status = 1");
                    vm.TotalInscripciones = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM academiccontrol_inscription_table WHERE academiccontrol_inscription_status = 1");

                    var byPreStatus = await connection.QueryAsync<dynamic>(@"
                        SELECT academiccontrol_preinscription_state AS Status, COUNT(*) AS Count
                        FROM academiccontrol_preinscription_table WHERE academiccontrol_preinscription_status = 1
                        GROUP BY academiccontrol_preinscription_state ORDER BY Count DESC");
                    var totalPS = byPreStatus.Sum(x => (int)x.Count);
                    vm.PreinscripcionesByStatus = byPreStatus.Select(x => new StatusStatItem
                    {
                        Status = (string)x.Status,
                        Count = (int)x.Count,
                        Percentage = totalPS > 0 ? Math.Round((decimal)(int)x.Count / totalPS * 100, 1) : 0
                    }).ToList();

                    var byPreCareer = await connection.QueryAsync<dynamic>(@"
                        SELECT academiccontrol_preinscription_careerRequested AS CareerName, COUNT(*) AS Count
                        FROM academiccontrol_preinscription_table WHERE academiccontrol_preinscription_status = 1
                        GROUP BY academiccontrol_preinscription_careerRequested ORDER BY Count DESC");
                    var totalPC = byPreCareer.Sum(x => (int)x.Count);
                    vm.PreinscripcionesByCareer = byPreCareer.Select(x => new CareerStatItem
                    {
                        CareerName = (string)x.CareerName,
                        Count = (int)x.Count,
                        Percentage = totalPC > 0 ? Math.Round((decimal)(int)x.Count / totalPC * 100, 1) : 0
                    }).ToList();
                }
                else if (await TableExists(connection, "Preinscripciones"))
                {
                    vm.TotalPreinscripciones = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Preinscripciones");
                    vm.TotalInscripciones = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Inscripciones");
                }
            }
            catch { /* tables don't exist */ }

            // Career change history
            try
            {
                vm.CareerChanges = (await connection.QueryAsync<CareerChangeItem>(@"
                    SELECT ISNULL(p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal,'—') AS StudentName,
                        ISNULL(s.management_student_Matricula,'—') AS Matricula,
                        c.management_career_Name AS CareerName,
                        h.management_studentcareer_history_StartDate AS StartDate,
                        h.management_studentcareer_history_EndDate AS EndDate,
                        ISNULL(h.management_studentcareer_history_Reason,'—') AS Reason
                    FROM management_studentcareer_history_table h
                    INNER JOIN management_student_table s ON h.management_studentcareer_history_StudentID = s.management_student_ID
                    INNER JOIN management_person_table p ON s.management_student_PersonID = p.management_person_ID
                    INNER JOIN management_career_table c ON h.management_studentcareer_history_CareerID = c.management_career_ID
                    WHERE h.management_studentcareer_history_status = 1
                    ORDER BY h.management_studentcareer_history_StartDate DESC")).ToList();
            }
            catch { vm.CareerChanges = new(); }

            // Group change history
            try
            {
                vm.GroupChanges = (await connection.QueryAsync<GroupChangeItem>(@"
                    SELECT ISNULL(p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal,'—') AS StudentName,
                        ISNULL(s.management_student_Matricula,'—') AS Matricula,
                        g.management_group_Code AS GroupCode,
                        ISNULL(c.management_career_Name,'—') AS CareerName,
                        h.management_studentgroup_history_StartDate AS StartDate,
                        h.management_studentgroup_history_EndDate AS EndDate,
                        ISNULL(h.management_studentgroup_history_Reason,'—') AS Reason
                    FROM management_studentgroup_history_table h
                    INNER JOIN management_student_table s ON h.management_studentgroup_history_StudentID = s.management_student_ID
                    INNER JOIN management_person_table p ON s.management_student_PersonID = p.management_person_ID
                    INNER JOIN management_group_table g ON h.management_studentgroup_history_GroupID = g.management_group_ID
                    LEFT JOIN management_career_table c ON g.management_group_CareerID = c.management_career_ID
                    WHERE h.management_studentgroup_history_status = 1
                    ORDER BY h.management_studentgroup_history_StartDate DESC")).ToList();
            }
            catch { vm.GroupChanges = new(); }

            return vm;
        }

        // ═══════════════════════════════════════════════════════════════
        // 2. INSCRIPCIONES (ADMISSIONS)
        // ═══════════════════════════════════════════════════════════════
        public async Task<AdmissionsViewModel> GetAdmissionsDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new AdmissionsViewModel();

            var hasNewTables = await TableExists(connection, "academiccontrol_preinscription_table");
            var hasOldTables = await TableExists(connection, "Preinscripciones");

            if (!hasNewTables && !hasOldTables)
            {
                vm.AvailableYears = new List<int> { DateTime.Now.Year };
                vm.SelectedYear = year ?? DateTime.Now.Year;
                vm.SelectedCuatrimestre = cuatrimestre ?? 0;
                return vm;
            }

            if (hasNewTables)
            {
                // ═══════════════════════════════════════
                // NEW ACADEMICCONTROL TABLES
                // ═══════════════════════════════════════
                var years = await connection.QueryAsync<int>(@"
                    SELECT DISTINCT YEAR(academiccontrol_preinscription_registrationDate) FROM academiccontrol_preinscription_table
                    UNION SELECT DISTINCT YEAR(academiccontrol_inscription_registrationDate) FROM academiccontrol_inscription_table
                    ORDER BY 1 DESC");
                vm.AvailableYears = years.ToList();
                if (!vm.AvailableYears.Any()) vm.AvailableYears.Add(DateTime.Now.Year);
                vm.SelectedYear = year ?? DateTime.Now.Year;
                vm.SelectedCuatrimestre = cuatrimestre ?? 0;

                int startMonth = 1, endMonth = 12;
                if (vm.SelectedCuatrimestre > 0)
                {
                    startMonth = vm.SelectedCuatrimestre switch { 1 => 1, 2 => 5, 3 => 9, _ => 1 };
                    endMonth = vm.SelectedCuatrimestre switch { 1 => 4, 2 => 8, 3 => 12, _ => 12 };
                }
                var fp = new { Year = vm.SelectedYear, StartMonth = startMonth, EndMonth = endMonth };
                string dfP = "YEAR(p.academiccontrol_preinscription_registrationDate) = @Year AND MONTH(p.academiccontrol_preinscription_registrationDate) BETWEEN @StartMonth AND @EndMonth";
                string dfI = "YEAR(i.academiccontrol_inscription_registrationDate) = @Year AND MONTH(i.academiccontrol_inscription_registrationDate) BETWEEN @StartMonth AND @EndMonth";

                // KPIs
                vm.TotalPreinscripciones = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM academiccontrol_preinscription_table p WHERE {dfP}", fp);
                vm.TotalInscripciones = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM academiccontrol_inscription_table i WHERE {dfI}", fp);
                vm.ConversionRate = vm.TotalPreinscripciones > 0 ? Math.Round((decimal)vm.TotalInscripciones / vm.TotalPreinscripciones * 100, 1) : 0;
                vm.PromedioGeneral = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(p.academiccontrol_preinscription_average) FROM academiccontrol_preinscription_table p WHERE {dfP}", fp) ?? 0;
                vm.PromedioGeneral = Math.Round(vm.PromedioGeneral, 2);

                // By Career
                var byCareers = await connection.QueryAsync<dynamic>($"SELECT p.academiccontrol_preinscription_careerRequested AS CareerName, COUNT(*) AS Count FROM academiccontrol_preinscription_table p WHERE {dfP} GROUP BY p.academiccontrol_preinscription_careerRequested ORDER BY Count DESC", fp);
                var totalC = byCareers.Sum(x => (int)x.Count);
                vm.PreinscripcionesByCareer = byCareers.Select(x => new CareerStatItem { CareerName = (string)x.CareerName, Count = (int)x.Count, Percentage = totalC > 0 ? Math.Round((decimal)(int)x.Count / totalC * 100, 1) : 0 }).ToList();

                // By Status (Preinscripciones)
                var byStatus = await connection.QueryAsync<dynamic>($"SELECT p.academiccontrol_preinscription_state AS Status, COUNT(*) AS Count FROM academiccontrol_preinscription_table p WHERE {dfP} GROUP BY p.academiccontrol_preinscription_state ORDER BY Count DESC", fp);
                var totalS = byStatus.Sum(x => (int)x.Count);
                vm.PreinscripcionesByStatus = byStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalS > 0 ? Math.Round((decimal)(int)x.Count / totalS * 100, 1) : 0 }).ToList();

                // By Status (Inscripciones)
                var byInsStatus = await connection.QueryAsync<dynamic>($"SELECT i.academiccontrol_inscription_state AS Status, COUNT(*) AS Count FROM academiccontrol_inscription_table i WHERE {dfI} GROUP BY i.academiccontrol_inscription_state ORDER BY Count DESC", fp);
                var totalIS = byInsStatus.Sum(x => (int)x.Count);
                vm.InscripcionesByStatus = byInsStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalIS > 0 ? Math.Round((decimal)(int)x.Count / totalIS * 100, 1) : 0 }).ToList();

                // Geographic
                var byEstado = await connection.QueryAsync<dynamic>($"SELECT a.academiccontrol_preinscription_address_state AS Name, COUNT(*) AS Count FROM academiccontrol_preinscription_address_table a INNER JOIN academiccontrol_preinscription_table p ON a.academiccontrol_preinscription_address_preinscriptionID = p.academiccontrol_preinscription_ID WHERE {dfP} GROUP BY a.academiccontrol_preinscription_address_state ORDER BY Count DESC", fp);
                var totalE = byEstado.Sum(x => (int)x.Count);
                vm.ByEstado = byEstado.Select(x => new GeoStatItem { Name = (string)x.Name, Count = (int)x.Count, Percentage = totalE > 0 ? Math.Round((decimal)(int)x.Count / totalE * 100, 1) : 0 }).ToList();

                var byMun = await connection.QueryAsync<dynamic>($"SELECT TOP 10 a.academiccontrol_preinscription_address_municipality AS Name, COUNT(*) AS Count FROM academiccontrol_preinscription_address_table a INNER JOIN academiccontrol_preinscription_table p ON a.academiccontrol_preinscription_address_preinscriptionID = p.academiccontrol_preinscription_ID WHERE {dfP} GROUP BY a.academiccontrol_preinscription_address_municipality ORDER BY Count DESC", fp);
                var totalM = byMun.Sum(x => (int)x.Count);
                vm.ByMunicipio = byMun.Select(x => new GeoStatItem { Name = (string)x.Name, Count = (int)x.Count, Percentage = totalM > 0 ? Math.Round((decimal)(int)x.Count / totalM * 100, 1) : 0 }).ToList();

                // Top Escuelas
                vm.TopEscuelas = (await connection.QueryAsync<EscuelaStatItem>($"SELECT TOP 10 ac.academiccontrol_preinscription_academic_originSchool AS EscuelaNombre, ISNULL(ac.academiccontrol_preinscription_academic_schoolState,'—') AS Estado, COUNT(*) AS Count FROM academiccontrol_preinscription_academic_table ac INNER JOIN academiccontrol_preinscription_table p ON ac.academiccontrol_preinscription_academic_preinscriptionID = p.academiccontrol_preinscription_ID WHERE {dfP} GROUP BY ac.academiccontrol_preinscription_academic_originSchool, ac.academiccontrol_preinscription_academic_schoolState ORDER BY Count DESC", fp)).ToList();

                // Medio Difusion
                var byMedio = await connection.QueryAsync<dynamic>($"SELECT ISNULL(p.academiccontrol_preinscription_diffusionMedia,'No especificado') AS Status, COUNT(*) AS Count FROM academiccontrol_preinscription_table p WHERE {dfP} GROUP BY p.academiccontrol_preinscription_diffusionMedia ORDER BY Count DESC", fp);
                var totalMd = byMedio.Sum(x => (int)x.Count);
                vm.ByMedioDifusion = byMedio.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalMd > 0 ? Math.Round((decimal)(int)x.Count / totalMd * 100, 1) : 0 }).ToList();

                // Gender
                var genderData = await connection.QueryAsync<dynamic>($"SELECT pd.academiccontrol_preinscription_personaldata_gender AS Gender, COUNT(*) AS Count FROM academiccontrol_preinscription_personaldata_table pd INNER JOIN academiccontrol_preinscription_table p ON pd.academiccontrol_preinscription_personaldata_preinscriptionID = p.academiccontrol_preinscription_ID WHERE {dfP} GROUP BY pd.academiccontrol_preinscription_personaldata_gender", fp);
                foreach (var g in genderData) { string gen = ((string)g.Gender).ToLower(); int cnt = (int)g.Count; if (gen.Contains("masculino") || gen == "m") vm.MaleCount += cnt; else if (gen.Contains("femenino") || gen == "f") vm.FemaleCount += cnt; else vm.OtherGenderCount += cnt; }

                // Social indicators
                vm.TotalSaludRecords = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM academiccontrol_preinscription_health_table h INNER JOIN academiccontrol_preinscription_table p ON h.academiccontrol_preinscription_health_preinscriptionID = p.academiccontrol_preinscription_ID WHERE {dfP}", fp);
                vm.ConDiscapacidad = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM academiccontrol_preinscription_health_table h INNER JOIN academiccontrol_preinscription_table p ON h.academiccontrol_preinscription_health_preinscriptionID = p.academiccontrol_preinscription_ID WHERE h.academiccontrol_preinscription_health_hasDisability = 1 AND {dfP}", fp);
                vm.ComunidadIndigena = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM academiccontrol_preinscription_health_table h INNER JOIN academiccontrol_preinscription_table p ON h.academiccontrol_preinscription_health_preinscriptionID = p.academiccontrol_preinscription_ID WHERE h.academiccontrol_preinscription_health_indigenousCommunity = 1 AND {dfP}", fp);
                vm.ConHijos = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM academiccontrol_preinscription_health_table h INNER JOIN academiccontrol_preinscription_table p ON h.academiccontrol_preinscription_health_preinscriptionID = p.academiccontrol_preinscription_ID WHERE h.academiccontrol_preinscription_health_hasChildren = 1 AND {dfP}", fp);

                // Promedio distribution
                vm.PromedioDistribution = (await connection.QueryAsync<PromedioRangeItem>($@"
                    SELECT CASE WHEN academiccontrol_preinscription_average >= 9.0 THEN '9.0 — 10.0' WHEN academiccontrol_preinscription_average >= 8.0 THEN '8.0 — 8.9' WHEN academiccontrol_preinscription_average >= 7.0 THEN '7.0 — 7.9' WHEN academiccontrol_preinscription_average >= 6.0 THEN '6.0 — 6.9' ELSE 'Menor a 6.0' END AS Range, COUNT(*) AS Count
                    FROM academiccontrol_preinscription_table p WHERE {dfP}
                    GROUP BY CASE WHEN academiccontrol_preinscription_average >= 9.0 THEN '9.0 — 10.0' WHEN academiccontrol_preinscription_average >= 8.0 THEN '8.0 — 8.9' WHEN academiccontrol_preinscription_average >= 7.0 THEN '7.0 — 7.9' WHEN academiccontrol_preinscription_average >= 6.0 THEN '6.0 — 6.9' ELSE 'Menor a 6.0' END ORDER BY Range DESC", fp)).ToList();

                // Recent
                vm.RecentPreinscripciones = (await connection.QueryAsync<PreinscripcionDetailItem>($@"
                    SELECT TOP 50 ISNULL(p.academiccontrol_preinscription_folio,'—') AS Folio,
                        ISNULL(pd.academiccontrol_preinscription_personaldata_name + ' ' + pd.academiccontrol_preinscription_personaldata_paternalSurname,'—') AS Nombre,
                        p.academiccontrol_preinscription_careerRequested AS Carrera, p.academiccontrol_preinscription_average AS Promedio,
                        ISNULL(a.academiccontrol_preinscription_address_state,'—') AS Estado,
                        p.academiccontrol_preinscription_state AS Estatus, p.academiccontrol_preinscription_registrationDate AS Fecha
                    FROM academiccontrol_preinscription_table p
                    LEFT JOIN academiccontrol_preinscription_personaldata_table pd ON pd.academiccontrol_preinscription_personaldata_preinscriptionID = p.academiccontrol_preinscription_ID
                    LEFT JOIN academiccontrol_preinscription_address_table a ON a.academiccontrol_preinscription_address_preinscriptionID = p.academiccontrol_preinscription_ID
                    WHERE {dfP} ORDER BY p.academiccontrol_preinscription_registrationDate DESC", fp)).ToList();

                // Monthly (full year)
                vm.MonthlyPreinscripciones = (await connection.QueryAsync<MonthlyStatItem>(@"
                    SELECT FORMAT(academiccontrol_preinscription_registrationDate,'MMM','es-MX') AS Month, YEAR(academiccontrol_preinscription_registrationDate) AS Year, COUNT(*) AS Count
                    FROM academiccontrol_preinscription_table WHERE YEAR(academiccontrol_preinscription_registrationDate) = @Year
                    GROUP BY FORMAT(academiccontrol_preinscription_registrationDate,'MMM','es-MX'), YEAR(academiccontrol_preinscription_registrationDate), MONTH(academiccontrol_preinscription_registrationDate)
                    ORDER BY YEAR(academiccontrol_preinscription_registrationDate), MONTH(academiccontrol_preinscription_registrationDate)", new { Year = vm.SelectedYear })).ToList();

                vm.MonthlyInscripciones = (await connection.QueryAsync<MonthlyStatItem>(@"
                    SELECT FORMAT(academiccontrol_inscription_registrationDate,'MMM','es-MX') AS Month, YEAR(academiccontrol_inscription_registrationDate) AS Year, COUNT(*) AS Count
                    FROM academiccontrol_inscription_table WHERE YEAR(academiccontrol_inscription_registrationDate) = @Year
                    GROUP BY FORMAT(academiccontrol_inscription_registrationDate,'MMM','es-MX'), YEAR(academiccontrol_inscription_registrationDate), MONTH(academiccontrol_inscription_registrationDate)
                    ORDER BY YEAR(academiccontrol_inscription_registrationDate), MONTH(academiccontrol_inscription_registrationDate)", new { Year = vm.SelectedYear })).ToList();
            }
            else
            {
                // FALLBACK: Old Preinscripciones tables
                var years = await connection.QueryAsync<int>("SELECT DISTINCT YEAR(FechaPreinscripcion) FROM Preinscripciones UNION SELECT DISTINCT YEAR(FechaInscripcion) FROM Inscripciones ORDER BY 1 DESC");
                vm.AvailableYears = years.ToList();
                if (!vm.AvailableYears.Any()) vm.AvailableYears.Add(DateTime.Now.Year);
                vm.SelectedYear = year ?? DateTime.Now.Year;
                vm.SelectedCuatrimestre = cuatrimestre ?? 0;

                int startMonth = 1, endMonth = 12;
                if (vm.SelectedCuatrimestre > 0)
                {
                    startMonth = vm.SelectedCuatrimestre switch { 1 => 1, 2 => 5, 3 => 9, _ => 1 };
                    endMonth = vm.SelectedCuatrimestre switch { 1 => 4, 2 => 8, 3 => 12, _ => 12 };
                }
                var fp = new { Year = vm.SelectedYear, StartMonth = startMonth, EndMonth = endMonth };

                vm.TotalPreinscripciones = await connection.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM Preinscripciones WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth", fp);
                vm.TotalInscripciones = await connection.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM Inscripciones WHERE YEAR(FechaInscripcion) = @Year AND MONTH(FechaInscripcion) BETWEEN @StartMonth AND @EndMonth", fp);
                vm.ConversionRate = vm.TotalPreinscripciones > 0 ? Math.Round((decimal)vm.TotalInscripciones / vm.TotalPreinscripciones * 100, 1) : 0;
                vm.PromedioGeneral = await connection.ExecuteScalarAsync<decimal?>(@"SELECT AVG(Promedio) FROM Preinscripciones WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth", fp) ?? 0;
                vm.PromedioGeneral = Math.Round(vm.PromedioGeneral, 2);

                var byCareers = await connection.QueryAsync<dynamic>($"SELECT CarreraSolicitada AS CareerName, COUNT(*) AS Count FROM Preinscripciones WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth GROUP BY CarreraSolicitada ORDER BY Count DESC", fp);
                var totalC = byCareers.Sum(x => (int)x.Count);
                vm.PreinscripcionesByCareer = byCareers.Select(x => new CareerStatItem { CareerName = (string)x.CareerName, Count = (int)x.Count, Percentage = totalC > 0 ? Math.Round((decimal)(int)x.Count / totalC * 100, 1) : 0 }).ToList();

                var byStatus = await connection.QueryAsync<dynamic>($"SELECT EstadoPreinscripcion AS Status, COUNT(*) AS Count FROM Preinscripciones WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth GROUP BY EstadoPreinscripcion ORDER BY Count DESC", fp);
                var totalS = byStatus.Sum(x => (int)x.Count);
                vm.PreinscripcionesByStatus = byStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalS > 0 ? Math.Round((decimal)(int)x.Count / totalS * 100, 1) : 0 }).ToList();

                var byInsStatus = await connection.QueryAsync<dynamic>($"SELECT EstadoInscripcion AS Status, COUNT(*) AS Count FROM Inscripciones WHERE YEAR(FechaInscripcion) = @Year AND MONTH(FechaInscripcion) BETWEEN @StartMonth AND @EndMonth GROUP BY EstadoInscripcion ORDER BY Count DESC", fp);
                var totalIS = byInsStatus.Sum(x => (int)x.Count);
                vm.InscripcionesByStatus = byInsStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalIS > 0 ? Math.Round((decimal)(int)x.Count / totalIS * 100, 1) : 0 }).ToList();

                vm.RecentPreinscripciones = (await connection.QueryAsync<PreinscripcionDetailItem>($@"
                    SELECT TOP 50 ISNULL(p.Folio,'—') AS Folio, ISNULL(d.Nombre + ' ' + d.ApellidoPaterno,'—') AS Nombre,
                        p.CarreraSolicitada AS Carrera, p.Promedio, ISNULL(dom.Estado,'—') AS Estado, p.EstadoPreinscripcion AS Estatus, p.FechaPreinscripcion AS Fecha
                    FROM Preinscripciones p LEFT JOIN PreinscripcionDatosPersonales d ON p.Id = d.PreinscripcionId
                    LEFT JOIN PreinscripcionDomicilio dom ON p.Id = dom.PreinscripcionId
                    WHERE YEAR(p.FechaPreinscripcion) = @Year AND MONTH(p.FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
                    ORDER BY p.FechaPreinscripcion DESC", fp)).ToList();
            }

            return vm;
        }

        // ═══════════════════════════════════════════════════════════════
        // 3. TRAMITES
        // ═══════════════════════════════════════════════════════════════
        public async Task<TramitesViewModel> GetTramitesDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new TramitesViewModel();

            if (!await TableExists(connection, "CE_TramitesSolicitud"))
            {
                vm.AvailableYears = new List<int> { DateTime.Now.Year };
                vm.SelectedYear = year ?? DateTime.Now.Year;
                vm.SelectedCuatrimestre = cuatrimestre ?? 0;
                return vm;
            }

            var years = await connection.QueryAsync<int>("SELECT DISTINCT YEAR(tramites_solicitud_fecha) FROM CE_TramitesSolicitud ORDER BY 1 DESC");
            vm.AvailableYears = years.ToList();
            if (!vm.AvailableYears.Any()) vm.AvailableYears.Add(DateTime.Now.Year);
            vm.SelectedYear = year ?? DateTime.Now.Year;
            vm.SelectedCuatrimestre = cuatrimestre ?? 0;

            int startMonth = 1, endMonth = 12;
            if (vm.SelectedCuatrimestre > 0)
            {
                startMonth = vm.SelectedCuatrimestre switch { 1 => 1, 2 => 5, 3 => 9, _ => 1 };
                endMonth = vm.SelectedCuatrimestre switch { 1 => 4, 2 => 8, 3 => 12, _ => 12 };
            }
            var fp = new { Year = vm.SelectedYear, StartMonth = startMonth, EndMonth = endMonth };
            string df = "YEAR(tramites_solicitud_fecha) = @Year AND MONTH(tramites_solicitud_fecha) BETWEEN @StartMonth AND @EndMonth";

            vm.TotalSolicitudes = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE {df}", fp);
            vm.Pendientes = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus = 'Pendiente' AND {df}", fp);
            vm.Completadas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada','Entregado','Entregada') AND {df}", fp);
            vm.Rechazadas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus IN ('Rechazado','Rechazada') AND {df}", fp);
            vm.TasaCompletado = vm.TotalSolicitudes > 0 ? Math.Round((decimal)vm.Completadas / vm.TotalSolicitudes * 100, 1) : 0;

            var avgDays = await connection.ExecuteScalarAsync<double?>($"SELECT AVG(CAST(DATEDIFF(DAY, tramites_solicitud_fecha, GETDATE()) AS FLOAT)) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada') AND {df}", fp);
            vm.PromedioResolucionDias = Math.Round(avgDays ?? 0, 1);

            var byStatus = await connection.QueryAsync<dynamic>($"SELECT ISNULL(tramites_solicitud_estatus,'Pendiente') AS Status, COUNT(*) AS Count FROM CE_TramitesSolicitud WHERE {df} GROUP BY tramites_solicitud_estatus ORDER BY Count DESC", fp);
            var totalS = byStatus.Sum(x => (int)x.Count);
            vm.ByStatus = byStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalS > 0 ? Math.Round((decimal)(int)x.Count / totalS * 100, 1) : 0 }).ToList();

            vm.ByTipoTramite = (await connection.QueryAsync<TramiteTipoItem>($@"
                SELECT t.nombre_tramite AS TipoNombre, COUNT(*) AS Total,
                    SUM(CASE WHEN s.tramites_solicitud_estatus = 'Pendiente' THEN 1 ELSE 0 END) AS Pendientes,
                    SUM(CASE WHEN s.tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada','Entregado','Entregada') THEN 1 ELSE 0 END) AS Completadas,
                    SUM(CASE WHEN s.tramites_solicitud_estatus IN ('Rechazado','Rechazada') THEN 1 ELSE 0 END) AS Rechazadas
                FROM CE_TramitesSolicitud s INNER JOIN CE_TramitesCategoria t ON s.id_tramite = t.id_tramite
                WHERE {df} GROUP BY t.nombre_tramite ORDER BY Total DESC", fp)).ToList();

            vm.DocsAprobados = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CE_TramitesDetalleDocumentos WHERE estatus_documento IN ('Aprobado','Aprobada','Validado','Validada')");
            vm.DocsPendientes = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CE_TramitesDetalleDocumentos WHERE estatus_documento = 'Pendiente'");
            vm.DocsRechazados = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CE_TramitesDetalleDocumentos WHERE estatus_documento IN ('Rechazado','Rechazada')");

            vm.MonthlyTrend = (await connection.QueryAsync<MonthlyStatItem>(@"
                SELECT FORMAT(tramites_solicitud_fecha,'MMM','es-MX') AS Month, YEAR(tramites_solicitud_fecha) AS Year, COUNT(*) AS Count
                FROM CE_TramitesSolicitud WHERE YEAR(tramites_solicitud_fecha) = @Year
                GROUP BY FORMAT(tramites_solicitud_fecha,'MMM','es-MX'), YEAR(tramites_solicitud_fecha), MONTH(tramites_solicitud_fecha)
                ORDER BY YEAR(tramites_solicitud_fecha), MONTH(tramites_solicitud_fecha)", new { Year = vm.SelectedYear })).ToList();

            vm.RecentSolicitudes = (await connection.QueryAsync<SolicitudDetailItem>($@"
                SELECT TOP 50 s.tramites_solicitud_id AS Id,
                    ISNULL(p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal,'—') AS Nombre,
                    ISNULL(st.management_student_Matricula,'—') AS Matricula, t.nombre_tramite AS TipoTramite,
                    ISNULL(s.tramites_solicitud_estatus,'Pendiente') AS Estatus, ISNULL(s.tramites_solicitud_observaciones,'') AS Observaciones,
                    s.tramites_solicitud_fecha AS Fecha, DATEDIFF(DAY, s.tramites_solicitud_fecha, GETDATE()) AS DiasTranscurridos
                FROM CE_TramitesSolicitud s INNER JOIN CE_TramitesCategoria t ON s.id_tramite = t.id_tramite
                LEFT JOIN management_user_table u ON s.id_usuario_propietario = u.management_user_ID
                LEFT JOIN management_person_table p ON u.management_user_PersonID = p.management_person_ID
                LEFT JOIN management_student_table st ON p.management_person_ID = st.management_student_PersonID
                WHERE {df} ORDER BY s.tramites_solicitud_fecha DESC", fp)).ToList();

            vm.OldestPending = (await connection.QueryAsync<SolicitudDetailItem>(@"
                SELECT TOP 10 s.tramites_solicitud_id AS Id,
                    ISNULL(p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal,'—') AS Nombre,
                    ISNULL(st.management_student_Matricula,'—') AS Matricula, t.nombre_tramite AS TipoTramite,
                    'Pendiente' AS Estatus, ISNULL(s.tramites_solicitud_observaciones,'') AS Observaciones,
                    s.tramites_solicitud_fecha AS Fecha, DATEDIFF(DAY, s.tramites_solicitud_fecha, GETDATE()) AS DiasTranscurridos
                FROM CE_TramitesSolicitud s INNER JOIN CE_TramitesCategoria t ON s.id_tramite = t.id_tramite
                LEFT JOIN management_user_table u ON s.id_usuario_propietario = u.management_user_ID
                LEFT JOIN management_person_table p ON u.management_user_PersonID = p.management_person_ID
                LEFT JOIN management_student_table st ON p.management_person_ID = st.management_student_PersonID
                WHERE s.tramites_solicitud_estatus = 'Pendiente' ORDER BY s.tramites_solicitud_fecha ASC")).ToList();

            return vm;
        }

        // ═══════════════════════════════════════════════════════════════
        // 4. ASPIRANTES
        // ═══════════════════════════════════════════════════════════════
        public async Task<AspirantesViewModel> GetAspirantesDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new AspirantesViewModel();

            if (!await TableExists(connection, "Aspirantes"))
            {
                vm.AvailableYears = new List<int> { DateTime.Now.Year };
                vm.SelectedYear = year ?? DateTime.Now.Year;
                vm.SelectedCuatrimestre = cuatrimestre ?? 0;
                return vm;
            }

            var years = await connection.QueryAsync<int>("SELECT DISTINCT YEAR(FechaRegistro) FROM Aspirantes ORDER BY 1 DESC");
            vm.AvailableYears = years.ToList();
            if (!vm.AvailableYears.Any()) vm.AvailableYears.Add(DateTime.Now.Year);
            vm.SelectedYear = year ?? DateTime.Now.Year;
            vm.SelectedCuatrimestre = cuatrimestre ?? 0;

            int startMonth = 1, endMonth = 12;
            if (vm.SelectedCuatrimestre > 0)
            {
                startMonth = vm.SelectedCuatrimestre switch { 1 => 1, 2 => 5, 3 => 9, _ => 1 };
                endMonth = vm.SelectedCuatrimestre switch { 1 => 4, 2 => 8, 3 => 12, _ => 12 };
            }
            var fp = new { Year = vm.SelectedYear, StartMonth = startMonth, EndMonth = endMonth };
            string df = "YEAR(a.FechaRegistro) = @Year AND MONTH(a.FechaRegistro) BETWEEN @StartMonth AND @EndMonth";

            vm.TotalAspirantes = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE {df}", fp);
            vm.Aceptados = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Aceptado' AND {df}", fp);
            vm.Pendientes = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Pendiente' AND {df}", fp);
            vm.DocIncompleta = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Documentación Incompleta' AND {df}", fp);
            vm.FichasPagadas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Ficha Pagada' AND {df}", fp);
            vm.ExamenPresentado = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Examen Presentado' AND {df}", fp);
            vm.Rechazados = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Rechazado' AND {df}", fp);
            vm.PromedioGeneral = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(e.Promedio) FROM AspiranteEscolar e INNER JOIN Aspirantes a ON e.Folio = a.Folio WHERE {df}", fp) ?? 0;
            vm.PromedioGeneral = Math.Round(vm.PromedioGeneral, 2);

            var byStatus = await connection.QueryAsync<dynamic>($"SELECT EstadoRegistro AS Status, COUNT(*) AS Count FROM Aspirantes a WHERE {df} GROUP BY EstadoRegistro ORDER BY Count DESC", fp);
            var totalSt = byStatus.Sum(x => (int)x.Count);
            vm.ByStatus = byStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalSt > 0 ? Math.Round((decimal)(int)x.Count / totalSt * 100, 1) : 0 }).ToList();

            var byCareer = await connection.QueryAsync<dynamic>($"SELECT CarreraSolicitada AS CareerName, COUNT(*) AS Count FROM Aspirantes a WHERE {df} GROUP BY CarreraSolicitada ORDER BY Count DESC", fp);
            var totalCr = byCareer.Sum(x => (int)x.Count);
            vm.ByCareer = byCareer.Select(x => new CareerStatItem { CareerName = (string)x.CareerName, Count = (int)x.Count, Percentage = totalCr > 0 ? Math.Round((decimal)(int)x.Count / totalCr * 100, 1) : 0 }).ToList();

            var gender = await connection.QueryAsync<dynamic>($"SELECT d.Sexo AS Gender, COUNT(*) AS Count FROM AspiranteDatosGenerales d INNER JOIN Aspirantes a ON d.Folio = a.Folio WHERE {df} GROUP BY d.Sexo", fp);
            foreach (var g in gender) { string gen = ((string)g.Gender).ToLower(); int cnt = (int)g.Count; if (gen.Contains("masculino") || gen == "m") vm.MaleCount += cnt; else if (gen.Contains("femenino") || gen == "f") vm.FemaleCount += cnt; }

            var byMun = await connection.QueryAsync<dynamic>($"SELECT TOP 10 dom.Municipio AS Name, COUNT(*) AS Count FROM AspiranteDomicilio dom INNER JOIN Aspirantes a ON dom.Folio = a.Folio WHERE {df} GROUP BY dom.Municipio ORDER BY Count DESC", fp);
            var totalM = byMun.Sum(x => (int)x.Count);
            vm.ByMunicipio = byMun.Select(x => new GeoStatItem { Name = (string)x.Name, Count = (int)x.Count, Percentage = totalM > 0 ? Math.Round((decimal)(int)x.Count / totalM * 100, 1) : 0 }).ToList();

            vm.TopEscuelas = (await connection.QueryAsync<EscuelaStatItem>($"SELECT TOP 10 e.EscuelaProcedencia AS EscuelaNombre, ISNULL(e.EstadoEscuela,'—') AS Estado, COUNT(*) AS Count FROM AspiranteEscolar e INNER JOIN Aspirantes a ON e.Folio = a.Folio WHERE {df} GROUP BY e.EscuelaProcedencia, e.EstadoEscuela ORDER BY Count DESC", fp)).ToList();

            var byTipo = await connection.QueryAsync<dynamic>($"SELECT e.TipoPreparatoria AS Status, COUNT(*) AS Count FROM AspiranteEscolar e INNER JOIN Aspirantes a ON e.Folio = a.Folio WHERE {df} GROUP BY e.TipoPreparatoria ORDER BY Count DESC", fp);
            var totalT = byTipo.Sum(x => (int)x.Count);
            vm.ByTipoPrepa = byTipo.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalT > 0 ? Math.Round((decimal)(int)x.Count / totalT * 100, 1) : 0 }).ToList();

            var byMedio = await connection.QueryAsync<dynamic>($"SELECT ISNULL(o.ComoSeEntero,'No especificado') AS Status, COUNT(*) AS Count FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE {df} GROUP BY o.ComoSeEntero ORDER BY Count DESC", fp);
            var totalMd = byMedio.Sum(x => (int)x.Count);
            vm.ByMedioDifusion = byMedio.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalMd > 0 ? Math.Round((decimal)(int)x.Count / totalMd * 100, 1) : 0 }).ToList();

            vm.TotalOtros = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE {df}", fp);
            vm.Trabajan = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.Trabaja = 1 AND {df}", fp);
            vm.ConBeca = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.ContabaConBeca = 1 AND {df}", fp);
            vm.OrigenIndigena = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.OrigenIndigena = 1 AND {df}", fp);
            vm.ConDiscapacidad = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.PadeceDiscapacidad = 1 AND {df}", fp);
            vm.ConEnfermedad = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.PadeceEnfermedad = 1 AND {df}", fp);

            vm.RecentAspirantes = (await connection.QueryAsync<AspiranteDetailItem>($@"
                SELECT TOP 50 a.Folio, d.Nombre + ' ' + d.ApellidoPaterno AS Nombre, a.CarreraSolicitada AS Carrera,
                    e.Promedio, e.EscuelaProcedencia AS Preparatoria, dom.Estado, a.EstadoRegistro AS Estatus, a.FechaRegistro AS Fecha
                FROM Aspirantes a LEFT JOIN AspiranteDatosGenerales d ON a.Folio = d.Folio
                LEFT JOIN AspiranteEscolar e ON a.Folio = e.Folio LEFT JOIN AspiranteDomicilio dom ON a.Folio = dom.Folio
                WHERE {df} ORDER BY a.FechaRegistro DESC", fp)).ToList();

            return vm;
        }

        // ═══════════════════════════════════════════════════════════════
        // 5. SERVICIOS MEDICOS
        // ═══════════════════════════════════════════════════════════════
        public async Task<MedicalViewModel> GetMedicalDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new MedicalViewModel();

            if (!await TableExists(connection, "Visitas"))
            {
                vm.AvailableYears = new List<int> { DateTime.Now.Year };
                vm.SelectedYear = year ?? DateTime.Now.Year;
                vm.SelectedCuatrimestre = cuatrimestre ?? 0;
                return vm;
            }

            var years = await connection.QueryAsync<int>(@"
                SELECT DISTINCT YEAR(FechaVisita) FROM Visitas
                UNION SELECT DISTINCT YEAR(FechaVisita) FROM VisitasPsicologicas
                ORDER BY 1 DESC");
            vm.AvailableYears = years.ToList();
            if (!vm.AvailableYears.Any()) vm.AvailableYears.Add(DateTime.Now.Year);
            vm.SelectedYear = year ?? DateTime.Now.Year;
            vm.SelectedCuatrimestre = cuatrimestre ?? 0;

            int startMonth = 1, endMonth = 12;
            if (vm.SelectedCuatrimestre > 0)
            {
                startMonth = vm.SelectedCuatrimestre switch { 1 => 1, 2 => 5, 3 => 9, _ => 1 };
                endMonth = vm.SelectedCuatrimestre switch { 1 => 4, 2 => 8, 3 => 12, _ => 12 };
            }
            var fp = new { Year = vm.SelectedYear, StartMonth = startMonth, EndMonth = endMonth };
            string dfV = "YEAR(FechaVisita) = @Year AND MONTH(FechaVisita) BETWEEN @StartMonth AND @EndMonth";

            vm.TotalVisitas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Visitas WHERE {dfV}", fp);
            vm.TotalPsicologicas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM VisitasPsicologicas WHERE {dfV}", fp);
            vm.ConAlergias = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Visitas WHERE TieneAlergias = 1 AND {dfV}", fp);
            vm.ConEnfermedadesCronicas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Visitas WHERE EnfermedadesCronicas IS NOT NULL AND EnfermedadesCronicas != '' AND {dfV}", fp);
            vm.ConTerapiaPrevia = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM VisitasPsicologicas WHERE TerapiaPrevia = 1 AND {dfV}", fp);
            vm.ConMedicacion = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM VisitasPsicologicas WHERE MedicacionPsiquiatrica IS NOT NULL AND MedicacionPsiquiatrica != '' AND {dfV}", fp);
            vm.PromedioEdad = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(CAST(Edad AS DECIMAL)) FROM Visitas WHERE {dfV}", fp) ?? 0;
            vm.PromedioEdad = Math.Round(vm.PromedioEdad, 1);

            vm.PromedioTemperatura = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(CAST(Temperatura AS DECIMAL(5,1))) FROM Visitas WHERE Temperatura IS NOT NULL AND {dfV}", fp) ?? 0;
            vm.PromedioTemperatura = Math.Round(vm.PromedioTemperatura, 1);
            vm.PromedioIMC = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(CAST(Peso / (Talla * Talla) AS DECIMAL(5,1))) FROM Visitas WHERE Talla > 0 AND Peso > 0 AND {dfV}", fp) ?? 0;
            vm.PromedioIMC = Math.Round(vm.PromedioIMC, 1);

            var diag = await connection.QueryAsync<dynamic>($"SELECT TOP 15 Diagnostico AS Status, COUNT(*) AS Count FROM Visitas WHERE {dfV} GROUP BY Diagnostico ORDER BY Count DESC", fp);
            var totalD = diag.Sum(x => (int)x.Count);
            vm.TopDiagnosticos = diag.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalD > 0 ? Math.Round((decimal)(int)x.Count / totalD * 100, 1) : 0 }).ToList();

            var motivos = await connection.QueryAsync<dynamic>($"SELECT TOP 15 MotivoConsulta AS Status, COUNT(*) AS Count FROM VisitasPsicologicas WHERE {dfV} GROUP BY MotivoConsulta ORDER BY Count DESC", fp);
            var totalMo = motivos.Sum(x => (int)x.Count);
            vm.TopMotivosPsicologicos = motivos.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalMo > 0 ? Math.Round((decimal)(int)x.Count / totalMo * 100, 1) : 0 }).ToList();

            vm.ByEdad = (await connection.QueryAsync<PromedioRangeItem>($@"
                SELECT CASE WHEN Edad < 18 THEN 'Menor de 18' WHEN Edad <= 20 THEN '18 — 20' WHEN Edad <= 23 THEN '21 — 23' WHEN Edad <= 25 THEN '24 — 25' ELSE '26+' END AS Range, COUNT(*) AS Count
                FROM Visitas WHERE {dfV}
                GROUP BY CASE WHEN Edad < 18 THEN 'Menor de 18' WHEN Edad <= 20 THEN '18 — 20' WHEN Edad <= 23 THEN '21 — 23' WHEN Edad <= 25 THEN '24 — 25' ELSE '26+' END
                ORDER BY Range", fp)).ToList();

            vm.MonthlyVisitas = (await connection.QueryAsync<MonthlyStatItem>(@"
                SELECT FORMAT(FechaVisita,'MMM','es-MX') AS Month, YEAR(FechaVisita) AS Year, COUNT(*) AS Count
                FROM Visitas WHERE YEAR(FechaVisita) = @Year
                GROUP BY FORMAT(FechaVisita,'MMM','es-MX'), YEAR(FechaVisita), MONTH(FechaVisita)
                ORDER BY YEAR(FechaVisita), MONTH(FechaVisita)", new { Year = vm.SelectedYear })).ToList();

            vm.MonthlyPsicologicas = (await connection.QueryAsync<MonthlyStatItem>(@"
                SELECT FORMAT(FechaVisita,'MMM','es-MX') AS Month, YEAR(FechaVisita) AS Year, COUNT(*) AS Count
                FROM VisitasPsicologicas WHERE YEAR(FechaVisita) = @Year
                GROUP BY FORMAT(FechaVisita,'MMM','es-MX'), YEAR(FechaVisita), MONTH(FechaVisita)
                ORDER BY YEAR(FechaVisita), MONTH(FechaVisita)", new { Year = vm.SelectedYear })).ToList();

            vm.RecentVisitas = (await connection.QueryAsync<VisitaDetailItem>($@"
                SELECT TOP 50 Id, Matricula, FechaVisita, Edad, Diagnostico,
                    ISNULL(CAST(Temperatura AS VARCHAR),'—') AS Temperatura,
                    ISNULL(PresionArterial,'—') AS PresionArterial,
                    ISNULL(Saturacion,'—') AS Saturacion,
                    TieneAlergias, ISNULL(EspecificarAlergia,'') AS Alergias
                FROM Visitas WHERE {dfV} ORDER BY FechaVisita DESC", fp)).ToList();

            vm.RecentPsicologicas = (await connection.QueryAsync<PsicologicaDetailItem>($@"
                SELECT TOP 50 Id, Matricula, FechaVisita, Edad, MotivoConsulta,
                    TerapiaPrevia, ISNULL(MedicacionPsiquiatrica,'') AS Medicacion
                FROM VisitasPsicologicas WHERE {dfV} ORDER BY FechaVisita DESC", fp)).ToList();

            return vm;
        }

        // ═══════════════════════════════════════════════════════════════
        // 6. VINCULACION
        // ═══════════════════════════════════════════════════════════════
        public async Task<VinculacionViewModel> GetVinculacionDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new VinculacionViewModel();

            if (!await TableExists(connection, "operational_studentassignment_table"))
            {
                vm.AvailableYears = new List<int> { DateTime.Now.Year };
                vm.SelectedYear = year ?? DateTime.Now.Year;
                vm.SelectedCuatrimestre = cuatrimestre ?? 0;
                return vm;
            }

            var years = await connection.QueryAsync<int>("SELECT DISTINCT YEAR(operational_studentassignment_createdDate) FROM operational_studentassignment_table ORDER BY 1 DESC");
            vm.AvailableYears = years.ToList();
            if (!vm.AvailableYears.Any()) vm.AvailableYears.Add(DateTime.Now.Year);
            vm.SelectedYear = year ?? DateTime.Now.Year;
            vm.SelectedCuatrimestre = cuatrimestre ?? 0;

            int startMonth = 1, endMonth = 12;
            if (vm.SelectedCuatrimestre > 0)
            {
                startMonth = vm.SelectedCuatrimestre switch { 1 => 1, 2 => 5, 3 => 9, _ => 1 };
                endMonth = vm.SelectedCuatrimestre switch { 1 => 4, 2 => 8, 3 => 12, _ => 12 };
            }
            var fp = new { Year = vm.SelectedYear, StartMonth = startMonth, EndMonth = endMonth };
            string df = "YEAR(sa.operational_studentassignment_createdDate) = @Year AND MONTH(sa.operational_studentassignment_createdDate) BETWEEN @StartMonth AND @EndMonth";

            vm.TotalPrograms = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM operational_program_table WHERE operational_program_status = 1");
            vm.TotalOrganizations = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM operational_organization_table WHERE operational_organization_status = 1");
            vm.TotalAssignments = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM operational_studentassignment_table sa WHERE {df}", fp);
            vm.TotalDocuments = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM operational_document_table WHERE operational_document_status = 1");
            vm.Completados = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM operational_studentassignment_table sa WHERE operational_studentassignment_StatusCode = 'Completado' AND {df}", fp);
            vm.EnProceso = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM operational_studentassignment_table sa WHERE operational_studentassignment_StatusCode = 'En Proceso' AND {df}", fp);
            vm.Asignados = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM operational_studentassignment_table sa WHERE operational_studentassignment_StatusCode IN ('Asignado','Por Iniciar') AND {df}", fp);
            vm.Cancelados = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM operational_studentassignment_table sa WHERE operational_studentassignment_StatusCode = 'Cancelado' AND {df}", fp);

            var avgHours = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(operational_studentassignment_ApprovedHours) FROM operational_studentassignment_table sa WHERE operational_studentassignment_StatusCode = 'Completado' AND {df}", fp);
            vm.PromedioHoras = Math.Round(avgHours ?? 0, 1);
            var avgEval = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(operational_studentassignment_EvaluationScore) FROM operational_studentassignment_table sa WHERE operational_studentassignment_EvaluationScore IS NOT NULL AND {df}", fp);
            vm.PromedioEvaluacion = Math.Round(avgEval ?? 0, 1);

            var byStatus = await connection.QueryAsync<dynamic>($"SELECT operational_studentassignment_StatusCode AS Status, COUNT(*) AS Count FROM operational_studentassignment_table sa WHERE {df} GROUP BY operational_studentassignment_StatusCode ORDER BY Count DESC", fp);
            var totalSt = byStatus.Sum(x => (int)x.Count);
            vm.ByStatus = byStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalSt > 0 ? Math.Round((decimal)(int)x.Count / totalSt * 100, 1) : 0 }).ToList();

            var byType = await connection.QueryAsync<dynamic>($@"
                SELECT p.operational_program_Type AS Status, COUNT(*) AS Count
                FROM operational_studentassignment_table sa
                INNER JOIN operational_program_table p ON sa.operational_studentassignment_ProgramID = p.operational_program_ID
                WHERE {df} GROUP BY p.operational_program_Type ORDER BY Count DESC", fp);
            var totalTy = byType.Sum(x => (int)x.Count);
            vm.ByProgramType = byType.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalTy > 0 ? Math.Round((decimal)(int)x.Count / totalTy * 100, 1) : 0 }).ToList();

            var byOrg = await connection.QueryAsync<dynamic>($@"
                SELECT TOP 10 o.operational_organization_Name AS Status, COUNT(*) AS Count
                FROM operational_studentassignment_table sa
                INNER JOIN operational_organization_table o ON sa.operational_studentassignment_OrganizationID = o.operational_organization_ID
                WHERE {df} GROUP BY o.operational_organization_Name ORDER BY Count DESC", fp);
            var totalOr = byOrg.Sum(x => (int)x.Count);
            vm.ByOrganization = byOrg.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalOr > 0 ? Math.Round((decimal)(int)x.Count / totalOr * 100, 1) : 0 }).ToList();

            var docStatus = await connection.QueryAsync<dynamic>("SELECT operational_document_StatusCode AS Status, COUNT(*) AS Count FROM operational_document_table GROUP BY operational_document_StatusCode ORDER BY Count DESC");
            var totalDs = docStatus.Sum(x => (int)x.Count);
            vm.DocsByStatus = docStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalDs > 0 ? Math.Round((decimal)(int)x.Count / totalDs * 100, 1) : 0 }).ToList();

            vm.Programs = (await connection.QueryAsync<ProgramDetailItem>(@"
                SELECT p.operational_program_Code AS Code, p.operational_program_Name AS Name, p.operational_program_Type AS Type,
                    ISNULL(p.operational_program_Period,'—') AS Period, ISNULL(p.operational_program_Year, 0) AS Year,
                    p.operational_program_RequiredHours AS RequiredHours, p.operational_program_IsActive AS IsActive,
                    COUNT(sa.operational_studentassignment_ID) AS TotalStudents
                FROM operational_program_table p
                LEFT JOIN operational_studentassignment_table sa ON p.operational_program_ID = sa.operational_studentassignment_ProgramID
                WHERE p.operational_program_status = 1
                GROUP BY p.operational_program_Code, p.operational_program_Name, p.operational_program_Type, p.operational_program_Period, p.operational_program_Year, p.operational_program_RequiredHours, p.operational_program_IsActive
                ORDER BY p.operational_program_Year DESC, p.operational_program_Name")).ToList();

            vm.Organizations = (await connection.QueryAsync<OrganizationDetailItem>(@"
                SELECT o.operational_organization_Name AS Name, o.operational_organization_Type AS Type,
                    ISNULL(o.operational_organization_City,'—') AS City, ISNULL(o.operational_organization_ContactName,'—') AS ContactName,
                    ISNULL(o.operational_organization_Phone,'—') AS Phone,
                    COUNT(sa.operational_studentassignment_ID) AS TotalStudents
                FROM operational_organization_table o
                LEFT JOIN operational_studentassignment_table sa ON o.operational_organization_ID = sa.operational_studentassignment_OrganizationID
                WHERE o.operational_organization_status = 1
                GROUP BY o.operational_organization_Name, o.operational_organization_Type, o.operational_organization_City, o.operational_organization_ContactName, o.operational_organization_Phone
                ORDER BY TotalStudents DESC")).ToList();

            vm.RecentAssignments = (await connection.QueryAsync<AssignmentDetailItem>($@"
                SELECT TOP 50 sa.operational_studentassignment_ID AS Id,
                    ISNULL(per.management_person_FirstName + ' ' + per.management_person_LastNamePaternal,'—') AS StudentName,
                    ISNULL(s.management_student_Matricula,'—') AS Matricula,
                    p.operational_program_Name AS ProgramName, p.operational_program_Type AS ProgramType,
                    ISNULL(o.operational_organization_Name,'—') AS OrganizationName,
                    sa.operational_studentassignment_StatusCode AS Status,
                    sa.operational_studentassignment_TotalHours AS TotalHours,
                    sa.operational_studentassignment_ApprovedHours AS ApprovedHours,
                    sa.operational_studentassignment_EvaluationScore AS EvaluationScore,
                    sa.operational_studentassignment_StartDate AS StartDate
                FROM operational_studentassignment_table sa
                INNER JOIN operational_program_table p ON sa.operational_studentassignment_ProgramID = p.operational_program_ID
                LEFT JOIN operational_organization_table o ON sa.operational_studentassignment_OrganizationID = o.operational_organization_ID
                LEFT JOIN management_student_table s ON sa.operational_studentassignment_StudentID = s.management_student_ID
                LEFT JOIN management_person_table per ON s.management_student_PersonID = per.management_person_ID
                WHERE {df} ORDER BY sa.operational_studentassignment_createdDate DESC", fp)).ToList();

            // Recent documents
            vm.RecentDocuments = (await connection.QueryAsync<DocumentDetailItem>(@"
                SELECT TOP 50 d.operational_document_ID AS Id,
                    d.operational_document_Title AS Title, d.operational_document_DocumentType AS DocumentType,
                    ISNULL(d.operational_document_FileName,'—') AS FileName, d.operational_document_StatusCode AS Status,
                    ISNULL(per.management_person_FirstName + ' ' + per.management_person_LastNamePaternal,'—') AS StudentName,
                    ISNULL(p.operational_program_Name,'—') AS ProgramName, d.operational_document_UploadDate AS UploadDate,
                    ISNULL(d.operational_document_ReviewComments,'') AS ReviewComments
                FROM operational_document_table d
                INNER JOIN operational_studentassignment_table sa ON d.operational_document_AssignmentID = sa.operational_studentassignment_ID
                INNER JOIN operational_program_table p ON sa.operational_studentassignment_ProgramID = p.operational_program_ID
                LEFT JOIN management_student_table s ON sa.operational_studentassignment_StudentID = s.management_student_ID
                LEFT JOIN management_person_table per ON s.management_student_PersonID = per.management_person_ID
                WHERE d.operational_document_status = 1 ORDER BY d.operational_document_UploadDate DESC")).ToList();

            return vm;
        }

        // ═══════════════════════════════════════════════════════════════
        // 7. CALIDAD ACADEMICA (GRADES)
        // ═══════════════════════════════════════════════════════════════
        public async Task<AcademicQualityViewModel> GetAcademicQualityDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new AcademicQualityViewModel();

            if (!await TableExists(connection, "grades_finalgrade_table"))
            {
                vm.AvailableYears = new List<int> { DateTime.Now.Year };
                vm.SelectedYear = year ?? DateTime.Now.Year;
                vm.SelectedCuatrimestre = cuatrimestre ?? 0;
                return vm;
            }

            var years = await connection.QueryAsync<int>("SELECT DISTINCT YEAR(grades_period_StartDate) FROM grades_period_table ORDER BY 1 DESC");
            vm.AvailableYears = years.ToList();
            if (!vm.AvailableYears.Any()) vm.AvailableYears.Add(DateTime.Now.Year);
            vm.SelectedYear = year ?? DateTime.Now.Year;
            vm.SelectedCuatrimestre = cuatrimestre ?? 0;

            vm.TotalSubjects = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grades_subject_table WHERE grades_subject_status = 1");
            vm.TotalTeacherAssignments = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grades_teachersubject_table WHERE grades_teachersubject_status = 1");
            vm.TotalGradeRecords = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grades_graderecord_table WHERE grades_graderecord_status = 1");
            vm.TotalFinalGrades = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grades_finalgrade_table WHERE grades_finalgrade_status = 1");
            vm.Aprobados = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grades_finalgrade_table WHERE grades_finalgrade_PassStatus = 'Aprobado' AND grades_finalgrade_status = 1");
            vm.Reprobados = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grades_finalgrade_table WHERE grades_finalgrade_PassStatus = 'Reprobado' AND grades_finalgrade_status = 1");
            vm.TasaAprobacion = vm.TotalFinalGrades > 0 ? Math.Round((decimal)vm.Aprobados / vm.TotalFinalGrades * 100, 1) : 0;
            vm.PromedioGeneral = await connection.ExecuteScalarAsync<decimal?>("SELECT AVG(grades_finalgrade_FinalValue) FROM grades_finalgrade_table WHERE grades_finalgrade_status = 1") ?? 0;
            vm.PromedioGeneral = Math.Round(vm.PromedioGeneral, 2);
            vm.TotalOpportunities = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grades_opportunity_table WHERE grades_opportunity_status = 1");
            vm.ActivePeriods = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM grades_period_table WHERE grades_period_IsActive = 1");

            var byPass = await connection.QueryAsync<dynamic>("SELECT grades_finalgrade_PassStatus AS Status, COUNT(*) AS Count FROM grades_finalgrade_table WHERE grades_finalgrade_status = 1 GROUP BY grades_finalgrade_PassStatus");
            var totalP = byPass.Sum(x => (int)x.Count);
            vm.ByPassStatus = byPass.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalP > 0 ? Math.Round((decimal)(int)x.Count / totalP * 100, 1) : 0 }).ToList();

            vm.SubjectsByCareer = (await connection.QueryAsync<SubjectStatItem>(@"
                SELECT c.management_career_Name AS CareerName, COUNT(*) AS SubjectCount, SUM(ISNULL(s.grades_subject_WeeklyHours,0)) AS TotalHours
                FROM grades_subject_table s INNER JOIN management_career_table c ON s.grades_subject_CareerID = c.management_career_ID
                WHERE s.grades_subject_status = 1 GROUP BY c.management_career_Name ORDER BY SubjectCount DESC")).ToList();

            vm.GradeDistribution = (await connection.QueryAsync<PromedioRangeItem>(@"
                SELECT CASE WHEN grades_finalgrade_FinalValue >= 9.0 THEN '9.0 — 10.0' WHEN grades_finalgrade_FinalValue >= 8.0 THEN '8.0 — 8.9' WHEN grades_finalgrade_FinalValue >= 7.0 THEN '7.0 — 7.9' WHEN grades_finalgrade_FinalValue >= 6.0 THEN '6.0 — 6.9' ELSE 'Menor a 6.0' END AS Range, COUNT(*) AS Count
                FROM grades_finalgrade_table WHERE grades_finalgrade_status = 1
                GROUP BY CASE WHEN grades_finalgrade_FinalValue >= 9.0 THEN '9.0 — 10.0' WHEN grades_finalgrade_FinalValue >= 8.0 THEN '8.0 — 8.9' WHEN grades_finalgrade_FinalValue >= 7.0 THEN '7.0 — 7.9' WHEN grades_finalgrade_FinalValue >= 6.0 THEN '6.0 — 6.9' ELSE 'Menor a 6.0' END ORDER BY Range DESC")).ToList();

            vm.GradesBySubject = (await connection.QueryAsync<GradeBySubjectItem>(@"
                SELECT s.grades_subject_Name AS SubjectName, c.management_career_Name AS CareerName,
                    AVG(fg.grades_finalgrade_FinalValue) AS Average, COUNT(*) AS TotalStudents,
                    SUM(CASE WHEN fg.grades_finalgrade_PassStatus = 'Aprobado' THEN 1 ELSE 0 END) AS Passed,
                    SUM(CASE WHEN fg.grades_finalgrade_PassStatus = 'Reprobado' THEN 1 ELSE 0 END) AS Failed,
                    CASE WHEN COUNT(*) > 0 THEN CAST(SUM(CASE WHEN fg.grades_finalgrade_PassStatus = 'Aprobado' THEN 1 ELSE 0 END) AS DECIMAL) / COUNT(*) * 100 ELSE 0 END AS PassRate
                FROM grades_finalgrade_table fg
                INNER JOIN grades_teachersubject_table ts ON fg.grades_finalgrade_TeacherSubjectID = ts.grades_teachersubject_ID
                INNER JOIN grades_subject_table s ON ts.grades_teachersubject_SubjectID = s.grades_subject_ID
                INNER JOIN management_career_table c ON s.grades_subject_CareerID = c.management_career_ID
                WHERE fg.grades_finalgrade_status = 1
                GROUP BY s.grades_subject_Name, c.management_career_Name ORDER BY Average DESC")).ToList();

            vm.GradesByTeacher = (await connection.QueryAsync<GradeByTeacherItem>(@"
                SELECT ISNULL(p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal,'—') AS TeacherName,
                    COUNT(DISTINCT ts.grades_teachersubject_SubjectID) AS TotalSubjects, COUNT(fg.grades_finalgrade_ID) AS TotalStudents,
                    AVG(fg.grades_finalgrade_FinalValue) AS AverageGrade,
                    CASE WHEN COUNT(fg.grades_finalgrade_ID) > 0 THEN CAST(SUM(CASE WHEN fg.grades_finalgrade_PassStatus = 'Aprobado' THEN 1 ELSE 0 END) AS DECIMAL) / COUNT(fg.grades_finalgrade_ID) * 100 ELSE 0 END AS PassRate
                FROM grades_finalgrade_table fg
                INNER JOIN grades_teachersubject_table ts ON fg.grades_finalgrade_TeacherSubjectID = ts.grades_teachersubject_ID
                INNER JOIN management_teacher_table t ON ts.grades_teachersubject_TeacherID = t.management_teacher_ID
                INNER JOIN management_person_table p ON t.management_teacher_PersonID = p.management_person_ID
                WHERE fg.grades_finalgrade_status = 1
                GROUP BY p.management_person_FirstName, p.management_person_LastNamePaternal ORDER BY AverageGrade DESC")).ToList();

            vm.Periods = (await connection.QueryAsync<PeriodItem>(@"
                SELECT gp.grades_period_Name AS Name, gp.grades_period_StartDate AS StartDate, gp.grades_period_EndDate AS EndDate, gp.grades_period_IsActive AS IsActive,
                    COUNT(ts.grades_teachersubject_ID) AS Assignments
                FROM grades_period_table gp LEFT JOIN grades_teachersubject_table ts ON gp.grades_period_ID = ts.grades_teachersubject_PeriodID AND ts.grades_teachersubject_status = 1
                WHERE gp.grades_period_status = 1 GROUP BY gp.grades_period_Name, gp.grades_period_StartDate, gp.grades_period_EndDate, gp.grades_period_IsActive
                ORDER BY gp.grades_period_StartDate DESC")).ToList();

            vm.SubjectCatalog = (await connection.QueryAsync<SubjectCatalogItem>(@"
                SELECT s.grades_subject_Code AS Code, s.grades_subject_Name AS Name, c.management_career_Name AS CareerName,
                    s.grades_subject_Semester AS Semester, ISNULL(s.grades_subject_WeeklyHours,0) AS WeeklyHours
                FROM grades_subject_table s INNER JOIN management_career_table c ON s.grades_subject_CareerID = c.management_career_ID
                WHERE s.grades_subject_status = 1 ORDER BY c.management_career_Name, s.grades_subject_Semester, s.grades_subject_Name")).ToList();

            vm.RecentFinalGrades = (await connection.QueryAsync<FinalGradeDetailItem>(@"
                SELECT TOP 50 ISNULL(p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal,'—') AS StudentName,
                    ISNULL(st.management_student_Matricula,'—') AS Matricula, s.grades_subject_Name AS SubjectName,
                    ISNULL(tp.management_person_FirstName + ' ' + tp.management_person_LastNamePaternal,'—') AS TeacherName,
                    fg.grades_finalgrade_FinalValue AS FinalValue, fg.grades_finalgrade_PassStatus AS PassStatus, gp.grades_period_Name AS PeriodName
                FROM grades_finalgrade_table fg
                INNER JOIN grades_teachersubject_table ts ON fg.grades_finalgrade_TeacherSubjectID = ts.grades_teachersubject_ID
                INNER JOIN grades_subject_table s ON ts.grades_teachersubject_SubjectID = s.grades_subject_ID
                INNER JOIN grades_period_table gp ON ts.grades_teachersubject_PeriodID = gp.grades_period_ID
                LEFT JOIN management_student_table st ON fg.grades_finalgrade_StudentID = st.management_student_ID
                LEFT JOIN management_person_table p ON st.management_student_PersonID = p.management_person_ID
                LEFT JOIN management_teacher_table t ON ts.grades_teachersubject_TeacherID = t.management_teacher_ID
                LEFT JOIN management_person_table tp ON t.management_teacher_PersonID = tp.management_person_ID
                WHERE fg.grades_finalgrade_status = 1 ORDER BY fg.grades_finalgrade_createdDate DESC")).ToList();

            vm.RecentOpportunities = (await connection.QueryAsync<OpportunityDetailItem>(@"
                SELECT ISNULL(p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal,'—') AS StudentName,
                    s.grades_subject_Name AS SubjectName, op.grades_opportunity_Type AS Type,
                    fg.grades_finalgrade_FinalValue AS OriginalGrade, op.grades_opportunity_GradeValue AS OpportunityGrade,
                    op.grades_opportunity_MaxAllowed AS MaxAllowed, ISNULL(op.grades_opportunity_Notes,'') AS Notes
                FROM grades_opportunity_table op
                INNER JOIN grades_finalgrade_table fg ON op.grades_opportunity_FinalGradeID = fg.grades_finalgrade_ID
                INNER JOIN grades_teachersubject_table ts ON fg.grades_finalgrade_TeacherSubjectID = ts.grades_teachersubject_ID
                INNER JOIN grades_subject_table s ON ts.grades_teachersubject_SubjectID = s.grades_subject_ID
                LEFT JOIN management_student_table st ON fg.grades_finalgrade_StudentID = st.management_student_ID
                LEFT JOIN management_person_table p ON st.management_student_PersonID = p.management_person_ID
                WHERE op.grades_opportunity_status = 1 ORDER BY op.grades_opportunity_createdDate DESC")).ToList();

            return vm;
        }
    }
}