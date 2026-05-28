using lab09;
using Microsoft.EntityFrameworkCore;

using lab09.Controllers;
using lab09.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddHttpClient();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDbContext")
        ?? throw new InvalidOperationException("Connection string 'AppDbContext' not found.")));

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.Use(async (ctx, next) =>
{
    await next();
    if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
    {
        ctx.Items["originalPath"] = ctx.Request.Path.Value;
        ctx.Request.Path = "/Home/Index";
        await next();
    }
});

app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value?.ToLower() ?? string.Empty;
    bool isPublic = path.StartsWith("/io/login") ||
                    path.StartsWith("/io/register") ||
                    path.StartsWith("/home/index") ||
                    path.StartsWith("/logins/register") ||
                    path.StartsWith("/logins");
    bool isApi = path.StartsWith("/posts");

    if (isPublic) { await next(); return; }

    if (string.IsNullOrEmpty(ctx.Session.GetString("IsLoggedIn")))
    {
        if (isApi)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { Error = "Unauthorized" });
            return;
        }
        ctx.Response.Redirect("/IO/Login");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapGet("/posts", async (AppDbContext db) =>
{
    var posts = await db.Posts
        .Include(p => p.PostInfo)
            .ThenInclude(pi => pi!.Przedmiot)
        .Include(p => p.PostInfo)
            .ThenInclude(pi => pi!.Prowadzacy)
        .Include(p => p.Login)
        .Include(p => p.Replies)
            .ThenInclude(r => r.Login)
        .ToListAsync();

    return Results.Ok(posts);
});

app.MapGet("/posts/{id:int}", async (int id, AppDbContext db) =>
{
    var post = await db.Posts
        .Include(p => p.PostInfo)
        .Include(p => p.Replies)
        .FirstOrDefaultAsync(p => p.Id == id);

    return post is not null ? Results.Ok(post) : Results.NotFound($"Post o ID {id} nie istnieje.");
});

app.MapPost("/posts", async (CreatePostRequest dto, AppDbContext db) =>
{
    var user = await db.Logins.FirstOrDefaultAsync(l => l.LoginName == dto.Username);
    if (user == null) return Results.BadRequest("Użytkownik nie istnieje.");

    var nowyPost = new Post
    {
        LoginId = user.Id,
        Informacja = dto.Informacja,
        PostInfo = new PostInfo
        {
            PrzedmiotId = dto.PrzedmiotId,
            ProwadzacyId = dto.ProwadzacyId,
            Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            CzyPytanie = dto.CzyPytanie
        }
    };

    db.Posts.Add(nowyPost);
    await db.SaveChangesAsync();
    return Results.Created($"/posts/{nowyPost.Id}", nowyPost);
});

app.MapPut("/posts/{id:int}", async (int id, Post zaktualizowanyPost, AppDbContext db) =>
{
    var postZBazy = await db.Posts.FindAsync(id);
    if (postZBazy is null) return Results.NotFound();

    postZBazy.Login = zaktualizowanyPost.Login;
    postZBazy.Informacja = zaktualizowanyPost.Informacja;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/posts/{id:int}", async (int id, AppDbContext db) =>
{
    var post = await db.Posts.FindAsync(id);
    if (post is null) return Results.NotFound();

    db.Posts.Remove(post);
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = $"Post o ID {id} został usunięty." });
});

app.MapPost("/replies", async (CreateReplyRequest dto, AppDbContext db) =>
{
    var user = await db.Logins.FirstOrDefaultAsync(l => l.LoginName == dto.Username);
    if (user == null) return Results.BadRequest("Użytkownik nie istnieje.");

    var reply = new PostReply
    {
        PostId = dto.PostId,
        LoginId = user.Id,
        Tresc = dto.Tresc,
        Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
    };

    db.PostReplies.Add(reply);
    await db.SaveChangesAsync();
    return Results.Created($"/replies/{reply.Id}", reply);
});

app.MapGet("/prowadzacy/{przedmiotId:int}", async (int przedmiotId, AppDbContext db) =>
{
    var przedmiot = await db.Przedmioty
        .Include(p => p.Prowadzacy)
        .FirstOrDefaultAsync(p => p.Id == przedmiotId);

    var prowadzacy = przedmiot?.Prowadzacy?
        .Select(p => new { p.Id, p.Nazwa })
        .ToList();

    return prowadzacy is not null ? Results.Ok(prowadzacy) : Results.NotFound();
});

app.MapPost("/posts/validate-prowadzacy", async (int przedmiotId, int prowadzacyId, AppDbContext db) =>
{
    var przedmiot = await db.Przedmioty
        .Include(p => p.Prowadzacy)
        .FirstOrDefaultAsync(p => p.Id == przedmiotId);

    bool valid = przedmiot?.Prowadzacy?.Any(p => p.Id == prowadzacyId) ?? false;
    return Results.Ok(new { valid });
});

app.MapGet("/wydzialy", async (AppDbContext db) =>
    Results.Ok(await db.Wydzialy.ToListAsync()));

app.MapGet("/prowadzacy", async (AppDbContext db) =>
    Results.Ok(await db.Prowadzacy
        .Include(p => p.Wydzial)
        .Include(p => p.Przedmioty)
        .ToListAsync()));

app.MapPost("/prowadzacy", async (Prowadzacy prowadzacy, AppDbContext db) =>
{
    db.Prowadzacy.Add(prowadzacy);
    await db.SaveChangesAsync();
    return Results.Created($"/prowadzacy/{prowadzacy.Id}", prowadzacy);
});

