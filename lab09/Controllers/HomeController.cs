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
        ViewData["Feed"] = DbManager.GetFeed();
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
    public IActionResult AddData(string newData, string przedmiot, string prowadzacy, bool czyPytanie)
    {
        if (!string.IsNullOrEmpty(newData))
        {
            using var connection = new SqliteConnection(DbManager.GetConnectionString());
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO data (login, informacja) VALUES ($login, $info)";
            cmd.Parameters.AddWithValue("$login", HttpContext.Session.GetString("User"));
            cmd.Parameters.AddWithValue("$info", newData);
            cmd.ExecuteNonQuery();

            var lastId = connection.CreateCommand();
            lastId.CommandText = "SELECT last_insert_rowid()";
            long postId = (long)lastId.ExecuteScalar();

            var cmdInfo = connection.CreateCommand();
            cmdInfo.CommandText = @"INSERT INTO post_info (post_id, przedmiot, prowadzacy, data, czy_pytanie) 
                                    VALUES ($postId, $przedmiot, $prowadzacy, $data, $czyPytanie)";
            cmdInfo.Parameters.AddWithValue("$postId", postId);
            cmdInfo.Parameters.AddWithValue("$przedmiot", przedmiot ?? "");
            cmdInfo.Parameters.AddWithValue("$prowadzacy", prowadzacy ?? "");
            cmdInfo.Parameters.AddWithValue("$data", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            cmdInfo.Parameters.AddWithValue("$czyPytanie", czyPytanie ? 1 : 0);
            cmdInfo.ExecuteNonQuery();
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
            DbManager.AddReply(postId, HttpContext.Session.GetString("User"), tresc);
        return RedirectToAction("Index");
    }
}