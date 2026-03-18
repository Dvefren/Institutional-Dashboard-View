using UTTN.Dashboard.Models.Management;

namespace UTTN.Dashboard.Services.Interfaces
{
    public interface IManagementService
    {
        // Person
        Task<IEnumerable<PersonModel>> GetAllPersonsAsync();
        Task<PersonModel?> GetPersonByIdAsync(int id);
        Task<int> InsertPersonAsync(PersonModel person);
        Task<int> UpdatePersonAsync(PersonModel person);
        Task<int> SoftDeletePersonAsync(int id);

        // Career
        Task<IEnumerable<CareerModel>> GetAllCareersAsync();
        Task<CareerModel?> GetCareerByIdAsync(int id);
        Task<int> InsertCareerAsync(CareerModel career);
        Task<int> UpdateCareerAsync(CareerModel career);
        Task<int> SoftDeleteCareerAsync(int id);

        // Group
        Task<IEnumerable<GroupModel>> GetAllGroupsAsync();
        Task<GroupModel?> GetGroupByIdAsync(int id);
        Task<int> InsertGroupAsync(GroupModel group);
        Task<int> UpdateGroupAsync(GroupModel group);
        Task<int> SoftDeleteGroupAsync(int id);

        // User
        Task<IEnumerable<UserModel>> GetAllUsersAsync();
        Task<UserModel?> GetUserByIdAsync(int id);
        Task<int> InsertUserAsync(UserModel user);
        Task<int> UpdateUserAsync(UserModel user);
        Task<int> SoftDeleteUserAsync(int id);

        // Role
        Task<IEnumerable<RoleModel>> GetAllRolesAsync();
        Task<RoleModel?> GetRoleByIdAsync(int id);
        Task<int> InsertRoleAsync(RoleModel role);
        Task<int> UpdateRoleAsync(RoleModel role);
        Task<int> SoftDeleteRoleAsync(int id);

        // Permission
        Task<IEnumerable<PermissionModel>> GetAllPermissionsAsync();

        // Student
        Task<IEnumerable<StudentModel>> GetAllStudentsAsync();
        Task<StudentModel?> GetStudentByIdAsync(int id);

        // Teacher
        Task<IEnumerable<TeacherModel>> GetAllTeachersAsync();
        Task<TeacherModel?> GetTeacherByIdAsync(int id);
    }
}