using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace lab09.Controllers;


public class AdminController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private string ApiBase => $"{Request.Scheme}://{Request.Host}";

    public AdminController(IHttpClientFactory httpClientFactory)
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

    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetString("User") != "admin")
            return RedirectToAction("Index", "Home");

        var client = _httpClientFactory.CreateClient();

        var prowadzacyRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/prowadzacy");
        var prowadzacy = await (await client.SendAsync(prowadzacyRequest)).Content.ReadFromJsonAsync<List<Prowadzacy>>() ?? new();

        var przedmiotyRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/przedmioty");
        var przedmioty = await (await client.SendAsync(przedmiotyRequest)).Content.ReadFromJsonAsync<List<Przedmiot>>() ?? new();

        var wydzialyRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/wydzialy");
        var wydzialy = await (await client.SendAsync(wydzialyRequest)).Content.ReadFromJsonAsync<List<Wydzial>>() ?? new();

        ViewData["Prowadzacy"] = prowadzacy;
        ViewData["Przedmioty"] = przedmioty;
        ViewData["Wydzialy"] = wydzialy;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddProwadzacy(string nazwa, int? wydzialId)
    {
        var client = _httpClientFactory.CreateClient();
        var request = CreateApiRequest(HttpMethod.Post, $"{ApiBase}/prowadzacy", new Prowadzacy { Nazwa = nazwa, WydzialId = wydzialId });
        await client.SendAsync(request);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> AddPrzedmiot(string nazwa, int? wydzialId, string? kierunek, int[] prowadzacyIds)
    {
        var client = _httpClientFactory.CreateClient();
        var request = CreateApiRequest(HttpMethod.Post, $"{ApiBase}/przedmioty", new PrzedmiotDto(nazwa, wydzialId, kierunek, prowadzacyIds));
        await client.SendAsync(request);
        return RedirectToAction("Index");
    }
}