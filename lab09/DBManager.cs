using System.Security.Cryptography;
using System.Text;

namespace lab09;
using Microsoft.Data.Sqlite;
public static class DbManager
{
    private const string ConnectionString = "Data Source=myDataBase.db";

    public static void Initialize()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var createTablesCmd = connection.CreateCommand();
        createTablesCmd.CommandText = @"
            DROP TABLE IF EXISTS logins;
            DROP TABLE IF EXISTS data;
            CREATE TABLE IF NOT EXISTS logins (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                login TEXT NOT NULL,
                haslo TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS data (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                informacja TEXT NOT NULL
            );";
        createTablesCmd.ExecuteNonQuery();

        var checkDataCmd = connection.CreateCommand();
        checkDataCmd.CommandText = "SELECT COUNT(*) FROM logins";
        if (Convert.ToInt32(checkDataCmd.ExecuteScalar()) == 0)
        {
            var initialLogins = connection.CreateCommand();
            initialLogins.CommandText = @$"
                INSERT INTO logins (login, haslo) 
                VALUES ('admin', '{CalculateMD5("1234")}'), ('user', '{CalculateMD5("qwerty")}');";
            initialLogins.ExecuteNonQuery();
        }
    }
    
    public static string CalculateMD5(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
    
        return Convert.ToHexString(hashBytes).ToLower();
    }

    public static string GetConnectionString() => ConnectionString;
}