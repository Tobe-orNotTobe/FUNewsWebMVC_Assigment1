using Newtonsoft.Json;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;

namespace FUNewsWebMVC.Services
{
    public class SystemAccountService : BaseService, ISystemAccountService
    {
        public SystemAccountService(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor)
            : base(clientFactory, contextAccessor) { }

        public async Task<List<SystemAccount>> GetAccountsAsync()
        {
            var client = CreateAuthorizedClient();
            var response = await client.GetAsync("SystemAccounts");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ODataResponse<SystemAccount>>(content);
            return result.Value;
        }

        public async Task<SystemAccount?> GetByIdAsync(int id)
        {
            var client = CreateAuthorizedClient();
            var response = await client.GetAsync($"SystemAccounts/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SystemAccount>(content);
        }

        //public async Task AddAsync(SystemAccount account)
        //{
        //    var client = CreateAuthorizedClient();
        //    var response = await client.PostAsJsonAsync("SystemAccounts", account);
        //    response.EnsureSuccessStatusCode();
        //}
        public async Task AddAsync(SystemAccount account)
        {
            var client = CreateAuthorizedClient();
            try
            {
                var response = await client.PostAsJsonAsync("SystemAccounts", account);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddAsync failed: " + ex.Message);
                throw;
            }
        }
        public async Task UpdateAsync(SystemAccount account)
        {
            var client = CreateAuthorizedClient();
            var response = await client.PutAsJsonAsync($"SystemAccounts/{account.AccountId}", account);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(int id)
        {
            var client = CreateAuthorizedClient();
            var response = await client.DeleteAsync($"SystemAccounts/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
