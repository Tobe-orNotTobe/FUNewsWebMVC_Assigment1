using System.Net.Http.Headers;

namespace FUNewsWebMVC.Services
{
	public class BaseService
	{
		protected readonly IHttpClientFactory _clientFactory;
		protected readonly IHttpContextAccessor _contextAccessor;
		private readonly IConfiguration _configuration;

		public BaseService(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor)
		{
			_clientFactory = clientFactory;
			_contextAccessor = contextAccessor;
			_configuration = contextAccessor.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
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

		protected string GetApiBase()
		{
			var baseUrl = _configuration["ApiSettings:BaseUrl"];
			return baseUrl!.Replace("/odata/", "/"); 
		}
	}
}
