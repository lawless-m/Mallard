using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mallard.Models;

namespace Mallard
{
    public class SqlAssistant
    {
        private readonly IQueryExecutor _executor;
        private readonly ClaudeApiClient _claudeClient;
        private readonly ConversationContext _context;
        private readonly ExcelExporter _excelExporter;
        private QueryResult? _lastQueryResult;
        private string _lastSql = string.Empty;

        public SqlAssistant(IQueryExecutor executor, ClaudeApiClient claudeClient, ConversationContext context)
        {
            _executor = executor;
            _claudeClient = claudeClient;
            _context = context;
            _excelExporter = new ExcelExporter();
        }

        public async Task RunAsync()
        {
            ShowWelcomeMessage();

            while (true)
            {
                Console.Write("\n> ");
                var input = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (await HandleSpecialCommands(input))
                {
                    continue;
                }

                await HandleUserQuery(input);
            }
        }

        private void ShowWelcomeMessage()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║            Mallard - DuckDB SQL Assistant                     ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine("\nNamed after the fastest steam train - helping you query at record speed!\n");
            Console.WriteLine($"Loaded {_context.Schemas.Count} tables");
            Console.WriteLine("\nType 'help' for commands or describe what you want to query.");
        }

        private async Task<bool> HandleSpecialCommands(string input)
        {
            var command = input.ToLower();

            switch (command)
            {
                case "exit":
                case "quit":
                    Console.WriteLine("\nGoodbye!");
                    Environment.Exit(0);
                    return true;

                case "help":
                    ShowHelp();
                    return true;

                case "schema":
                    ShowSchema();
                    return true;

                case "history":
                    ShowHistory();
                    return true;

                case "clear":
                    ClearHistory();
                    return true;
            }

            if (command.StartsWith("export"))
            {
                var parts = input.Split(new[] { ' ' }, 2);
                var filename = parts.Length > 1 ? parts[1] : $"results_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                await ExportResults(filename);
                return true;
            }

            if (command.StartsWith("explain"))
            {
                var sql = input.Length > 8 ? input.Substring(8).Trim() : _lastSql;
                if (!string.IsNullOrEmpty(sql))
                {
                    await ExplainQuery(sql);
                }
                else
                {
                    Console.WriteLine("No query to explain. Provide SQL or execute a query first.");
                }
                return true;
            }

            return false;
        }

        private async Task HandleUserQuery(string userMessage)
        {
            Console.WriteLine("\nGenerating query...\n");

            var response = await _claudeClient.SendMessageAsync(userMessage, _context);

            if (!response.Success)
            {
                Console.WriteLine($"Error: {response.Error}");
                return;
            }

            // Add to conversation history
            _context.History.Add(new Message { Role = "user", Content = userMessage });
            _context.History.Add(new Message { Role = "assistant", Content = response.RawResponse });

            // Display explanation
            if (!string.IsNullOrEmpty(response.Explanation))
            {
                Console.WriteLine("Explanation:");
                Console.WriteLine(response.Explanation);
                Console.WriteLine();
            }

            // Display SQL
            if (!string.IsNullOrEmpty(response.Sql))
            {
                Console.WriteLine("SQL:");
                Console.WriteLine(response.Sql);
                Console.WriteLine();
                _lastSql = response.Sql;

                // Display teaching note if present
                if (!string.IsNullOrEmpty(response.TeachingNote))
                {
                    Console.WriteLine("💡 Teaching Note:");
                    Console.WriteLine(response.TeachingNote);
                    Console.WriteLine();
                }

                // Prompt to execute
                Console.Write("Execute this query? (y/n/e for explain): ");
                var answer = Console.ReadLine()?.Trim().ToLower() ?? "n";

                if (answer == "y" || answer == "yes")
                {
                    await ExecuteQuery(response.Sql);
                }
                else if (answer == "e" || answer == "explain")
                {
                    await ExplainQuery(response.Sql);
                }
            }
            else
            {
                Console.WriteLine("No SQL query was generated. Try rephrasing your request.");
            }
        }

        private async Task ExecuteQuery(string sql)
        {
            Console.WriteLine("\nExecuting...\n");

            var result = await _executor.ExecuteAsync(sql);
            _lastQueryResult = result;

            if (!result.Success)
            {
                Console.WriteLine($"Error: {result.Error}");
                return;
            }

            // Record in query history
            _context.QueryHistory.Add(new ExecutedQuery
            {
                Sql = sql,
                ExecutedAt = DateTime.Now,
                Success = result.Success,
                RowCount = result.RowCount
            });

            // Display results
            DisplayResults(result);

            Console.WriteLine($"\nExecuted in {result.ExecutionTimeMs:F2}ms");
        }

