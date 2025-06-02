using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Newtonsoft.Json;
using System.Text;

namespace FUNewsWebMVC.Services
{
	public class CategoryService : BaseService, ICategoryService
	{
		private readonly ILogger<CategoryService> _logger;

		public CategoryService(
			IHttpClientFactory clientFactory,
			IHttpContextAccessor contextAccessor,
			ILogger<CategoryService> logger)
			: base(clientFactory, contextAccessor)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<List<Category>> GetCategoriesAsync()
		{
			try
			{
				_logger.LogInformation("Fetching all categories");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync("Categories?$expand=ParentCategory");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<Category>>(content);

				_logger.LogInformation($"Successfully fetched {result?.Value?.Count ?? 0} categories");
				return result?.Value ?? new List<Category>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching categories");
				throw new Exception("Failed to retrieve categories", ex);
			}
		}

		public async Task<Category?> GetCategoryByIdAsync(int id)
		{
			try
			{
				_logger.LogInformation($"Fetching category with ID: {id}");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync($"Categories({id})?$expand=ParentCategory");

				if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					_logger.LogWarning($"Category with ID {id} not found");
					return null;
				}

				response.EnsureSuccessStatusCode();
				var content = await response.Content.ReadAsStringAsync();
				var category = JsonConvert.DeserializeObject<Category>(content);

				_logger.LogInformation($"Successfully fetched category: {category?.CategoryName}");
				return category;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error fetching category {id}");
				throw new Exception($"Failed to retrieve category with ID {id}", ex);
			}
		}

		public async Task<bool> CreateCategoryAsync(Category category)
		{
			if (category == null)
			{
				throw new ArgumentNullException(nameof(category));
			}

			try
			{
				_logger.LogInformation($"Creating category: {category.CategoryName}, IsActive: {category.IsActive}");

				var client = CreateAuthorizedClient();

				// Prepare the category for creation - ensure IsActive is properly set
				var categoryToCreate = new
				{
					CategoryName = category.CategoryName?.Trim(),
					CategoryDesciption = category.CategoryDesciption?.Trim(),
					ParentCategoryId = category.ParentCategoryId,
					IsActive = category.IsActive ?? true // ← Đảm bảo có giá trị mặc định
				};

				var json = JsonConvert.SerializeObject(categoryToCreate, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Include // ← Đổi thành Include để gửi cả null values
				});

				_logger.LogInformation($"Sending JSON: {json}");

				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await client.PostAsync("Categories", content);

				// Log response
				var responseContent = await response.Content.ReadAsStringAsync();
				_logger.LogInformation($"Response: {response.StatusCode}, Content: {responseContent}");

				var success = response.IsSuccessStatusCode;

				if (success)
				{
					_logger.LogInformation($"Successfully created category: {category.CategoryName}");
				}
				else
				{
					_logger.LogWarning($"Failed to create category. Status: {response.StatusCode}, Error: {responseContent}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating category");
				throw new Exception("Failed to create category", ex);
			}
		}

		public async Task<bool> UpdateCategoryAsync(Category category)
		{
			if (category == null)
			{
				throw new ArgumentNullException(nameof(category));
			}

			try
			{
				_logger.LogInformation($"Updating category: {category.CategoryId}, IsActive: {category.IsActive}");

				var client = CreateAuthorizedClient();

				// Prepare the category for update - ensure IsActive is properly set
				var categoryToUpdate = new
				{
					CategoryId = category.CategoryId,
					CategoryName = category.CategoryName?.Trim(),
					CategoryDesciption = category.CategoryDesciption?.Trim(),
					ParentCategoryId = category.ParentCategoryId,
					IsActive = category.IsActive ?? true // ← Đảm bảo có giá trị mặc định
				};

				var json = JsonConvert.SerializeObject(categoryToUpdate, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Include // ← Đổi thành Include để gửi cả null values
				});

				_logger.LogInformation($"Sending JSON: {json}");

				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await client.PutAsync($"Categories({category.CategoryId})", content);

				// Log response
				var responseContent = await response.Content.ReadAsStringAsync();
				_logger.LogInformation($"Response: {response.StatusCode}, Content: {responseContent}");

				var success = response.IsSuccessStatusCode;

				if (success)
				{
					_logger.LogInformation($"Successfully updated category: {category.CategoryId}");
				}
				else
				{
					_logger.LogWarning($"Failed to update category. Status: {response.StatusCode}, Error: {responseContent}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating category {category.CategoryId}");
				throw new Exception($"Failed to update category with ID {category.CategoryId}", ex);
			}
		}

