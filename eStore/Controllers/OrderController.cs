using BusinessObject.Models;
using BusinessObject.Res;
using eStore.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace eStore.Controllers
{
    public class OrderController : Controller
    {

        [Authorize(Roles = "1")]
        public IActionResult OrderManager(
           [FromQuery] PaginationParams @params,
          
           DateTime? from,
           DateTime? to)
        {

            if (@params.ItemsPerPage == 0) @params.ItemsPerPage = 10;
            var conn = $"api/Orders?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}";
            conn = from is null
                ? $"{conn}"
                : $"{conn}&from={DateTime.Parse(from.ToString()!).ToString("MM/dd/yyyy")}";
            conn = to is null
                ? $"{conn}"
                : $"{conn}&to={DateTime.Parse(to.ToString()!).ToString("MM/dd/yyyy")}";

            var Res = ResponseConfig.GetData(conn).Result;

            var orders = JsonConvert.DeserializeObject<List<OrderRes>>(Res.Content.ReadAsStringAsync().Result);
            var pagination = JsonConvert.DeserializeObject<PaginationMetadata>(Res.Headers.GetValues("X-Pagination").FirstOrDefault()!);
         
            if (from is not null) ViewData["from"] = DateTime.Parse(from.ToString()!).ToString("yyyy-MM-dd");
            if (to is not null) ViewData["to"] = DateTime.Parse(to.ToString()!).ToString("yyyy-MM-dd");
            ViewData["pagination"] = pagination;
            return View(orders);
        }
    }
}
