using Microsoft.Extensions.Configuration;
using System.Net.Http;

public class ApiHelper
{
	private readonly HttpClient _client;

	public ApiHelper(IConfiguration configuration)
	{
		_client = new HttpClient();
		_client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]);
	}

	public HttpClient Client => _client;
}
