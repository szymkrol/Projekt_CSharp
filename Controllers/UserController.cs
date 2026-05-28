using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;

namespace lab09.Controllers;

public class UserController : Controller
{   
    private readonly IHttpClientFactory _httpClientFactory;
    private string ApiBase => $"{Request.Scheme}://{Request.Host}";

    public UserController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpRequestMessage CreateApiRequest(HttpMethod method, string url, object? content = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (HttpContext.Request.Headers.TryGetValue("Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
                request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        if (content != null)
            request.Content = JsonContent.Create(content);

        return request;
    }

    public async Task<IActionResult> Profile(string username)
    {
        if (string.IsNullOrEmpty(username))
            return RedirectToAction("Index", "Home");

        var client = _httpClientFactory.CreateClient();

        var postsRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/users/{username}/posts");
        var postsResponse = await client.SendAsync(postsRequest);
        if (!postsResponse.IsSuccessStatusCode)
            return RedirectToAction("Index", "Home");

        var posty = await postsResponse.Content.ReadFromJsonAsync<List<Post>>() ?? new List<Post>();

        var infoRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/users/{username}/info");
        var infoResponse = await client.SendAsync(infoRequest);
        var userInfo = infoResponse.IsSuccessStatusCode
            ? await infoResponse.Content.ReadFromJsonAsync<UserProfileInfoDto>()
            : null;

        ViewData["Username"] = username;
        ViewData["Posty"] = posty;
        ViewData["Wydzial"] = userInfo?.WydzialNazwa;
        ViewData["Kierunek"] = userInfo?.Kierunek;
        
        return View();
    }

    public async Task<ActionResult> AllProfiles()
    {
        var client = _httpClientFactory.CreateClient();
        
        var request = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/users/usernames");
        var profiles = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<List<string>>();
        
        ViewData["Profiles"] = profiles ?? new List<string>();
        return View();
    }

    public async Task<IActionResult> EditProfile()
    {
        var username = HttpContext.Session.GetString("User");
        var client = _httpClientFactory.CreateClient();

        var infoRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/users/{username}/info");
        var userInfo = await (await client.SendAsync(infoRequest)).Content.ReadFromJsonAsync<UserProfileInfoDto>();

        var wydzialRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/wydzialy");
        var wydzialy = await (await client.SendAsync(wydzialRequest)).Content.ReadFromJsonAsync<List<WydzialDto>>();

        ViewData["Wydzialy"] = new SelectList(wydzialy ?? new List<WydzialDto>(), "Id", "Nazwa", userInfo?.WydzialId);
        ViewData["WydzialId"] = userInfo?.WydzialId;
        ViewData["Kierunek"] = userInfo?.Kierunek;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> EditProfile(int? wydzialId, string kierunek)
    {
        var username = HttpContext.Session.GetString("User");
        if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "IO");

        var client = _httpClientFactory.CreateClient();
        
        var requestData = new UpdateUserInfoRequest(username, wydzialId, kierunek);
        var request = CreateApiRequest(HttpMethod.Post, $"{ApiBase}/users/info/update", requestData);
        await client.SendAsync(request);

        return RedirectToAction("Profile", new { username });
    }
}