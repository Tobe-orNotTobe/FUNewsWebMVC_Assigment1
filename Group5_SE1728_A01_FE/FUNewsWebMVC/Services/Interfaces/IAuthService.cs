using FUNewsWebMVC.Models;

namespace FUNewsWebMVC.Services.Interfaces
{
	public interface IAuthService
	{
		Task<AuthResponse?> LoginAsync(LoginViewModel login);
	}
}