app.MapGet("/przedmioty", async (AppDbContext db) =>
    Results.Ok(await db.Przedmioty
        .Include(p => p.Wydzial)
        .ToListAsync()));

app.MapPost("/przedmioty", async (PrzedmiotDto dto, AppDbContext db) =>
{
    var przedmiot = new Przedmiot 
    { 
        Nazwa = dto.Nazwa, 
        WydzialId = dto.WydzialId, 
        Kierunek = dto.Kierunek 
    };

    if (dto.ProwadzacyIds?.Length > 0)
        przedmiot.Prowadzacy = await db.Prowadzacy
            .Where(p => dto.ProwadzacyIds.Contains(p.Id))
            .ToListAsync();

    db.Przedmioty.Add(przedmiot);
    await db.SaveChangesAsync();
    return Results.Created($"/przedmioty/{przedmiot.Id}", przedmiot);
});

app.MapGet("/logins/check", async (string login, string mail, AppDbContext db) =>
{
    bool loginExists = await db.Logins.AnyAsync(l => l.LoginName == login);
    bool mailExists = await db.Logins.AnyAsync(l => l.Mail == mail);
    return Results.Ok(new { loginExists, mailExists });
});

app.MapPost("/logins/login", async (lab09.LoginDto dto, AppDbContext db) =>
{
    string hashed = AppDbContext.CalculateMD5(dto.Password);
    bool valid = await db.Logins.AnyAsync(l => l.LoginName == dto.Login && l.Haslo == hashed);
    return Results.Ok(new { valid });
});

app.MapPost("/logins/register", async (Login login, AppDbContext db) =>
{
    db.Logins.Add(login);
    await db.SaveChangesAsync();
    return Results.Created($"/logins/{login.Id}", login);
});

app.MapGet("/statystyki/wydzialy", async (AppDbContext db) =>
{
    var wynik = await db.Posts
        .Include(p => p.PostInfo).ThenInclude(pi => pi!.Przedmiot).ThenInclude(p => p!.Wydzial)
        .Where(p => p.PostInfo != null && p.PostInfo.Przedmiot != null && p.PostInfo.Przedmiot.Wydzial != null)
        .GroupBy(p => p.PostInfo!.Przedmiot!.Wydzial!.Nazwa)
        .Select(g => new StatystykaDto(g.Key, g.Count()))
        .ToListAsync();
        
    return Results.Ok(wynik);
});

app.MapGet("/statystyki/przedmioty", async (AppDbContext db) =>
{
    var wynik = await db.Posts
        .Include(p => p.PostInfo).ThenInclude(pi => pi!.Przedmiot)
        .Where(p => p.PostInfo != null && p.PostInfo.Przedmiot != null)
        .GroupBy(p => p.PostInfo!.Przedmiot!.Nazwa)
        .Select(g => new StatystykaDto(g.Key, g.Count()))
        .ToListAsync();
        
    return Results.Ok(wynik);
});

app.MapGet("/statystyki/prowadzacy", async (AppDbContext db) =>
{
    var wynik = await db.Posts
        .Include(p => p.PostInfo).ThenInclude(pi => pi!.Prowadzacy)
        .Where(p => p.PostInfo != null && p.PostInfo.Prowadzacy != null)
        .GroupBy(p => p.PostInfo!.Prowadzacy!.Nazwa)
        .Select(g => new StatystykaDto(g.Key, g.Count()))
        .ToListAsync();
        
    return Results.Ok(wynik);
});

app.MapGet("/users/{username}/posts", async (string username, AppDbContext db) =>
{
    var loginUser = await db.Logins.FirstOrDefaultAsync(l => l.LoginName == username);
    if (loginUser == null) return Results.NotFound();

    var posty = await db.Posts
        .Include(p => p.PostInfo).ThenInclude(pi => pi!.Przedmiot)
        .Include(p => p.PostInfo).ThenInclude(pi => pi!.Prowadzacy)
        .Include(p => p.Login)
        .Include(p => p.Replies).ThenInclude(r => r.Login)
        .Where(p => p.LoginId == loginUser.Id)
        .OrderByDescending(p => p.Id)
        .ToListAsync();

    return Results.Ok(posty);
});

app.MapGet("/users/{username}/info", async (string username, AppDbContext db) =>
{
    var userInfo = await db.UserInfos
        .Include(u => u.Wydzial)
        .FirstOrDefaultAsync(u => u.Login == username);

    return Results.Ok(new UserProfileInfoDto(
        userInfo?.Wydzial?.Nazwa,
        userInfo?.WydzialId,
        userInfo?.Kierunek
    ));
});

app.MapGet("/users/usernames", async (AppDbContext db) =>
{
    var profiles = await db.Logins.Select(l => l.LoginName).Distinct().ToListAsync();
    return Results.Ok(profiles);
});

app.MapPost("/users/info/update", async (UpdateUserInfoRequest req, AppDbContext db) =>
{
    var userInfo = await db.UserInfos.FirstOrDefaultAsync(u => u.Login == req.Username);
    
    if (userInfo == null)
    {
        db.UserInfos.Add(new UserInfo { Login = req.Username, WydzialId = req.WydzialId, Kierunek = req.Kierunek });
    }
    else
    {
        userInfo.WydzialId = req.WydzialId;
        userInfo.Kierunek = req.Kierunek;
    }
    
    await db.SaveChangesAsync();
    return Results.Ok();
});


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=IO}/{action=Login}/{id?}");

app.Run();