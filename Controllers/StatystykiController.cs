using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace lab09.Controllers;

public class StatystykiController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private string ApiBase => $"{Request.Scheme}://{Request.Host}";

    public StatystykiController(IHttpClientFactory httpClientFactory)
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
        var client = _httpClientFactory.CreateClient();

        var wydzialyRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/statystyki/wydzialy");
        var poWydzialach = await (await client.SendAsync(wydzialyRequest)).Content.ReadFromJsonAsync<List<StatystykaDto>>();

        var przedmiotyRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/statystyki/przedmioty");
        var poPrzedmiotach = await (await client.SendAsync(przedmiotyRequest)).Content.ReadFromJsonAsync<List<StatystykaDto>>();

        var prowadzacyRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/statystyki/prowadzacy");
        var poProwadzacych = await (await client.SendAsync(prowadzacyRequest)).Content.ReadFromJsonAsync<List<StatystykaDto>>();

        ViewData["PoWydzialach"] = poWydzialach ?? new List<StatystykaDto>();
        ViewData["PoPrzedmiotach"] = poPrzedmiotach ?? new List<StatystykaDto>();
        ViewData["PoProwadzacych"] = poProwadzacych ?? new List<StatystykaDto>();

        return View();
    }
}