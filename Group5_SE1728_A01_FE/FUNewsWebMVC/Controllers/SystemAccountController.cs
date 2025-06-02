using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using FUNewsWebMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Controllers
{
	[AuthFilter]
	public class SystemAccountController : Controller
	{
		private readonly ISystemAccountService _service;
		private readonly ILogger<SystemAccountController> _logger;

		public SystemAccountController(ISystemAccountService service, ILogger<SystemAccountController> logger)
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		private bool IsAdmin()
		{
			var role = Request.Cookies["Role"];
			return role == "Admin";
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

		public int PageSize { get; set; } = 10;

		public async Task<IActionResult> Index(string? searchTerm, int pageIndex = 1, int? roleFilter = null)
		{
			if (!IsAdmin())
			{
				TempData["Error"] = "Access denied. Only administrators can manage system accounts.";
				return RedirectToAction("Index", "Home");
			}

			try
			{
				_logger.LogInformation($"Loading system accounts with search term: {searchTerm}, page: {pageIndex}, role filter: {roleFilter}");

				var accounts = await _service.GetAccountsAsync();

				if (!string.IsNullOrWhiteSpace(searchTerm))
				{
					accounts = accounts
						.Where(c => c.AccountName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
								   c.AccountEmail.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
						.ToList();
				}

				if (roleFilter.HasValue)
				{
					accounts = accounts.Where(a => a.AccountRole == roleFilter.Value).ToList();
				}

				var totalCount = accounts.Count;
				var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);

				var pagedAccounts = accounts
				   .Skip((pageIndex - 1) * PageSize)
				   .Take(PageSize)
				   .ToList();

				var stats = await _service.GetAccountStatisticsAsync();

				var model = new SystemAccountListViewModel
				{
					Accounts = pagedAccounts,
					PageIndex = pageIndex,
					TotalPages = totalPages,
					SearchTerm = searchTerm,
					TotalAccounts = stats["Total"],
					StaffCount = stats["Staff"],
					LecturerCount = stats["Lecturer"],
					AdminCount = stats["Admin"]
				};

				ViewBag.RoleFilter = roleFilter;
				ViewBag.AvailableRoles = GetRoleOptions();

				_logger.LogInformation($"Successfully loaded {accounts.Count} system accounts");
				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading system accounts index");
				TempData["Error"] = "Failed to load system accounts. Please try again.";
				return View(new SystemAccountListViewModel());
			}
		}

		[HttpGet]
		public IActionResult Create()
		{
			if (!IsAdmin())
			{
				return Forbid();
			}

			try
			{
				ViewBag.Action = "Create";
				ViewBag.AvailableRoles = GetRoleOptions();
				var model = new SystemAccount();
				return PartialView("_AccountForm", model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading create form");
				return PartialView("_AccountForm", new SystemAccount());
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SystemAccount account)
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			ModelState.Remove("AccountId");

			if (ModelState.IsValid)
			{
				try
				{
					_logger.LogInformation($"Creating system account: {account.AccountName}");

					var emailExists = await _service.EmailExistsAsync(account.AccountEmail);
					if (emailExists)
					{
						return Json(new { success = false, message = "An account with this email already exists." });
					}

					var existingAccounts = await _service.GetAccountsAsync();
					var maxId = existingAccounts.Any() ? existingAccounts.Max(a => a.AccountId) : 0;
					account.AccountId = (short)(maxId + 1);

					await _service.AddAsync(account);

					_logger.LogInformation($"Successfully created system account: {account.AccountName}");
					return Json(new { success = true });
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error creating system account");
					return Json(new { success = false, message = ex.Message });
				}
			}

			ViewBag.Action = "Create";
			ViewBag.AvailableRoles = GetRoleOptions();
			return PartialView("_AccountForm", account);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			if (!IsAdmin())
			{
				return Forbid();
			}

			try
			{
				var account = await _service.GetByIdAsync(id);
				if (account == null)
				{
					return NotFound("Account not found.");
				}

				ViewBag.Action = "Edit";
				ViewBag.AvailableRoles = GetRoleOptions();
				ViewBag.IsCurrentUser = GetCurrentUserId() == account.AccountId;
				return PartialView("_AccountForm", account);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading account for edit: {id}");
				return BadRequest("Failed to load account for editing.");
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(SystemAccount account)
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			if (ModelState.IsValid)
			{
				try
				{
					_logger.LogInformation($"Updating system account: {account.AccountId}");

					var emailExists = await _service.EmailExistsAsync(account.AccountEmail, account.AccountId);
					if (emailExists)
					{
						return Json(new { success = false, message = "An account with this email already exists." });
					}

					await _service.UpdateAsync(account);

					_logger.LogInformation($"Successfully updated system account: {account.AccountId}");
					return Json(new { success = true });
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating system account");
					return Json(new { success = false, message = ex.Message });
				}
			}

			ViewBag.Action = "Edit";
			ViewBag.AvailableRoles = GetRoleOptions();
			return PartialView("_AccountForm", account);
		}

		[HttpPost]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				_logger.LogInformation($"Attempting to delete system account: {id}");

				var currentUserId = GetCurrentUserId();
				if (currentUserId == id)
				{
					return Json(new { success = false, message = "You cannot delete your own account." });
				}

				var account = await _service.GetByIdAsync(id);
				if (account == null)
				{
					return Json(new { success = false, message = "Account not found." });
				}

				if (account.AccountRole == 0)
				{
					return Json(new { success = false, message = "Cannot delete admin accounts." });
				}

				await _service.DeleteAsync(id);

				_logger.LogInformation($"Successfully deleted system account: {id}");
				return Json(new { success = true, message = "Account deleted successfully." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting system account: {id}");

				if (ex.Message.Contains("news articles") ||
					ex.Message.Contains("foreign key") ||
					ex.Message.Contains("references") ||
					ex.Message.Contains("data constraints"))
				{
					return Json(new { success = false, message = "Cannot delete account that has created news articles." });
				}

				if (ex.Message.Contains("not found") || ex.Message.Contains("deleted"))
				{
					return Json(new { success = false, message = "Account not found or has already been deleted." });
				}

				return Json(new { success = false, message = "Cannot delete this account due to existing data references." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var account = await _service.GetByIdAsync(id);
				if (account == null)
				{
					return Json(new { success = false, message = "Account not found." });
				}

				ViewBag.CanManage = IsAdmin();
				return PartialView("_AccountDetails", account);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading account details for ID: {id}");
				return Json(new { success = false, message = "Failed to load account details." });
			}
		}

		[HttpPost]
		public async Task<IActionResult> ToggleStatus(int id)
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var account = await _service.GetByIdAsync(id);
				if (account == null)
				{
					return Json(new { success = false, message = "Account not found." });
				}

				if (account.AccountRole == 0)
				{
					return Json(new { success = false, message = "Cannot modify admin account status." });
				}

				var success = true;
				if (success)
				{
					return Json(new { success = true, message = "Account status updated successfully." });
				}
				else
				{
					return Json(new { success = false, message = "Failed to update account status." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error toggling status for account: {id}");
				return Json(new { success = false, message = "Error updating account status." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetAccountStats()
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var stats = await _service.GetAccountStatisticsAsync();
				return Json(new { success = true, data = stats });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting account statistics");
				return Json(new { success = false, message = "Failed to get account statistics." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Search(string term)
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var accounts = await _service.SearchAccountsAsync(term);
				var result = accounts.Select(a => new
				{
					id = a.AccountId,
					name = a.AccountName,
					email = a.AccountEmail,
					role = a.AccountRole,
					roleName = GetRoleName(a.AccountRole)
				});

				return Json(new { success = true, data = result });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error searching accounts with term: {term}");
				return Json(new { success = false, message = "Search failed." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetAccountsByRole(int role)
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var accounts = await _service.GetAccountsByRoleAsync(role);
				var result = accounts.Select(a => new
				{
					id = a.AccountId,
					name = a.AccountName,
					email = a.AccountEmail
				});

				return Json(new { success = true, data = result });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting accounts by role: {role}");
				return Json(new { success = false, message = "Failed to get accounts by role." });
			}
		}

		[HttpPost]
		public async Task<IActionResult> CheckEmailExists(string email, int? excludeId = null)
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var exists = await _service.EmailExistsAsync(email, excludeId);
				return Json(new { exists = exists });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error checking email existence: {email}");
				return Json(new { exists = false });
			}
		}

		[HttpPost]
		public async Task<IActionResult> BulkDelete(List<int> ids)
		{
			if (!IsAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var currentUserId = GetCurrentUserId();
				var deletedCount = 0;
				var errors = new List<string>();

				foreach (var id in ids)
				{
					try
					{
						if (currentUserId == id)
						{
							errors.Add($"Cannot delete your own account (ID: {id})");
							continue;
						}

						var account = await _service.GetByIdAsync(id);
						if (account?.AccountRole == 0)
						{
							errors.Add($"Cannot delete admin account (ID: {id})");
							continue;
						}

						await _service.DeleteAsync(id);
						deletedCount++;
					}
					catch (Exception ex)
					{
						errors.Add($"Failed to delete account {id}: {ex.Message}");
					}
				}

				return Json(new
				{
					success = true,
					deletedCount = deletedCount,
					totalRequested = ids.Count,
					errors = errors
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in bulk delete operation");
				return Json(new { success = false, message = "Bulk delete operation failed." });
			}
		}

		private List<RoleOption> GetRoleOptions()
		{
			return new List<RoleOption>
			{
				new RoleOption { Value = 1, Text = "Staff", Description = "Can manage categories, news articles, and tags", BadgeClass = "bg-warning text-dark" },
				new RoleOption { Value = 2, Text = "Lecturer", Description = "Can view and create news articles", BadgeClass = "bg-info" }
			};
		}

		private string GetRoleName(int? role)
		{
			return role switch
			{
				0 => "Admin",
				1 => "Staff",
				2 => "Lecturer",
				_ => "Unknown"
			};
		}

		private string GetRoleBadgeClass(int? role)
		{
			return role switch
			{
				0 => "bg-danger",
				1 => "bg-warning text-dark",
				2 => "bg-info",
				_ => "bg-secondary"
			};
		}
	}
}