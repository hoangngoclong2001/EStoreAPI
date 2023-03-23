using BusinessObject.Models;
using BusinessObject.Res;
using eStore.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace eStore.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Products([FromQuery] PaginationParams @params, string? search, int? categoryId)
        {
            if (@params.ItemsPerPage == 0) @params.ItemsPerPage = 8;

            var conn = string.IsNullOrEmpty(search)
                ? $"api/Products?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}"
                : $"api/Products?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}&productName={search}";
            conn = categoryId is null
               ? $"{conn}"
               : $"{conn}&categoryId={categoryId}";
            var _conn = $"api/Categories/selectlist";
            var Res = ResponseConfig.GetData(conn).Result;
            var _Res = ResponseConfig.GetData(_conn).Result;
            var products = JsonConvert.DeserializeObject<List<ProductRes>>(Res.Content.ReadAsStringAsync().Result);
            var pagination = JsonConvert.DeserializeObject<PaginationMetadata>(Res.Headers.GetValues("X-Pagination").FirstOrDefault()!);
            List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
            ViewData["search"] = search;
            ViewData["pagination"] = pagination;
            ViewBag.categories = category;
            ViewBag.categoryId = categoryId;
            return View(products);
        }

        [HttpGet]
        [Route("/Product/delete/{id}")]
        public IActionResult productdelete(string id)
        {
            var conn = $"api/Products/delete/{id}";


            var Res = ResponseConfig.GetData(conn).Result;

            if (!Res.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status500InternalServerError);
            ViewData["deleteProduct"] = "Can't delete because this product are in " + Res + " Order";
            return Redirect("/Product/Products");
        }
        [HttpGet]
        [Route("/product/create")]
        public IActionResult AddProduct()
        {
            var _conn = $"api/Categories/selectlist";
            var _Res = ResponseConfig.GetData(_conn).Result;
            List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
            ViewBag.categories = category;
            return View();
        }
        [HttpPost]
        [Route("/product/create")]
        public IActionResult AddProduct(Product product)
        {
            List<SelectListItem> categories = JsonConvert.DeserializeObject<List<SelectListItem>>(ResponseConfig.GetData("api/categories/selectlist").Result.Content.ReadAsStringAsync().Result);
            ViewData["categories"] = categories;


            if (!ModelState.IsValid)
            {
                foreach (var error in ViewData.ModelState.Values.SelectMany(modelState => modelState.Errors))
                {
                    string err = error.ErrorMessage;
                }
            }

            var Res = ResponseConfig.PostData("api/products/create", JsonConvert.SerializeObject(product));

            if (!Res.Result.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status500InternalServerError);

            return RedirectToAction("Product/Proudcts");
        }
        [HttpGet]
        [Route("/product/edit/{id}")]
        public IActionResult EditProduct(string id)
        {
            var conn = $"api/Products/{id}";

            var _conn = $"api/Categories/selectlist";
            var _Res = ResponseConfig.GetData(_conn).Result;
            var Res = ResponseConfig.GetData(conn).Result;

            ProductRes products = JsonConvert.DeserializeObject<ProductRes>(Res.Content.ReadAsStringAsync().Result);

            List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
            ViewBag.categories = category;

            return View(products);
        }


    }
}
