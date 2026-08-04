using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Quản_lý_quán_cafe.Areas.Customer.ViewModels;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Areas.Customer.Controllers;

[Area("Customer")]
public class ReservationsController(
    IReservationService reservationService,
    IRestaurantTableRepository tableRepository,
    ICompositeViewEngine viewEngine,
    Quản_lý_quán_cafe.Services.CustomerSessionService customerSessionService) : Controller
{
    private readonly ICompositeViewEngine _viewEngine = viewEngine;
    private readonly Quản_lý_quán_cafe.Services.CustomerSessionService _customerSessionService = customerSessionService;
    [HttpGet]
    public async Task<IActionResult> History()
    {
        if (!IsLoggedIn()) return RedirectToLogin();
        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        var reservations = await reservationService.GetCustomerReservationsAsync(customer.CustomerID);
        return View("History", reservations);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsLoggedIn()) return RedirectToLogin();
        var model = new ReservationViewModel();
        await LoadTablesAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationViewModel model)
    {
        if (!IsLoggedIn()) return RedirectToLogin();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        var result = await reservationService.CreateReservationAsync(
            customer.CustomerID,
            model.TableID,
            model.ReservationDate,
            model.NumberOfGuests,
            model.Notes);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            await LoadTablesAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(History));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsLoggedIn()) return RedirectToLogin();
        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        var reservation = await reservationService.GetReservationDetailsAsync(id, customer.CustomerID);

        if (reservation == null)
            return NotFound();

        return View(reservation);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!IsLoggedIn()) return RedirectToLogin();
        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        var result = await reservationService.CancelReservationAsync(id, customer.CustomerID);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(History));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(History));
    }

    [HttpPost, ValidateAntiForgeryTokenFromHeader]
    public async Task<IActionResult> SearchAvailableTables([FromBody] SearchAvailableTablesRequest request)
    {
        if (!IsLoggedIn())
            return Json(new { success = false, message = "Vui lòng đăng nhập." });

        if (request?.ReservationDate == null || request.NumberOfGuests <= 0)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

        try
        {
            var availableTables = await reservationService.GetAvailableTablesAsync(
                request.ReservationDate,
                request.NumberOfGuests,
                request.DurationMinutes ?? 120);

            var viewModels = availableTables
                .Select(t => new AvailableTableViewModel
                {
                    TableID = t.TableID,
                    TableNumber = t.TableNumber,
                    Capacity = t.Capacity,
                    Location = t.Location,
                    IsSelected = false
                })
                .OrderBy(t => t.TableNumber)
                .ToList();

            var html = await RenderPartialViewToStringAsync("_AvailableTablesList", viewModels);

            return Json(new
            {
                success = true,
                data = new
                {
                    tables = viewModels,
                    html = html,
                    count = viewModels.Count
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    private async Task<string> RenderPartialViewToStringAsync(string viewName, object model)
    {
        var modelMetadataProvider = HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
        var viewData = new ViewDataDictionary<object>(
            metadataProvider: modelMetadataProvider,
            modelState: new ModelStateDictionary())
        {
            Model = model
        };

        using (var sw = new StringWriter())
        {
            var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);

            if (viewResult.View == null)
                return string.Empty;

            var tempDataProvider = HttpContext.RequestServices.GetRequiredService<ITempDataProvider>();
            var viewContext = new ViewContext(
                ControllerContext,
                viewResult.View,
                viewData,
                new TempDataDictionary(HttpContext, tempDataProvider),
                sw,
                new HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return sw.GetStringBuilder().ToString();
        }
    }

    private async Task LoadTablesAsync(ReservationViewModel model)
    {
        var tables = await tableRepository.GetAllAsync();
        model.Tables = tables
            .Where(t => t.TableStatus != TableStatus.Maintenance)
            .OrderBy(t => t.TableNumber)
            .Select(t => new SelectListItem(
                $"{t.TableNumber} - {t.Capacity} khách - {t.Location}",
                t.TableID.ToString()))
            .ToList();
    }

    private bool IsLoggedIn() => (HttpContext.Session.GetInt32("UserId") ?? 0) > 0;
    private IActionResult RedirectToLogin() => RedirectToAction("Login", "Account", new { area = "" });

    private async Task<Models.Entities.Customer> GetOrCreateCustomerAsync()
    {
        return await _customerSessionService.GetOrCreateCustomerAsync();
    }
}
