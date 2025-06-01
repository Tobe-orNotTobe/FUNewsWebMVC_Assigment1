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
	}
}
