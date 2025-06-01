using FUNewsWebMVC.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace FUNewsWebMVC.Services
{
	public class CategoryService
	{
		private readonly IHttpContextAccessor _contextAccessor;
		private readonly IHttpClientFactory _clientFactory;

		public CategoryService(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor)
		{
			_clientFactory = clientFactory;
			_contextAccessor = contextAccessor;
		}

		public async Task<List<Category>> GetCategoriesAsync()
		{
			var token = _contextAccessor.HttpContext.Request.Cookies["Token"];
			var client = _clientFactory.CreateClient("ApiClient");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.GetAsync("Categories");
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<ODataResponse<Category>>(content);
			return result.Value;
		}
	}
}
