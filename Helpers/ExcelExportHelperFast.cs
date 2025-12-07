using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace BlazorWebApp.Helpers
{
    public static class ExcelExportHelperFast
    {
        // --------------------------
        // Public: Create Excel File
        // --------------------------
        public static byte[] CreateExcel(Dictionary<string, IEnumerable<string[]>> sheetsData)
        {
            using var mem = new MemoryStream();

            using (var doc = SpreadsheetDocument.Create(mem, SpreadsheetDocumentType.Workbook))
            {
                var wb = doc.AddWorkbookPart();
                wb.Workbook = new Workbook();

                // Minimal styling (optional)
                var styles = wb.AddNewPart<WorkbookStylesPart>();
                styles.Stylesheet = CreateMinimalStylesheet();
                styles.Stylesheet.Save();

                var sheets = wb.Workbook.AppendChild(new Sheets());
                uint sheetId = 1;

                foreach (var sheet in sheetsData)
                {
                    var wsPart = wb.AddNewPart<WorksheetPart>();
                    WriteSheetStream(wsPart, sheet.Value);

                    AddFreezePane(wsPart);
                    AddAutoFilter(wsPart, sheet.Value.First().Count());

                    sheets.Append(new Sheet
                    {
                        Id = wb.GetIdOfPart(wsPart),
                        SheetId = sheetId++,
                        Name = sheet.Key
                    });
                }

                wb.Workbook.Save();
            }

            return mem.ToArray();
        }

        // ----------------------------------
        // High-performance streaming writer
        // ----------------------------------
        private static void WriteSheetStream(WorksheetPart wsPart, IEnumerable<string[]> rows)
        {
            using var writer = OpenXmlWriter.Create(wsPart);

            writer.WriteStartElement(new Worksheet());

            // ===== ADD FIXED COLUMNS (width = 20) =====
            int columnCount = rows.First().Count();
            writer.WriteStartElement(new Columns());
            for (int i = 1; i <= columnCount; i++)
            {
                writer.WriteElement(new Column
                {
                    Min = (uint)i,
                    Max = (uint)i,
                    Width = 20,
                    CustomWidth = true
                });
            }
            writer.WriteEndElement(); // </Columns>
            // ==========================================

            writer.WriteStartElement(new SheetData());

            foreach (var rowItems in rows)
            {
                writer.WriteStartElement(new Row());

                foreach (var value in rowItems)
                {
                    writer.WriteElement(CreateCell(value));
                }

                writer.WriteEndElement(); // </Row>
            }

            writer.WriteEndElement(); // </SheetData>
            writer.WriteEndElement(); // </Worksheet>
        }

        // -----------------------------
        // Cell creation (type aware)
        // -----------------------------
        private static Cell CreateCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new Cell { DataType = CellValues.String, CellValue = new CellValue("") };

            // Number?
            if (double.TryParse(value, out double num))
                return new Cell
                {
                    DataType = CellValues.Number,
                    CellValue = new CellValue(num.ToString(System.Globalization.CultureInfo.InvariantCulture))
                };

            // Date?
            if (DateTime.TryParse(value, out var dt))
                return new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(dt.ToString("yyyy-MM-dd"))
                };

            // Default: string
            return new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(value)
            };
        }

        // -----------------------------
        // Freeze header row
        // -----------------------------
        private static void AddFreezePane(WorksheetPart wsPart)
        {
            var ws = wsPart.Worksheet;

            var sheetViews = new SheetViews();
            var view = new SheetView { WorkbookViewId = 0 };

            view.Append(new Pane
            {
                VerticalSplit = 1D,
                TopLeftCell = "A2",
                ActivePane = PaneValues.BottomLeft,
                State = PaneStateValues.Frozen
            });

            sheetViews.Append(view);

            ws.InsertAt(sheetViews, 0);
        }

        // -----------------------------
        // AutoFilter on first row
        // -----------------------------
        private static void AddAutoFilter(WorksheetPart wsPart, int columnCount)
        {
            string lastColumn = ToExcelColumnName(columnCount);
            string range = $"A1:{lastColumn}1";

            wsPart.Worksheet.Append(new AutoFilter { Reference = range });
        }

        // Convert 1 → A, 27 → AA
        private static string ToExcelColumnName(int col)
        {
            string result = "";
            while (col > 0)
            {
                col--;
                result = (char)('A' + col % 26) + result;
                col /= 26;
            }
            return result;
        }

        // --------------------------------
        // Minimal stylesheet for fast write
        // --------------------------------
        private static Stylesheet CreateMinimalStylesheet()
        {
            return new Stylesheet(
                new Fonts(new Font()),
                new Fills(new Fill(new PatternFill())),
                new Borders(new Border()),
                new CellFormats(new CellFormat())
            );
        }
    }
}
