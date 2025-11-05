using System;
using System.IO;
using Mallard.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;

namespace Mallard
{
    public class ExcelExporter
    {
        private readonly string _databasePath;
        private readonly bool _addPowerQuery;

        public ExcelExporter(string databasePath = "", bool addPowerQuery = true)
        {
            // Set EPPlus license context (required for EPPlus 5.0+)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _databasePath = databasePath;
            _addPowerQuery = addPowerQuery;
        }

        public string ExportToExcel(QueryResult result, string filename, string sql)
        {
            if (result == null || result.Rows.Count == 0)
            {
                throw new InvalidOperationException("No data to export");
            }

            // Ensure filename has .xlsx extension
            if (!filename.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                filename += ".xlsx";
            }

            // Get absolute path
            var fullPath = Path.GetFullPath(filename);

            // Save SQL to external file for Power Query
            string sqlFilePath = "";
            string mFilePath = "";
            if (_addPowerQuery)
            {
                sqlFilePath = Path.ChangeExtension(fullPath, ".sql");
                File.WriteAllText(sqlFilePath, sql);

                // Also save the M code file for direct import
                mFilePath = Path.ChangeExtension(fullPath, ".m");
                var mCode = GeneratePowerQueryMCode(sql, sqlFilePath);
                File.WriteAllText(mFilePath, mCode);
            }

            using (var package = new ExcelPackage())
            {
                // Create Results sheet
                CreateResultsSheet(package, result);

                // Create Query Info sheet
                CreateQueryInfoSheet(package, result, sql);

                // Create Power Query sheet (refreshable from DuckDB)
                if (_addPowerQuery && !string.IsNullOrEmpty(_databasePath))
                {
                    CreatePowerQuerySheet(package, sql, sqlFilePath, mFilePath);
                }

                // Save to file
                var fileInfo = new FileInfo(fullPath);
                package.SaveAs(fileInfo);
            }

            return fullPath;
        }

