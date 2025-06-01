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

        public async Task<IActionResult> Index(int pageIndex = 1)
        {
            PageIndex = pageIndex;
            var accounts = await _service.GetAccountsAsync();
            TotalCount = accounts.Count;

            var pagedAccounts = accounts
                                .Skip((pageIndex - 1) * PageSize)
                                .Take(PageSize)
                                .ToList();

            var model = new SystemAccountListViewModel
            {
                Accounts = pagedAccounts,
                PageIndex = pageIndex,
                TotalPages = TotalPages
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Action = "Create";
            return PartialView("_SystemAccountPartial", new SystemAccount());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SystemAccount account)
        {
            if (ModelState.IsValid)
            {
                await _service.AddAsync(account);
                return Json(new { success = true });
            }
            return PartialView("_SystemAccountPartial", account);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(short id)
        {
            var account = await _service.GetByIdAsync(id);
            if (account == null) return NotFound();

            ViewBag.Action = "Edit";
            return PartialView("_SystemAccountPartial", account);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SystemAccount account)
        {
            if (ModelState.IsValid)
            {
                await _service.UpdateAsync(account);
                return Json(new { success = true });
            }
            return PartialView("_SystemAccountPartial", account);
        }

        public async Task<IActionResult> Detail(short id)
        {
            var account = await _service.GetByIdAsync(id);
            if (account == null) return NotFound();

            return View(account);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            await _service.DeleteAsync(id);
            return Json(new { success = true });
        }
    }
}
