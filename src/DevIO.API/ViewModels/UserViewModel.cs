using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DevIO.Api.ViewModels
{
 
    /// <summary>
    /// Utilizada para cadastrar novo usuário
    /// </summary>
    public class RegisterUserViewModel
    {
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [EmailAddress(ErrorMessage = "O campo {0} está em formato inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [StringLength(100, ErrorMessage = "O campo {0} precisa ter entre {2} e {1} caracteres", MinimumLength = 6)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "As senhas não conferem.")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>
    /// Utilizada para fazer login do usuário
    /// </summary>
    public class LoginUserViewModel
    {
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [EmailAddress(ErrorMessage = "O campo {0} está em formato inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [StringLength(100, ErrorMessage = "O campo {0} precisa ter entre {2} e {1} caracteres", MinimumLength = 6)]
        public string Password { get; set; }
    }


    /// <summary>
    /// Utilizada na classe LoginResponseViewModel
    /// </summary>
    public class UserTokenViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public IEnumerable<ClaimViewModel> Claims { get; set; }
    }

    /// <summary>
    /// Utilizada para devolver dados de login para o front-end
    /// </summary>
    public class LoginResponseViewModel
    {
        public string AccessToken { get; set; }
        public double ExpiresIn { get; set; }
        public UserTokenViewModel UserToken { get; set; }
    }

    /// <summary>
    /// Utilizada na classe LoginResponseViewModel 
    /// </summary>
    public class ClaimViewModel
    {
        public string Value { get; set; }
        public string Type { get; set; }
    }
}