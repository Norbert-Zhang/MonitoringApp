using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlazorWebApp.Services;

public class XmlStatisticsService
{
    private readonly string _uploadsPath;

    public XmlStatisticsService(IWebHostEnvironment env)
    {
        _uploadsPath = Path.Combine(env.ContentRootPath, "Uploads");
        Directory.CreateDirectory(_uploadsPath);
    }

    public Dictionary<string, List<(DateOnly Date, int Count, string Level)>> LoadStatistics()
    {
        var result = new Dictionary<string, List<(DateOnly, int, string)>>();
        // Order by Directory Name (Customer Name)
        foreach (var clientDir in Directory.GetDirectories(_uploadsPath).OrderBy(dir => dir))
        {
            var clientName = Path.GetFileName(clientDir);

            var list = new List<(DateOnly, int, string)>();

            foreach (var file in Directory.GetFiles(clientDir, "*.xml"))
            {
                var xdoc = XDocument.Load(file);

                var root = xdoc.Root!;
                var login = root.Element("LoginStatistics")!;
                var total = login.Element("TotalStatistics")!;
                var entries = XmlStatisticsHelper.ParseStatistics(total);
                foreach(var entry in entries)
                {
                    if (entry.Target == "Stats")
                    {
                        if (entry.Level == "YearStatistics")
                        {
                            var dateOnly = new DateOnly(entry.Year ?? 1900, 12, 31);
                            list.Add((dateOnly, entry.Count, entry.Level));
                        } else if (entry.Level == "MonthStatistics")
                        {
                            int year = entry.Year ?? 1900;
                            int month = entry.Month ?? 1;
                            int lastDay = DateTime.DaysInMonth(year, month);
                            var dateOnly = new DateOnly(year, month, lastDay);
                            list.Add((dateOnly, entry.Count, entry.Level));
                        }
                    }
                }
            }
            if (list.Count > 0)
            {
                result[clientName] = list.ToList();
            }
        }

        return result;
    }
}
