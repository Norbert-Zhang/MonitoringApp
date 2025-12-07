using System.Xml.Linq;

namespace BlazorWebApp.Services;

public class GlobalStatisticsCache
{
    private readonly IWebHostEnvironment _env;
    private readonly XmlStatisticsService _statsService;

    public Dictionary<string, CustomerCache> Customers { get; private set; } = new();

    public GlobalStatisticsCache(IWebHostEnvironment env, XmlStatisticsService statsService)
    {
        _env = env;
        _statsService = statsService;
    }

    // ------------- Initialization: Load all XML Files -------------
    public void Initialize()
    {
        Customers.Clear();

        var uploads = Path.Combine(_env.ContentRootPath, "Uploads");
        if (!Directory.Exists(uploads))
            return;

        foreach (var dir in Directory.GetDirectories(uploads))
        {
            var customer = Path.GetFileName(dir);
            var customerCache = new CustomerCache();

            foreach (var file in Directory.GetFiles(dir, "*.xml"))
            {
                var fileName = Path.GetFileName(file);

                var xdoc = XDocument.Load(file);

                customerCache.Files.Add(fileName);
                customerCache.XmlDocs[fileName] = xdoc;

                var stats = _statsService.ConvertXmlToStatistics(xdoc);
                customerCache.Statistics[fileName] = stats;
            }
            if (customerCache.XmlDocs.Count > 0)
            {
                Customers[customer] = customerCache;
            }
        }
    }

    // ------------- Update cache after adding a XML file -------------
    public void AddFile(string customer, string filePath)
    {
        if (!Customers.ContainsKey(customer))
            Customers[customer] = new CustomerCache();

        var file = Path.GetFileName(filePath);

        var xdoc = XDocument.Load(filePath);

        Customers[customer].Files.Add(file);
        Customers[customer].XmlDocs[file] = xdoc;

        var stats = _statsService.ConvertXmlToStatistics(xdoc);
        Customers[customer].Statistics[file] = stats;
    }

    // ------------- Update cache after deleting a XML File -------------
    public void RemoveFile(string customer, string file)
    {
        if (!Customers.ContainsKey(customer))
            return;

        Customers[customer].Files.Remove(file);
        Customers[customer].XmlDocs.Remove(file);
        Customers[customer].Statistics.Remove(file);
    }
}

// --------------------------------------------------------------------

public class CustomerCache
{
    public List<string> Files { get; set; } = new();
    public Dictionary<string, XDocument> XmlDocs { get; set; } = new();
    public Dictionary<string, List<(DateOnly Date, int Count, string Level)>> Statistics { get; set; } = new();
}
