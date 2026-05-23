using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lab09.Controllers;

public class UserController : Controller
{   
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Profile(string username)
    {
        var posty = _context.Posts
            .Include(p => p.PostInfo)
            .Where(p => p.Login == username)
            .OrderByDescending(p => p.Id)
            .ToList();

        var userInfo = _context.UserInfos.FirstOrDefault(u => u.Login == username);

        ViewData["Username"] = username;
        ViewData["Posty"] = posty;
        ViewData["Wydzial"] = userInfo?.Wydzial;
        ViewData["Kierunek"] = userInfo?.Kierunek;
        return View();
    }

    public ActionResult AllProfiles()
    {
        // var profiles = new List<string>();
        // using var connection = new SqliteConnection(DbManager.GetConnectionString());
        // connection.Open();

        // var command = connection.CreateCommand();
        // command.CommandText = "SELECT DISTINCT login FROM logins";
        // using var reader = command.ExecuteReader();
        // while (reader.Read())
        // {
        //     profiles.Add(reader.GetString(0));
        // }

        // ViewData["Profiles"] = profiles;
        // return View();
        var profiles = _context.Logins.Select(l => l.LoginName).Distinct().ToList();
        ViewData["Profiles"] = profiles;
        return View();
    }

    public IActionResult EditProfile()
    {
        var username = HttpContext.Session.GetString("User");
        var userInfo = _context.UserInfos.FirstOrDefault(u => u.Login == username);
        ViewData["Wydzial"] = userInfo?.Wydzial;
        ViewData["Kierunek"] = userInfo?.Kierunek;
        return View();
    }

    [HttpPost]
    public IActionResult EditProfile(string wydzial, string kierunek)
    {
        var username = HttpContext.Session.GetString("User");
        var userInfo = _context.UserInfos.FirstOrDefault(u => u.Login == username);
        if (userInfo == null)
        {
            _context.UserInfos.Add(new UserInfo { Login = username!, Wydzial = wydzial, Kierunek = kierunek });
        }
        else
        {
            userInfo.Wydzial = wydzial;
            userInfo.Kierunek = kierunek;
        }
        _context.SaveChanges();
        return RedirectToAction("Profile", new { username });
    }
}