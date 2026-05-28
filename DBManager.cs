using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

using lab09.Models;

namespace lab09;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Login> Logins { get; set; } = default!;
    public DbSet<Post> Posts { get; set; } = default!;
    public DbSet<PostInfo> PostInfos { get; set; } = default!;
    public DbSet<PostReply> PostReplies { get; set; } = default!;
    public DbSet<UserInfo> UserInfos { get; set; } = default!;
    public DbSet<Wydzial> Wydzialy { get; set; } = default!;
    public DbSet<Prowadzacy> Prowadzacy { get; set; } = default!;
    public DbSet<Przedmiot> Przedmioty { get; set; } = default!;

    public static string CalculateMD5(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostInfo>()
            .HasKey(p => p.PostId);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.PostInfo)
            .WithOne(pi => pi.Post)
            .HasForeignKey<PostInfo>(pi => pi.PostId);

        modelBuilder.Entity<Post>()
            .HasMany(p => p.Replies)
            .WithOne(r => r.Post)
            .HasForeignKey(r => r.PostId);

        modelBuilder.Entity<Login>()
            .HasMany(l => l.Posts)
            .WithOne(p => p.Login)
            .HasForeignKey(p => p.LoginId);

        modelBuilder.Entity<Login>()
            .HasMany(l => l.Replies)
            .WithOne(r => r.Login)
            .HasForeignKey(r => r.LoginId);

        modelBuilder.Entity<Prowadzacy>()
            .HasMany(p => p.Przedmioty)
            .WithMany(p => p.Prowadzacy)
            .UsingEntity(j => j.ToTable("ProwadzacyPrzedmioty"));

        modelBuilder.Entity<Wydzial>().HasData(
            new Wydzial { Id = 1, Nazwa = "EAIiIB" },
            new Wydzial { Id = 2, Nazwa = "MS" },
            new Wydzial { Id = 3, Nazwa = "I" },
            new Wydzial { Id = 4, Nazwa = "IET" }
        );

        modelBuilder.Entity<Login>().HasData(
            new Login { Id = 1, LoginName = "admin", Mail = "admin@example.com", Haslo = CalculateMD5("1234") },
            new Login { Id = 2, LoginName = "user",  Mail = "user@example.com",  Haslo = CalculateMD5("qwerty") }
        );
    }
}