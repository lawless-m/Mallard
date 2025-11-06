using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mallard.Models;

namespace Mallard.Executors
{
    public class DuckDbCliExecutor : IQueryExecutor
    {
        private readonly string _duckDbExePath;
        private readonly string _databasePath;
        private readonly int _timeoutSeconds;

        public DuckDbCliExecutor(string duckDbExePath, string databasePath, int timeoutSeconds = 60)
        {
            _duckDbExePath = duckDbExePath;
            _databasePath = databasePath;
            _timeoutSeconds = timeoutSeconds;
        }

        public async Task<QueryResult> ExecuteAsync(string sql)
        {
            var result = new QueryResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _duckDbExePath,
                    Arguments = $"\"{_databasePath}\" -csv -c \"{sql.Replace("\"", "\"\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) error.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var completed = await Task.Run(() => process.WaitForExit(_timeoutSeconds * 1000));

                if (!completed)
                {
                    process.Kill();
                    result.Success = false;
                    result.Error = $"Query timeout after {_timeoutSeconds} seconds";
                    return result;
                }

                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

                var errorText = error.ToString().Trim();
                if (!string.IsNullOrEmpty(errorText))
                {
                    result.Success = false;
                    result.Error = errorText;
                    return result;
                }

                var outputText = output.ToString();
                result.Output = outputText;

                // Parse CSV output
                ParseCsvOutput(outputText, result);

                result.Success = true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Error = ex.Message;
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            }

            return result;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (!File.Exists(_duckDbExePath))
                {
                    return false;
                }

                var result = await ExecuteAsync("SELECT 1 as test");
                return result.Success;
            }
            catch
            {
                return false;
            }
        }

        public string GetExecutorType()
        {
            return "DuckDB CLI";
        }

        private void ParseCsvOutput(string csvOutput, QueryResult result)
        {
            if (string.IsNullOrWhiteSpace(csvOutput))
            {
                result.RowCount = 0;
                return;
            }

            var lines = csvOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                result.RowCount = 0;
                return;
            }

            // First line is headers
            var headers = ParseCsvLine(lines[0]);
            result.ColumnNames = headers;

            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);
                if (values.Count == headers.Count)
                {
                    var row = new Dictionary<string, object>();
                    for (int j = 0; j < headers.Count; j++)
                    {
                        row[headers[j]] = values[j];
                    }
                    result.Rows.Add(row);
                }
            }

            result.RowCount = result.Rows.Count;
        }

        private List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return values;
        }
    }
}
