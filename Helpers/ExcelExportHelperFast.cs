using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace BlazorWebApp.Helpers
{
    public static class ExcelExportHelperFast
    {
        public static byte[] CreateExcel(Dictionary<string, IEnumerable<string[]>> sheetsData)
        {
            using var mem = new MemoryStream();

            using (var doc = SpreadsheetDocument.Create(mem, SpreadsheetDocumentType.Workbook))
            {
                var wb = doc.AddWorkbookPart();
                wb.Workbook = new Workbook();

                var sheets = wb.Workbook.AppendChild(new Sheets());
                uint sheetId = 1;

                foreach (var kv in sheetsData)
                {
                    string sheetName = kv.Key;
                    IEnumerable<string[]> rows = kv.Value;

                    var wsPart = wb.AddNewPart<WorksheetPart>();

                    WriteWorksheetStream(wsPart, rows);

                    sheets.Append(
                        new Sheet
                        {
                            Id = wb.GetIdOfPart(wsPart),
                            SheetId = sheetId++,
                            Name = sheetName
                        });
                }

                wb.Workbook.Save();
            }

            return mem.ToArray();
        }

        private static void WriteWorksheetStream(WorksheetPart wsPart, IEnumerable<string[]> rows)
        {
            using var writer = OpenXmlWriter.Create(wsPart);

            writer.WriteStartElement(new Worksheet());

            // ========= Freeze Pane + AutoFilter written as XML elements ============
            writer.WriteStartElement(new SheetViews());
            writer.WriteStartElement(new SheetView() { WorkbookViewId = 0 });
            writer.WriteStartElement(new Pane()
            {
                State = PaneStateValues.Frozen,
                ActivePane = PaneValues.BottomLeft,
                TopLeftCell = "A2",
                VerticalSplit = 1
            });
            writer.WriteEndElement(); // </Pane>
            writer.WriteStartElement(new Selection()
            {
                Pane = PaneValues.BottomLeft,
                ActiveCell = "A2"
            });
            writer.WriteEndElement(); // </Selection>
            writer.WriteEndElement(); // </SheetView>
            writer.WriteEndElement(); // </SheetViews>
            // =================================================================

            // ===== Columns (fixed width = 20) =====
            var first = rows.First();
            int colCount = first.Length;

            writer.WriteStartElement(new Columns());
            for (int i = 1; i <= colCount; i++)
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
            // ======================================

            writer.WriteStartElement(new SheetData());

            bool isHeaderDone = false;

            foreach (var rowData in rows)
            {
                writer.WriteStartElement(new Row());

                foreach (var v in rowData)
                {
                    writer.WriteElement(CreateCell(v));
                }

                writer.WriteEndElement(); // </Row>

                if (!isHeaderDone)
                {
                    // AutoFilter must be directly after header row
                    string lastColumn = ToCol(colCount);

                    writer.WriteStartElement(new AutoFilter() { Reference = $"A1:{lastColumn}1" });
                    writer.WriteEndElement(); // </AutoFilter>
                    isHeaderDone = true;
                }
            }

            writer.WriteEndElement(); // </SheetData>
            writer.WriteEndElement(); // </Worksheet>
        }

        private static Cell CreateCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new Cell { DataType = CellValues.String, CellValue = new CellValue("") };

            if (double.TryParse(value, out double num))
                return new Cell
                {
                    DataType = CellValues.Number,
                    CellValue = new CellValue(num.ToString(System.Globalization.CultureInfo.InvariantCulture))
                };

            if (DateTime.TryParse(value, out var dt))
                return new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(dt.ToString("yyyy-MM-dd"))
                };

            return new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(value)
            };
        }

        // Convert number to column letters
        private static string ToCol(int col)
        {
            string s = "";
            while (col > 0)
            {
                col--;
                s = (char)('A' + (col % 26)) + s;
                col /= 26;
            }
            return s;
        }
    }
}
