using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using lab09.Models;
using Microsoft.Data.Sqlite;

namespace lab09.Controllers;

public class IOController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public IOController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string login, string password)
    {
        string hashedPassword = DbManager.CalculateMD5(password);
        
        
        using var connection = new SqliteConnection(DbManager.GetConnectionString());
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM logins WHERE login = $login AND haslo = $password";
    
        command.Parameters.AddWithValue("$login", login);
        command.Parameters.AddWithValue("$password", hashedPassword);
        long count = (long)command.ExecuteScalar();

        if (count > 0)
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
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}