        private void DisplayResults(QueryResult result)
        {
            if (result.RowCount == 0)
            {
                Console.WriteLine("No results.");
                return;
            }

            Console.WriteLine($"Results ({result.RowCount} rows):");
            Console.WriteLine();

            // Calculate column widths
            var columnWidths = new Dictionary<string, int>();
            foreach (var col in result.ColumnNames)
            {
                columnWidths[col] = Math.Max(col.Length, 10);
            }

            foreach (var row in result.Rows.Take(10)) // Show first 10 rows
            {
                foreach (var col in result.ColumnNames)
                {
                    if (row.ContainsKey(col))
                    {
                        var value = row[col]?.ToString() ?? "";
                        columnWidths[col] = Math.Max(columnWidths[col], Math.Min(value.Length, 50));
                    }
                }
            }

            // Print header
            foreach (var col in result.ColumnNames)
            {
                Console.Write(col.PadRight(columnWidths[col] + 2));
            }
            Console.WriteLine();

            // Print separator
            foreach (var col in result.ColumnNames)
            {
                Console.Write(new string('-', columnWidths[col] + 2));
            }
            Console.WriteLine();

            // Print rows (first 10)
            foreach (var row in result.Rows.Take(10))
            {
                foreach (var col in result.ColumnNames)
                {
                    var value = row.ContainsKey(col) ? (row[col]?.ToString() ?? "NULL") : "NULL";
                    if (value.Length > 50)
                    {
                        value = value.Substring(0, 47) + "...";
                    }
                    Console.Write(value.PadRight(columnWidths[col] + 2));
                }
                Console.WriteLine();
            }

            if (result.RowCount > 10)
            {
                Console.WriteLine($"\n... and {result.RowCount - 10} more rows");
            }
        }

        private async Task ExplainQuery(string sql)
        {
            Console.WriteLine("\nAsking Claude to explain this query...\n");

            var explainMessage = $"Please explain this DuckDB SQL query in detail, including what it does and any advanced features it uses:\n\n{sql}";
            var response = await _claudeClient.SendMessageAsync(explainMessage, _context);

            if (response.Success)
            {
                Console.WriteLine(response.RawResponse);
            }
            else
            {
                Console.WriteLine($"Error: {response.Error}");
            }
        }

        private async Task ExportResults(string filename)
        {
            if (_lastQueryResult == null || _lastQueryResult.RowCount == 0)
            {
                Console.WriteLine("No query results to export. Execute a query first.");
                return;
            }

            try
            {
                Console.WriteLine($"\nExporting to Excel...");

                var fullPath = _excelExporter.ExportToExcel(_lastQueryResult, filename, _lastSql);

                Console.WriteLine($"✓ Created: {fullPath}");
                Console.WriteLine($"  - Results sheet: {_lastQueryResult.RowCount} rows, {_lastQueryResult.ColumnNames.Count} columns");
                Console.WriteLine($"  - Query Info sheet: metadata and SQL");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting to Excel: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        private void ShowHelp()
        {
            Console.WriteLine("\nAvailable Commands:");
            Console.WriteLine("  help              - Show this help message");
            Console.WriteLine("  schema            - Display available tables and columns");
            Console.WriteLine("  history           - Show query history");
            Console.WriteLine("  clear             - Clear conversation history");
            Console.WriteLine("  explain [query]   - Get detailed explanation of a query");
            Console.WriteLine("  export [filename] - Export last query results to Excel (.xlsx)");
            Console.WriteLine("  exit / quit       - Exit the program");
            Console.WriteLine("\nOr just type your question in natural language!");
        }

        private void ShowSchema()
        {
            Console.WriteLine("\nAvailable Tables:\n");

            foreach (var schema in _context.Schemas.Values)
            {
                Console.WriteLine($"📊 {schema.TableName}");
                Console.WriteLine($"   Source: {System.IO.Path.GetFileName(schema.SourceFile)}");
                Console.WriteLine($"   Columns ({schema.Columns.Count}):");

                foreach (var col in schema.Columns)
                {
                    var nullable = col.Nullable ? "nullable" : "not null";
                    Console.WriteLine($"     - {col.Name}: {col.Type} ({nullable})");
                }

                Console.WriteLine();
            }
        }

        private void ShowHistory()
        {
            if (_context.QueryHistory.Count == 0)
            {
                Console.WriteLine("\nNo query history yet.");
                return;
            }

            Console.WriteLine("\nQuery History:\n");

            foreach (var query in _context.QueryHistory.TakeLast(10))
            {
                var status = query.Success ? "✓" : "✗";
                Console.WriteLine($"{status} [{query.ExecutedAt:HH:mm:ss}] {query.RowCount} rows");
                Console.WriteLine($"  {query.Sql.Substring(0, Math.Min(100, query.Sql.Length))}...");
                Console.WriteLine();
            }
        }

        private void ClearHistory()
        {
            _context.History.Clear();
            Console.WriteLine("\nConversation history cleared.");
        }
    }
}
