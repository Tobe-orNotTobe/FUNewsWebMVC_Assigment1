using FUNewsWebMVC.Models;

namespace FUNewsWebMVC.Services.Interfaces
{
	public interface INewsArticleService
	{
		Task<List<NewsArticle>> GetNewsArticlesAsync();
		Task<NewsArticle?> GetNewsArticleByIdAsync(string id);
		Task<bool> CreateNewsArticleAsync(NewsArticle article);
		Task<bool> UpdateNewsArticleAsync(NewsArticle article);
		Task<bool> DeleteNewsArticleAsync(string id);
		Task<List<NewsArticle>> SearchNewsArticlesAsync(string searchTerm);
		Task<List<NewsArticle>> GetMyArticlesAsync(short createdById);
		Task<List<NewsArticle>> GetActiveNewsArticlesAsync();
		Task<List<NewsArticle>> GetNewsArticlesByCategoryAsync(short categoryId);
		Task<bool> ToggleArticleStatusAsync(string id);
	}
}
