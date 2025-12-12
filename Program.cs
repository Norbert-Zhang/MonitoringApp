using BlazorWebApp.Components;
using BlazorWebApp.Models;
using BlazorWebApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

// 1. Enable authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "Monitor_Auth";
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.MaxAge = TimeSpan.FromDays(7);
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
        options.Cookie.IsEssential = true;
    });
// 2. Configure Authorization - Only needs to be called once
builder.Services.AddAuthorization();

// 3. Add Blazor service - note the order
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 4. Add Blazor-specific authentication services - must be done after AddRazorComponents.
builder.Services.AddCascadingAuthenticationState();

// 5. Other service registration
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true;
});
// Add the services
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<XmlStatisticsService>();
builder.Services.AddSingleton<GlobalStatisticsCache>();
builder.Services.AddHostedService<CachePreloader>();
builder.Services.AddHttpClient();
//builder.Services.AddHttpContextAccessor();

// Upload File Max size Config
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // Max 200 MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200 * 1024 * 1024; // Max 200MB
});

var apiToken = builder.Configuration["ApiSettings:UploadApiToken"];

// Ensure the "users" object is not nullable before passing it to AddSingleton.
var users = builder.Configuration
    .GetSection("AuthSettings:Users")
    .Get<List<AuthUser>>() ?? new List<AuthUser>();
builder.Services.AddSingleton(users);

var app = builder.Build();

// 6. Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// 7. Static file middleware
app.UseStaticFiles();

//app.UseRouting();
// 8. Authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// 9. Antiforgery Middleware
app.UseAntiforgery();

// 10. Map Blazor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ------------------- Global cache initialization -------------------
var cache = app.Services.GetRequiredService<GlobalStatisticsCache>();
//cache.Initialize();

// XML-File upload API
app.MapPost("/api/upload-xml", async (
    HttpRequest request,
    IWebHostEnvironment env,
    FileService fileService) =>
{
    var token = request.Headers["x-api-key"].ToString();
    if (token != apiToken)
        return Results.Unauthorized();

    if (!request.HasFormContentType)
        return Results.BadRequest("Form data is required.");

    IFormFile? file = request.Form.Files["file"];
    string? client = request.Form["client"];
    if (file == null)
        return Results.BadRequest("XML file missing.");
    if (!file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Only XML files are allowed.");
    if (string.IsNullOrWhiteSpace(client))
        return Results.BadRequest("Customer missing");

    client = client.Trim();
    // CALL BACKUP BEFORE SAVING NEW FILE
    fileService.BackupFileFromDir(client);

    // Now safe to write new file
    var clientDir = Path.Combine(env.ContentRootPath, "Uploads", client);
    Directory.CreateDirectory(clientDir);
    var savePath = Path.Combine(clientDir, file.FileName);
    using var fs = new FileStream(savePath, FileMode.Create);
    using var s = file.OpenReadStream();
    await s.CopyToAsync(fs);
    fs.Dispose();
    s.Dispose();

    // Update cache
    cache.AddFile(client, savePath);

    return Results.Ok($"Successfully uploaded: the '{file.FileName}' file for customer '{client}'.");
}); // Protect with api-key!!

// XML-File download API
app.MapGet("/download", (string client, string file, IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "Uploads", client, file);

    if (!System.IO.File.Exists(path))
        return Results.NotFound();

    var bytes = System.IO.File.ReadAllBytes(path);
    return Results.File(bytes, "application/xml", file);
})
    .RequireAuthorization(policy => policy.RequireRole("Administrator")); // Protect with authentication (only the Role "Administrator")!!

//Excel-File download API
app.MapGet("/download-excel", (string client, string file, IWebHostEnvironment env) =>
{
    var excelDir = Path.Combine(env.ContentRootPath, "Uploads", client, "Excel");
    var excelFileName = Path.GetFileNameWithoutExtension(file) + ".xlsx";
    var excelPath = Path.Combine(excelDir, excelFileName);
    if (!System.IO.File.Exists(excelPath))
        return Results.NotFound();

    var excelBytes = System.IO.File.ReadAllBytes(excelPath);
    return Results.File(
        excelBytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        excelFileName
    );
})
    .RequireAuthorization(policy => policy.RequireRole("Administrator")); // Protect with authentication (only the Role "Administrator")!!

app.Run();
