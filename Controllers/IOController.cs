using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using lab09.Models;


namespace lab09.Controllers;



public class IOController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private string ApiBase => $"{Request.Scheme}://{Request.Host}";

    public IOController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpRequestMessage CreateApiRequest(HttpMethod method, string url, object? content = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (content != null)
            request.Content = JsonContent.Create(content);

        if (HttpContext.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());

        return request;
    }

    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string login, string password)
    {
        var client = _httpClientFactory.CreateClient();
        var request = CreateApiRequest(HttpMethod.Post, $"{ApiBase}/logins/login", new LoginDto(login, password));
        var response = await client.SendAsync(request);

        // Forward any Set-Cookie headers from API to the browser
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var sc in setCookies)
                HttpContext.Response.Headers.Append("Set-Cookie", sc);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResult>();

        if (result?.Valid == true)
        {
            HttpContext.Session.SetString("IsLoggedIn", "true");
            HttpContext.Session.SetString("User", login);
            HttpContext.Session.Remove("Error");
            return RedirectToAction("Index", "Home");
        }

        HttpContext.Session.SetString("Error", "Błędne dane logowania!");
        return RedirectToAction("Login");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string login, string mail, string password)
    {
        var client = _httpClientFactory.CreateClient();
        var checkResponse = await client.GetAsync($"{ApiBase}/logins/check?login={login}&mail={mail}");
        var check = await checkResponse.Content.ReadFromJsonAsync<CheckResult>();

        if (check?.LoginExists == true)
        {
            HttpContext.Session.SetString("Error", "Login już istnieje!");
            return RedirectToAction("Register");
        }

        if (check?.MailExists == true)
        {
            HttpContext.Session.SetString("Error", "Email już istnieje!");
            return RedirectToAction("Register");
        }

        var registerRequest = CreateApiRequest(HttpMethod.Post, $"{ApiBase}/logins/register", new Login
        {
            LoginName = login,
            Mail = mail,
            Haslo = AppDbContext.CalculateMD5(password)
        });

        var registerResponse = await client.SendAsync(registerRequest);
        if (registerResponse.Headers.TryGetValues("Set-Cookie", out var regCookies))
        {
            foreach (var sc in regCookies)
                HttpContext.Response.Headers.Append("Set-Cookie", sc);
        }

        return RedirectToAction("Login");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

public record LoginDto(string Login, string Password);
public record LoginResult(bool Valid);
public record CheckResult(bool LoginExists, bool MailExists);