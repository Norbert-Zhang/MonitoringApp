using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace BlazorWebApp.Helpers
{
    public static class ExcelExportHelper
    {
        // ---------------------------
        // Public: Generate Excel File
        // ---------------------------
        public static byte[] CreateExcel(Dictionary<string, List<List<string>>> sheetsData)
        {
            using var mem = new MemoryStream();

            using (var doc = SpreadsheetDocument.Create(mem, SpreadsheetDocumentType.Workbook))
            {
                var wb = doc.AddWorkbookPart();
                wb.Workbook = new Workbook();

                // add styles
                var styles = wb.AddNewPart<WorkbookStylesPart>();
                styles.Stylesheet = CreateStylesheet();
                styles.Stylesheet.Save();

                var sheets = wb.Workbook.AppendChild(new Sheets());
                uint sheetId = 1;

                // create sheets
                foreach (var sheet in sheetsData)
                {
                    var wsPart = wb.AddNewPart<WorksheetPart>();
                    WriteSheet(wsPart, sheet.Value);

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

        // ---------------------------
        // Private: Write One Sheet
        // ---------------------------
        private static void WriteSheet(WorksheetPart ws, List<List<string>> rows)
        {
            var sheetData = new SheetData();

            // == Header Row ==
            var headerRow = new Row();
            foreach (var header in rows[0])
            {
                headerRow.Append(new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(header),
                    StyleIndex = 1 // header style
                });
            }
            sheetData.Append(headerRow);

            // == Data Rows ==
            for (int i = 1; i < rows.Count; i++)
            {
                var row = new Row();
                foreach (var cellValue in rows[i])
                {
                    row.Append(CreateCell(cellValue));
                }
                sheetData.Append(row);
            }

            // == Auto-fit Columns ==
            var columns = new Columns();
            for (int col = 0; col < rows[0].Count; col++)
            {
                int maxLen = rows.Max(r => r[col].Length);
                double width = Math.Min(maxLen + 4, 60); // clamp max width

                columns.Append(new Column
                {
                    Min = (uint)(col + 1),
                    Max = (uint)(col + 1),
                    Width = width,
                    CustomWidth = true
                });
            }

            ws.Worksheet = new Worksheet(columns, sheetData);
        }

        /// <summary>
        /// Create a cell with automatic type detection.
        /// Support:
        ///   - number
        ///   - date
        ///   - string
        /// </summary>
        public static Cell CreateCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return CreateStringCell("");

            // 1. Try parse as date
            if (DateTime.TryParse(value, out DateTime dt))
                return CreateStringCell(dt.ToString("yyyy-MM-dd"));

            // 2. Try parse as integer/float
            if (double.TryParse(value, out double num))
                return CreateNumberCell(num);

            // 3. Fallback as string
            return CreateStringCell(value);
        }

        public static Cell CreateStringCell(string text)
        {
            return new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(text),
                StyleIndex = 2 // body style
            };
        }

        public static Cell CreateNumberCell(double number)
        {
            return new Cell
            {
                DataType = CellValues.Number,
                CellValue = new CellValue(number.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                StyleIndex = 2 // body style
            };
        }

        // ---------------------------
        // Private: Stylesheet
        // ---------------------------
        private static Stylesheet CreateStylesheet()
        {
            return new Stylesheet(
                new Fonts(
                    new Font(), // 0 = normal
                    new Font(new Bold()) // 1 = bold
                ),
                new Fills(
                    new Fill(new PatternFill { PatternType = PatternValues.None }),
                    new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
                    new Fill(new PatternFill // header background
                    {
                        PatternType = PatternValues.Solid,
                        ForegroundColor = new ForegroundColor { Rgb = "FFDDEBF7" },
                        BackgroundColor = new BackgroundColor { Indexed = 64 }
                    })
                ),
                new Borders(
                    new Border(),
                    new Border( // thin border
                        new LeftBorder { Style = BorderStyleValues.Thin },
                        new RightBorder { Style = BorderStyleValues.Thin },
                        new TopBorder { Style = BorderStyleValues.Thin },
                        new BottomBorder { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                ),
                new CellFormats(
                    new CellFormat(), // 0 = default

                    // 1 = header cell
                    new CellFormat
                    {
                        FontId = 1,
                        FillId = 2,
                        BorderId = 1,
                        ApplyFont = true,
                        ApplyFill = true,
                        ApplyBorder = true
                    },

                    // 2 = normal cell
                    new CellFormat
                    {
                        BorderId = 1,
                        ApplyBorder = true
                    }
                )
            );
        }
    }
}