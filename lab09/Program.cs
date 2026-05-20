using lab09;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Dodanie obsługo sesji
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

DbManager.Initialize();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (ctx, next) =>
{
    await next();
    if(ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
    {
        //Re-execute the request so the user gets the error page
        string originalPath = ctx.Request.Path.Value;
        ctx.Items["originalPath"] = originalPath;
        ctx.Request.Path = "/Home/Index";
        await next();
    }
});

app.UseRouting();
//Dodanie obsługo sesji
app.UseSession();
app.UseAuthorization();

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.Value.Contains("/IO/Login") || ctx.Request.Path.Value.Contains("/IO/Register") || ctx.Request.Path.Value.Contains("/Home/Index"))
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