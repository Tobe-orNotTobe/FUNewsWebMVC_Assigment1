using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Newtonsoft.Json;
using System.Text;

namespace FUNewsWebMVC.Services
{
	public class NewsArticleService : BaseService, INewsArticleService
	{
		private readonly ILogger<NewsArticleService> _logger;

		public NewsArticleService(
			IHttpClientFactory clientFactory,
			IHttpContextAccessor contextAccessor,
			ILogger<NewsArticleService> logger)
			: base(clientFactory, contextAccessor)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<List<NewsArticle>> GetNewsArticlesAsync()
		{
			try
			{
				_logger.LogInformation("Fetching all news articles");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync("NewsArticles?$expand=Category,CreatedBy,Tags&$orderby=CreatedDate desc");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<NewsArticle>>(content);

				_logger.LogInformation($"Successfully fetched {result?.Value?.Count ?? 0} news articles");
				return result?.Value ?? new List<NewsArticle>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching news articles");
				throw new Exception("Failed to retrieve news articles", ex);
			}
		}

		public async Task<NewsArticle?> GetNewsArticleByIdAsync(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("Article ID cannot be null or empty", nameof(id));
			}

			try
			{
				_logger.LogInformation($"Fetching news article with ID: {id}");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync($"NewsArticles('{id}')?$expand=Category,CreatedBy,Tags");

				if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					_logger.LogWarning($"News article with ID {id} not found");
					return null;
				}

				response.EnsureSuccessStatusCode();
				var content = await response.Content.ReadAsStringAsync();
				var article = JsonConvert.DeserializeObject<NewsArticle>(content);

