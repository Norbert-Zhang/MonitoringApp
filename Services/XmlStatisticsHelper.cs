using System.Xml.Linq;

namespace BlazorWebApp.Services;

public class XmlNodeEntry
{
    public string Level { get; set; } = "";
    public int? Year { get; set; }
    public int? HalfYear { get; set; }
    public int? Quarter { get; set; }
    public int? Month { get; set; }
    public int? Week { get; set; }
    public int? Day { get; set; }
    public string Id { get; set; } = "";
    public int Count { get; set; }
    public string Target { get; set; } = "";
}

public static class XmlStatisticsHelper
{

    /// <summary>
    /// Parse login statistics nodes at all levels in XML (fully recursive expansion)
    /// Support：Year -> HalfYear -> Quarter -> Month -> Week -> Day
    /// Both UserInfo and UserGroupInfo are supported
    /// </summary>
    public static List<XmlNodeEntry> ParseStatistics(XElement rootLoginStatistics)
    {
        return ParseRecursive(rootLoginStatistics);
    }

    /// <summary>
    /// Extract the content after the last period from the string.
    /// e.g. "A.B.C.YearStatistics" → "YearStatistics"
    /// </summary>
    private static string TrimAfterLastDot(string value)
    {
        int i = value.LastIndexOf('.');
        return i >= 0 ? value[(i + 1)..] : value;
    }

    private static List<XmlNodeEntry> ParseRecursive(
        XElement node,
        int? year = null,
        int? halfYear = null,
        int? quarter = null,
        int? month = null,
        int? week = null,
        int? day = null)
    {
        var list = new List<XmlNodeEntry>();

        // Get the type without relying on the prefix
        var typeAttr = node.Attributes().FirstOrDefault(a => a.Name.LocalName == "type");
        if (typeAttr != null)
        {
            string level = TrimAfterLastDot(typeAttr.Value);

            // Read the hierarchy attribute (inherit from the parent hierarchy if it does not exist).
            int? ReadInt(string name)
            {
                var attr = node.Attribute(name);
                return attr != null ? int.Parse(attr.Value) : null;
            }

            year = ReadInt("Year") ?? year;
            halfYear = ReadInt("HalfYear") ?? halfYear;
            quarter = ReadInt("Quarter") ?? quarter;
            month = ReadInt("Month") ?? month;
            week = ReadInt("Week") ?? week;
            day = ReadInt("Day") ?? day;

            // -------------------------
            // Process the current node
            // -------------------------
            list.Add(new XmlNodeEntry
            {
                Level = level,
                Year = year,
                HalfYear = halfYear,
                Quarter = quarter,
                Month = month,
                Week = week,
                Day = day,
                //Id = (string)node.Attribute("ID")!,
                Count = (int)node.Attribute("Count")!,
                Target = "Stats"
            });

            // -------------------------
            // Process the UserInfo node (user)
            // -------------------------
            foreach (var u in node.Elements("Users").Elements("GOBENCH.Users.UserStatistics.UserStatistics.UserLoginStatistics.UserInfo"))
            {
                list.Add(new XmlNodeEntry
                {
                    Level = level,
                    Year = year,
                    HalfYear = halfYear,
                    Quarter = quarter,
                    Month = month,
                    Week = week,
                    Day = day,
                    Id = (string)u.Attribute("ID")!,
                    Count = (int)u.Attribute("Count")!,
                    Target = "User"
                });
            }

            // -------------------------
            // Process the UserGroupInfo node (user group)
            // -------------------------
            foreach (var g in node.Elements("UserGroups").Elements("GOBENCH.Users.UserStatistics.UserStatistics.UserLoginStatistics.UserGroupInfo"))
            {
                list.Add(new XmlNodeEntry
                {
                    Level = level,
                    Year = year,
                    HalfYear = halfYear,
                    Quarter = quarter,
                    Month = month,
                    Week = week,
                    Day = day,
                    Id = (string)g.Attribute("ID")!,
                    Count = (int)g.Attribute("Count")!,
                    Target = "UserGroup"
                });
            }

        }

        // -------------------------
        // Recursively process all SubStatistics
        // -------------------------
        var subNodes = node.Element("SubStatistics")?.Elements("GOBENCH.Users.UserStatistics.UserStatistics.UserLoginStatistics.LoginStatistics");
        if (subNodes != null)
        {
            foreach (var sub in subNodes)
            {
                list.AddRange(
                    ParseRecursive(
                        sub,
                        year, halfYear, quarter, month, week, day
                    )
                );
            }
        }

        return list;
    }
}
