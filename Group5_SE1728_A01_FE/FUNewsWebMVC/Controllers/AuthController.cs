using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

public class AuthController : Controller
{
	private readonly AuthService _authService;

	public AuthController(AuthService authService)
	{
		_authService = authService;
	}

	[HttpGet]
	public IActionResult Login()
	{
		return View();
	}

	[HttpPost]
	public async Task<IActionResult> Login(LoginViewModel model)
	{
		if (!ModelState.IsValid) return View(model);

		var result = await _authService.LoginAsync(model);
		if (result == null)
		{
			ViewBag.Error = "Sai email hoặc mật khẩu.";
			return View(model);
		}

		// ✅ Lưu vào cookie
		var cookieOptions = new CookieOptions
		{
			HttpOnly = true,
			Secure = true, // BẮT BUỘC dùng HTTPS để bật Secure
			Expires = DateTimeOffset.Now.AddMinutes(30)
		};

		Response.Cookies.Append("Token", result.Token, cookieOptions);
		Response.Cookies.Append("Role", result.RoleName, cookieOptions);
		Response.Cookies.Append("Name", result.AccountName, cookieOptions);

		return RedirectToAction("Index", "Home");
	}

	[HttpGet]
	public IActionResult Logout()
	{
		Response.Cookies.Delete("Token");
		Response.Cookies.Delete("Role");
		Response.Cookies.Delete("Name");

		return RedirectToAction("Login");
	}
}
