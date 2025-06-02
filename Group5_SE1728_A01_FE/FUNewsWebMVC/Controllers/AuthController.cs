using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Controllers
{
	public class AuthController : Controller
	{
		private readonly IAuthService _authService;

		public AuthController(IAuthService authService)
		{
			_authService = authService;
		}

		[HttpGet]
		public IActionResult Login()
		{
			if (!string.IsNullOrEmpty(Request.Cookies["Token"]))
			{
				return RedirectToAction("Index", "Home");
			}
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			if (!ModelState.IsValid)
				return View(model);

			try
			{
				var result = await _authService.LoginAsync(model);
				if (result == null)
				{
					ViewBag.Error = "Invalid email or password.";
					return View(model);
				}

				var cookieOptions = new CookieOptions
				{
					HttpOnly = true,
					Secure = true,
					Expires = DateTimeOffset.Now.AddMinutes(30)
				};

				Response.Cookies.Append("Token", result.Token, cookieOptions);
				Response.Cookies.Append("Role", result.RoleName, cookieOptions);
				Response.Cookies.Append("Name", result.AccountName, cookieOptions);
				Response.Cookies.Append("UserId", result.AccountId.ToString(), cookieOptions);
				Response.Cookies.Append("Email", result.AccountEmail, cookieOptions); 

				TempData["Success"] = $"Welcome back, {result.AccountName}!";
				return RedirectToAction("Index", "Home");
			}
			catch (Exception ex)
			{
				ViewBag.Error = "An error occurred during login. Please try again.";
				return View(model);
			}
		}

		[HttpGet]
		public IActionResult Logout()
		{
			Response.Cookies.Delete("Token");
			Response.Cookies.Delete("Role");
			Response.Cookies.Delete("Name");
			Response.Cookies.Delete("UserId");
			Response.Cookies.Delete("Email");

			TempData["Info"] = "You have been logged out successfully.";
			return RedirectToAction("Login");
		}
	}
}