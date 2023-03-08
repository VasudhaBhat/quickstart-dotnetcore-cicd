using System.ComponentModel.DataAnnotations;


namespace OpenSoftware.OidcTemplate.Auth.Models.AccountViewModels
{
    public class LoginInputModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public string ReturnUrl { get; set; }
    }
}
