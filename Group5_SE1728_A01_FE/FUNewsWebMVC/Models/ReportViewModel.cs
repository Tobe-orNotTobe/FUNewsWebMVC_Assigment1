using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FUNewsWebMVC.Models
{
	public class ReportViewModel
	{
		[Required(ErrorMessage = "Start date is required")]
		[DataType(DataType.Date)]
		[Display(Name = "Start Date")]
		public DateTime StartDate { get; set; }

		[Required(ErrorMessage = "End date is required")]
		[DataType(DataType.Date)]
		[Display(Name = "End Date")]
		public DateTime EndDate { get; set; }

		public List<NewsArticleReportItem> Articles { get; set; } = new List<NewsArticleReportItem>();
		public ReportSummary Summary { get; set; } = new ReportSummary();
	}

	public class NewsArticleReportItem
	{
		public string NewsArticleId { get; set; }
		public string NewsTitle { get; set; }
		public string Headline { get; set; }
		public DateTime? CreatedDate { get; set; }
		public string CategoryName { get; set; }
		public string CreatedByName { get; set; }
		public bool? NewsStatus { get; set; }
		public int TagCount { get; set; }
	}

	public class ReportSummary
	{
		public int TotalArticles { get; set; }
		public int ActiveArticles { get; set; }
		public int InactiveArticles { get; set; }
		public Dictionary<string, int> ArticlesByCategory { get; set; } = new Dictionary<string, int>();
		public Dictionary<string, int> ArticlesByAuthor { get; set; } = new Dictionary<string, int>();
	}
}

