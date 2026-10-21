using Microsoft.AspNetCore.Mvc;

namespace CanteenX.Controllers
{
    public class CanteenOwnerController : Controller
    {
        public IActionResult CanteenOwnerDashboard()
        {
            return View();
        }
    }
}
