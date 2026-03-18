using Dapper;
using System.Data;
using UTTN.Dashboard.Data;
using UTTN.Dashboard.Models.Management;
using UTTN.Dashboard.Services.Interfaces;

namespace UTTN.Dashboard.Services.Implementations
{
    public class ManagementService : IManagementService
    {
        private readonly DapperContext _context;

        public ManagementService(DapperContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════
        // PERSON
        // ═══════════════════════════════════════
        public async Task<IEnumerable<PersonModel>> GetAllPersonsAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<PersonModel>(
                "sp_management",
                new { Option = "management_person_get" },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<PersonModel?> GetPersonByIdAsync(int id)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<PersonModel>(
                "sp_management",
                new { Option = "management_person_getbyid", ID = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> InsertPersonAsync(PersonModel person)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_person_insert",
                    FirstName = person.management_person_FirstName,
                    LastNamePaternal = person.management_person_LastNamePaternal,
                    LastNameMaternal = person.management_person_LastNameMaternal,
                    BirthDate = person.management_person_BirthDate,
                    Gender = person.management_person_Gender,
                    CURP = person.management_person_CURP,
                    Email = person.management_person_Email,
                    Phone = person.management_person_Phone
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.management_person_ID ?? 0);
        }

        public async Task<int> UpdatePersonAsync(PersonModel person)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_person_update",
                    ID = person.management_person_ID,
                    FirstName = person.management_person_FirstName,
                    LastNamePaternal = person.management_person_LastNamePaternal,
                    LastNameMaternal = person.management_person_LastNameMaternal,
                    BirthDate = person.management_person_BirthDate,
                    Gender = person.management_person_Gender,
                    CURP = person.management_person_CURP,
                    Email = person.management_person_Email,
                    Phone = person.management_person_Phone
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        public async Task<int> SoftDeletePersonAsync(int id)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new { Option = "management_person_softdelete", ID = id },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        // ═══════════════════════════════════════
        // CAREER
        // ═══════════════════════════════════════
        public async Task<IEnumerable<CareerModel>> GetAllCareersAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<CareerModel>(
                "sp_management",
                new { Option = "management_career_get" },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<CareerModel?> GetCareerByIdAsync(int id)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<CareerModel>(
                "sp_management",
                new { Option = "management_career_getbyid", ID = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> InsertCareerAsync(CareerModel career)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_career_insert",
                    CareerCode = career.management_career_Code,
                    CareerName = career.management_career_Name
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.management_career_ID ?? 0);
        }

        public async Task<int> UpdateCareerAsync(CareerModel career)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_career_update",
                    ID = career.management_career_ID,
                    CareerCode = career.management_career_Code,
                    CareerName = career.management_career_Name
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        public async Task<int> SoftDeleteCareerAsync(int id)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new { Option = "management_career_softdelete", ID = id },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        // ═══════════════════════════════════════
        // GROUP
        // ═══════════════════════════════════════
        public async Task<IEnumerable<GroupModel>> GetAllGroupsAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<GroupModel>(
                "sp_management",
                new { Option = "management_group_get" },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<GroupModel?> GetGroupByIdAsync(int id)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<GroupModel>(
                "sp_management",
                new { Option = "management_group_getbyid", ID = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> InsertGroupAsync(GroupModel group)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_group_insert",
                    GroupCareerID = group.management_group_CareerID,
                    GroupCode = group.management_group_Code,
                    GroupName = group.management_group_Name,
                    GroupShift = group.management_group_Shift
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.management_group_ID ?? 0);
        }

        public async Task<int> UpdateGroupAsync(GroupModel group)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_group_update",
                    ID = group.management_group_ID,
                    GroupCareerID = group.management_group_CareerID,
                    GroupCode = group.management_group_Code,
                    GroupName = group.management_group_Name,
                    GroupShift = group.management_group_Shift
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        public async Task<int> SoftDeleteGroupAsync(int id)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new { Option = "management_group_softdelete", ID = id },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        // ═══════════════════════════════════════
        // USER
        // ═══════════════════════════════════════
        public async Task<IEnumerable<UserModel>> GetAllUsersAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<UserModel>(
                "sp_management",
                new { Option = "management_user_get" },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<UserModel?> GetUserByIdAsync(int id)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<UserModel>(
                "sp_management",
                new { Option = "management_user_getbyid", ID = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> InsertUserAsync(UserModel user)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_user_insert",
                    UserPersonID = user.management_user_PersonID,
                    Username = user.management_user_Username,
                    UserEmail = user.management_user_Email,
                    PasswordHash = user.management_user_PasswordHash,
                    IsLocked = user.management_user_IsLocked,
                    LockReason = user.management_user_LockReason
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.management_user_ID ?? 0);
        }

        public async Task<int> UpdateUserAsync(UserModel user)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_user_update",
                    ID = user.management_user_ID,
                    UserPersonID = user.management_user_PersonID,
                    Username = user.management_user_Username,
                    UserEmail = user.management_user_Email,
                    PasswordHash = user.management_user_PasswordHash,
                    IsLocked = user.management_user_IsLocked,
                    LockReason = user.management_user_LockReason
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        public async Task<int> SoftDeleteUserAsync(int id)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new { Option = "management_user_softdelete", ID = id },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        // ═══════════════════════════════════════
        // ROLE
        // ═══════════════════════════════════════
        public async Task<IEnumerable<RoleModel>> GetAllRolesAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<RoleModel>(
                "sp_management",
                new { Option = "management_role_get" },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<RoleModel?> GetRoleByIdAsync(int id)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<RoleModel>(
                "sp_management",
                new { Option = "management_role_getbyid", ID = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> InsertRoleAsync(RoleModel role)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_role_insert",
                    RoleName = role.management_role_Name,
                    RoleDescription = role.management_role_Description
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.management_role_ID ?? 0);
        }

        public async Task<int> UpdateRoleAsync(RoleModel role)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new
                {
                    Option = "management_role_update",
                    ID = role.management_role_ID,
                    RoleName = role.management_role_Name,
                    RoleDescription = role.management_role_Description
                },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        public async Task<int> SoftDeleteRoleAsync(int id)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new { Option = "management_role_softdelete", ID = id },
                commandType: CommandType.StoredProcedure
            );
            return (int)(result?.affected_rows ?? 0);
        }

        // ═══════════════════════════════════════
        // PERMISSION
        // ═══════════════════════════════════════
        public async Task<IEnumerable<PermissionModel>> GetAllPermissionsAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<PermissionModel>(
                "sp_management",
                new { Option = "management_permission_get" },
                commandType: CommandType.StoredProcedure
            );
        }

        // ═══════════════════════════════════════
        // STUDENT
        // ═══════════════════════════════════════
        public async Task<IEnumerable<StudentModel>> GetAllStudentsAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<StudentModel>(
                "sp_management",
                new { Option = "management_student_get" },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<StudentModel?> GetStudentByIdAsync(int id)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<StudentModel>(
                "sp_management",
                new { Option = "management_student_getbyid", ID = id },
                commandType: CommandType.StoredProcedure
            );
        }

        // ═══════════════════════════════════════
        // TEACHER
        // ═══════════════════════════════════════
        public async Task<IEnumerable<TeacherModel>> GetAllTeachersAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<TeacherModel>(
                "sp_management",
                new { Option = "management_teacher_get" },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<TeacherModel?> GetTeacherByIdAsync(int id)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<TeacherModel>(
                "sp_management",
                new { Option = "management_teacher_getbyid", ID = id },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}