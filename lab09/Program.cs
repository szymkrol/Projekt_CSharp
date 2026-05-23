using lab09;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>();
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

app.Use(async (ctx, next) =>
{
    await next();
    if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
    {
        string? originalPath = ctx.Request.Path.Value;
        ctx.Items["originalPath"] = originalPath;
        ctx.Request.Path = "/Home/Index";
        await next();
    }
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.Value!.Contains("/IO/Login") || 
        ctx.Request.Path.Value.Contains("/IO/Register") || 
        ctx.Request.Path.Value.Contains("/Home/Index"))
    {
        await next();
        return;
    }

    if (string.IsNullOrEmpty(ctx.Session.GetString("IsLoggedIn")))
    {
        ctx.Response.Redirect("/IO/Login");
        return;
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=IO}/{action=Login}/{id?}");

app.Run();