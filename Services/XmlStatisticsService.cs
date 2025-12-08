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

    public List<(DateOnly Date, int Count, string Level)> ConvertXmlToStatistics(XDocument xdoc, out List<XmlNodeEntry> entries)
    {
        var list = new List<(DateOnly, int, string)>();
        var root = xdoc.Root!;
        var login = root.Element("LoginStatistics")!;
        var total = login.Element("TotalStatistics")!;
        // OUT PARAMETER!
        entries = XmlStatisticsHelper.ParseStatistics(total);
        foreach (var entry in entries)
        {
            if (entry.Target == "Stats")
            {
                if (entry.Level == "YearStatistics")
                {
                    var dateOnly = new DateOnly(entry.Year ?? 1900, 12, 31);
                    list.Add((dateOnly, entry.Count, entry.Level));
                }
                else if (entry.Level == "MonthStatistics")
                {
                    int year = entry.Year ?? 1900;
                    int month = entry.Month ?? 1;
                    int lastDay = DateTime.DaysInMonth(year, month);
                    var dateOnly = new DateOnly(year, month, lastDay);
                    list.Add((dateOnly, entry.Count, entry.Level));
                }
            }
        }
        return list;
    }

    public Dictionary<string, IEnumerable<string[]>> ConvertXmlToExcelDataSheets(XDocument xdoc, List<XmlNodeEntry> entries)
    {
        return new Dictionary<string, IEnumerable<string[]>>
        {
            ["TotalStats"] = BuildTotalStatsSheet(xdoc),
            ["UserHierarchy"] = BuildUserSheet(entries),
            ["UserGroupHierarchy"] = BuildUserGroupSheet(entries),
            ["StatsHierarchy"] = BuildStatsSheet(entries)
        };
    }
   
    private IEnumerable<string[]> BuildTotalStatsSheet(XDocument xdoc)
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

    private IEnumerable<string[]> BuildUserSheet(List<XmlNodeEntry> entries)
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

    private IEnumerable<string[]> BuildUserGroupSheet(List<XmlNodeEntry> entries)
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

    private IEnumerable<string[]> BuildStatsSheet(List<XmlNodeEntry> entries)
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
}
