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
    }
}
