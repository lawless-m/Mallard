using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Mallard.Executors;
using Mallard.Models;

namespace Mallard
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Mallard - DuckDB SQL Assistant");

            var schemaPathOption = new Option<string>(
                new[] { "--schema-path", "-s" },
                () => "./schemas",
                "Path to directory containing Parquet files");

            var dbPathOption = new Option<string>(
                new[] { "--db-path", "-d" },
                () => "./data.duckdb",
                "Path to DuckDB database file");

            var executorOption = new Option<string>(
                new[] { "--executor", "-e" },
                () => "cli",
                "Executor type: cli, odbc, native");

            var duckDbExeOption = new Option<string>(
                "--duckdb-exe",
                () => GetDefaultDuckDbPath(),
                "Path to duckdb executable");

            rootCommand.AddOption(schemaPathOption);
            rootCommand.AddOption(dbPathOption);
            rootCommand.AddOption(executorOption);
            rootCommand.AddOption(duckDbExeOption);

            rootCommand.SetHandler(async (InvocationContext context) =>
            {
                var schemaPath = context.ParseResult.GetValueForOption(schemaPathOption) ?? "./schemas";
                var dbPath = context.ParseResult.GetValueForOption(dbPathOption) ?? "./data.duckdb";
                var executor = context.ParseResult.GetValueForOption(executorOption) ?? "cli";
                var duckDbExe = context.ParseResult.GetValueForOption(duckDbExeOption) ?? GetDefaultDuckDbPath();

                await RunAssistant(schemaPath, dbPath, executor, duckDbExe);
            });

            return await rootCommand.InvokeAsync(args);
        }

        static async Task RunAssistant(string schemaPath, string dbPath, string executorType, string duckDbExe)
        {
            try
            {
                // Check for API key
                var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("Error: ANTHROPIC_API_KEY environment variable not set.");
                    Console.WriteLine("Please set your Anthropic API key:");
                    Console.WriteLine("  export ANTHROPIC_API_KEY=your_key_here");
                    return;
                }

                // Initialize executor
                IQueryExecutor executor = executorType.ToLower() switch
                {
                    "cli" => new DuckDbCliExecutor(duckDbExe, dbPath),
                    "odbc" => throw new NotImplementedException("ODBC executor not yet implemented"),
                    "native" => throw new NotImplementedException("Native executor not yet implemented"),
                    _ => throw new ArgumentException($"Unknown executor type: {executorType}")
                };

                // Test connection
                Console.WriteLine($"Testing connection to DuckDB ({executor.GetExecutorType()})...");
                if (!await executor.TestConnectionAsync())
                {
                    Console.WriteLine("Error: Could not connect to DuckDB.");
                    Console.WriteLine($"DuckDB executable: {duckDbExe}");
                    Console.WriteLine($"Database path: {dbPath}");
                    Console.WriteLine("\nPlease verify:");
                    Console.WriteLine("  1. DuckDB is installed and in your PATH");
                    Console.WriteLine("  2. Or specify the path with --duckdb-exe");
                    return;
                }
                Console.WriteLine("✓ Connection successful");

                // Load schemas
                var schemaLoader = new SchemaLoader(executor);
                var schemas = await schemaLoader.LoadSchemasAsync(schemaPath);

                // Initialize conversation context
                var context = new ConversationContext
                {
                    Schemas = schemas
                };

                // Initialize Claude API client
                var claudeClient = new ClaudeApiClient(apiKey);

                // Start the assistant
                var assistant = new SqlAssistant(executor, claudeClient, context);
                await assistant.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static string GetDefaultDuckDbPath()
        {
            // Try common locations
            var windowsPath = @"Y:\Data Warehouse\duckdb\duckdb.exe";
            if (File.Exists(windowsPath))
            {
                return windowsPath;
            }

            // Default to "duckdb" in PATH
            return "duckdb";
        }
    }
}
