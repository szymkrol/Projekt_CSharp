using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace lab09;

public class Login
{
    public int Id { get; set; }
    public string LoginName { get; set; } = "";
    public string Mail { get; set; } = "";
    public string Haslo { get; set; } = "";

    public ICollection<Post>? Posts { get; set; }
    public ICollection<PostReply>? Replies { get; set; }
}

public class Post
{
    public int Id { get; set; }
    public int LoginId { get; set; }
    public string Informacja { get; set; } = "";

    public Login Login { get; set; } = null!;
    public PostInfo? PostInfo { get; set; }
    public ICollection<PostReply>? Replies { get; set; }
}

public class PostInfo
{
    public int PostId { get; set; }
    public int? PrzedmiotId { get; set; }       
    public int? ProwadzacyId { get; set; }     
    public string Data { get; set; } = "";
    public bool CzyPytanie { get; set; }

    public Post Post { get; set; } = null!;
    public Przedmiot? Przedmiot { get; set; }
    public Prowadzacy? Prowadzacy { get; set; }
}

public class PostReply
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int LoginId { get; set; }
    public string Tresc { get; set; } = "";
    public string Data { get; set; } = "";

    public Post Post { get; set; } = null!;
    public Login Login { get; set; } = null!;
}

public class UserInfo
{
    [Key]
    public string Login { get; set; } = "";
    public int? WydzialId { get; set; }         
    public string? Kierunek { get; set; }

    public Wydzial? Wydzial { get; set; }
}

public class Wydzial
{
    public int Id { get; set; }
    public string Nazwa { get; set; } = "";

    public ICollection<Prowadzacy>? Prowadzacy { get; set; }
    public ICollection<Przedmiot>? Przedmioty { get; set; }
    public ICollection<UserInfo>? Users { get; set; }
}

public class Prowadzacy
{
    public int Id { get; set; }
    public string Nazwa { get; set; } = "";
    public int? WydzialId { get; set; }       

    public Wydzial? Wydzial { get; set; }
    public ICollection<Przedmiot>? Przedmioty { get; set; }
    public ICollection<PostInfo>? PostInfos { get; set; }
}

public class Przedmiot
{
    public int Id { get; set; }
    public string Nazwa { get; set; } = "";
    public int? WydzialId { get; set; }       
    public string? Kierunek { get; set; }

    public Wydzial? Wydzial { get; set; }
    public ICollection<Prowadzacy>? Prowadzacy { get; set; }
    public ICollection<PostInfo>? PostInfos { get; set; }
}

public record PrzedmiotDto(string Nazwa, int? WydzialId, string? Kierunek, int[]? ProwadzacyIds);

public record LoginDto(string Login, string Password);

public record StatystykaDto(string Nazwa, int Liczba);

public record UserProfileInfoDto(string? WydzialNazwa, int? WydzialId, string? Kierunek);
public record UpdateUserInfoRequest(string Username, int? WydzialId, string? Kierunek);
public record WydzialDto(int Id, string Nazwa);