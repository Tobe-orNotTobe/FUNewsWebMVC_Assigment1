using FUNewsWebMVC.Models;

namespace FUNewsWebMVC.ViewModels
{
    public class CategoryListViewModel
    {
        public List<Category> Categories { get; set; } = new List<Category>();
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; }
    }

}
