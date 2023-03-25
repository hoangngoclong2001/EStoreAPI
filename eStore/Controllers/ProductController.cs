using BusinessObject.Models;
using BusinessObject.Req;
using BusinessObject.Res;
using eStore.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Drawing;
using System.Net.Http.Headers;
using System.Text;

namespace eStore.Controllers
{
    public class ProductController : Controller
    {

        public async Task<IActionResult> Products([FromQuery] PaginationParams @params, string? search, int? categoryId, int item)
        {
            if (@params.ItemsPerPage == 0) @params.ItemsPerPage = 10;
            if (item > 10) @params.ItemsPerPage = item;

            var conn = string.IsNullOrEmpty(search)
                ? $"api/Products?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}"
                : $"api/Products?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}&productName={search}";
            conn = categoryId is null
               ? $"{conn}"
               : $"{conn}&categoryId={categoryId}";
            var _conn = $"api/Categories/selectlist";
            var Res = await ResponseConfig.GetData(conn);
            var _Res = await ResponseConfig.GetData(_conn);
            var products = JsonConvert.DeserializeObject<List<ProductRes>>(Res.Content.ReadAsStringAsync().Result);
            var pagination = JsonConvert.DeserializeObject<PaginationMetadata>(Res.Headers.GetValues("X-Pagination").FirstOrDefault()!);
            List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
            ViewData["search"] = search;
            ViewData["pagination"] = pagination;
            ViewBag.categories = category;
            ViewBag.categoryId = categoryId;
            ViewBag.Item = item;
            return View(products);
        }

        [HttpGet]
        [Route("/Product/delete/{id}")]
        public async Task<IActionResult> productdelete(string id)
        {
            var conn = $"api/Products/delete/{id}";


            var Res = await ResponseConfig.GetData(conn);

            if (!Res.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status500InternalServerError);
            ViewData["deleteProduct"] = "Can't delete because this product are in " + Res + " Order";
            return Redirect("/Product/Products");
        }


        public async Task<IActionResult> EditProduct(int id)
        {
            var conn = $"api/products/{id}";
            var _conn = $"api/Categories/selectlist";
            var Res = await ResponseConfig.GetData(conn);
            var _Res = await ResponseConfig.GetData(_conn);
            var product = JsonConvert.DeserializeObject<ProductRes>(Res.Content.ReadAsStringAsync().Result);
            List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
            ViewBag.categories = category;
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(ProductReq pmp, [FromForm] IFormFile fileImage)
        {
            var bytes = new byte[fileImage.OpenReadStream().Length + 1];
            fileImage.OpenReadStream().Read(bytes, 0, bytes.Length);
            ProductReq req = new ProductReq
            {
                ProductId = pmp.ProductId,
                ProductName = pmp.ProductName,
                UnitPrice = pmp.UnitPrice,
                UnitsOnOrder = pmp.UnitsOnOrder,
                QuantityPerUnit = pmp.QuantityPerUnit,
                ReorderLevel = pmp.ReorderLevel,
                UnitsInStock = pmp.UnitsInStock,
                Discontinued = pmp.Discontinued,
                CategoryId = pmp.CategoryId,
                Picture = bytes
            };

            var _conn = $"api/products/{req.ProductId}";
            var Res = await ResponseConfig.PutData(_conn, JsonConvert.SerializeObject(req));
            return RedirectToAction("Products");
        }


        public async Task<IActionResult> AddProduct()
        {

            var _conn = $"api/Categories/selectlist";
            var _Res = await ResponseConfig.GetData(_conn);
            List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
            ViewBag.categories = category;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(ProductReq pmp, [FromForm] IFormFile fileImage)
        {

            var bytes = new byte[fileImage.OpenReadStream().Length + 1];
            fileImage.OpenReadStream().Read(bytes, 0, bytes.Length);
            ProductReq req = new ProductReq
            {
                ProductName = pmp.ProductName,
                UnitPrice = pmp.UnitPrice,
                UnitsOnOrder = pmp.UnitsOnOrder,
                QuantityPerUnit = pmp.QuantityPerUnit,
                ReorderLevel = pmp.ReorderLevel,
                UnitsInStock = pmp.UnitsInStock,
                Discontinued = pmp.Discontinued,
                CategoryId = pmp.CategoryId,
                Picture = bytes
            };

            var _conn = $"api/products";
            var Res = await ResponseConfig.PostData(_conn, JsonConvert.SerializeObject(req));
            return RedirectToAction("Products");
        }
    }
}
