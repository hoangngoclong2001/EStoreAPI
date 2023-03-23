using Microsoft.AspNetCore.Mvc;

namespace eStore.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult OrderManager()
        {
            return View();
        }
        public IActionResult OrderDetail()
        {
            return View();
        }
    }
}
