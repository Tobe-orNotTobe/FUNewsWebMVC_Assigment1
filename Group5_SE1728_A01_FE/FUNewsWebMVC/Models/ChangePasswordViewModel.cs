using System.ComponentModel.DataAnnotations;

namespace FUNewsWebMVC.Models
{
	public class ChangePasswordViewModel
	{
		[Required(ErrorMessage = "Current password is required")]
		[Display(Name = "Current Password")]
		[DataType(DataType.Password)]
		public string CurrentPassword { get; set; } = string.Empty;

		[Required(ErrorMessage = "New password is required")]
		[Display(Name = "New Password")]
		[DataType(DataType.Password)]
		[StringLength(70, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 70 characters")]
		public string NewPassword { get; set; } = string.Empty;

		[Required(ErrorMessage = "Please confirm your new password")]
		[Display(Name = "Confirm New Password")]
		[DataType(DataType.Password)]
		[Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match")]
		public string ConfirmPassword { get; set; } = string.Empty;
	}
}