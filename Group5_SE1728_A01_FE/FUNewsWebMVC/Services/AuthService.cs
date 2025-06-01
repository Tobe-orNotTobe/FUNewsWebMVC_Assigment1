using FUNewsWebMVC.Models;
using Newtonsoft.Json;
using System.Text;

namespace FUNewsWebMVC.Services
{
	public class AuthService
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;

		public AuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
		}

		public async Task<AuthResponse?> LoginAsync(LoginViewModel login)
		{
			var client = _httpClientFactory.CreateClient();
			var apiBase = _configuration["ApiSettings:BaseUrl"];
			client.BaseAddress = new Uri(apiBase.Replace("/odata/", "/"));

			var json = JsonConvert.SerializeObject(login);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await client.PostAsync("api/Auth/login", content);
			if (!response.IsSuccessStatusCode)
				return null;

			var resultString = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<AuthResponse>(resultString);
			return result;
		}
	}
}
