using Newtonsoft.Json;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using System.Text;
using System.Net;

namespace FUNewsWebMVC.Services
{
	public class SystemAccountService : BaseService, ISystemAccountService
	{
		private readonly ILogger<SystemAccountService> _logger;

		public SystemAccountService(
			IHttpClientFactory clientFactory,
			IHttpContextAccessor contextAccessor,
			ILogger<SystemAccountService> logger)
			: base(clientFactory, contextAccessor)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<List<SystemAccount>> GetAccountsAsync()
		{
			try
			{
				_logger.LogInformation("Fetching all system accounts");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync("SystemAccounts");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<SystemAccount>>(content);

				_logger.LogInformation($"Successfully fetched {result?.Value?.Count ?? 0} system accounts");
				return result?.Value ?? new List<SystemAccount>();
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "HTTP error while fetching system accounts");
				throw new Exception("Failed to connect to the API server", ex);
			}
			catch (TaskCanceledException ex)
			{
				_logger.LogError(ex, "Request timeout while fetching system accounts");
				throw new Exception("Request timed out while retrieving system accounts", ex);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching system accounts");
				throw new Exception("Failed to retrieve system accounts", ex);
			}
		}

		public async Task<SystemAccount?> GetByIdAsync(int id)
		{
			try
			{
				_logger.LogInformation($"Fetching system account with ID: {id}");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync($"SystemAccounts({id})");

				if (response.StatusCode == HttpStatusCode.NotFound)
				{
					_logger.LogWarning($"System account with ID {id} not found");
					return null;
				}

				response.EnsureSuccessStatusCode();
				var content = await response.Content.ReadAsStringAsync();
				var account = JsonConvert.DeserializeObject<SystemAccount>(content);

				_logger.LogInformation($"Successfully fetched system account: {account?.AccountName}");
				return account;
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, $"HTTP error while fetching system account {id}");
				throw new Exception($"Failed to connect to the API server for account {id}", ex);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error fetching system account {id}");
				throw new Exception($"Failed to retrieve system account with ID {id}", ex);
			}
		}

		public async Task AddAsync(SystemAccount account)
		{
			if (account == null)
			{
				throw new ArgumentNullException(nameof(account));
			}

			try
			{
				_logger.LogInformation($"Creating system account: {account.AccountName}");

				var client = CreateAuthorizedClient();

				var accountToCreate = new
				{
					AccountId = account.AccountId,
					AccountName = account.AccountName?.Trim(),
					AccountEmail = account.AccountEmail?.Trim()?.ToLowerInvariant(),
					AccountRole = account.AccountRole,
					AccountPassword = account.AccountPassword
				};

				var json = JsonConvert.SerializeObject(accountToCreate, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					DateFormatHandling = DateFormatHandling.IsoDateFormat
				});

				_logger.LogInformation($"Sending account creation request for: {accountToCreate.AccountEmail}");

				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await client.PostAsync("SystemAccounts", content);

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to create system account. Status: {response.StatusCode}, Error: {errorContent}");

					if (response.StatusCode == HttpStatusCode.BadRequest)
					{
						if (errorContent.Contains("email") || errorContent.Contains("duplicate") || errorContent.Contains("exists"))
						{
							throw new Exception("An account with this email address already exists.");
						}
						throw new Exception($"Invalid account data: {errorContent}");
					}
					else if (response.StatusCode == HttpStatusCode.Conflict)
					{
						throw new Exception("An account with this email address already exists.");
					}

					throw new Exception($"Failed to create system account: {errorContent}");
				}

				_logger.LogInformation($"Successfully created system account: {account.AccountName}");
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "HTTP error while creating system account");
				throw new Exception("Failed to connect to the API server", ex);
			}
			catch (TaskCanceledException ex)
			{
				_logger.LogError(ex, "Request timeout while creating system account");
				throw new Exception("Request timed out while creating system account", ex);
			}
			catch (Exception ex) when (ex.Message.Contains("email") || ex.Message.Contains("duplicate"))
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating system account");
				throw new Exception("Failed to create system account", ex);
			}
		}

		public async Task UpdateAsync(SystemAccount account)
		{
			if (account == null)
			{
				throw new ArgumentNullException(nameof(account));
			}

			try
			{
				_logger.LogInformation($"Updating system account: {account.AccountId}");

				var client = CreateAuthorizedClient();

				var accountToUpdate = new
				{
					AccountId = account.AccountId,
					AccountName = account.AccountName?.Trim(),
					AccountEmail = account.AccountEmail?.Trim()?.ToLowerInvariant(),
					AccountRole = account.AccountRole,
					AccountPassword = account.AccountPassword
				};

				var json = JsonConvert.SerializeObject(accountToUpdate, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					DateFormatHandling = DateFormatHandling.IsoDateFormat
				});

				_logger.LogInformation($"Sending account update request for ID: {account.AccountId}");

				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await client.PutAsync($"SystemAccounts({account.AccountId})", content);

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to update system account. Status: {response.StatusCode}, Error: {errorContent}");

					if (response.StatusCode == HttpStatusCode.NotFound)
					{
						throw new Exception("Account not found or has been deleted.");
					}
					else if (response.StatusCode == HttpStatusCode.BadRequest)
					{
						if (errorContent.Contains("email") || errorContent.Contains("duplicate") || errorContent.Contains("exists"))
						{
							throw new Exception("An account with this email address already exists.");
						}
						throw new Exception($"Invalid account data: {errorContent}");
					}
					else if (response.StatusCode == HttpStatusCode.Conflict)
					{
						throw new Exception("An account with this email address already exists.");
					}

					throw new Exception($"Failed to update system account: {errorContent}");
				}

				_logger.LogInformation($"Successfully updated system account: {account.AccountId}");
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "HTTP error while updating system account");
				throw new Exception("Failed to connect to the API server", ex);
			}
			catch (TaskCanceledException ex)
			{
				_logger.LogError(ex, "Request timeout while updating system account");
				throw new Exception("Request timed out while updating system account", ex);
			}
			catch (Exception ex) when (ex.Message.Contains("email") || ex.Message.Contains("duplicate") || ex.Message.Contains("not found"))
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating system account {account.AccountId}");
				throw new Exception($"Failed to update system account with ID {account.AccountId}", ex);
			}
		}

		public async Task DeleteAsync(int id)
		{
			try
			{
				_logger.LogInformation($"Deleting system account: {id}");

				var client = CreateAuthorizedClient();
				var response = await client.DeleteAsync($"SystemAccounts({id})");

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to delete system account. Status: {response.StatusCode}, Error: {errorContent}");

					if (response.StatusCode == HttpStatusCode.NotFound)
					{
						throw new Exception("Account not found or has already been deleted.");
					}
					else if (response.StatusCode == HttpStatusCode.BadRequest)
					{
						if (errorContent.Contains("news articles") || errorContent.Contains("foreign key") || errorContent.Contains("reference"))
						{
							throw new Exception("Cannot delete account that has created news articles.");
						}
						throw new Exception($"Cannot delete account: {errorContent}");
					}
					else if (response.StatusCode == HttpStatusCode.Conflict)
					{
						throw new Exception("Cannot delete account that has created news articles.");
					}

					throw new Exception($"Failed to delete system account: {errorContent}");
				}

				_logger.LogInformation($"Successfully deleted system account: {id}");
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "HTTP error while deleting system account");
				throw new Exception("Failed to connect to the API server", ex);
			}
			catch (TaskCanceledException ex)
			{
				_logger.LogError(ex, "Request timeout while deleting system account");
				throw new Exception("Request timed out while deleting system account", ex);
			}
			catch (Exception ex) when (ex.Message.Contains("news articles") || ex.Message.Contains("not found"))
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting system account {id}");
				throw;
			}
		}

		public async Task<List<SystemAccount>> SearchAccountsAsync(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				return await GetAccountsAsync();
			}

			try
			{
				_logger.LogInformation($"Searching system accounts with term: {searchTerm}");

				var client = CreateAuthorizedClient();
				var encodedSearchTerm = Uri.EscapeDataString(searchTerm.Trim());
				var query = $"SystemAccounts?$filter=contains(tolower(AccountName),'{encodedSearchTerm.ToLowerInvariant()}') or contains(tolower(AccountEmail),'{encodedSearchTerm.ToLowerInvariant()}')";

				var response = await client.GetAsync(query);
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<SystemAccount>>(content);

				_logger.LogInformation($"Search returned {result?.Value?.Count ?? 0} accounts for term: {searchTerm}");
				return result?.Value ?? new List<SystemAccount>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error searching system accounts with term: {searchTerm}");
				throw new Exception("Failed to search system accounts", ex);
			}
		}

		public async Task<List<SystemAccount>> GetAccountsByRoleAsync(int role)
		{
			try
			{
				_logger.LogInformation($"Fetching accounts with role: {role}");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync($"SystemAccounts?$filter=AccountRole eq {role}");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<SystemAccount>>(content);

				_logger.LogInformation($"Successfully fetched {result?.Value?.Count ?? 0} accounts with role {role}");
				return result?.Value ?? new List<SystemAccount>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error fetching accounts with role {role}");
				throw new Exception($"Failed to retrieve accounts with role {role}", ex);
			}
		}

		public async Task<bool> EmailExistsAsync(string email, int? excludeAccountId = null)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				return false;
			}

			try
			{
				_logger.LogInformation($"Checking if email exists: {email}");

				var accounts = await GetAccountsAsync();
				var emailLower = email.Trim().ToLowerInvariant();

				var exists = accounts.Any(a =>
					a.AccountEmail?.ToLowerInvariant() == emailLower &&
					(!excludeAccountId.HasValue || a.AccountId != excludeAccountId.Value));

				_logger.LogInformation($"Email {email} exists: {exists}");
				return exists;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error checking if email exists: {email}");
				return false;
			}
		}

		public async Task<Dictionary<string, int>> GetAccountStatisticsAsync()
		{
			try
			{
				_logger.LogInformation("Fetching account statistics");

				var accounts = await GetAccountsAsync();

				var stats = new Dictionary<string, int>
				{
					["Total"] = accounts.Count,
					["Admin"] = accounts.Count(a => a.AccountRole == 0),
					["Staff"] = accounts.Count(a => a.AccountRole == 1),
					["Lecturer"] = accounts.Count(a => a.AccountRole == 2),
					["Unknown"] = accounts.Count(a => !a.AccountRole.HasValue || (a.AccountRole != 0 && a.AccountRole != 1 && a.AccountRole != 2))
				};

				_logger.LogInformation($"Account statistics calculated: Total={stats["Total"]}, Admin={stats["Admin"]}, Staff={stats["Staff"]}, Lecturer={stats["Lecturer"]}");
				return stats;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting account statistics");
				throw new Exception("Failed to get account statistics", ex);
			}
		}

		public async Task<List<SystemAccount>> GetActiveAccountsAsync()
		{
			try
			{
				_logger.LogInformation("Fetching active accounts");

				var accounts = await GetAccountsAsync();
				var activeAccounts = accounts.Where(a => a.AccountRole.HasValue).ToList();

				_logger.LogInformation($"Successfully fetched {activeAccounts.Count} active accounts");
				return activeAccounts;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching active accounts");
				throw new Exception("Failed to retrieve active accounts", ex);
			}
		}

		public async Task<List<SystemAccount>> GetStaffAccountsAsync()
		{
			try
			{
				return await GetAccountsByRoleAsync(1);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching staff accounts");
				throw new Exception("Failed to retrieve staff accounts", ex);
			}
		}

		public async Task<List<SystemAccount>> GetLecturerAccountsAsync()
		{
			try
			{
				return await GetAccountsByRoleAsync(2);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching lecturer accounts");
				throw new Exception("Failed to retrieve lecturer accounts", ex);
			}
		}

		public async Task<SystemAccount?> GetAccountByEmailAsync(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				return null;
			}

			try
			{
				_logger.LogInformation($"Fetching account by email: {email}");

				var accounts = await GetAccountsAsync();
				var emailLower = email.Trim().ToLowerInvariant();
				var account = accounts.FirstOrDefault(a => a.AccountEmail?.ToLowerInvariant() == emailLower);

				if (account != null)
				{
					_logger.LogInformation($"Found account for email {email}: {account.AccountName}");
				}
				else
				{
					_logger.LogInformation($"No account found for email: {email}");
				}

				return account;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error fetching account by email: {email}");
				throw new Exception($"Failed to retrieve account with email {email}", ex);
			}
		}

		public async Task<bool> ValidateAccountAsync(int accountId)
		{
			try
			{
				var account = await GetByIdAsync(accountId);
				return account != null && account.AccountRole.HasValue;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error validating account: {accountId}");
				return false;
			}
		}

		public async Task<List<SystemAccount>> GetRecentAccountsAsync(int count = 10)
		{
			try
			{
				_logger.LogInformation($"Fetching {count} most recent accounts");

				var accounts = await GetAccountsAsync();
				var recentAccounts = accounts
					.OrderByDescending(a => a.AccountId)
					.Take(count)
					.ToList();

				_logger.LogInformation($"Successfully fetched {recentAccounts.Count} recent accounts");
				return recentAccounts;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching recent accounts");
				throw new Exception("Failed to retrieve recent accounts", ex);
			}
		}

		public async Task<int> GetTotalAccountCountAsync()
		{
			try
			{
				var accounts = await GetAccountsAsync();
				return accounts.Count;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting total account count");
				throw new Exception("Failed to get total account count", ex);
			}
		}

		public async Task<bool> CanDeleteAccountAsync(int accountId)
		{
			try
			{
				var account = await GetByIdAsync(accountId);
				if (account == null)
				{
					return false;
				}

				if (account.AccountRole == 0)
				{
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error checking if account can be deleted: {accountId}");
				return false;
			}
		}
	}
}