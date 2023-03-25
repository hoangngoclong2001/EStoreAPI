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
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;

namespace eStore.Controllers;

public class HomeController : Controller

{
    private static readonly string BaseUrl = "https://localhost:7177/";

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
  
    [HttpGet]
    public IActionResult Profile()
    {
        var identity = (ClaimsIdentity)User.Identity;
        var claims = identity.Claims.ToList();
        var email = claims?.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        email= email ?? string.Empty;   
        var conn = $"api/Accounts/getEmail/{email}";
        var Res = ResponseConfig.GetData(conn).Result;
        var account = JsonConvert.DeserializeObject<AccRes>(Res.Content.ReadAsStringAsync().Result);
        var conn1 = $"api/Customers/{account.CustomerId}";
        var Res2 = ResponseConfig.GetData(conn1).Result;
        var cus = JsonConvert.DeserializeObject<CusRes>(Res2.Content.ReadAsStringAsync().Result);
        ViewBag.Customer = cus;
        return View(account);
    }
    [HttpGet]
    [Route("/ChangePass")]
    public IActionResult ChangePass()
    {
        return View();
    }

    [HttpGet]
    [Route("/cart")]
    public IActionResult cart()
    {
        return View();
    }

    [HttpPost]
    [Route("/cart")]
    public IActionResult cart([FromForm] OrderDto orderDto)
    {
        var allProductName = JsonConvert.DeserializeObject<List<string>>(GetData("api/Products/allProductName").Result.Content.ReadAsStringAsync().Result);
        if (allProductName == null)
            return StatusCode(StatusCodes.Status500InternalServerError);

        if (!string.IsNullOrEmpty(orderDto.action))
        {
            switch (orderDto.action)
            {
                case "BUY NOW":
                    if (!string.IsNullOrEmpty(orderDto.name) && allProductName.Contains(orderDto.name))
                    {
                        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("cart")))
                        {
                            List<OrderDetailDTO> cart = JsonConvert.DeserializeObject<List<OrderDetailDTO>>(HttpContext.Session.GetString("cart"));
                            foreach (var item in cart)
                            {
                                if (item.Product.ProductName == orderDto.name)
                                {
                                    item.Quantity++;
                                    item.Total = (decimal)item.Product.UnitPrice * item.Quantity;
                                    break;
                                }
                                else
                                {
                                    AddToCart(cart, orderDto.name);
                                    break;
                                }
                            }
                            HttpContext.Session.SetString("cart", JsonConvert.SerializeObject(cart));
                        }
                        else
                        {
                            List<OrderDetailDTO> cart = new List<OrderDetailDTO>();
                            AddToCart(cart, orderDto.name);
                            HttpContext.Session.SetString("cart", JsonConvert.SerializeObject(cart));
                        }
                    }
                    return cart();
                case "ADD TO CART":
                    if (!string.IsNullOrEmpty(orderDto.name) && allProductName.Contains(orderDto.name))
                    {
                        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("cart")))
                        {
                            List<OrderDetailDTO> cart = JsonConvert.DeserializeObject<List<OrderDetailDTO>>(HttpContext.Session.GetString("cart"));
                            foreach (var item in cart)
                            {
                                if (item.Product.ProductName == orderDto.name)
                                {
                                    item.Quantity++;
                                    item.Total = (decimal)item.Product.UnitPrice * item.Quantity;
                                    break;
                                }
                                else
                                {
                                    AddToCart(cart, orderDto.name);
                                    break;
                                }
                            }
                            HttpContext.Session.SetString("cart", JsonConvert.SerializeObject(cart));
                        }
                        else
                        {
                            List<OrderDetailDTO> cart = new List<OrderDetailDTO>();
                            AddToCart(cart, orderDto.name);
                            HttpContext.Session.SetString("cart", JsonConvert.SerializeObject(cart));
                        }
                    }
                    return Redirect("/cart" + orderDto.name);
                case "Remove":
                    if (!string.IsNullOrEmpty(orderDto.name) && allProductName.Contains(orderDto.name))
                    {
                        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("cart")))
                        {
                            List<OrderDetailDTO> cart = JsonConvert.DeserializeObject<List<OrderDetailDTO>>(HttpContext.Session.GetString("cart"));
                            foreach (var item in cart)
                            {
                                if (item.Product.ProductName == orderDto.name)
                                {
                                    cart.Remove(item);
                                    break;
                                }
                            }
                            HttpContext.Session.SetString("cart", JsonConvert.SerializeObject(cart));
                        }
                    }
                    return cart();
                case "+":
                    if (!string.IsNullOrEmpty(orderDto.name) && allProductName.Contains(orderDto.name))
                    {
                        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("cart")))
                        {
                            List<OrderDetailDTO> cart = JsonConvert.DeserializeObject<List<OrderDetailDTO>>(HttpContext.Session.GetString("cart"));
                            foreach (var item in cart)
                            {
                                if (item.Product.ProductName == orderDto.name)
                                {
                                    item.Quantity++;
                                    item.Total = (decimal)item.Product.UnitPrice * item.Quantity;
                                    break;
                                }
                            }
                            HttpContext.Session.SetString("cart", JsonConvert.SerializeObject(cart));
                        }
                    }
                    return cart();
                case "-":
                    if (!string.IsNullOrEmpty(orderDto.name) && allProductName.Contains(orderDto.name))
                    {
                        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("cart")))
                        {
                            List<OrderDetailDTO> cart = JsonConvert.DeserializeObject<List<OrderDetailDTO>>(HttpContext.Session.GetString("cart"));
                            foreach (var item in cart)
                            {
                                if (item.Product.ProductName == orderDto.name)
                                {
                                    item.Quantity--;
                                    item.Total = (decimal)item.Product.UnitPrice * item.Quantity;
                                    if (item.Quantity <= 0)
                                        cart.Remove(item);

                                    break;
                                }
                            }
                            if (cart.Count <= 0)
                                return Redirect("cart");

                            HttpContext.Session.SetString("cart", JsonConvert.SerializeObject(cart));
                        }
                    }
                    return cart();
                case "ORDER":
                    if (!string.IsNullOrEmpty(HttpContext.Session.GetString("cart")))
                    {
                        List<OrderDetailDTO> cart = JsonConvert.DeserializeObject<List<OrderDetailDTO>>(HttpContext.Session.GetString("cart"));
                        if (string.IsNullOrEmpty(HttpContext.Session.GetString("user")))
                            return Redirect("/Home/Index");

                        UserRes user = JsonConvert.DeserializeObject<UserRes>(HttpContext.Session.GetString("user"));
                        List<OrderDetail> orderDetail = new List<OrderDetail>();
                        foreach (var item in cart)
                        {
                            OrderDetail product = new OrderDetail
                            {
                                ProductId = item.Product.ProductId,
                                UnitPrice = (decimal)item.Product.UnitPrice,
                                Quantity = (short)item.Quantity,
                                Discount = 0
                            };
                            orderDetail.Add(product);
                        }

                        if (((ClaimsIdentity)User.Identity).HasClaim(ClaimTypes.Role, "2"))
                        {
                            Order order = new Order
                            {
                                CustomerId = user.Account.CustomerId,
                                OrderDate = DateTime.Now,
                                RequiredDate = DateTime.Now.AddDays(30),
                                ShipName = user.Account.Customer.CompanyName,
                                ShipAddress = user.Account.Customer.Address,
                                OrderDetails = orderDetail
                            };

                            var Res = PostData("api/Orders/"+ user.Account.Email, JsonConvert.SerializeObject(order));
                            if (!Res.Result.IsSuccessStatusCode)
                                return StatusCode(StatusCodes.Status500InternalServerError);

                            Order confirmedOrder = JsonConvert.DeserializeObject<Order>(Res.Result.Content.ReadAsStringAsync().Result);

                            //string body = WriteEmailBody(confirmedOrder);

                            //MailMessage mail = new MailMessage();
                            //mail.To.Add("nghiahthe153608@fpt.edu.vn");
                            //mail.From = new MailAddress("nghiahthe153608@fpt.edu.vn");
                            //mail.Subject = "Invoice Details";
                            //mail.Body = body;
                            //mail.IsBodyHtml = true;
                            //smtp.Send(mail);
                            HttpContext.Session.SetString("cart", "");
                        }
                    }
                    return Redirect("/Home/Index");
            }
        }
        return View();
    }
    public void AddToCart(List<OrderDetailDTO> cart, string name)
    {
        var P = JsonConvert.DeserializeObject<Product>(ResponseConfig.GetData("api/Products/GetProductbyName/" + name).Result.Content.ReadAsStringAsync().Result);
        decimal Total = (decimal)P.UnitPrice * 1;
        cart.Add(new OrderDetailDTO { Product = P, Quantity = 1, Total = Total });
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

    [HttpGet]
    public IActionResult Login()
    {
        if (string.IsNullOrEmpty(HttpContext.Request.Cookies["accessToken"]) && !string.IsNullOrEmpty(HttpContext.Request.Cookies["refreshToken"]))
        {
            UserRes u = new UserRes();
            u.RefreshToken = HttpContext.Request.Cookies["refreshToken"];
            var conn = $"api/Accounts/signin";
            var Res = PostData(conn, JsonConvert.SerializeObject(u));
            if (!Res.Result.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status500InternalServerError);

            var user = JsonConvert.DeserializeObject<UserRes>(Res.Result.Content.ReadAsStringAsync().Result);

            HttpContext.Session.SetString("user", Res.Result.Content.ReadAsStringAsync().Result);

            ValidateToken(user!.AccessToken!.Replace("\"", ""));

            Response.Cookies.Append("refreshToken", user.RefreshToken!, new CookieOptions { Expires = user.TokenExpires, HttpOnly = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict });

            return Redirect("/");
        }
        return View();
    }


    [HttpPost]
    public IActionResult Login(AuthReq req)
    {
        var conn = $"api/Accounts/signin";
        var Res = PostData(conn, JsonConvert.SerializeObject(req));
        if (!Res.Result.IsSuccessStatusCode)
            return StatusCode(StatusCodes.Status500InternalServerError);

        var user = JsonConvert.DeserializeObject<UserRes>(Res.Result.Content.ReadAsStringAsync().Result);

        HttpContext.Session.SetString("user", Res.Result.Content.ReadAsStringAsync().Result);

        ValidateToken(user!.AccessToken!.Replace("\"", ""));

        Response.Cookies.Append("refreshToken", user.RefreshToken!, new CookieOptions 
        { Expires = user.TokenExpires, HttpOnly = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict });
        return RedirectToAction("index");
    }

  

    [HttpPost]
    public IActionResult Signup(SignUpReq req)
    {
        var conn = $"api/Accounts/signup";
        var Res = PostData(conn, JsonConvert.SerializeObject(req));
        if (!Res.Result.IsSuccessStatusCode) return StatusCode(StatusCodes.Status500InternalServerError);
        return RedirectToAction("Signup");
    }

 
    [HttpPost]
    public IActionResult Forgot(string email)
    {
        var conn = $"api/Accounts/reset/{email}";
        var Res = GetData(conn);
        if (!Res.Result.IsSuccessStatusCode) return StatusCode(StatusCodes.Status500InternalServerError);
        return RedirectToAction("Forgot");
    }


    [Authorize]
    [HttpGet]
    [Route("/logout")]
    public IActionResult signout()
    {
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
        return RedirectToAction("index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private void ValidateToken(string token)
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

    public async Task<HttpResponseMessage> PostData(string targerAddress, string content)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(BaseUrl);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(HttpContext.Request.Cookies["accessToken"]))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HttpContext.Request.Cookies["accessToken"]);
        }
        else
        {
            client.DefaultRequestHeaders.Add("refreshToken", HttpContext.Request.Cookies["refreshToken"]);
        }
        var Response = await client.PostAsync(targerAddress, new StringContent(content, Encoding.UTF8, "application/json"));
        return Response;
    }
}
