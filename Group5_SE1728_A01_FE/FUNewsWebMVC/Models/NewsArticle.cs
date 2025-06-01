namespace FUNewsWebMVC.Models;

public class NewsArticle
{
	public string NewsArticleId { get; set; } = null!;

	public string? NewsTitle { get; set; }

	public string Headline { get; set; } = null!;

	public DateTime? CreatedDate { get; set; }

	public string? NewsContent { get; set; }

	public string? NewsSource { get; set; }

	public short? CategoryId { get; set; }

	public bool? NewsStatus { get; set; }

	public short? CreatedById { get; set; }

	public short? UpdatedById { get; set; }

	public DateTime? ModifiedDate { get; set; }

	public SystemAccount? CreatedBy { get; set; }

	public Category? Category { get; set; }
	public List<Tag>? Tags { get; set; }
}
