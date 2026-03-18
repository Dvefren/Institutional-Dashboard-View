using System.ComponentModel.DataAnnotations;

namespace UTTN.Dashboard.Models.Auth
{
    public class LoginModel
    {
        [Required(ErrorMessage = "El usuario es requerido")]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}