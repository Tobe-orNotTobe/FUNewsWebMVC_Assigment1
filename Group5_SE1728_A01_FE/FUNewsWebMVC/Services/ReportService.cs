using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Newtonsoft.Json;

namespace FUNewsWebMVC.Services
{
	public class ReportService : BaseService, IReportService
	{
		public ReportService(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor)
			: base(clientFactory, contextAccessor) { }

		public async Task<ReportViewModel> GenerateReportAsync(DateTime startDate, DateTime endDate)
		{
			try
			{
				var client = CreateAuthorizedClient();

				var response = await client.GetAsync($"NewsArticles?$expand=Category,CreatedBy,Tags&$filter=CreatedDate ge {startDate:yyyy-MM-dd} and CreatedDate le {endDate:yyyy-MM-dd}&$orderby=CreatedDate desc");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var apiResponse = JsonConvert.DeserializeObject<ODataResponse<NewsArticle>>(content);
				var articles = apiResponse.Value ?? new List<NewsArticle>();

				var report = new ReportViewModel
				{
					StartDate = startDate,
					EndDate = endDate,
					Articles = articles.Select(a => new NewsArticleReportItem
					{
						NewsArticleId = a.NewsArticleId,
						NewsTitle = a.NewsTitle,
						Headline = a.Headline,
						CreatedDate = a.CreatedDate,
						CategoryName = a.Category?.CategoryName ?? "Uncategorized",
						CreatedByName = a.CreatedBy?.AccountName ?? "Unknown",
						NewsStatus = a.NewsStatus,
						TagCount = a.Tags?.Count ?? 0
					}).ToList(),
					Summary = GenerateSummary(articles)
				};

				return report;
			}
			catch (Exception ex)
			{
				return new ReportViewModel
				{
					StartDate = startDate,
					EndDate = endDate
				};
			}
		}

		private ReportSummary GenerateSummary(List<NewsArticle> articles)
		{
			var summary = new ReportSummary
			{
				TotalArticles = articles.Count,
				ActiveArticles = articles.Count(a => a.NewsStatus == true),
				InactiveArticles = articles.Count(a => a.NewsStatus == false)
			};

			summary.ArticlesByCategory = articles
				.GroupBy(a => a.Category?.CategoryName ?? "Uncategorized")
				.ToDictionary(g => g.Key, g => g.Count());

			summary.ArticlesByAuthor = articles
				.GroupBy(a => a.CreatedBy?.AccountName ?? "Unknown")
				.ToDictionary(g => g.Key, g => g.Count());

			return summary;
		}
	}
}
