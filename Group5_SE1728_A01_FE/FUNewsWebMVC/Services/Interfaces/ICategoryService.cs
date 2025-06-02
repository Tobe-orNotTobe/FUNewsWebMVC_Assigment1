using FUNewsWebMVC.Models;

namespace FUNewsWebMVC.Services.Interfaces
{
    public interface ICategoryService
    {
		Task<List<Category>> GetCategoriesAsync();
		Task<Category?> GetCategoryByIdAsync(int id);
		Task<bool> CreateCategoryAsync(Category category);
		Task<bool> UpdateCategoryAsync(Category category);
		Task<bool> DeleteCategoryAsync(int id);
		Task<List<Category>> SearchCategoriesAsync(string searchTerm);
	}

}
