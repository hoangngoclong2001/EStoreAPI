using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using eStore.Models;
using BusinessObject.Req;
using Newtonsoft.Json;
using eStore.Config;
using BusinessObject.Res;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusinessObject.Models;
using System.Net.Http.Headers;

namespace eStore.Controllers;

public class HomeController : Controller
{
    private readonly IConfiguration configuration;
    public HomeController(IConfiguration configuration)
    {
        this.configuration = configuration;
    }
    public static async Task<HttpResponseMessage> GetData(string targetAddress)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri("https://localhost:7177/");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var Response = await client.GetAsync(targetAddress);
        return Response;
    }
    public IActionResult Index([FromQuery] PaginationParams @params, string? search, int? categoryId)
    {
        if (@params.ItemsPerPage == 0) @params.ItemsPerPage = 8;
        var conn2 = string.IsNullOrEmpty(search)
               ? $"api/Products?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}"
               : $"api/Products?Page={@params.Page}&ItemsPerPage={@params.ItemsPerPage}&productName={search}";
        conn2 = categoryId is null
               ? $"{conn2}"
               : $"{conn2}&categoryId={categoryId}";

        var conn1 = $"api/Products/sale";
        var conn = $"api/Products/top4";
        var _conn = $"api/Categories/selectlist";
        var _Res2 = GetData(conn2).Result;
        var Res = ResponseConfig.GetData(conn).Result;
        var _Res = ResponseConfig.GetData(_conn).Result;
        var _Res1 = ResponseConfig.GetData(conn1).Result;
        var products = JsonConvert.DeserializeObject<List<ProductRes>>(_Res2.Content.ReadAsStringAsync().Result);
        List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
        List<ProductRes>? productlastest = JsonConvert.DeserializeObject<List<ProductRes>>(Res.Content.ReadAsStringAsync().Result);
        List<ProductRes>? productSales = JsonConvert.DeserializeObject<List<ProductRes>>(_Res1.Content.ReadAsStringAsync().Result);
        var pagination = JsonConvert.DeserializeObject<PaginationMetadata>(_Res2.Headers.GetValues("X-Pagination").FirstOrDefault()!);
        ViewBag.categories = category;
        ViewBag.categoryId = categoryId;
        ViewData["search"] = search;
        ViewData["pagination"] = pagination;
        ViewBag.productlastest = productlastest;
        ViewBag.productSales = productSales;
        return View(products);
    }
    public IActionResult Login()
    {
        return View();
    }
    public IActionResult Profile()
    {
        return View();
    }
    public IActionResult Forgot()
    {
        return View();
    }
    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult Cart()
    {
        return View();
    }
    public IActionResult Detail()
    {

        return View();
    }
    [HttpGet]
    [Route("/product/detail/{id}")]
    public IActionResult Detail(string id)
    {
        var conn = $"api/Products/{id}";

        var _conn = $"api/Categories/selectlist";
        var _Res = ResponseConfig.GetData(_conn).Result;
        var Res = ResponseConfig.GetData(conn).Result;

        ProductRes products = JsonConvert.DeserializeObject<ProductRes>(Res.Content.ReadAsStringAsync().Result);

        List<CateSelectRes>? category = JsonConvert.DeserializeObject<List<CateSelectRes>>(_Res.Content.ReadAsStringAsync().Result);
        ViewBag.categories = category;
        ViewBag.products = products;
        return View();
    }


    [HttpPost]
    public IActionResult Login(AuthReq req)
    {
        var Res = ResponseConfig.PostData("api/Accounts/signin", JsonConvert.SerializeObject(req));
        if (!Res.Result.IsSuccessStatusCode)
            return StatusCode(StatusCodes.Status500InternalServerError);

        var user = JsonConvert.DeserializeObject<UserRes>(Res.Result.Content.ReadAsStringAsync().Result);

        HttpContext.Session.SetString("user", Res.Result.Content.ReadAsStringAsync().Result);

        validateToken(user!.AccessToken!.Replace("\"", ""));

        Response.Cookies.Append("refreshToken", user.RefreshToken!, new CookieOptions 
        { Expires = user.TokenExpires, HttpOnly = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict });
        return RedirectToAction("index");
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private void validateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["JWT:Issuer"],
                ValidAudience = configuration["JWT:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"])),
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var expires = jwtToken.ValidTo;
            var role = jwtToken.Claims.First(x => x.Type == ClaimTypes.Role);
            var email = jwtToken.Claims.First(x => x.Type == ClaimTypes.Email);
            List<ClaimsIdentity> identities = new List<ClaimsIdentity>
                {
                    new ClaimsIdentity(new[] { role }),
                    new ClaimsIdentity(new[] { email })
                };

            User.AddIdentities(identities);

            Response.Cookies.Append("accessToken", token, new CookieOptions 
            { Expires = expires, HttpOnly = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict });

        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
