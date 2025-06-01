using System.Net.Http.Headers;

namespace FUNewsWebMVC.Services
{
	public class BaseService
	{
		protected readonly IHttpClientFactory _clientFactory;
		protected readonly IHttpContextAccessor _contextAccessor;

		public BaseService(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor)
		{
			_clientFactory = clientFactory;
			_contextAccessor = contextAccessor;
		}

		protected HttpClient CreateAuthorizedClient()
		{
			var token = _contextAccessor.HttpContext?.Request.Cookies["Token"];
			var client = _clientFactory.CreateClient("ApiClient");
			if (!string.IsNullOrEmpty(token))
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}
			return client;
		}
	}
}