		public async Task<bool> DeleteCategoryAsync(int id)
		{
			try
			{
				_logger.LogInformation($"Deleting category: {id}");

				var client = CreateAuthorizedClient();
				var response = await client.DeleteAsync($"Categories({id})");

				var success = response.IsSuccessStatusCode;

				if (success)
				{
					_logger.LogInformation($"Successfully deleted category: {id}");
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to delete category. Status: {response.StatusCode}, Error: {errorContent}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting category {id}");
				throw new Exception($"Failed to delete category with ID {id}", ex);
			}
		}

		public async Task<List<Category>> SearchCategoriesAsync(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				return await GetCategoriesAsync();
			}

			try
			{
				_logger.LogInformation($"Searching categories with term: {searchTerm}");

				var client = CreateAuthorizedClient();
				var encodedSearchTerm = Uri.EscapeDataString(searchTerm);
				var response = await client.GetAsync($"Categories?$filter=contains(CategoryName,'{encodedSearchTerm}') or contains(CategoryDesciption,'{encodedSearchTerm}')&$expand=ParentCategory");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<Category>>(content);

				_logger.LogInformation($"Search returned {result?.Value?.Count ?? 0} categories for term: {searchTerm}");
				return result?.Value ?? new List<Category>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error searching categories with term: {searchTerm}");
				throw new Exception("Failed to search categories", ex);
			}
		}

		public async Task<List<Category>> GetActiveCategoriesAsync()
		{
			try
			{
				_logger.LogInformation("Fetching active categories");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync("Categories?$filter=IsActive eq true&$expand=ParentCategory");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<Category>>(content);

				_logger.LogInformation($"Successfully fetched {result?.Value?.Count ?? 0} active categories");
				return result?.Value ?? new List<Category>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching active categories");
				throw new Exception("Failed to retrieve active categories", ex);
			}
		}

		public async Task<List<Category>> GetRootCategoriesAsync()
		{
			try
			{
				_logger.LogInformation("Fetching root categories");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync("Categories?$filter=ParentCategoryId eq null");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<Category>>(content);

				_logger.LogInformation($"Successfully fetched {result?.Value?.Count ?? 0} root categories");
				return result?.Value ?? new List<Category>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching root categories");
				throw new Exception("Failed to retrieve root categories", ex);
			}
		}

		public async Task<List<Category>> GetSubCategoriesAsync(int parentId)
		{
			try
			{
				_logger.LogInformation($"Fetching subcategories for parent ID: {parentId}");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync($"Categories?$filter=ParentCategoryId eq {parentId}&$expand=ParentCategory");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<Category>>(content);

				_logger.LogInformation($"Successfully fetched {result?.Value?.Count ?? 0} subcategories for parent {parentId}");
				return result?.Value ?? new List<Category>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error fetching subcategories for parent {parentId}");
				throw new Exception($"Failed to retrieve subcategories for parent ID {parentId}", ex);
			}
		}

		public async Task<bool> IsCategoryInUseAsync(int categoryId)
		{
			try
			{
				_logger.LogInformation($"Checking if category {categoryId} is in use");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync($"NewsArticles?$filter=CategoryId eq {categoryId}&$top=1");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<NewsArticle>>(content);

				var isInUse = result?.Value?.Any() == true;
				_logger.LogInformation($"Category {categoryId} is {(isInUse ? "in use" : "not in use")}");

				return isInUse;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error checking if category {categoryId} is in use");
				throw new Exception($"Failed to check if category {categoryId} is in use", ex);
			}
		}

		public async Task<bool> ToggleCategoryStatusAsync(int id)
		{
			try
			{
				var category = await GetCategoryByIdAsync(id);
				if (category == null)
				{
					_logger.LogWarning($"Cannot toggle status - category {id} not found");
					return false;
				}

				category.IsActive = !category.IsActive;

				var success = await UpdateCategoryAsync(category);

				if (success)
				{
					_logger.LogInformation($"Successfully toggled status for category {id} to {category.IsActive}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error toggling status for category {id}");
				throw new Exception($"An error occurred while updating the category status.", ex);
			}
		}

		// Legacy methods to maintain compatibility with existing code
		public async Task<Category> GetByIdAsync(int id)
		{
			return await GetCategoryByIdAsync(id);
		}

		public async Task AddAsync(Category category)
		{
			var success = await CreateCategoryAsync(category);
			if (!success)
			{
				throw new Exception("Failed to create category");
			}
		}

		public async Task UpdateAsync(Category category)
		{
			var success = await UpdateCategoryAsync(category);
			if (!success)
			{
				throw new Exception("Failed to update category");
			}
		}

		public async Task DeleteAsync(int id)
		{
			var success = await DeleteCategoryAsync(id);
			if (!success)
			{
				throw new Exception("Failed to delete category");
			}
		}
	}
}