using BusinessObject.Models;
using BusinessObject.Req;
using BusinessObject.Res;
using DocumentFormat.OpenXml.Wordprocessing;
using eStore.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace eStore.Controllers
{
    public class CustomerController : Controller
    {
        private readonly string BaseUrl = "https://localhost:7177/";

        [Authorize(Roles = "1")]
        public IActionResult Customers([FromQuery] PaginationParams @params, string search, string title, int item)
        {
            if (@params.ItemsPerPage == 0) @params.ItemsPerPage = 10;
            if (item > 10) @params.ItemsPerPage = item;

            var conn = string.IsNullOrEmpty(search)
                ? $"api/Customers?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}"
                : $"api/Customers?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}&name={search}";
            conn = string.IsNullOrEmpty(title)
                ? $"{conn}"
                : $"{conn}&title={title}";
            var _conn = $"api/Customers/select";
            var Res = ResponseConfig.GetData(conn).Result;
            var _Res = ResponseConfig.GetData(_conn).Result;
            var customers = JsonConvert.DeserializeObject<List<CusRes>>(Res.Content.ReadAsStringAsync().Result);
            var pagination = JsonConvert.DeserializeObject<PaginationMetadata>(Res.Headers.GetValues("X-Pagination").FirstOrDefault()!);
            HashSet<CusSelectRes>? listTitle = JsonConvert.DeserializeObject<HashSet<CusSelectRes>>(_Res.Content.ReadAsStringAsync().Result);

            ViewData["search"] = search;
            ViewData["pagination"] = pagination;
            listTitle = listTitle!.DistinctBy(x => x.ContactTitle).ToHashSet();
            ViewBag.Title = listTitle;
            ViewBag.Contact = title;
            ViewBag.Item = item;
            return View(customers);
        }

        [Authorize(Roles = "1")]
        public IActionResult Status(string id)
        {
            var conn = $"api/Customers/{id}";
            var Res = ResponseConfig.GetData(conn).Result;
            var cus = JsonConvert.DeserializeObject<CusRes>(Res.Content.ReadAsStringAsync().Result);
            CusReq req = new CusReq
            {
                CustomerId = cus!.CustomerId,
                CompanyName = cus!.CompanyName,
                ContactName = cus.ContactName!.Contains("(deactive)") 
                ? $"{cus!.ContactName.Split("(")[0]}" 
                : $"{cus!.ContactName}(deactive)",
                ContactTitle = cus.ContactTitle,
                Address = cus!.Address,

            };
            var _Res = ResponseConfig.PutData(conn, JsonConvert.SerializeObject(req));

            return RedirectToAction("Customers");

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