        private void CreateResultsSheet(ExcelPackage package, QueryResult result)
        {
            var worksheet = package.Workbook.Worksheets.Add("Results");

            // Write headers
            for (int i = 0; i < result.ColumnNames.Count; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = result.ColumnNames[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Write data rows
            for (int rowIndex = 0; rowIndex < result.Rows.Count; rowIndex++)
            {
                var row = result.Rows[rowIndex];
                for (int colIndex = 0; colIndex < result.ColumnNames.Count; colIndex++)
                {
                    var columnName = result.ColumnNames[colIndex];
                    var cell = worksheet.Cells[rowIndex + 2, colIndex + 1];

                    if (row.ContainsKey(columnName) && row[columnName] != null)
                    {
                        var value = row[columnName];

                        // Try to parse and set appropriate data type
                        if (DateTime.TryParse(value.ToString(), out DateTime dateValue))
                        {
                            cell.Value = dateValue;
                            cell.Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                        }
                        else if (decimal.TryParse(value.ToString(), out decimal decimalValue))
                        {
                            cell.Value = decimalValue;
                            cell.Style.Numberformat.Format = "#,##0.00";
                        }
                        else if (int.TryParse(value.ToString(), out int intValue))
                        {
                            cell.Value = intValue;
                            cell.Style.Numberformat.Format = "#,##0";
                        }
                        else if (bool.TryParse(value.ToString(), out bool boolValue))
                        {
                            cell.Value = boolValue;
                        }
                        else
                        {
                            cell.Value = value.ToString();
                        }
                    }
                    else
                    {
                        cell.Value = "NULL";
                        cell.Style.Font.Italic = true;
                        cell.Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                    }
                }
            }

            // Freeze top row
            worksheet.View.FreezePanes(2, 1);

            // Auto-fit columns
            for (int col = 1; col <= result.ColumnNames.Count; col++)
            {
                worksheet.Column(col).AutoFit();

                // Set max width to prevent extremely wide columns
                if (worksheet.Column(col).Width > 50)
                {
                    worksheet.Column(col).Width = 50;
                }
            }

            // Add borders
            var dataRange = worksheet.Cells[1, 1, result.Rows.Count + 1, result.ColumnNames.Count];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        private void CreateQueryInfoSheet(ExcelPackage package, QueryResult result, string sql)
        {
            var worksheet = package.Workbook.Worksheets.Add("Query Info");

            // Add metadata
            int row = 1;

            // Title
            var titleCell = worksheet.Cells[row, 1];
            titleCell.Value = "Query Execution Information";
            titleCell.Style.Font.Size = 16;
            titleCell.Style.Font.Bold = true;
            row += 2;

            // Execution timestamp
            AddInfoRow(worksheet, ref row, "Executed At:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Row count
            AddInfoRow(worksheet, ref row, "Rows Returned:", result.RowCount.ToString("#,##0"));

            // Execution time
            AddInfoRow(worksheet, ref row, "Execution Time:", $"{result.ExecutionTimeMs:F2} ms");

            // Success status
            AddInfoRow(worksheet, ref row, "Status:", result.Success ? "Success" : "Failed");

            if (!string.IsNullOrEmpty(result.Error))
            {
                AddInfoRow(worksheet, ref row, "Error:", result.Error);
            }

            row += 2;

            // SQL Query
            var sqlLabelCell = worksheet.Cells[row, 1];
            sqlLabelCell.Value = "SQL Query:";
            sqlLabelCell.Style.Font.Bold = true;
            row++;

            var sqlCell = worksheet.Cells[row, 1];
            sqlCell.Value = sql;
            sqlCell.Style.WrapText = true;
            sqlCell.Style.Font.Name = "Consolas";
            sqlCell.Style.Font.Size = 10;
            sqlCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            sqlCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));

            // Merge cells for SQL query to make it more readable
            worksheet.Cells[row, 1, row, 5].Merge = true;

            // Auto-fit columns
            worksheet.Column(1).Width = 20;
            worksheet.Column(2).Width = 60;
        }

        private void CreatePowerQuerySheet(ExcelPackage package, string sql, string sqlFilePath, string mFilePath)
        {
            var worksheet = package.Workbook.Worksheets.Add("Power Query Setup");

            int row = 1;

            // Title
            var titleCell = worksheet.Cells[row, 1];
            titleCell.Value = "Power Query - Refreshable Connection Setup";
            titleCell.Style.Font.Size = 16;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);
            row += 2;

            // Quick start section
            var quickStartCell = worksheet.Cells[row, 1];
            quickStartCell.Value = "QUICK START - Import Pre-Made Query:";
            quickStartCell.Style.Font.Size = 13;
            quickStartCell.Style.Font.Bold = true;
            quickStartCell.Style.Font.Color.SetColor(System.Drawing.Color.DarkGreen);
            row++;

            var quickNote = worksheet.Cells[row, 1];
            quickNote.Value = $"A Power Query M file has been saved alongside this Excel file: {Path.GetFileName(mFilePath)}";
            quickNote.Style.Font.Italic = true;
            worksheet.Cells[row, 1, row, 6].Merge = true;
            row++;

            AddPowerQueryInstruction(worksheet, ref row, "1.", $"In Excel, go to Data > Get Data > Launch Power Query Editor");
            AddPowerQueryInstruction(worksheet, ref row, "2.", "In Power Query Editor: Home > New Source > Blank Query");
            AddPowerQueryInstruction(worksheet, ref row, "3.", $"Click Advanced Editor and paste the contents of {Path.GetFileName(mFilePath)}");
            AddPowerQueryInstruction(worksheet, ref row, "4.", "Click Done, then Close & Load to import the data");
            AddPowerQueryInstruction(worksheet, ref row, "5.", "Use Refresh in Excel to update data from DuckDB anytime!");
            row += 3;

            // Alternative manual method
            var altHeader = worksheet.Cells[row, 1];
            altHeader.Value = "ALTERNATIVE - Manual Setup (No External Files):";
            altHeader.Style.Font.Size = 12;
            altHeader.Style.Font.Bold = true;
            row += 2;

            AddPowerQueryInstruction(worksheet, ref row, "1.", "In Excel, go to the Data tab and click 'Get Data' > 'From Other Sources' > 'From ODBC'");
            AddPowerQueryInstruction(worksheet, ref row, "2.", "Select 'DuckDB' from the DSN list (or enter DSN=DuckDB)");
            AddPowerQueryInstruction(worksheet, ref row, "3.", "Click 'Advanced options' and paste the SQL query below into the SQL statement box");
            AddPowerQueryInstruction(worksheet, ref row, "4.", "Click 'OK' and then 'Load' to import the data");
            AddPowerQueryInstruction(worksheet, ref row, "5.", "Use 'Refresh' button in Excel to update data from DuckDB anytime");
            row += 2;

            // Connection details
            var connHeader = worksheet.Cells[row, 1];
            connHeader.Value = "Connection Details:";
            connHeader.Style.Font.Bold = true;
            connHeader.Style.Font.Size = 11;
            row++;

            AddCodeBlock(worksheet, ref row, "ODBC DSN:", "DSN=DuckDB");
            if (!string.IsNullOrEmpty(_databasePath))
            {
                AddCodeBlock(worksheet, ref row, "Database Path:", _databasePath);
            }
            row += 2;

            // SQL Query
            var sqlHeader = worksheet.Cells[row, 1];
            sqlHeader.Value = "SQL Query to Use:";
            sqlHeader.Style.Font.Bold = true;
            sqlHeader.Style.Font.Size = 11;
            row++;

            var sqlCell = worksheet.Cells[row, 1];
            sqlCell.Value = sql;
            sqlCell.Style.WrapText = true;
            sqlCell.Style.Font.Name = "Consolas";
            sqlCell.Style.Font.Size = 10;
            sqlCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            sqlCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 248, 255));
            sqlCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            sqlCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            sqlCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            sqlCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[row, 1, row, 6].Merge = true;
            row += 3;

