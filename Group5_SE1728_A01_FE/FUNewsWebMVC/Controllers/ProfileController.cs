using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Controllers
{
	[AuthFilter]
	public class ProfileController : Controller
	{
		private readonly ISystemAccountService _accountService;
		private readonly ILogger<ProfileController> _logger;

		public ProfileController(ISystemAccountService accountService, ILogger<ProfileController> logger)
		{
			_accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		private short? GetCurrentUserId()
		{
			var userIdCookie = Request.Cookies["UserId"];
			if (short.TryParse(userIdCookie, out short userId))
			{
				return userId;
			}
			return null;
		}

		private bool IsStaffOrAdmin()
		{
			var role = Request.Cookies["Role"];
			return role == "Staff" || role == "Admin";
		}

		public async Task<IActionResult> Index()
		{
			try
			{
				var userId = GetCurrentUserId();
				if (!userId.HasValue)
				{
					TempData["Error"] = "Unable to identify current user.";
					return RedirectToAction("Login", "Auth");
				}

				_logger.LogInformation($"Loading profile for user: {userId.Value}");

				var account = await _accountService.GetByIdAsync(userId.Value);
				if (account == null)
				{
					TempData["Error"] = "Account not found.";
					return RedirectToAction("Login", "Auth");
				}

				return View(account);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading user profile");
				TempData["Error"] = "Failed to load profile. Please try again.";
				return RedirectToAction("Index", "Home");
			}
		}

		[HttpGet]
		public async Task<IActionResult> Edit()
		{
			try
			{
				var userId = GetCurrentUserId();
				if (!userId.HasValue)
				{
					return Json(new { success = false, message = "Unable to identify current user." });
				}

				var account = await _accountService.GetByIdAsync(userId.Value);
				if (account == null)
				{
					return Json(new { success = false, message = "Account not found." });
				}

				return PartialView("_ProfileForm", account);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading profile edit form");
				return Json(new { success = false, message = "Failed to load edit form." });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(SystemAccount model)
		{
			try
			{
				var userId = GetCurrentUserId();
				if (!userId.HasValue || userId.Value != model.AccountId)
				{
					return Json(new { success = false, message = "Invalid user access." });
				}

				_logger.LogInformation($"Updating profile for user: {model.AccountId}");
				_logger.LogInformation($"Model data - Name: {model.AccountName}, Email: {model.AccountEmail}, Role: {model.AccountRole}");

				if (!ModelState.IsValid)
				{
					var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
					_logger.LogWarning($"Model validation failed: {string.Join(", ", errors)}");
					return PartialView("_ProfileForm", model);
				}

				var existingAccount = await _accountService.GetByIdAsync(userId.Value);
				if (existingAccount == null)
				{
					return Json(new { success = false, message = "Account not found." });
				}

				existingAccount.AccountName = model.AccountName?.Trim();
				existingAccount.AccountEmail = model.AccountEmail?.Trim()?.ToLowerInvariant();

				if (!string.IsNullOrEmpty(model.AccountPassword) &&
					model.AccountPassword != existingAccount.AccountPassword)
				{
					existingAccount.AccountPassword = model.AccountPassword;
					_logger.LogInformation("Password will be updated");
				}

				_logger.LogInformation($"Calling UpdateAsync with data - ID: {existingAccount.AccountId}, Name: {existingAccount.AccountName}, Email: {existingAccount.AccountEmail}");

				var emailExists = await _accountService.EmailExistsAsync(existingAccount.AccountEmail, existingAccount.AccountId);
				if (emailExists)
				{
					return Json(new { success = false, message = "An account with this email already exists." });
				}

				await _accountService.UpdateAsync(existingAccount);

				var cookieOptions = new CookieOptions
				{
					HttpOnly = true,
					Secure = true,
					Expires = DateTimeOffset.Now.AddMinutes(30)
				};
				Response.Cookies.Append("Name", existingAccount.AccountName, cookieOptions);
				Response.Cookies.Append("Email", existingAccount.AccountEmail, cookieOptions);

				_logger.LogInformation($"Successfully updated profile in database for user: {existingAccount.AccountId}");
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating profile: {ex.Message}");

				if (ex.InnerException != null)
				{
					_logger.LogError(ex.InnerException, $"Inner exception: {ex.InnerException.Message}");
				}

				var errorMessage = ex.Message;
				if (ex.InnerException != null)
				{
					errorMessage += $" Details: {ex.InnerException.Message}";
				}

				return Json(new { success = false, message = $"Failed to update profile: {errorMessage}" });
			}
		}

		[HttpGet]
		public IActionResult ChangePassword()
		{
			try
			{
				var model = new ChangePasswordViewModel();
				return PartialView("_ChangePasswordForm", model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading change password form");
				return Json(new { success = false, message = "Failed to load password change form." });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return PartialView("_ChangePasswordForm", model);
			}

			try
			{
				var userId = GetCurrentUserId();
				if (!userId.HasValue)
				{
					return Json(new { success = false, message = "Unable to identify current user." });
				}

				_logger.LogInformation($"Attempting to change password for user: {userId.Value}");

				var account = await _accountService.GetByIdAsync(userId.Value);
				if (account == null)
				{
					return Json(new { success = false, message = "Account not found." });
				}

				if (account.AccountPassword != model.CurrentPassword)
				{
					return Json(new { success = false, message = "Current password is incorrect." });
				}

				if (model.CurrentPassword == model.NewPassword)
				{
					return Json(new { success = false, message = "New password must be different from current password." });
				}

				account.AccountPassword = model.NewPassword;
				await _accountService.UpdateAsync(account);

				_logger.LogInformation($"Successfully changed password in database for user: {userId.Value}");
				return Json(new
				{
					success = true,
					message = "Password changed successfully! Please log in again with your new password."
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error changing password");
				return Json(new { success = false, message = $"Failed to change password: {ex.Message}" });
			}
		}

		public IActionResult MyArticles()
		{
			if (!IsStaffOrAdmin())
			{
				TempData["Error"] = "Access denied. Only Staff members can view their articles.";
				return RedirectToAction("Index", "Home");
			}

			try
			{
				var userId = GetCurrentUserId();
				if (!userId.HasValue)
				{
					TempData["Error"] = "Unable to identify current user.";
					return RedirectToAction("Login", "Auth");
				}

				return RedirectToAction("Index", "NewsArticle", new { filter = "my-articles" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error accessing my articles");
				TempData["Error"] = "Failed to load your articles. Please try again.";
				return RedirectToAction("Index", "Home");
			}
		}
	}
}