namespace FUNewsWebMVC.Models;

public class SystemAccount
{
	public short AccountId { get; set; }

	public string? AccountName { get; set; }

	public string? AccountEmail { get; set; }

	public int? AccountRole { get; set; }

	public string? AccountPassword { get; set; }

	public NewsArticle? NewsArticles { get; set; }
}