				_logger.LogInformation($"Successfully fetched news article: {article?.NewsTitle}");
				return article;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error fetching news article {id}");
				throw new Exception($"Failed to retrieve news article with ID {id}", ex);
			}
		}

		public async Task<bool> CreateNewsArticleAsync(NewsArticle article, List<int>? selectedTags = null)
		{
			if (article == null)
			{
				throw new ArgumentNullException(nameof(article));
			}

			try
			{
				_logger.LogInformation($"Creating news article: {article.NewsTitle} with {selectedTags?.Count ?? 0} tags");

				var client = CreateAuthorizedClient();

				// Set metadata
				article.CreatedDate = DateTime.Now;
				article.NewsStatus = article.NewsStatus ?? true;

				// Generate ID if not provided
				if (string.IsNullOrEmpty(article.NewsArticleId))
				{
					article.NewsArticleId = GenerateArticleId();
				}

				// ✅ SIMPLER APPROACH: Add tags directly to the article
				if (selectedTags != null && selectedTags.Any())
				{
					article.Tags = new List<Tag>();
					foreach (var tagId in selectedTags)
					{
						article.Tags.Add(new Tag { TagId = tagId });
					}
				}
				else
				{
					article.Tags = new List<Tag>();
				}

				var json = JsonConvert.SerializeObject(article, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					DateFormatHandling = DateFormatHandling.IsoDateFormat,
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				});

				_logger.LogInformation($"Sending JSON: {json}");

				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await client.PostAsync("NewsArticles", content);

				var success = response.IsSuccessStatusCode;

				if (success)
				{
					_logger.LogInformation($"Successfully created news article: {article.NewsTitle}");
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to create news article. Status: {response.StatusCode}, Error: {errorContent}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating news article");
				throw new Exception("Failed to create news article", ex);
			}
		}

		public async Task<bool> UpdateNewsArticleAsync(NewsArticle article, List<int>? selectedTags = null)
		{
			if (article == null)
			{
				throw new ArgumentNullException(nameof(article));
			}

			if (string.IsNullOrWhiteSpace(article.NewsArticleId))
			{
				throw new ArgumentException("Article ID is required for update", nameof(article));
			}

			try
			{
				_logger.LogInformation($"Updating news article: {article.NewsArticleId} with {selectedTags?.Count ?? 0} tags");

				var client = CreateAuthorizedClient();

				// Set modification metadata
				article.ModifiedDate = DateTime.Now;

				// ✅ SIMPLER APPROACH: Add tags directly to the article
				if (selectedTags != null)
				{
					article.Tags = new List<Tag>();
					foreach (var tagId in selectedTags)
					{
						article.Tags.Add(new Tag { TagId = tagId });
					}
				}
				else
				{
					article.Tags = new List<Tag>();
				}

				var json = JsonConvert.SerializeObject(article, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					DateFormatHandling = DateFormatHandling.IsoDateFormat,
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				});

				_logger.LogInformation($"Sending JSON: {json}");

				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await client.PutAsync($"NewsArticles('{article.NewsArticleId}')", content);

				var success = response.IsSuccessStatusCode;

				if (success)
				{
					_logger.LogInformation($"Successfully updated news article: {article.NewsArticleId}");
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to update news article. Status: {response.StatusCode}, Error: {errorContent}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating news article {article.NewsArticleId}");
				throw new Exception("Failed to update news article", ex);
			}
		}

		public async Task<bool> DeleteNewsArticleAsync(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("Article ID cannot be null or empty", nameof(id));
			}

			try
			{
				_logger.LogInformation($"Deleting news article: {id}");

				var client = CreateAuthorizedClient();
				var response = await client.DeleteAsync($"NewsArticles('{id}')");

				var success = response.IsSuccessStatusCode;

				if (success)
				{
					_logger.LogInformation($"Successfully deleted news article: {id}");
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to delete news article. Status: {response.StatusCode}, Error: {errorContent}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting news article {id}");
				throw new Exception("Failed to delete news article", ex);
			}
		}

		public async Task<List<NewsArticle>> SearchNewsArticlesAsync(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				return await GetNewsArticlesAsync();
			}

			try
			{
				_logger.LogInformation($"Searching news articles with term: {searchTerm}");

				var client = CreateAuthorizedClient();
				var encodedSearchTerm = Uri.EscapeDataString(searchTerm);

				var filterQuery = $"NewsArticles?$filter=contains(NewsTitle,'{encodedSearchTerm}') or contains(Headline,'{encodedSearchTerm}') or contains(NewsContent,'{encodedSearchTerm}')&$expand=Category,CreatedBy,Tags&$orderby=CreatedDate desc";

				var response = await client.GetAsync(filterQuery);
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<NewsArticle>>(content);

				_logger.LogInformation($"Search returned {result?.Value?.Count ?? 0} articles for term: {searchTerm}");
				return result?.Value ?? new List<NewsArticle>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error searching news articles with term: {searchTerm}");
				throw new Exception("An error occurred while searching news articles.", ex);
			}
		}

		public async Task<List<NewsArticle>> GetMyArticlesAsync(short createdById)
		{
			try
			{
				_logger.LogInformation($"Fetching articles for user: {createdById}");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync($"NewsArticles?$filter=CreatedById eq {createdById}&$expand=Category,CreatedBy,Tags&$orderby=CreatedDate desc");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<NewsArticle>>(content);

				_logger.LogInformation($"Found {result?.Value?.Count ?? 0} articles for user {createdById}");
				return result?.Value ?? new List<NewsArticle>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error fetching articles for user {createdById}");
				throw new Exception("An error occurred while retrieving your articles.", ex);
			}
		}

		public async Task<List<NewsArticle>> GetActiveNewsArticlesAsync()
		{
			try
			{
				_logger.LogInformation("Fetching active news articles");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync("NewsArticles?$filter=NewsStatus eq true&$expand=Category,CreatedBy,Tags&$orderby=CreatedDate desc");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<NewsArticle>>(content);

				_logger.LogInformation($"Found {result?.Value?.Count ?? 0} active articles");
				return result?.Value ?? new List<NewsArticle>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching active news articles");
				throw new Exception("An error occurred while retrieving active news articles.", ex);
			}
		}

		public async Task<List<NewsArticle>> GetNewsArticlesByCategoryAsync(short categoryId)
		{
			try
			{
				_logger.LogInformation($"Fetching articles for category: {categoryId}");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync($"NewsArticles?$filter=CategoryId eq {categoryId}&$expand=Category,CreatedBy,Tags&$orderby=CreatedDate desc");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<NewsArticle>>(content);

				_logger.LogInformation($"Found {result?.Value?.Count ?? 0} articles in category {categoryId}");
				return result?.Value ?? new List<NewsArticle>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error fetching articles for category {categoryId}");
				throw new Exception($"An error occurred while retrieving articles for the selected category.", ex);
			}
		}

		public async Task<bool> ToggleArticleStatusAsync(string id)
		{
			try
			{
				var article = await GetNewsArticleByIdAsync(id);
				if (article == null)
				{
					_logger.LogWarning($"Cannot toggle status - article {id} not found");
					return false;
				}

				article.NewsStatus = !article.NewsStatus;
				article.ModifiedDate = DateTime.Now;

				var success = await UpdateNewsArticleAsync(article);

				if (success)
				{
					_logger.LogInformation($"Successfully toggled status for article {id} to {article.NewsStatus}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error toggling status for article {id}");
				throw new Exception($"An error occurred while updating the article status.", ex);
			}
		}

		private static string GenerateArticleId()
		{
			return $"ART{DateTime.Now:yyyyMMdd}{DateTime.Now:HHmmss}{new Random().Next(10, 99)}";
		}
	}
}