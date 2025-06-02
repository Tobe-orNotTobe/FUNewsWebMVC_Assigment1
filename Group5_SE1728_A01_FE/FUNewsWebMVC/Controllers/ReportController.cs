using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Controllers
{
	[AuthFilter]
	public class ReportController : Controller
	{
		private readonly IReportService _reportService;

		public ReportController(IReportService reportService)
		{
			_reportService = reportService;
		}

		// Only Admin can access reports
		private bool IsAdmin()
		{
			return Request.Cookies["Role"] == "Admin";
		}

		public IActionResult Index()
		{
			if (!IsAdmin())
			{
				TempData["Error"] = "Access denied. Only administrators can view reports.";
				return RedirectToAction("Index", "Home");
			}

			var model = new ReportViewModel
			{
				StartDate = DateTime.Now.AddMonths(-1).Date,
				EndDate = DateTime.Now.Date
			};

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Generate(ReportViewModel model)
		{
			if (!IsAdmin())
			{
				TempData["Error"] = "Access denied. Only administrators can generate reports.";
				return RedirectToAction("Index", "Home");
			}

			if (!ModelState.IsValid)
			{
				return View("Index", model);
			}

			if (model.StartDate > model.EndDate)
			{
				ModelState.AddModelError("EndDate", "End date must be greater than or equal to start date.");
				return View("Index", model);
			}

			if (model.EndDate > DateTime.Now.Date)
			{
				ModelState.AddModelError("EndDate", "End date cannot be in the future.");
				return View("Index", model);
			}

			try
			{
				var report = await _reportService.GenerateReportAsync(model.StartDate, model.EndDate);
				return View("Report", report);
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", "An error occurred while generating the report. Please try again.");
				return View("Index", model);
			}
		}

		[HttpGet]
		public async Task<IActionResult> ExportCsv(DateTime startDate, DateTime endDate)
		{
			if (!IsAdmin())
			{
				return Forbid();
			}

			try
			{
				var report = await _reportService.GenerateReportAsync(startDate, endDate);

				var csv = new System.Text.StringBuilder();
				csv.AppendLine("Article ID,Title,Headline,Created Date,Category,Author,Status,Tag Count");

				foreach (var article in report.Articles)
				{
					csv.AppendLine($"\"{article.NewsArticleId}\",\"{article.NewsTitle}\",\"{article.Headline}\",\"{article.CreatedDate:yyyy-MM-dd}\",\"{article.CategoryName}\",\"{article.CreatedByName}\",\"{(article.NewsStatus == true ? "Active" : "Inactive")}\",{article.TagCount}");
				}

				var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
				var fileName = $"news_report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";

				return File(bytes, "text/csv", fileName);
			}
			catch (Exception)
			{
				TempData["Error"] = "Failed to export report.";
				return RedirectToAction("Index");
			}
		}
	}
}
