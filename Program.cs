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

static IEnumerable<string[]> BuildTotalStatsSheet(XDocument xdoc)
{
    var root = xdoc.Root!;
    var login = root.Element("LoginStatistics")!;
    var total = login.Element("TotalStatistics")!;

    yield return new[] { "Field", "Value" };
    yield return new[] { "System Name", root.Attribute("SystemName")?.Value ?? "" };
    yield return new[] { "System Version", "v_" + (root.Attribute("SystemVersion")?.Value ?? "") };
    yield return new[] { "Start Date", login.Attribute("StartDate")?.Value ?? "" };
    yield return new[] { "Total Login Count", total.Attribute("Count")?.Value ?? "" };
    yield return new[] { "", "" };
    yield return new[] { "User ID", "Total Login Count" };

    foreach (var u in total
        .Element("Users")?
        .Elements("GOBENCH.Users.UserStatistics.UserStatistics.UserLoginStatistics.UserInfo")
            ?? Enumerable.Empty<XElement>())
    {
        yield return new[] {
            u.Attribute("ID")?.Value ?? "",
            u.Attribute("Count")?.Value ?? ""
        };
    }

    yield return new[] { "", "" };
    yield return new[] { "User Group ID", "Total Login Count" };

    foreach (var g in total
        .Element("UserGroups")?
        .Elements("GOBENCH.Users.UserStatistics.UserStatistics.UserLoginStatistics.UserGroupInfo")
            ?? Enumerable.Empty<XElement>())
    {
        yield return new[] {
            g.Attribute("ID")?.Value ?? "",
            g.Attribute("Count")?.Value ?? ""
        };
    }
}

static IEnumerable<string[]> BuildUserSheet(List<XmlNodeEntry> entries)
{
    yield return new[]
    {
        "Level","Year","Half Year","Quarter","Month","Week","Day","User ID","Login Count"
    };

    foreach (var e in entries.Where(e => e.Target == "User"))
    {
        yield return new[]
        {
            e.Level,
            e.Year?.ToString() ?? "",
            e.HalfYear?.ToString() ?? "",
            e.Quarter?.ToString() ?? "",
            e.Month?.ToString() ?? "",
            e.Week?.ToString() ?? "",
            e.Day?.ToString() ?? "",
            e.Id,
            e.Count.ToString()
        };
    }
}

static IEnumerable<string[]> BuildUserGroupSheet(List<XmlNodeEntry> entries)
{
    yield return new[]
    {
        "Level","Year","Half Year","Quarter","Month","Week","Day","User Group ID","Login Count"
    };

    foreach (var e in entries.Where(e => e.Target == "UserGroup"))
    {
        yield return new[]
        {
            e.Level,
            e.Year?.ToString() ?? "",
            e.HalfYear?.ToString() ?? "",
            e.Quarter?.ToString() ?? "",
            e.Month?.ToString() ?? "",
            e.Week?.ToString() ?? "",
            e.Day?.ToString() ?? "",
            e.Id,
            e.Count.ToString()
        };
    }
}

static IEnumerable<string[]> BuildStatsSheet(List<XmlNodeEntry> entries)
{
    yield return new[]
    {
        "Level","Year","Half Year","Quarter","Month","Week","Day","Login Count"
    };

    foreach (var e in entries.Where(e => e.Target == "Stats"))
    {
        yield return new[]
        {
            e.Level,
            e.Year?.ToString() ?? "",
            e.HalfYear?.ToString() ?? "",
            e.Quarter?.ToString() ?? "",
            e.Month?.ToString() ?? "",
            e.Week?.ToString() ?? "",
            e.Day?.ToString() ?? "",
            e.Count.ToString()
        };
    }
}

//Excel-File download API
app.MapGet("/download-excel", (string client, string file, IWebHostEnvironment env) =>
{
    //var xmlPath = Path.Combine(env.ContentRootPath, "Uploads", client, file);
    //if (!System.IO.File.Exists(xmlPath)) return Results.NotFound();
    //var xdoc = XDocument.Load(xmlPath);
    if (!cache.Customers.ContainsKey(client) || !cache.Customers[client].XmlDocs.ContainsKey(file)) return Results.NotFound();
    var xdoc = cache.Customers[client].XmlDocs[file];
    var entries = XmlStatisticsHelper.ParseStatistics(xdoc.Root!.Element("LoginStatistics")!.Element("TotalStatistics")!);
    // prepare sheet data (your existing logic)
    var dataSheets = new Dictionary<string, IEnumerable<string[]>>
    {
        ["TotalStats"] = BuildTotalStatsSheet(xdoc),
        ["UserHierarchy"] = BuildUserSheet(entries),
        ["UserGroupHierarchy"] = BuildUserGroupSheet(entries),
        ["StatsHierarchy"] = BuildStatsSheet(entries)
    };

    // generate excel
    var excelBytes = ExcelExportHelperFast.CreateExcel(dataSheets); // ExcelExportHelper.CreateExcel(dataSheets);
    var excelFileName = Path.GetFileNameWithoutExtension(file) + ".xlsx";
    return Results.File(
        excelBytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        excelFileName
    );
});

app.Run();
