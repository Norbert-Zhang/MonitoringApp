using BlazorWebApp.Components;
using BlazorWebApp.Helpers;
using BlazorWebApp.Services;
using Microsoft.AspNetCore.Http.Features;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

var app = builder.Build();

// ------------------- Global cache initialization -------------------
var cache = app.Services.GetRequiredService<GlobalStatisticsCache>();
//cache.Initialize();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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
});

// XML-File download API
app.MapGet("/download", (string client, string file, IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "Uploads", client, file);

    if (!System.IO.File.Exists(path))
        return Results.NotFound();

    var bytes = System.IO.File.ReadAllBytes(path);
    return Results.File(bytes, "application/xml", file);
});

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
});

app.Run();
