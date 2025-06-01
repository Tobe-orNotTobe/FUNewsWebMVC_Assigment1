using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Newtonsoft.Json;
using System.Text;

namespace FUNewsWebMVC.Services
{
	public class AuthService : BaseService, IAuthService
	{
		public AuthService(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor) : base(clientFactory, contextAccessor)
		{
		}

		public async Task<AuthResponse?> LoginAsync(LoginViewModel login)
		{
			var client = CreateAuthorizedClient();
			var json = JsonConvert.SerializeObject(login);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await client.PostAsync(GetApiBase() + "api/Auth/login", content);
			if (!response.IsSuccessStatusCode)
				return null;

			var resultString = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<AuthResponse>(resultString);
			return result;
		}
	}
}
