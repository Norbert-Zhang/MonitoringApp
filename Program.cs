using BlazorWebApp.Components;
using BlazorWebApp.Helpers;
using BlazorWebApp.Services;
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

var apiToken = builder.Configuration["ApiSettings:UploadApiToken"];

var app = builder.Build();

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
    IConfiguration config,
    BlazorWebApp.Services.FileService fileService) =>
{
    var token = request.Headers["x-api-key"].ToString();
    var expectedToken = config["ApiSettings:UploadApiToken"];

    if (token != expectedToken)
        return Results.Unauthorized();

    var client = request.Query["client"].ToString();
    if (string.IsNullOrWhiteSpace(client))
        return Results.BadRequest("Customer name is required.");

    if (!request.HasFormContentType)
        return Results.BadRequest("Form data is required.");

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();

    if (file == null)
        return Results.BadRequest("XML file is required.");

    if (!file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Only XML files are allowed.");

    // CALL BACKUP BEFORE SAVING NEW FILE
    fileService.BackupFileFromDir(client);

    // Now safe to write new file
    var clientDir = Path.Combine(env.ContentRootPath, "Uploads", client);
    Directory.CreateDirectory(clientDir);
    var savePath = Path.Combine(clientDir, file.FileName);
    using var fs = new FileStream(savePath, FileMode.Create);
    await file.CopyToAsync(fs);

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

static List<List<string>> BuildTotalStatsSheet(XDocument xdoc)
{
    var root = xdoc.Root!;
    var login = root.Element("LoginStatistics")!;
    var total = login.Element("TotalStatistics")!;

    var totalRows = new List<List<string>>
        {
            new() { "Field", "Value" },
            new() { "System Name", (string)root.Attribute("SystemName")! },
            new() { "System Version", "v_" + (string)root.Attribute("SystemVersion")! },
            new() { "Start Date", (string)login.Attribute("StartDate")! },
            new() { "Total Login Count", (string)total.Attribute("Count")! },
            new() { "", "" },
            // User statistics table
            new() { "User ID", "Total Login Count" }
        };
    // add user info rows
    totalRows.AddRange(
        total.Element("Users")?.Elements("GOBENCH.Users.UserStatistics.UserStatistics.UserLoginStatistics.UserInfo")
        .Select(u => new List<string>
        {
                (string)u.Attribute("ID")!,
                (string)u.Attribute("Count")!
        }) ?? Enumerable.Empty<List<string>>()
    );
    totalRows.Add(new List<string> { "", "" });
    // UserGroup statistics header
    totalRows.Add(new List<string> { "User Group ID", "Total Login Count" });
    // add user group rows
    totalRows.AddRange(
        total.Element("UserGroups")?
        .Elements("GOBENCH.Users.UserStatistics.UserStatistics.UserLoginStatistics.UserGroupInfo")
        .Select(g => new List<string>
        {
                (string)g.Attribute("ID")!,
                (string)g.Attribute("Count")!
        }) ?? Enumerable.Empty<List<string>>()
    );

    return totalRows;
}

static List<List<string>> BuildUserSheet(XDocument xdoc, List<XmlNodeEntry> entries)
{
    var userRows = new List<List<string>>
        {
            new() { "Level","Year","Half Year","Quarter","Month","Week","Day","User ID", "Login Count" }
        }.Concat(
            entries
                .Where(e => e.Target == "User") // users
                .Select(e => new List<string>
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
                })
        ).ToList();
    return userRows;
}

static List<List<string>> BuildUserGroupSheet(XDocument xdoc, List<XmlNodeEntry> entries)
{
    var userGroupRows = new List<List<string>>
        {
            new() { "Level","Year","Half Year","Quarter","Month","Week","Day","User Group ID", "Login Count" }
        }.Concat(
            entries
                .Where(e => e.Target == "UserGroup") // groups
                .Select(e => new List<string>
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
                })
        ).ToList();
    return userGroupRows;
}

static List<List<string>> BuildStatsSheet(XDocument xdoc, List<XmlNodeEntry> entries)
{
    var statsRows = new List<List<string>>
        {
            new() { "Level","Year","Half Year","Quarter","Month","Week","Day", "Login Count" }
        }.Concat(
            entries
                .Where(e => e.Target == "Stats") // Stats
                .Select(e => new List<string>
                {
                    e.Level,
                    e.Year?.ToString() ?? "",
                    e.HalfYear?.ToString() ?? "",
                    e.Quarter?.ToString() ?? "",
                    e.Month?.ToString() ?? "",
                    e.Week?.ToString() ?? "",
                    e.Day?.ToString() ?? "",
                    e.Count.ToString()
                })
        ).ToList();
    return statsRows;
}

//Excel-File download API
app.MapGet("/download-excel", (string client, string file, IWebHostEnvironment env) =>
{
    var xmlPath = Path.Combine(env.ContentRootPath, "Uploads", client, file);
    if (!System.IO.File.Exists(xmlPath))
        return Results.NotFound();

    var xdoc = XDocument.Load(xmlPath);
    var entries = XmlStatisticsHelper.ParseStatistics(xdoc.Root!.Element("LoginStatistics")!.Element("TotalStatistics")!);
    // prepare sheet data (your existing logic)
    var dataSheets = new Dictionary<string, List<List<string>>>
    {
        ["TotalStats"] = BuildTotalStatsSheet(xdoc),
        ["UserHierarchy"] = BuildUserSheet(xdoc, entries),
        ["UserGroupHierarchy"] = BuildUserGroupSheet(xdoc, entries),
        ["StatsHierarchy"] = BuildStatsSheet(xdoc, entries)
    };

    // generate excel
    var excelBytes = ExcelExportHelper.CreateExcel(dataSheets);
    var excelFileName = Path.GetFileNameWithoutExtension(file) + ".xlsx";
    return Results.File(
        excelBytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        excelFileName
    );
});

app.Run();
