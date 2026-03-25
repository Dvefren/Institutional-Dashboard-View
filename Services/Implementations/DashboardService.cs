using Dapper;
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

        public async Task<RectorateViewModel> GetRectorateDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new RectorateViewModel();

            // Available years
            var years = await connection.QueryAsync<int>(@"
                SELECT DISTINCT YEAR(management_student_createdDate) FROM management_student_table WHERE management_student_status = 1
                UNION SELECT DISTINCT YEAR(FechaPreinscripcion) FROM Preinscripciones
                UNION SELECT DISTINCT YEAR(FechaRegistro) FROM Aspirantes
                UNION SELECT DISTINCT YEAR(FechaInscripcion) FROM Inscripciones
                UNION SELECT DISTINCT YEAR(tramites_solicitud_fecha) FROM CE_TramitesSolicitud
                UNION SELECT DISTINCT YEAR(FechaVisita) FROM Visitas
                ORDER BY 1 DESC");
            vm.AvailableYears = years.ToList();
            if (!vm.AvailableYears.Any()) vm.AvailableYears.Add(DateTime.Now.Year);

            vm.SelectedYear = year ?? DateTime.Now.Year;
            vm.SelectedCuatrimestre = cuatrimestre ?? 0;

            // Build month range
            int startMonth = 1, endMonth = 12;
            if (vm.SelectedCuatrimestre > 0)
            {
                startMonth = vm.SelectedCuatrimestre switch { 1 => 1, 2 => 5, 3 => 9, _ => 1 };
                endMonth = vm.SelectedCuatrimestre switch { 1 => 4, 2 => 8, 3 => 12, _ => 12 };
            }

            var filterParams = new { Year = vm.SelectedYear, StartMonth = startMonth, EndMonth = endMonth };

            // ═══════════════════════════════════════
            // KPIs — ALL date-filtered
            // ═══════════════════════════════════════
            vm.TotalStudents = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM management_student_table 
                WHERE management_student_status = 1 
                AND YEAR(management_student_createdDate) = @Year 
                AND MONTH(management_student_createdDate) BETWEEN @StartMonth AND @EndMonth", filterParams);

            vm.ActiveStudents = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM management_student_table 
                WHERE management_student_status = 1 AND management_student_StatusCode = 'INSCRITO'
                AND YEAR(management_student_createdDate) = @Year 
                AND MONTH(management_student_createdDate) BETWEEN @StartMonth AND @EndMonth", filterParams);

            vm.Preinscritos = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM management_student_table 
                WHERE management_student_status = 1 AND management_student_StatusCode = 'PREINSCRITO'
                AND YEAR(management_student_createdDate) = @Year 
                AND MONTH(management_student_createdDate) BETWEEN @StartMonth AND @EndMonth", filterParams);

            vm.TotalTeachers = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_teacher_table WHERE management_teacher_status = 1");

            vm.TotalCareers = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_career_table WHERE management_career_status = 1");

            vm.TotalGroups = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_group_table WHERE management_group_status = 1");

            vm.TotalTramitesPendientes = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM CE_TramitesSolicitud 
                WHERE tramites_solicitud_estatus = 'Pendiente'
                AND YEAR(tramites_solicitud_fecha) = @Year 
                AND MONTH(tramites_solicitud_fecha) BETWEEN @StartMonth AND @EndMonth", filterParams);

            vm.TotalVisitasMedicas = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM Visitas 
                WHERE YEAR(FechaVisita) = @Year 
                AND MONTH(FechaVisita) BETWEEN @StartMonth AND @EndMonth", filterParams);

            vm.Inscritos = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM Inscripciones 
                WHERE YEAR(FechaInscripcion) = @Year 
                AND MONTH(FechaInscripcion) BETWEEN @StartMonth AND @EndMonth", filterParams);

            // ═══════════════════════════════════════
            // Students by career — date-filtered
            // ═══════════════════════════════════════
            var studentsByCareers = await connection.QueryAsync<dynamic>(@"
                SELECT ISNULL(c.management_career_Name, 'Sin carrera') AS CareerName,
                       ISNULL(c.management_career_Code, '—') AS CareerCode, COUNT(*) AS Count
                FROM management_student_table s
                LEFT JOIN management_career_table c ON s.management_student_CareerID = c.management_career_ID
                WHERE s.management_student_status = 1
                  AND YEAR(s.management_student_createdDate) = @Year 
                  AND MONTH(s.management_student_createdDate) BETWEEN @StartMonth AND @EndMonth
                GROUP BY c.management_career_Name, c.management_career_Code ORDER BY Count DESC", filterParams);
            var total = studentsByCareers.Sum(x => (int)x.Count);
            vm.StudentsByCareers = studentsByCareers.Select(x => new CareerStatItem
            {
                CareerName = (string)x.CareerName,
                CareerCode = (string)x.CareerCode,
                Count = (int)x.Count,
                Percentage = total > 0 ? Math.Round((decimal)(int)x.Count / total * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // Students by status — date-filtered
            // ═══════════════════════════════════════
            var studentsByStatus = await connection.QueryAsync<dynamic>(@"
                SELECT management_student_StatusCode AS Status, COUNT(*) AS Count
                FROM management_student_table WHERE management_student_status = 1
                  AND YEAR(management_student_createdDate) = @Year 
                  AND MONTH(management_student_createdDate) BETWEEN @StartMonth AND @EndMonth
                GROUP BY management_student_StatusCode ORDER BY Count DESC", filterParams);
            var totalStatus = studentsByStatus.Sum(x => (int)x.Count);
            vm.StudentsByStatus = studentsByStatus.Select(x => new StatusStatItem
            {
                Status = (string)x.Status,
                Count = (int)x.Count,
                Percentage = totalStatus > 0 ? Math.Round((decimal)(int)x.Count / totalStatus * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // Gender — date-filtered
            // ═══════════════════════════════════════
            var genderData = await connection.QueryAsync<dynamic>(@"
        SELECT ISNULL(p.management_person_Gender, 'No especificado') AS Gender, COUNT(*) AS Count
        FROM management_student_table s
        INNER JOIN management_person_table p ON s.management_student_PersonID = p.management_person_ID
        WHERE s.management_student_status = 1
          AND YEAR(s.management_student_createdDate) = @Year 
          AND MONTH(s.management_student_createdDate) BETWEEN @StartMonth AND @EndMonth
        GROUP BY p.management_person_Gender", filterParams);
            foreach (var g in genderData)
            {
                string gender = ((string)g.Gender).ToLower(); int count = (int)g.Count;
                if (gender.Contains("masculino") || gender.Contains("hombre") || gender == "m") vm.MaleCount += count;
                else if (gender.Contains("femenino") || gender.Contains("mujer") || gender == "f") vm.FemaleCount += count;
                else vm.OtherGenderCount += count;
            }

            // ═══════════════════════════════════════
            // Preinscripciones by career — date-filtered
            // ═══════════════════════════════════════
            var preByCareers = await connection.QueryAsync<dynamic>(@"
        SELECT CarreraSolicitada AS CareerName, COUNT(*) AS Count
        FROM Preinscripciones 
        WHERE YEAR(FechaPreinscripcion) = @Year 
          AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY CarreraSolicitada ORDER BY Count DESC", filterParams);
            var totalPre = preByCareers.Sum(x => (int)x.Count);
            vm.PreinscripcionesByCareer = preByCareers.Select(x => new CareerStatItem
            {
                CareerName = (string)x.CareerName,
                Count = (int)x.Count,
                Percentage = totalPre > 0 ? Math.Round((decimal)(int)x.Count / totalPre * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // Preinscripciones by status — date-filtered
            // ═══════════════════════════════════════
            var preByStatus = await connection.QueryAsync<dynamic>(@"
        SELECT EstadoPreinscripcion AS Status, COUNT(*) AS Count
        FROM Preinscripciones 
        WHERE YEAR(FechaPreinscripcion) = @Year 
          AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY EstadoPreinscripcion ORDER BY Count DESC", filterParams);
            var totalPreStatus = preByStatus.Sum(x => (int)x.Count);
            vm.PreinscripcionesByStatus = preByStatus.Select(x => new StatusStatItem
            {
                Status = (string)x.Status,
                Count = (int)x.Count,
                Percentage = totalPreStatus > 0 ? Math.Round((decimal)(int)x.Count / totalPreStatus * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // Groups overview — shows groups with student count for the filtered period
            // ═══════════════════════════════════════
            vm.GroupsOverview = (await connection.QueryAsync<GroupStatItem>(@"
        SELECT g.management_group_Code AS GroupCode, ISNULL(c.management_career_Name, 'Sin carrera') AS CareerName,
               ISNULL(g.management_group_Shift, '—') AS Shift,
               SUM(CASE WHEN YEAR(s.management_student_createdDate) = @Year 
                         AND MONTH(s.management_student_createdDate) BETWEEN @StartMonth AND @EndMonth THEN 1 ELSE 0 END) AS StudentCount
        FROM management_group_table g
        LEFT JOIN management_career_table c ON g.management_group_CareerID = c.management_career_ID
        LEFT JOIN management_student_table s ON g.management_group_ID = s.management_student_GroupID AND s.management_student_status = 1
        WHERE g.management_group_status = 1
        GROUP BY g.management_group_Code, c.management_career_Name, g.management_group_Shift
        ORDER BY c.management_career_Name, g.management_group_Code", filterParams)).ToList();

            // ═══════════════════════════════════════
            // Careers overview — date-filtered student counts
            // ═══════════════════════════════════════
            vm.CareersOverview = (await connection.QueryAsync<CareerOverviewItem>(@"
        SELECT c.management_career_Name AS CareerName, c.management_career_Code AS CareerCode,
               SUM(CASE WHEN YEAR(s.management_student_createdDate) = @Year 
                         AND MONTH(s.management_student_createdDate) BETWEEN @StartMonth AND @EndMonth THEN 1 ELSE 0 END) AS TotalStudents,
               SUM(CASE WHEN s.management_student_StatusCode = 'INSCRITO' 
                         AND YEAR(s.management_student_createdDate) = @Year 
                         AND MONTH(s.management_student_createdDate) BETWEEN @StartMonth AND @EndMonth THEN 1 ELSE 0 END) AS Inscritos,
               SUM(CASE WHEN s.management_student_StatusCode = 'PREINSCRITO' 
                         AND YEAR(s.management_student_createdDate) = @Year 
                         AND MONTH(s.management_student_createdDate) BETWEEN @StartMonth AND @EndMonth THEN 1 ELSE 0 END) AS Preinscritos,
               SUM(CASE WHEN s.management_student_StatusCode = 'BAJA' 
                         AND YEAR(s.management_student_createdDate) = @Year 
                         AND MONTH(s.management_student_createdDate) BETWEEN @StartMonth AND @EndMonth THEN 1 ELSE 0 END) AS Bajas,
               COUNT(DISTINCT CASE WHEN YEAR(s.management_student_createdDate) = @Year 
                         AND MONTH(s.management_student_createdDate) BETWEEN @StartMonth AND @EndMonth 
                         THEN s.management_student_GroupID ELSE NULL END) AS Groups,
               CAST(0 AS DECIMAL(5,1)) AS Percentage
        FROM management_career_table c
        LEFT JOIN management_student_table s ON c.management_career_ID = s.management_student_CareerID AND s.management_student_status = 1
        WHERE c.management_career_status = 1
        GROUP BY c.management_career_Name, c.management_career_Code ORDER BY TotalStudents DESC", filterParams)).ToList();
            var totalCareerStudents = vm.CareersOverview.Sum(x => x.TotalStudents);
            foreach (var c in vm.CareersOverview)
                c.Percentage = totalCareerStudents > 0 ? Math.Round((decimal)c.TotalStudents / totalCareerStudents * 100, 1) : 0;

            // ═══════════════════════════════════════
            // Monthly preinscripciones — for selected year
            // ═══════════════════════════════════════
            vm.MonthlyPreinscripciones = (await connection.QueryAsync<MonthlyStatItem>(@"
        SELECT FORMAT(FechaPreinscripcion,'MMM','es-MX') AS Month, YEAR(FechaPreinscripcion) AS Year, COUNT(*) AS Count
        FROM Preinscripciones WHERE YEAR(FechaPreinscripcion) = @Year
        GROUP BY FORMAT(FechaPreinscripcion,'MMM','es-MX'), YEAR(FechaPreinscripcion), MONTH(FechaPreinscripcion)
        ORDER BY YEAR(FechaPreinscripcion), MONTH(FechaPreinscripcion)",
                new { Year = vm.SelectedYear })).ToList();

            // Career change history
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

            // Group change history
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

            return vm;
        }

        public async Task<AdmissionsViewModel> GetAdmissionsDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new AdmissionsViewModel();

            // Available years
            var years = await connection.QueryAsync<int>(@"
        SELECT DISTINCT YEAR(FechaPreinscripcion) FROM Preinscripciones
        UNION SELECT DISTINCT YEAR(FechaInscripcion) FROM Inscripciones
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
            vm.TotalPreinscripciones = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM Preinscripciones WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth", fp);
            vm.TotalInscripciones = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM Inscripciones WHERE YEAR(FechaInscripcion) = @Year AND MONTH(FechaInscripcion) BETWEEN @StartMonth AND @EndMonth", fp);
            vm.ConversionRate = vm.TotalPreinscripciones > 0 ? Math.Round((decimal)vm.TotalInscripciones / vm.TotalPreinscripciones * 100, 1) : 0;
            vm.PromedioGeneral = await connection.ExecuteScalarAsync<decimal?>(
                @"SELECT AVG(Promedio) FROM Preinscripciones WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth", fp) ?? 0;
            vm.PromedioGeneral = Math.Round(vm.PromedioGeneral, 2);

            // By Career
            var byCareers = await connection.QueryAsync<dynamic>(@"
        SELECT CarreraSolicitada AS CareerName, COUNT(*) AS Count FROM Preinscripciones
        WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY CarreraSolicitada ORDER BY Count DESC", fp);
            var totalC = byCareers.Sum(x => (int)x.Count);
            vm.PreinscripcionesByCareer = byCareers.Select(x => new CareerStatItem { CareerName = (string)x.CareerName, Count = (int)x.Count, Percentage = totalC > 0 ? Math.Round((decimal)(int)x.Count / totalC * 100, 1) : 0 }).ToList();

            // By Status (Preinscripciones)
            var byStatus = await connection.QueryAsync<dynamic>(@"
        SELECT EstadoPreinscripcion AS Status, COUNT(*) AS Count FROM Preinscripciones
        WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY EstadoPreinscripcion ORDER BY Count DESC", fp);
            var totalS = byStatus.Sum(x => (int)x.Count);
            vm.PreinscripcionesByStatus = byStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalS > 0 ? Math.Round((decimal)(int)x.Count / totalS * 100, 1) : 0 }).ToList();

            // By Status (Inscripciones)
            var byInsStatus = await connection.QueryAsync<dynamic>(@"
        SELECT EstadoInscripcion AS Status, COUNT(*) AS Count FROM Inscripciones
        WHERE YEAR(FechaInscripcion) = @Year AND MONTH(FechaInscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY EstadoInscripcion ORDER BY Count DESC", fp);
            var totalIS = byInsStatus.Sum(x => (int)x.Count);
            vm.InscripcionesByStatus = byInsStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalIS > 0 ? Math.Round((decimal)(int)x.Count / totalIS * 100, 1) : 0 }).ToList();

            // Geographic
            var byEstado = await connection.QueryAsync<dynamic>(@"
        SELECT d.Estado AS Name, COUNT(*) AS Count FROM PreinscripcionDomicilio d
        INNER JOIN Preinscripciones p ON d.PreinscripcionId = p.Id
        WHERE YEAR(p.FechaPreinscripcion) = @Year AND MONTH(p.FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY d.Estado ORDER BY Count DESC", fp);
            var totalE = byEstado.Sum(x => (int)x.Count);
            vm.ByEstado = byEstado.Select(x => new GeoStatItem { Name = (string)x.Name, Count = (int)x.Count, Percentage = totalE > 0 ? Math.Round((decimal)(int)x.Count / totalE * 100, 1) : 0 }).ToList();

            var byMun = await connection.QueryAsync<dynamic>(@"
        SELECT TOP 10 d.Municipio AS Name, COUNT(*) AS Count FROM PreinscripcionDomicilio d
        INNER JOIN Preinscripciones p ON d.PreinscripcionId = p.Id
        WHERE YEAR(p.FechaPreinscripcion) = @Year AND MONTH(p.FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY d.Municipio ORDER BY Count DESC", fp);
            var totalM = byMun.Sum(x => (int)x.Count);
            vm.ByMunicipio = byMun.Select(x => new GeoStatItem { Name = (string)x.Name, Count = (int)x.Count, Percentage = totalM > 0 ? Math.Round((decimal)(int)x.Count / totalM * 100, 1) : 0 }).ToList();

            // Top Escuelas
            vm.TopEscuelas = (await connection.QueryAsync<EscuelaStatItem>(@"
        SELECT TOP 10 e.EscuelaProcedencia AS EscuelaNombre, ISNULL(e.EstadoEscuela,'—') AS Estado, COUNT(*) AS Count
        FROM PreinscripcionEscolar e INNER JOIN Preinscripciones p ON e.PreinscripcionId = p.Id
        WHERE YEAR(p.FechaPreinscripcion) = @Year AND MONTH(p.FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY e.EscuelaProcedencia, e.EstadoEscuela ORDER BY Count DESC", fp)).ToList();

            // Medio Difusion
            var byMedio = await connection.QueryAsync<dynamic>(@"
        SELECT ISNULL(MedioDifusion,'No especificado') AS Status, COUNT(*) AS Count FROM Preinscripciones
        WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY MedioDifusion ORDER BY Count DESC", fp);
            var totalMd = byMedio.Sum(x => (int)x.Count);
            vm.ByMedioDifusion = byMedio.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalMd > 0 ? Math.Round((decimal)(int)x.Count / totalMd * 100, 1) : 0 }).ToList();

            // Gender
            var genderData = await connection.QueryAsync<dynamic>(@"
        SELECT d.Sexo AS Gender, COUNT(*) AS Count FROM PreinscripcionDatosPersonales d
        INNER JOIN Preinscripciones p ON d.PreinscripcionId = p.Id
        WHERE YEAR(p.FechaPreinscripcion) = @Year AND MONTH(p.FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY d.Sexo", fp);
            foreach (var g in genderData)
            {
                string gender = ((string)g.Gender).ToLower(); int count = (int)g.Count;
                if (gender.Contains("masculino") || gender == "m") vm.MaleCount += count;
                else if (gender.Contains("femenino") || gender == "f") vm.FemaleCount += count;
                else vm.OtherGenderCount += count;
            }

            // Social indicators
            vm.TotalSaludRecords = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PreinscripcionSalud");
            vm.ConDiscapacidad = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PreinscripcionSalud WHERE TieneDiscapacidad = 1");
            vm.ComunidadIndigena = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PreinscripcionSalud WHERE ComunidadIndigena = 1");
            vm.ConHijos = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PreinscripcionSalud WHERE TieneHijos = 1");

            // Promedio distribution
            vm.PromedioDistribution = (await connection.QueryAsync<PromedioRangeItem>(@"
        SELECT CASE WHEN Promedio >= 9.0 THEN '9.0 — 10.0' WHEN Promedio >= 8.0 THEN '8.0 — 8.9' WHEN Promedio >= 7.0 THEN '7.0 — 7.9' WHEN Promedio >= 6.0 THEN '6.0 — 6.9' ELSE 'Menor a 6.0' END AS Range, COUNT(*) AS Count
        FROM Preinscripciones WHERE YEAR(FechaPreinscripcion) = @Year AND MONTH(FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        GROUP BY CASE WHEN Promedio >= 9.0 THEN '9.0 — 10.0' WHEN Promedio >= 8.0 THEN '8.0 — 8.9' WHEN Promedio >= 7.0 THEN '7.0 — 7.9' WHEN Promedio >= 6.0 THEN '6.0 — 6.9' ELSE 'Menor a 6.0' END ORDER BY Range DESC", fp)).ToList();

            // Recent
            vm.RecentPreinscripciones = (await connection.QueryAsync<PreinscripcionDetailItem>(@"
        SELECT TOP 50 ISNULL(p.Folio,'—') AS Folio, ISNULL(d.Nombre + ' ' + d.ApellidoPaterno,'—') AS Nombre,
            p.CarreraSolicitada AS Carrera, p.Promedio, ISNULL(dom.Estado,'—') AS Estado, p.EstadoPreinscripcion AS Estatus, p.FechaPreinscripcion AS Fecha
        FROM Preinscripciones p LEFT JOIN PreinscripcionDatosPersonales d ON p.Id = d.PreinscripcionId
        LEFT JOIN PreinscripcionDomicilio dom ON p.Id = dom.PreinscripcionId
        WHERE YEAR(p.FechaPreinscripcion) = @Year AND MONTH(p.FechaPreinscripcion) BETWEEN @StartMonth AND @EndMonth
        ORDER BY p.FechaPreinscripcion DESC", fp)).ToList();

            // Monthly trends (full year)
            vm.MonthlyPreinscripciones = (await connection.QueryAsync<MonthlyStatItem>(@"
        SELECT FORMAT(FechaPreinscripcion,'MMM','es-MX') AS Month, YEAR(FechaPreinscripcion) AS Year, COUNT(*) AS Count
        FROM Preinscripciones WHERE YEAR(FechaPreinscripcion) = @Year
        GROUP BY FORMAT(FechaPreinscripcion,'MMM','es-MX'), YEAR(FechaPreinscripcion), MONTH(FechaPreinscripcion)
        ORDER BY YEAR(FechaPreinscripcion), MONTH(FechaPreinscripcion)", new { Year = vm.SelectedYear })).ToList();

            vm.MonthlyInscripciones = (await connection.QueryAsync<MonthlyStatItem>(@"
        SELECT FORMAT(FechaInscripcion,'MMM','es-MX') AS Month, YEAR(FechaInscripcion) AS Year, COUNT(*) AS Count
        FROM Inscripciones WHERE YEAR(FechaInscripcion) = @Year
        GROUP BY FORMAT(FechaInscripcion,'MMM','es-MX'), YEAR(FechaInscripcion), MONTH(FechaInscripcion)
        ORDER BY YEAR(FechaInscripcion), MONTH(FechaInscripcion)", new { Year = vm.SelectedYear })).ToList();

            return vm;
        }

        public async Task<TramitesViewModel> GetTramitesDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new TramitesViewModel();

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

            // KPIs
            vm.TotalSolicitudes = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE {df}", fp);
            vm.Pendientes = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus = 'Pendiente' AND {df}", fp);
            vm.Completadas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada','Entregado','Entregada') AND {df}", fp);
            vm.Rechazadas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus IN ('Rechazado','Rechazada') AND {df}", fp);
            vm.TasaCompletado = vm.TotalSolicitudes > 0 ? Math.Round((decimal)vm.Completadas / vm.TotalSolicitudes * 100, 1) : 0;

            var avgDays = await connection.ExecuteScalarAsync<double?>($"SELECT AVG(CAST(DATEDIFF(DAY, tramites_solicitud_fecha, GETDATE()) AS FLOAT)) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada') AND {df}", fp);
            vm.PromedioResolucionDias = Math.Round(avgDays ?? 0, 1);

            // By Status
            var byStatus = await connection.QueryAsync<dynamic>($"SELECT ISNULL(tramites_solicitud_estatus,'Pendiente') AS Status, COUNT(*) AS Count FROM CE_TramitesSolicitud WHERE {df} GROUP BY tramites_solicitud_estatus ORDER BY Count DESC", fp);
            var totalS = byStatus.Sum(x => (int)x.Count);
            vm.ByStatus = byStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalS > 0 ? Math.Round((decimal)(int)x.Count / totalS * 100, 1) : 0 }).ToList();

            // By Type
            vm.ByTipoTramite = (await connection.QueryAsync<TramiteTipoItem>($@"
        SELECT t.nombre_tramite AS TipoNombre, COUNT(*) AS Total,
            SUM(CASE WHEN s.tramites_solicitud_estatus = 'Pendiente' THEN 1 ELSE 0 END) AS Pendientes,
            SUM(CASE WHEN s.tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada','Entregado','Entregada') THEN 1 ELSE 0 END) AS Completadas,
            SUM(CASE WHEN s.tramites_solicitud_estatus IN ('Rechazado','Rechazada') THEN 1 ELSE 0 END) AS Rechazadas
        FROM CE_TramitesSolicitud s INNER JOIN CE_TramitesCategoria t ON s.id_tramite = t.id_tramite
        WHERE {df} GROUP BY t.nombre_tramite ORDER BY Total DESC", fp)).ToList();

            // Documents
            vm.DocsAprobados = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CE_TramitesDetalleDocumentos WHERE estatus_documento IN ('Aprobado','Aprobada','Validado','Validada')");
            vm.DocsPendientes = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CE_TramitesDetalleDocumentos WHERE estatus_documento = 'Pendiente'");
            vm.DocsRechazados = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CE_TramitesDetalleDocumentos WHERE estatus_documento IN ('Rechazado','Rechazada')");

            // Monthly trend (full year)
            vm.MonthlyTrend = (await connection.QueryAsync<MonthlyStatItem>(@"
        SELECT FORMAT(tramites_solicitud_fecha,'MMM','es-MX') AS Month, YEAR(tramites_solicitud_fecha) AS Year, COUNT(*) AS Count
        FROM CE_TramitesSolicitud WHERE YEAR(tramites_solicitud_fecha) = @Year
        GROUP BY FORMAT(tramites_solicitud_fecha,'MMM','es-MX'), YEAR(tramites_solicitud_fecha), MONTH(tramites_solicitud_fecha)
        ORDER BY YEAR(tramites_solicitud_fecha), MONTH(tramites_solicitud_fecha)", new { Year = vm.SelectedYear })).ToList();

            // Recent
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

            // Oldest pending
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

        public async Task<AspirantesViewModel> GetAspirantesDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new AspirantesViewModel();

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

            // KPIs
            vm.TotalAspirantes = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE {df}", fp);
            vm.Aceptados = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Aceptado' AND {df}", fp);
            vm.Pendientes = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Pendiente' AND {df}", fp);
            vm.DocIncompleta = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Documentación Incompleta' AND {df}", fp);
            vm.FichasPagadas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Ficha Pagada' AND {df}", fp);
            vm.ExamenPresentado = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Examen Presentado' AND {df}", fp);
            vm.Rechazados = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Aspirantes a WHERE EstadoRegistro = 'Rechazado' AND {df}", fp);
            vm.PromedioGeneral = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(e.Promedio) FROM AspiranteEscolar e INNER JOIN Aspirantes a ON e.Folio = a.Folio WHERE {df}", fp) ?? 0;
            vm.PromedioGeneral = Math.Round(vm.PromedioGeneral, 2);

            // By Status
            var byStatus = await connection.QueryAsync<dynamic>($"SELECT EstadoRegistro AS Status, COUNT(*) AS Count FROM Aspirantes a WHERE {df} GROUP BY EstadoRegistro ORDER BY Count DESC", fp);
            var totalSt = byStatus.Sum(x => (int)x.Count);
            vm.ByStatus = byStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalSt > 0 ? Math.Round((decimal)(int)x.Count / totalSt * 100, 1) : 0 }).ToList();

            // By Career
            var byCareer = await connection.QueryAsync<dynamic>($"SELECT CarreraSolicitada AS CareerName, COUNT(*) AS Count FROM Aspirantes a WHERE {df} GROUP BY CarreraSolicitada ORDER BY Count DESC", fp);
            var totalCr = byCareer.Sum(x => (int)x.Count);
            vm.ByCareer = byCareer.Select(x => new CareerStatItem { CareerName = (string)x.CareerName, Count = (int)x.Count, Percentage = totalCr > 0 ? Math.Round((decimal)(int)x.Count / totalCr * 100, 1) : 0 }).ToList();

            // Gender
            var gender = await connection.QueryAsync<dynamic>($"SELECT d.Sexo AS Gender, COUNT(*) AS Count FROM AspiranteDatosGenerales d INNER JOIN Aspirantes a ON d.Folio = a.Folio WHERE {df} GROUP BY d.Sexo", fp);
            foreach (var g in gender) { string gen = ((string)g.Gender).ToLower(); int cnt = (int)g.Count; if (gen.Contains("masculino") || gen == "m") vm.MaleCount += cnt; else if (gen.Contains("femenino") || gen == "f") vm.FemaleCount += cnt; }

            // Geographic
            var byMun = await connection.QueryAsync<dynamic>($"SELECT TOP 10 dom.Municipio AS Name, COUNT(*) AS Count FROM AspiranteDomicilio dom INNER JOIN Aspirantes a ON dom.Folio = a.Folio WHERE {df} GROUP BY dom.Municipio ORDER BY Count DESC", fp);
            var totalM = byMun.Sum(x => (int)x.Count);
            vm.ByMunicipio = byMun.Select(x => new GeoStatItem { Name = (string)x.Name, Count = (int)x.Count, Percentage = totalM > 0 ? Math.Round((decimal)(int)x.Count / totalM * 100, 1) : 0 }).ToList();

            // Top Escuelas
            vm.TopEscuelas = (await connection.QueryAsync<EscuelaStatItem>($"SELECT TOP 10 e.EscuelaProcedencia AS EscuelaNombre, ISNULL(e.EstadoEscuela,'—') AS Estado, COUNT(*) AS Count FROM AspiranteEscolar e INNER JOIN Aspirantes a ON e.Folio = a.Folio WHERE {df} GROUP BY e.EscuelaProcedencia, e.EstadoEscuela ORDER BY Count DESC", fp)).ToList();

            // Tipo Prepa
            var byTipo = await connection.QueryAsync<dynamic>($"SELECT e.TipoPreparatoria AS Status, COUNT(*) AS Count FROM AspiranteEscolar e INNER JOIN Aspirantes a ON e.Folio = a.Folio WHERE {df} GROUP BY e.TipoPreparatoria ORDER BY Count DESC", fp);
            var totalT = byTipo.Sum(x => (int)x.Count);
            vm.ByTipoPrepa = byTipo.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalT > 0 ? Math.Round((decimal)(int)x.Count / totalT * 100, 1) : 0 }).ToList();

            // Medio difusion
            var byMedio = await connection.QueryAsync<dynamic>($"SELECT ISNULL(o.ComoSeEntero,'No especificado') AS Status, COUNT(*) AS Count FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE {df} GROUP BY o.ComoSeEntero ORDER BY Count DESC", fp);
            var totalMd = byMedio.Sum(x => (int)x.Count);
            vm.ByMedioDifusion = byMedio.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalMd > 0 ? Math.Round((decimal)(int)x.Count / totalMd * 100, 1) : 0 }).ToList();

            // Social indicators
            vm.TotalOtros = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE {df}", fp);
            vm.Trabajan = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.Trabaja = 1 AND {df}", fp);
            vm.ConBeca = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.ContabaConBeca = 1 AND {df}", fp);
            vm.OrigenIndigena = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.OrigenIndigena = 1 AND {df}", fp);
            vm.ConDiscapacidad = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.PadeceDiscapacidad = 1 AND {df}", fp);
            vm.ConEnfermedad = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM AspiranteOtros o INNER JOIN Aspirantes a ON o.Folio = a.Folio WHERE o.PadeceEnfermedad = 1 AND {df}", fp);

            // Recent
            vm.RecentAspirantes = (await connection.QueryAsync<AspiranteDetailItem>($@"
        SELECT TOP 50 a.Folio, d.Nombre + ' ' + d.ApellidoPaterno AS Nombre, a.CarreraSolicitada AS Carrera,
            e.Promedio, e.EscuelaProcedencia AS Preparatoria, dom.Estado, a.EstadoRegistro AS Estatus, a.FechaRegistro AS Fecha
        FROM Aspirantes a LEFT JOIN AspiranteDatosGenerales d ON a.Folio = d.Folio
        LEFT JOIN AspiranteEscolar e ON a.Folio = e.Folio LEFT JOIN AspiranteDomicilio dom ON a.Folio = dom.Folio
        WHERE {df} ORDER BY a.FechaRegistro DESC", fp)).ToList();

            return vm;
        }

        public async Task<MedicalViewModel> GetMedicalDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new MedicalViewModel();

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

            // KPIs
            vm.TotalVisitas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Visitas WHERE {dfV}", fp);
            vm.TotalPsicologicas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM VisitasPsicologicas WHERE {dfV}", fp);
            vm.ConAlergias = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Visitas WHERE TieneAlergias = 1 AND {dfV}", fp);
            vm.ConEnfermedadesCronicas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Visitas WHERE EnfermedadesCronicas IS NOT NULL AND EnfermedadesCronicas != '' AND {dfV}", fp);
            vm.ConTerapiaPrevia = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM VisitasPsicologicas WHERE TerapiaPrevia = 1 AND {dfV}", fp);
            vm.ConMedicacion = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM VisitasPsicologicas WHERE MedicacionPsiquiatrica IS NOT NULL AND MedicacionPsiquiatrica != '' AND {dfV}", fp);
            vm.PromedioEdad = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(CAST(Edad AS DECIMAL)) FROM Visitas WHERE {dfV}", fp) ?? 0;
            vm.PromedioEdad = Math.Round(vm.PromedioEdad, 1);

            // Vital signs
            vm.PromedioTemperatura = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(CAST(Temperatura AS DECIMAL(5,1))) FROM Visitas WHERE Temperatura IS NOT NULL AND {dfV}", fp) ?? 0;
            vm.PromedioTemperatura = Math.Round(vm.PromedioTemperatura, 1);
            vm.PromedioIMC = await connection.ExecuteScalarAsync<decimal?>($"SELECT AVG(CAST(Peso / (Talla * Talla) AS DECIMAL(5,1))) FROM Visitas WHERE Talla > 0 AND Peso > 0 AND {dfV}", fp) ?? 0;
            vm.PromedioIMC = Math.Round(vm.PromedioIMC, 1);

            // Top diagnosticos
            var diag = await connection.QueryAsync<dynamic>($"SELECT TOP 15 Diagnostico AS Status, COUNT(*) AS Count FROM Visitas WHERE {dfV} GROUP BY Diagnostico ORDER BY Count DESC", fp);
            var totalD = diag.Sum(x => (int)x.Count);
            vm.TopDiagnosticos = diag.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalD > 0 ? Math.Round((decimal)(int)x.Count / totalD * 100, 1) : 0 }).ToList();

            // Top motivos psicologicos
            var motivos = await connection.QueryAsync<dynamic>($"SELECT TOP 15 MotivoConsulta AS Status, COUNT(*) AS Count FROM VisitasPsicologicas WHERE {dfV} GROUP BY MotivoConsulta ORDER BY Count DESC", fp);
            var totalMo = motivos.Sum(x => (int)x.Count);
            vm.TopMotivosPsicologicos = motivos.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalMo > 0 ? Math.Round((decimal)(int)x.Count / totalMo * 100, 1) : 0 }).ToList();

            // By age range
            vm.ByEdad = (await connection.QueryAsync<PromedioRangeItem>($@"
        SELECT CASE WHEN Edad < 18 THEN 'Menor de 18' WHEN Edad <= 20 THEN '18 — 20' WHEN Edad <= 23 THEN '21 — 23' WHEN Edad <= 25 THEN '24 — 25' ELSE '26+' END AS Range, COUNT(*) AS Count
        FROM Visitas WHERE {dfV}
        GROUP BY CASE WHEN Edad < 18 THEN 'Menor de 18' WHEN Edad <= 20 THEN '18 — 20' WHEN Edad <= 23 THEN '21 — 23' WHEN Edad <= 25 THEN '24 — 25' ELSE '26+' END
        ORDER BY Range", fp)).ToList();

            // Monthly
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

            // Recent visitas
            vm.RecentVisitas = (await connection.QueryAsync<VisitaDetailItem>($@"
        SELECT TOP 50 Id, Matricula, FechaVisita, Edad, Diagnostico,
            ISNULL(CAST(Temperatura AS VARCHAR),'—') AS Temperatura,
            ISNULL(PresionArterial,'—') AS PresionArterial,
            ISNULL(Saturacion,'—') AS Saturacion,
            TieneAlergias, ISNULL(EspecificarAlergia,'') AS Alergias
        FROM Visitas WHERE {dfV} ORDER BY FechaVisita DESC", fp)).ToList();

            // Recent psicologicas
            vm.RecentPsicologicas = (await connection.QueryAsync<PsicologicaDetailItem>($@"
        SELECT TOP 50 Id, Matricula, FechaVisita, Edad, MotivoConsulta,
            TerapiaPrevia, ISNULL(MedicacionPsiquiatrica,'') AS Medicacion
        FROM VisitasPsicologicas WHERE {dfV} ORDER BY FechaVisita DESC", fp)).ToList();

            return vm;
        }

        public async Task<VinculacionViewModel> GetVinculacionDataAsync(int? year = null, int? cuatrimestre = null)
        {
            using var connection = _context.CreateConnection();
            var vm = new VinculacionViewModel();

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

            // KPIs
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

            // By Status
            var byStatus = await connection.QueryAsync<dynamic>($"SELECT operational_studentassignment_StatusCode AS Status, COUNT(*) AS Count FROM operational_studentassignment_table sa WHERE {df} GROUP BY operational_studentassignment_StatusCode ORDER BY Count DESC", fp);
            var totalSt = byStatus.Sum(x => (int)x.Count);
            vm.ByStatus = byStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalSt > 0 ? Math.Round((decimal)(int)x.Count / totalSt * 100, 1) : 0 }).ToList();

            // By Program Type
            var byType = await connection.QueryAsync<dynamic>($@"
        SELECT p.operational_program_Type AS Status, COUNT(*) AS Count
        FROM operational_studentassignment_table sa
        INNER JOIN operational_program_table p ON sa.operational_studentassignment_ProgramID = p.operational_program_ID
        WHERE {df} GROUP BY p.operational_program_Type ORDER BY Count DESC", fp);
            var totalTy = byType.Sum(x => (int)x.Count);
            vm.ByProgramType = byType.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalTy > 0 ? Math.Round((decimal)(int)x.Count / totalTy * 100, 1) : 0 }).ToList();

            // By Organization
            var byOrg = await connection.QueryAsync<dynamic>($@"
        SELECT TOP 10 o.operational_organization_Name AS Status, COUNT(*) AS Count
        FROM operational_studentassignment_table sa
        INNER JOIN operational_organization_table o ON sa.operational_studentassignment_OrganizationID = o.operational_organization_ID
        WHERE {df} GROUP BY o.operational_organization_Name ORDER BY Count DESC", fp);
            var totalOr = byOrg.Sum(x => (int)x.Count);
            vm.ByOrganization = byOrg.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalOr > 0 ? Math.Round((decimal)(int)x.Count / totalOr * 100, 1) : 0 }).ToList();

            // Documents by status
            var docStatus = await connection.QueryAsync<dynamic>("SELECT operational_document_StatusCode AS Status, COUNT(*) AS Count FROM operational_document_table GROUP BY operational_document_StatusCode ORDER BY Count DESC");
            var totalDs = docStatus.Sum(x => (int)x.Count);
            vm.DocsByStatus = docStatus.Select(x => new StatusStatItem { Status = (string)x.Status, Count = (int)x.Count, Percentage = totalDs > 0 ? Math.Round((decimal)(int)x.Count / totalDs * 100, 1) : 0 }).ToList();

            // Programs list
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

            // Organizations list
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

            // Recent assignments
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
            d.operational_document_Title AS Title,
            d.operational_document_DocumentType AS DocumentType,
            ISNULL(d.operational_document_FileName,'—') AS FileName,
            d.operational_document_StatusCode AS Status,
            ISNULL(per.management_person_FirstName + ' ' + per.management_person_LastNamePaternal,'—') AS StudentName,
            ISNULL(p.operational_program_Name,'—') AS ProgramName,
            d.operational_document_UploadDate AS UploadDate,
            ISNULL(d.operational_document_ReviewComments,'') AS ReviewComments
        FROM operational_document_table d
        INNER JOIN operational_studentassignment_table sa ON d.operational_document_AssignmentID = sa.operational_studentassignment_ID
        INNER JOIN operational_program_table p ON sa.operational_studentassignment_ProgramID = p.operational_program_ID
        LEFT JOIN management_student_table s ON sa.operational_studentassignment_StudentID = s.management_student_ID
        LEFT JOIN management_person_table per ON s.management_student_PersonID = per.management_person_ID
        WHERE d.operational_document_status = 1
        ORDER BY d.operational_document_UploadDate DESC")).ToList();

            return vm;
        }
    }
}