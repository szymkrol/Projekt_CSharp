using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using lab09.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace lab09.Controllers;

public class IOController : Controller
{
    private readonly AppDbContext _context;

    public IOController(AppDbContext context)
    {
        _context = context;
    }   

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string login, string password)
    {
        string hashedPassword = AppDbContext.CalculateMD5(password);

        bool valid = _context.Logins.Any(l => l.LoginName == login && l.Haslo == hashedPassword);

        if (valid)
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

    public IActionResult Register()
    {
        return View();
    }


    [HttpPost]
    public IActionResult Register(string login, string mail, string password)
    {
        if (_context.Logins.Any(l => l.LoginName == login))
        {
            HttpContext.Session.SetString("Error", "Login już istnieje!");
            return RedirectToAction("Register");
        }

        if (_context.Logins.Any(l => l.Mail == mail))
        {
            HttpContext.Session.SetString("Error", "Email już istnieje!");
            return RedirectToAction("Register");
        }

        _context.Logins.Add(new Login
        {
            LoginName = login,
            Mail = mail,
            Haslo = AppDbContext.CalculateMD5(password)
        });
        _context.SaveChanges();

        return RedirectToAction("Login");
    }
}