// UpsPoC.Api/Program.cs
using Microsoft.AspNetCore.Authentication.Cookies;
using UpsPoC.Api.Models;
using UpsPoC.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Settings
builder.Services.Configure<AppSettings>(builder.Configuration);

// SNMP + Data services
builder.Services.AddSingleton<ISnmpService, SnmpService>();
builder.Services.AddSingleton<IUpsDataService>(_ => new UpsDataService());

// Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Use HTTPS in production
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// CORS — allow Vite dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("DevCors");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SPA fallback for React routing in production
app.MapFallbackToFile("index.html");

app.Run();
