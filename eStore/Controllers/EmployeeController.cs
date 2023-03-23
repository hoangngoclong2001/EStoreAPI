using BusinessObject.Models;
using BusinessObject.Req;
using BusinessObject.Res;
using eStore.Config;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Drawing;
using System.Net.Http.Headers;
using System.Text;

namespace eStore.Controllers
{
    public class EmployeeController : Controller
    {
        private static readonly string BaseUrl = "https://localhost:7177/";

        public IActionResult Employees(
            [FromQuery] PaginationParams @params,
            int item,
            string? search,
            int? dep,
            string? courtesy,
            string? title,
            DateTime? from,
            DateTime? to)
        {

            if (@params.ItemsPerPage == 0) @params.ItemsPerPage = 10;
            if (item > 10) @params.ItemsPerPage = item;
            var conn = string.IsNullOrEmpty(search)
                ? $"api/Employees?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}"
                : $"api/Employees?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}&name={search}";
            
            conn = dep is null
                ? $"{conn}"
                : $"{conn}&dep={dep}";
            conn = courtesy is null
                ? $"{conn}"
                : $"{conn}&courtesy={courtesy}";
            conn = title is null
                ? $"{conn}"
                : $"{conn}&title={title}";
            conn = from is null
                ? $"{conn}"
                : $"{conn}&from={DateTime.Parse(from.ToString()!).ToString("MM/dd/yyyy")}";
            conn = to is null
                ? $"{conn}"
                : $"{conn}&to={DateTime.Parse(to.ToString()!).ToString("MM/dd/yyyy")}";

            var _conn = $"api/Departments/selectlist";
            var Res = GetData(conn).Result;
            var _Res = GetData(_conn).Result;

            var employees = JsonConvert.DeserializeObject<List<EmpRes>>(Res.Content.ReadAsStringAsync().Result);
            var pagination = JsonConvert.DeserializeObject<PaginationMetadata>(Res.Headers.GetValues("X-Pagination").FirstOrDefault()!);
            List<DepSelectRes>? depart = JsonConvert.DeserializeObject<List<DepSelectRes>>(_Res.Content.ReadAsStringAsync().Result);

            ViewData["search"] = search;
            if (from is not null) ViewData["from"] = DateTime.Parse(from.ToString()!).ToString("yyyy-MM-dd");
            if (to is not null) ViewData["to"] = DateTime.Parse(to.ToString()!).ToString("yyyy-MM-dd");
            ViewData["courtesy"] = courtesy;
            ViewData["title"] = title;
            ViewBag.departments = depart;
            ViewBag.Dep = dep;
            ViewBag.Item = item;
            ViewData["pagination"] = pagination;
            return View(employees);
        }

        public IActionResult Upload(IFormFile? file)
        {
             if (file == null) return RedirectToAction("Employees");
             var bytes = new byte[file.OpenReadStream().Length + 1];
             file.OpenReadStream().Read(bytes, 0, bytes.Length);
            var Res = UploadData("api/Accounts/import-employees", bytes, file.FileName);
            return RedirectToAction("Employees");
        }

        public IActionResult Edit(int id)
        {
            var conn = $"api/Employees/{id}";
            var _conn = $"api/Departments/selectlist";
            var Res = GetData(conn).Result;
            var _Res = GetData(_conn).Result;
            var employee = JsonConvert.DeserializeObject<EmpRes>(Res.Content.ReadAsStringAsync().Result);
            List<DepSelectRes>? depart = JsonConvert.DeserializeObject<List<DepSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
            ViewBag.departments = depart;
            ViewData["BirthDate"] = DateTime.Parse(employee!.BirthDateString!).ToString("yyyy-MM-dd");
            ViewData["HireDate"] = DateTime.Parse(employee!.HireDateString!).ToString("yyyy-MM-dd");
            return View(employee);
        }
        [HttpPost]
        public IActionResult Edit(EmpRes emp)
        {
            EmpReq req = new EmpReq
            {
                EmployeeId= emp.EmployeeId,
                LastName= emp.LastName,
                FirstName= emp.FirstName,
                DepartmentId= emp.DepartmentId,
                Title= emp.Title,
                TitleOfCourtesy= emp.TitleOfCourtesy,
                BirthDate= emp.BirthDate,
                HireDate= emp.HireDate,
                Address= emp.Address,
            };

            EmpSignUpReq empSignUpReq = new EmpSignUpReq
            {
                EmployeeId = emp.EmployeeId,
                Email= emp.Email,
                Password= emp.Password,
            };
            var conn = $"api/Employees/{emp.EmployeeId}";
            var _conn = $"api/Accounts/add-edit-account";
            var Res = PutData(conn, JsonConvert.SerializeObject(req));
            if(emp.Password is not null)
            {
                var _Res = PostData(_conn, JsonConvert.SerializeObject(empSignUpReq));
            }
            return RedirectToAction("Employees"); 
        }

        public IActionResult Status(int id)
        {
            var conn = $"api/Employees/{id}";
            var Res = ResponseConfig.GetData(conn).Result;
            var emp = JsonConvert.DeserializeObject<EmpRes>(Res.Content.ReadAsStringAsync().Result);
            EmpReq req = new EmpReq
            {
                EmployeeId = emp!.EmployeeId,
                LastName = emp.LastName,
                FirstName = emp.FirstName,
                DepartmentId = emp.DepartmentId,
               
                Title = emp.Title!.Contains("(deactive)") 
                ? $"{emp.Title!.Split("(")[0]}" 
                : $"{emp.Title!}(deactive)",
                TitleOfCourtesy = emp.TitleOfCourtesy,
                BirthDate = emp.BirthDate,
                HireDate = emp.HireDate,
                Address = emp.Address,
            };
            var _Res = PutData(conn, JsonConvert.SerializeObject(req));
            return RedirectToAction("Employees");
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
