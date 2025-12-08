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

                // minimal stylesheet (you can extend it if needed)
                var styles = wb.AddNewPart<WorkbookStylesPart>();
                styles.Stylesheet = CreateStylesheet();
                styles.Stylesheet.Save();

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
            bool isHeaderRow = true;
            foreach (var rowData in rows)
            {
                writer.WriteStartElement(new Row());
                foreach (var v in rowData)
                {
                    writer.WriteElement(CreateCell(v, isHeaderRow));
                }
                writer.WriteEndElement(); // </Row>
                isHeaderRow = false;
            }

            writer.WriteEndElement(); // </SheetData>

            // --- AutoFilter as sibling to SheetData (direct child of Worksheet) ---
            string lastColumn = ToCol(colCount);
            writer.WriteElement(new AutoFilter { Reference = $"A1:{lastColumn}1" });

            writer.WriteEndElement(); // </Worksheet>
        }

        private static Cell CreateCell(string value, bool isHeaderRow)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new Cell { DataType = CellValues.String, CellValue = new CellValue(""), StyleIndex = UInt32Value.FromUInt32((uint)(isHeaderRow ? 1 : 2)) };

            if (double.TryParse(value, out double num))
                return new Cell
                {
                    DataType = CellValues.Number,
                    CellValue = new CellValue(num.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    StyleIndex = UInt32Value.FromUInt32((uint)(isHeaderRow ? 1 : 2))
                };

            if (DateTime.TryParse(value, out var dt))
                return new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(dt.ToString("yyyy-MM-dd")),
                    StyleIndex = UInt32Value.FromUInt32((uint)(isHeaderRow ? 1 : 2))
                };

            return new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(value),
                StyleIndex = UInt32Value.FromUInt32((uint)(isHeaderRow ? 1 : 2))
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

        private static Stylesheet CreateStylesheet()
        {
            return new Stylesheet(
                new Fonts(
                    new Font(), // 0 = normal (default font)
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
                    new Border(), // default border
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
