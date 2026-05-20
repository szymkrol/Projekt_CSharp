using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace lab09.Controllers;

public class UserController : Controller
{
    public IActionResult Profile(string username)
    {
        var posty = DbManager.GetPostyUzytkownika(username);
        var (wydzial, kierunek) = DbManager.GetUserInfo(username);
        ViewData["Username"] = username;
        ViewData["Posty"] = posty;
        ViewData["Wydzial"] = wydzial;
        ViewData["Kierunek"] = kierunek;
        return View();
    }

    public ActionResult AllProfiles()
    {
        var profiles = new List<string>();
        using var connection = new SqliteConnection(DbManager.GetConnectionString());
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT login FROM logins";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            profiles.Add(reader.GetString(0));
        }

        ViewData["Profiles"] = profiles;
        return View();
    }

    public IActionResult EditProfile()
    {
        var username = HttpContext.Session.GetString("User");
        var (wydzial, kierunek) = DbManager.GetUserInfo(username);
        ViewData["Wydzial"] = wydzial;
        ViewData["Kierunek"] = kierunek;
        return View();
    }

    [HttpPost]
    public IActionResult EditProfile(string wydzial, string kierunek)
    {
        var username = HttpContext.Session.GetString("User");
        DbManager.SaveUserInfo(username, wydzial, kierunek);
        return RedirectToAction("Profile", new { username });
    }


}