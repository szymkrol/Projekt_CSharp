using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using lab09.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace lab09.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index(string? przedmiot, string? prowadzacy, bool? bezOdpowiedzi)
    {
        var query = _context.Posts
            .Include(p => p.PostInfo)
            .Include(p => p.Replies)
            .AsQueryable();

        if (bezOdpowiedzi == true)
            query = query.Where(p => p.PostInfo != null && p.PostInfo.CzyPytanie && !p.Replies.Any());

        if (!string.IsNullOrEmpty(przedmiot))
            query = query.Where(p => p.PostInfo != null && p.PostInfo.Przedmiot.Contains(przedmiot));

        if (!string.IsNullOrEmpty(prowadzacy))
            query = query.Where(p => p.PostInfo != null && p.PostInfo.Prowadzacy != null && p.PostInfo.Prowadzacy.Contains(prowadzacy));

        ViewData["Feed"] = query.OrderByDescending(p => p.Id).ToList();
        ViewData["Przedmiot"] = przedmiot;
        ViewData["Prowadzacy"] = prowadzacy;
        return View();
    }
    
    public IActionResult ManageData()
    {
        var dataList = _context.Posts
            .Select(p => p.Informacja)
            .ToList();

        return View(dataList);
    }

    [HttpPost]
    public IActionResult AddData(string newData, string przedmiot, string prowadzacy, bool czyPytanie)
    {
        if (!string.IsNullOrEmpty(newData))
        {
            var post = new Post
            {
                Login = HttpContext.Session.GetString("User") ?? "",
                Informacja = newData,
                PostInfo = new PostInfo
                {
                    Przedmiot = przedmiot ?? "",
                    Prowadzacy = prowadzacy ?? "",
                    Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    CzyPytanie = czyPytanie
                }
            };
            _context.Posts.Add(post);
            _context.SaveChanges();
        }
        return RedirectToAction("ManageData");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public IActionResult AddReply(int postId, string tresc)
    {
        if (!string.IsNullOrEmpty(tresc))
        {
            _context.PostReplies.Add(new PostReply
            {
                PostId = postId,
                Login = HttpContext.Session.GetString("User") ?? "",
                Tresc = tresc,
                Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            });
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }
}