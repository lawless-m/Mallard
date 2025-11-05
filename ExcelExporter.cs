using System;
using System.IO;
using Mallard.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Mallard
{
    public class ExcelExporter
    {
        public ExcelExporter()
        {
            // Set EPPlus license context (required for EPPlus 5.0+)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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

            using (var package = new ExcelPackage())
            {
                // Create Results sheet
                CreateResultsSheet(package, result);

                // Create Query Info sheet
                CreateQueryInfoSheet(package, result, sql);

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
