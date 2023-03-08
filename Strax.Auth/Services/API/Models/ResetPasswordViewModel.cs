using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class RetrievePasswordModel
    {
        public string CallBackURL { get; set; }
        public string Username { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [RegularExpression("^((?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*[#@$!%*?&])|(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[^a-zA-Z0-9])(?=.*[#@$!%*?&])|(?=.*?[A-Z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])(?=.*[#@$!%*?&])|(?=.*?[a-z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])(?=.*[#@$!%*?&])).{8,}$",
                                ErrorMessage = "Password must be at least 12 characters. Please include at least one special character (e.g. !@#$%^&*), upper case (A-Z), lower case (a-z) and number (0-9)")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string UserId { get; set; }
        public string Code { get; set; }
    }


}