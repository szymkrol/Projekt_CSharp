using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using lab09.Models;
using Microsoft.Data.Sqlite;

namespace lab09.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult ManageData()
    {
        var dataList = new List<string>();
        using var connection = new SqliteConnection(DbManager.GetConnectionString());
        connection.Open();
    
        var command = connection.CreateCommand();
        command.CommandText = "SELECT informacja FROM data";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            dataList.Add(reader.GetString(0));
        }
    
        return View(dataList);
    }

    [HttpPost]
    public IActionResult AddData(string newData)
    {
        if (!string.IsNullOrEmpty(newData))
        {
            using var connection = new SqliteConnection(DbManager.GetConnectionString());
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO data (informacja) VALUES ($info)";
            command.Parameters.AddWithValue("$info", newData);
            command.ExecuteNonQuery();
        }
        return RedirectToAction("ManageData");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}