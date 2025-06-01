using FUNewsWebMVC.Models;

namespace FUNewsWebMVC.ViewModels
{
    public class SystemAccountListViewModel
    {
        public List<SystemAccount> Accounts { get; set; } = new();
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
    }
}
