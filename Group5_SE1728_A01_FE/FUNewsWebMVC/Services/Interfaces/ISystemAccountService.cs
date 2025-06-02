using FUNewsWebMVC.Models;

namespace FUNewsWebMVC.Services.Interfaces
{
    public interface ISystemAccountService
    {
		Task<List<SystemAccount>> GetAccountsAsync();
		Task<SystemAccount?> GetByIdAsync(int id);
		Task AddAsync(SystemAccount account);
		Task UpdateAsync(SystemAccount account);
		Task DeleteAsync(int id);
		Task<List<SystemAccount>> SearchAccountsAsync(string searchTerm);
		Task<List<SystemAccount>> GetAccountsByRoleAsync(int role);
		Task<bool> EmailExistsAsync(string email, int? excludeAccountId = null);
		Task<Dictionary<string, int>> GetAccountStatisticsAsync();
	}
}