            // Power Query M Code alternative
            var mCodeHeader = worksheet.Cells[row, 1];
            mCodeHeader.Value = "Alternative: Power Query M Code (Advanced Editor):";
            mCodeHeader.Style.Font.Bold = true;
            mCodeHeader.Style.Font.Size = 11;
            row++;

            var mCodeNote = worksheet.Cells[row, 1];
            mCodeNote.Value = $"Power Query M Code (saved to {Path.GetFileName(mFilePath)}):";
            mCodeNote.Style.Font.Italic = true;
            worksheet.Cells[row, 1, row, 6].Merge = true;
            row++;

            var mCode = GeneratePowerQueryMCode(sql, sqlFilePath);

            var mCodeCell = worksheet.Cells[row, 1];
            mCodeCell.Value = mCode;
            mCodeCell.Style.WrapText = true;
            mCodeCell.Style.Font.Name = "Consolas";
            mCodeCell.Style.Font.Size = 10;
            mCodeCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            mCodeCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(245, 245, 245));
            mCodeCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            mCodeCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            mCodeCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            mCodeCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[row, 1, row, 6].Merge = true;
            row += 3;

            // Benefits section
            var benefitsHeader = worksheet.Cells[row, 1];
            benefitsHeader.Value = "Benefits of Power Query Connection:";
            benefitsHeader.Style.Font.Bold = true;
            benefitsHeader.Style.Font.Size = 11;
            row++;

            AddBulletPoint(worksheet, ref row, "• Data stays fresh - click Refresh to update from DuckDB");
            AddBulletPoint(worksheet, ref row, "• No need to re-run Mallard for data updates");
            AddBulletPoint(worksheet, ref row, "• Schedule automatic refreshes in Excel");
            AddBulletPoint(worksheet, ref row, "• Transform data with Power Query tools");
            row += 2;

            // Note section
            var noteCell = worksheet.Cells[row, 1];
            noteCell.Value = "Note: DuckDB ODBC driver must be installed and configured with DSN=DuckDB for this to work.";
            noteCell.Style.Font.Italic = true;
            noteCell.Style.Font.Color.SetColor(System.Drawing.Color.DarkOrange);
            worksheet.Cells[row, 1, row, 6].Merge = true;

            // Format columns
            worksheet.Column(1).Width = 100;
        }

        private string GeneratePowerQueryMCode(string sql, string sqlFilePath)
        {
            // Generate M code that loads SQL from external file (more flexible)
            // or embeds it directly if no file path provided
            if (!string.IsNullOrEmpty(sqlFilePath))
            {
                return $@"let
    // Load SQL query from external file
    SqlFilePath = ""{sqlFilePath.Replace("\\", "\\\\")}"",
    SqlText = Text.FromBinary(File.Contents(SqlFilePath)),

    // Execute query against DuckDB via ODBC
    Source = Odbc.Query(""DSN=DuckDB"", SqlText)
in
    Source";
            }
            else
            {
                // Fallback: embed SQL directly in M code
                return $@"let
    // Execute query against DuckDB via ODBC
    Source = Odbc.Query(""DSN=DuckDB"", ""{sql.Replace("\"", "\"\"")}"")
in
    Source";
            }
        }

        private void AddPowerQueryInstruction(ExcelWorksheet worksheet, ref int row, string step, string instruction)
        {
            var stepCell = worksheet.Cells[row, 1];
            stepCell.Value = step;
            stepCell.Style.Font.Bold = true;

            var instructionCell = worksheet.Cells[row, 2];
            instructionCell.Value = instruction;
            worksheet.Cells[row, 2, row, 6].Merge = true;

            row++;
        }

        private void AddCodeBlock(ExcelWorksheet worksheet, ref int row, string label, string code)
        {
            var labelCell = worksheet.Cells[row, 1];
            labelCell.Value = label;
            labelCell.Style.Font.Bold = true;

            var codeCell = worksheet.Cells[row, 2];
            codeCell.Value = code;
            codeCell.Style.Font.Name = "Consolas";
            codeCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            codeCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(250, 250, 250));

            row++;
        }

        private void AddBulletPoint(ExcelWorksheet worksheet, ref int row, string text)
        {
            var cell = worksheet.Cells[row, 1];
            cell.Value = text;
            worksheet.Cells[row, 1, row, 6].Merge = true;
            row++;
        }

        private void AddInfoRow(ExcelWorksheet worksheet, ref int row, string label, string value)
        {
            var labelCell = worksheet.Cells[row, 1];
            labelCell.Value = label;
            labelCell.Style.Font.Bold = true;

            var valueCell = worksheet.Cells[row, 2];
            valueCell.Value = value;

            row++;
        }
    }
}
