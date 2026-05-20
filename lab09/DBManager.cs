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
            DROP TABLE IF EXISTS post_reply;
            DROP TABLE IF EXISTS post_info;
            DROP TABLE IF EXISTS user_info;
            DROP TABLE IF EXISTS data;
            DROP TABLE IF EXISTS logins;
            CREATE TABLE IF NOT EXISTS logins (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                login TEXT NOT NULL,
                mail TEXT NOT NULL,
                haslo TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS data (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                login TEXT NOT NULL,
                informacja TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS user_info (
                login TEXT PRIMARY KEY,
                wydzial TEXT,
                kierunek TEXT   
            );
            CREATE TABLE IF NOT EXISTS post_info (
                post_id INTEGER PRIMARY KEY,
                przedmiot TEXT NOT NULL,
                prowadzacy TEXT,
                data TEXT NOT NULL,
                czy_pytanie INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (post_id) REFERENCES data(id)
            );
            CREATE TABLE IF NOT EXISTS post_reply (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                post_id INTEGER NOT NULL,
                login TEXT NOT NULL,
                tresc TEXT NOT NULL,
                data TEXT NOT NULL,
                FOREIGN KEY (post_id) REFERENCES data(id)
            );
            ";
        createTablesCmd.ExecuteNonQuery();

        var checkDataCmd = connection.CreateCommand();
        checkDataCmd.CommandText = "SELECT COUNT(*) FROM logins";
        if (Convert.ToInt32(checkDataCmd.ExecuteScalar()) == 0)
        {
            var initialLogins = connection.CreateCommand();
            initialLogins.CommandText = @$"
                INSERT INTO logins (login, mail, haslo) 
                VALUES ('admin', 'admin@example.com', '{CalculateMD5("1234")}'), ('user', 'user@example.com', '{CalculateMD5("qwerty")}');";
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


    public static List<(int id, string login, string informacja, string przedmiot, string prowadzacy, string data, bool czyPytanie)> GetPostyUzytkownika(string username)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT d.id, d.login, d.informacja, p.przedmiot, p.prowadzacy, p.data, p.czy_pytanie 
                            FROM data d 
                            LEFT JOIN post_info p ON d.id = p.post_id 
                            WHERE d.login = $login
                            ORDER BY d.id DESC";
        cmd.Parameters.AddWithValue("$login", username);
        var list = new List<(int, string, string, string, string, string, bool)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add((
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(3) ? "" : reader.GetString(3),
                reader.IsDBNull(4) ? "" : reader.GetString(4),
                reader.IsDBNull(5) ? "" : reader.GetString(5),
                reader.IsDBNull(6) ? false : reader.GetInt32(6) == 1
            ));
        }
        return list;
    }

    public static (string wydzial, string kierunek) GetUserInfo(string login)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT wydzial, kierunek FROM user_info WHERE login = $login";
        cmd.Parameters.AddWithValue("$login", login);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return (reader.GetString(0), reader.GetString(1));
        return ("", "");
    }

    public static void SaveUserInfo(string login, string wydzial, string kierunek)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO user_info (login, wydzial, kierunek) 
                            VALUES ($login, $wydzial, $kierunek)
                            ON CONFLICT(login) DO UPDATE SET 
                            wydzial = $wydzial, kierunek = $kierunek";
        cmd.Parameters.AddWithValue("$login", login);
        cmd.Parameters.AddWithValue("$wydzial", wydzial);
        cmd.Parameters.AddWithValue("$kierunek", kierunek);
        cmd.ExecuteNonQuery();
    }

    public static List<(int id, string login, string informacja, string przedmiot, string prowadzacy, string data, bool czyPytanie)> GetFeed()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT d.id, d.login, d.informacja, p.przedmiot, p.prowadzacy, p.data, p.czy_pytanie 
                            FROM data d 
                            LEFT JOIN post_info p ON d.id = p.post_id 
                            ORDER BY d.id DESC";
        var list = new List<(int, string, string, string, string, string, bool)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add((
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? "" : reader.GetString(3),
                reader.IsDBNull(4) ? "" : reader.GetString(4),
                reader.IsDBNull(5) ? "" : reader.GetString(5),
                reader.IsDBNull(6) ? false : reader.GetInt32(6) == 1
            ));
        return list;
    }

    public static List<(string login, string tresc, string data)> GetReplies(int postId)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT login, tresc, data FROM post_reply WHERE post_id = $postId ORDER BY id ASC";
        cmd.Parameters.AddWithValue("$postId", postId);
        var list = new List<(string, string, string)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        return list;
    }

    public static void AddReply(int postId, string login, string tresc)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO post_reply (post_id, login, tresc, data) VALUES ($postId, $login, $tresc, $data)";
        cmd.Parameters.AddWithValue("$postId", postId);
        cmd.Parameters.AddWithValue("$login", login);
        cmd.Parameters.AddWithValue("$tresc", tresc);
        cmd.Parameters.AddWithValue("$data", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        cmd.ExecuteNonQuery();
    }   

}