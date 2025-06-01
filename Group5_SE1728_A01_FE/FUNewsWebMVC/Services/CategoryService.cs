using FUNewsWebMVC.Models;
using Newtonsoft.Json;

namespace FUNewsWebMVC.Services
{
	public class CategoryService : BaseService
	{
		public CategoryService(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor)
			: base(clientFactory, contextAccessor) { }

		public async Task<List<Category>> GetCategoriesAsync()
		{
			var client = CreateAuthorizedClient();
			var response = await client.GetAsync("Categories");
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<ODataResponse<Category>>(content);
			return result.Value;
		}
        public async Task<Category> GetByIdAsync(int id)
        {
            var client = CreateAuthorizedClient();
            var response = await client.GetAsync($"Categories/{id}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Category>(content);
        }

        public async Task AddAsync(Category category)
        {
            var client = CreateAuthorizedClient();
            try
            {
                var response = await client.PostAsJsonAsync("Categories", category);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddAsync failed: " + ex.Message);
                throw;
            }
        }

        public async Task UpdateAsync(Category category)
        {
            var client = CreateAuthorizedClient();
            var response = await client.PutAsJsonAsync($"Categories/{category.CategoryId}", category);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(int id)
        {
            var client = CreateAuthorizedClient();
            var response = await client.DeleteAsync($"Categories/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
