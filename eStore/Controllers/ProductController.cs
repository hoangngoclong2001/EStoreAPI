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
        private static readonly string BaseUrl = "https://localhost:7177/";

        public IActionResult Products([FromQuery] PaginationParams @params, string? search, int? categoryId, int item)
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
            var Res = GetData(conn).Result;
            var _Res = GetData(_conn).Result;
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
        public IActionResult productdelete(string id)
        {
            var conn = $"api/Products/delete/{id}";


            var Res = ResponseConfig.GetData(conn).Result;

            if (!Res.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status500InternalServerError);
            ViewData["deleteProduct"] = "Can't delete because this product are in " + Res + " Order";
            return Redirect("/Product/Products");
        }


        public IActionResult EditProduct(int id)
        {
            var conn = $"api/products/{id}";

            var _conn = $"api/Categories/selectlist";
            var Res = ResponseConfig.GetData(conn).Result;
            var _Res = ResponseConfig.GetData(_conn).Result;
            var product = JsonConvert.DeserializeObject<ProductRes>(Res.Content.ReadAsStringAsync().Result);
            List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
            ViewBag.categories = category;
            return View(product);
        }

        [HttpPost]
        public IActionResult EditProduct(ProductReq pmp, [FromForm] IFormFile fileImage)
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
            var Res = ResponseConfig.PutData(_conn, JsonConvert.SerializeObject(req));

            return RedirectToAction("Products");
        }


        public IActionResult AddProduct()
        {

            var _conn = $"api/Categories/selectlist";
            var _Res = ResponseConfig.GetData(_conn).Result;
            List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
            ViewBag.categories = category;
            return View();
        }

        [HttpPost]
        public IActionResult AddProduct(ProductReq pmp, [FromForm] IFormFile fileImage)
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
            var Res = ResponseConfig.PostData(_conn, JsonConvert.SerializeObject(req));

            return RedirectToAction("Products");
        }

        public async Task<HttpResponseMessage> GetData(string targetAddress)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(HttpContext.Request.Cookies["accessToken"]))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HttpContext.Request.Cookies["accessToken"]);
            }
            var Response = await client.GetAsync(targetAddress);
            return Response;
        }

        public async Task<HttpResponseMessage> PostData(string targetAddress, string content)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(HttpContext.Request.Cookies["accessToken"]))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HttpContext.Request.Cookies["accessToken"]);
            }
            var Response = await client.PostAsync(targetAddress, new StringContent(content, Encoding.UTF8, "application/json"));
            return Response;
        }

        public async Task<HttpResponseMessage> UploadData(string targerAddress, byte[] bytes, string fileName)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var multiPartFromDataContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            multiPartFromDataContent.Add(fileContent, "file", fileName);
            if (!string.IsNullOrEmpty(HttpContext.Request.Cookies["accessToken"]))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HttpContext.Request.Cookies["accessToken"]);
            }
            var Response = await client.PostAsync(targerAddress, multiPartFromDataContent);
            return Response;
        }

        public async Task<HttpResponseMessage> PutData(string targetAddress, string content)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(HttpContext.Request.Cookies["accessToken"]))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HttpContext.Request.Cookies["accessToken"]);
            }
            var Response = await client.PutAsync(targetAddress, new StringContent(content, Encoding.UTF8, "application/json"));
            return Response;
        }

        public async Task<HttpResponseMessage> DeleteData(string targetAddress, string content)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(HttpContext.Request.Cookies["accessToken"]))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HttpContext.Request.Cookies["accessToken"]);
            }
            var Response = await client.DeleteAsync(targetAddress);
            return Response;
        }

    }
}
