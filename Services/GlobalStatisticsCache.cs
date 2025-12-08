using BlazorWebApp.Helpers;
using System.Xml.Linq;

namespace BlazorWebApp.Services;

public class GlobalStatisticsCache
{
    private readonly IWebHostEnvironment _env;
    private readonly XmlStatisticsService _statsService;

    public bool IsReady { get; private set; } = false;

    public Dictionary<string, CustomerCache> Customers { get; private set; } = new();

    public GlobalStatisticsCache(IWebHostEnvironment env, XmlStatisticsService statsService)
    {
        _env = env;
        _statsService = statsService;
    }

    public async Task LoadAllCustomersAsync()
    {
        Customers.Clear();
        IsReady = false;

        var uploads = Path.Combine(_env.ContentRootPath, "Uploads");
        if (!Directory.Exists(uploads))
            return;

        var dirs = Directory.GetDirectories(uploads);

        foreach (var dir in dirs)
        {
            var customer = Path.GetFileName(dir);
            var customerCache = new CustomerCache();

            var xmlFiles = Directory.GetFiles(dir, "*.xml");

            foreach (var file in xmlFiles)
            {
                try
                {
                    // Asynchronous I/O
                    using var fs = File.OpenRead(file);
                    var xdoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
                    fs.Dispose();
                    var fileName = Path.GetFileName(file);

                    customerCache.Files.Add(fileName);
                    // Asynchronous parsing of statistics → Non-blocking UI
                    var stats = _statsService.ConvertXmlToStatistics(xdoc, out var entries);
                    customerCache.Statistics[fileName] = stats;

                    WriteExcelFile(customer, fileName, xdoc, entries);
                }
                catch
                {
                    // Skip corrupt/uploading XML files
                }
            }
            if (customerCache.Files.Count > 0)
            {
                Customers[customer] = customerCache;
            }
        }

        IsReady = true;
    }

    // ------------- Update cache after adding a XML file -------------
    public void AddFile(string customer, string filePath)
    {
        if (!Customers.ContainsKey(customer))
            Customers[customer] = new CustomerCache();

        var file = Path.GetFileName(filePath);
        var xdoc = XDocument.Load(filePath);
        Customers[customer].Files.Add(file);
        var stats = _statsService.ConvertXmlToStatistics(xdoc, out var entries);
        Customers[customer].Statistics[file] = stats;

        WriteExcelFile(customer, file, xdoc, entries, true);
    }

    // ------------- Update cache after deleting a XML File -------------
    public void RemoveFile(string customer, string file)
    {
        if (!Customers.ContainsKey(customer))
            return;

        Customers[customer].Files.Remove(file);
        Customers[customer].Statistics.Remove(file);
    }

    // ------------- Write the excel file by the XML File -------------
    public void WriteExcelFile(string customer, string fileName, XDocument xdoc, List<XmlNodeEntry> entries, bool overwrite = false)
    {
        try
        {
            var excelDir = Path.Combine(_env.ContentRootPath, "Uploads", customer, "Excel");
            var excelFileName = Path.GetFileNameWithoutExtension(fileName) + ".xlsx";
            var excelPath = Path.Combine(excelDir, excelFileName);
            if (!File.Exists(excelPath) || overwrite)
            {
                Directory.CreateDirectory(excelDir);
                // Convert to Excel-Data-Sheets
                var dataSheets = _statsService.ConvertXmlToExcelDataSheets(xdoc, entries);
                // generate Excel byte[]
                var excelBytes = ExcelExportHelperFast.CreateExcel(dataSheets);
                // save excel
                File.WriteAllBytes(excelPath, excelBytes);
            }
        }
        catch (Exception ex) {

            Console.WriteLine("Excel generation error: " + ex.ToString());
            throw; // or return Results.Problem()
        }
    }

    public void ResetCache()
    {
        // Asynchronous background cache rebuild
        _ = Task.Run(() => LoadAllCustomersAsync());
    }

}

// --------------------------------------------------------------------

public class CustomerCache
{
    public List<string> Files { get; set; } = new();
    public Dictionary<string, List<(DateOnly Date, int Count, string Level)>> Statistics { get; set; } = new();
}
