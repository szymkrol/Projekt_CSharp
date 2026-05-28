using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http.Json;
using lab09.Models;

namespace lab09.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private string ApiBase => $"{Request.Scheme}://{Request.Host}";
    private string ApiUrl => $"{ApiBase}/posts";

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
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

    public async Task<IActionResult> Index(string? przedmiot, string? prowadzacy, bool? bezOdpowiedzi)
    {
        var client = _httpClientFactory.CreateClient();
        var request = CreateApiRequest(HttpMethod.Get, ApiUrl);
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            ViewData["Feed"] = new List<Post>();
            return View();
        }

        var posts = await response.Content.ReadFromJsonAsync<List<Post>>() ?? new List<Post>();
        var query = posts.AsQueryable();

        if (bezOdpowiedzi == true)
            query = query.Where(p => p.PostInfo != null && p.PostInfo.CzyPytanie && !p.Replies.Any());

        if (!string.IsNullOrEmpty(przedmiot))
        {
            query = query.Where(p => p.PostInfo != null
                                  && p.PostInfo.Przedmiot != null
                                  && p.PostInfo.Przedmiot.Nazwa != null
                                  && p.PostInfo.Przedmiot.Nazwa.Contains(przedmiot));
        }

        if (!string.IsNullOrEmpty(prowadzacy))
        {
            query = query.Where(p => p.PostInfo != null
                                  && p.PostInfo.Prowadzacy != null
                                  && p.PostInfo.Prowadzacy.Nazwa != null
                                  && p.PostInfo.Prowadzacy.Nazwa.Contains(prowadzacy));
        }

        ViewData["Feed"] = query.OrderByDescending(p => p.Id).ToList();
        ViewData["Przedmiot"] = przedmiot;
        ViewData["Prowadzacy"] = prowadzacy;
        return View();
    }

    public async Task<IActionResult> ManageData()
    {
        var client = _httpClientFactory.CreateClient();
        var request = CreateApiRequest(HttpMethod.Get, ApiUrl);
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            ViewData["Error"] = $"Błąd API: {response.StatusCode}";
            return View(new List<string>()); 
        }

        var posts = await response.Content.ReadFromJsonAsync<List<Post>>() ?? new List<Post>();
        
        var przedmiotyRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/przedmioty");
        var przedmiotyResponse = await client.SendAsync(przedmiotyRequest);
		
		if (przedmiotyResponse.IsSuccessStatusCode)
		{
			ViewBag.Przedmioty = await przedmiotyResponse.Content.ReadFromJsonAsync<List<Przedmiot>>() ?? new List<Przedmiot>();
		}
		else
		{
			ViewBag.Przedmioty = new List<Przedmiot>();
		}

		var prowadzacyRequest = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/prowadzacy");
        var prowadzacyResponse = await client.SendAsync(prowadzacyRequest);

        if (prowadzacyResponse.IsSuccessStatusCode)
        {
            ViewBag.Prowadzacy = await prowadzacyResponse.Content.ReadFromJsonAsync<List<Prowadzacy>>() ?? new List<Prowadzacy>();
        }
        else
        {
            ViewBag.Prowadzacy = new List<Prowadzacy>();
        }


		return View(posts.Select(p => p.Informacja).ToList());
    }

    public async Task<IActionResult> GetProwadzacy(int przedmiotId)
    {
        var client = _httpClientFactory.CreateClient();
        var request = CreateApiRequest(HttpMethod.Get, $"{ApiBase}/prowadzacy/{przedmiotId}");
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return Json(new List<object>());

        var prowadzacy = await response.Content.ReadFromJsonAsync<List<object>>();
        return Json(prowadzacy);
    }

[HttpPost]
    public async Task<IActionResult> AddData(string newData, int? przedmiotId, int? prowadzacyId, bool czyPytanie)
    {
        var client = _httpClientFactory.CreateClient();
        var username = HttpContext.Session.GetString("User") ?? "";

        if (przedmiotId.HasValue && prowadzacyId.HasValue)
        {
            var validRequest = CreateApiRequest(HttpMethod.Post,
                $"{ApiBase}/posts/validate-prowadzacy?przedmiotId={przedmiotId}&prowadzacyId={prowadzacyId}");
            var validResponse = await client.SendAsync(validRequest);

            var result = await validResponse.Content.ReadFromJsonAsync<ValidResult>();

        }

        if (!string.IsNullOrEmpty(newData))
        {
            // Przesyłamy uproszczone DTO zamiast bolesnego modelu bazodanowego
            var postDto = new CreatePostRequest(username, newData, przedmiotId, prowadzacyId, czyPytanie);
            var postRequest = CreateApiRequest(HttpMethod.Post, ApiUrl, postDto);
            await client.SendAsync(postRequest);
        }
        return RedirectToAction("ManageData");
    }

    [HttpPost]
    public async Task<IActionResult> AddReply(int postId, string tresc)
    {
        if (!string.IsNullOrEmpty(tresc))
        {
            var username = HttpContext.Session.GetString("User") ?? "";
            var replyDto = new CreateReplyRequest(postId, username, tresc);
            
            var client = _httpClientFactory.CreateClient();
            var request = CreateApiRequest(HttpMethod.Post, $"{ApiBase}/replies", replyDto);
            await client.SendAsync(request);
        }
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

public record ValidResult(bool Valid);
public record CreatePostRequest(string Username, string Informacja, int? PrzedmiotId, int? ProwadzacyId, bool CzyPytanie);
public record CreateReplyRequest(int PostId, string Username, string Tresc);