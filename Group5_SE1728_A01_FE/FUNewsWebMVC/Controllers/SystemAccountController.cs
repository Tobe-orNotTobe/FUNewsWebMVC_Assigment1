using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using FUNewsWebMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Controllers
{
    [AuthFilter]
    public class SystemAccountController : Controller
    {
        private readonly ISystemAccountService _service;

        public SystemAccountController(ISystemAccountService service)
        {
            _service = service;
        }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 3;
        public int PageIndex { get; set; } = 1;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize); 

        public async Task<IActionResult> Index(string? searchTerm, int pageIndex = 1)
        {
            var accounts = await _service.GetAccountsAsync();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                accounts = accounts
                    .Where(c => c.AccountName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            PageIndex = pageIndex;

            TotalCount = accounts.Count;

            var pagedAccounts = accounts
               .Skip((pageIndex - 1) * PageSize)
               .Take(PageSize)
               .ToList();
            var model = new SystemAccountListViewModel
            {
                Accounts = pagedAccounts,
                PageIndex = pageIndex,
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize),
                SearchTerm = searchTerm
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Action = "Create";
            return PartialView("_AccountForm", new SystemAccount());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SystemAccount account)
        {
            if (ModelState.IsValid)
            {
                await _service.AddAsync(account);
                return Json(new { success = true });
            }
            ViewBag.Action = "Create";
            return PartialView("_AccountForm", account);

        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var account = await _service.GetByIdAsync(id);
            return PartialView("_AccountForm", account);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SystemAccount account)
        {
            if (ModelState.IsValid)
            {
                await _service.UpdateAsync(account);
                return Json(new { success = true });
            }
            return PartialView("_AccountForm", account);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var account = await _service.GetByIdAsync(id);
            return View(account);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _service.DeleteAsync(id);
            return Json(new { success = true });
        }
    }
}
