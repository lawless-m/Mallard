using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mallard.Models;

namespace Mallard
{
    public class SchemaLoader
    {
        private readonly IQueryExecutor _executor;
        private Dictionary<string, TableSchema> _schemas = new Dictionary<string, TableSchema>();

        public SchemaLoader(IQueryExecutor executor)
        {
            _executor = executor;
        }

        public async Task<Dictionary<string, TableSchema>> LoadSchemasAsync(string directory)
        {
            _schemas.Clear();

            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Warning: Schema directory does not exist: {directory}");
                return _schemas;
            }

            var parquetFiles = Directory.GetFiles(directory, "*.parquet", SearchOption.AllDirectories);

            if (parquetFiles.Length == 0)
            {
                Console.WriteLine($"Warning: No Parquet files found in {directory}");
                return _schemas;
            }

            Console.WriteLine($"Loading schemas from {parquetFiles.Length} Parquet files...");

            foreach (var file in parquetFiles)
            {
                try
                {
                    var schema = await ExtractSchemaFromParquet(file);
                    if (schema != null)
                    {
                        _schemas[schema.TableName] = schema;
                        Console.WriteLine($"  ✓ Loaded schema for {schema.TableName} ({schema.Columns.Count} columns)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed to load schema from {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            return _schemas;
        }

        private async Task<TableSchema?> ExtractSchemaFromParquet(string filePath)
        {
            // Use DuckDB's DESCRIBE to get schema information
            var sql = $"DESCRIBE SELECT * FROM '{filePath.Replace("\\", "/")}' LIMIT 1";
            var result = await _executor.ExecuteAsync(sql);

            if (!result.Success || result.Rows.Count == 0)
            {
                return null;
            }

            var tableName = Path.GetFileNameWithoutExtension(filePath);
            var schema = new TableSchema
            {
                TableName = tableName,
                SourceFile = filePath
            };

            foreach (var row in result.Rows)
            {
                var columnName = row.ContainsKey("column_name") ? row["column_name"].ToString() ?? "" : "";
                var columnType = row.ContainsKey("column_type") ? row["column_type"].ToString() ?? "" : "";
                var nullable = row.ContainsKey("null") ? row["null"].ToString()?.ToLower() == "yes" : true;

                if (!string.IsNullOrEmpty(columnName))
                {
                    schema.Columns.Add(new ColumnInfo
                    {
                        Name = columnName,
                        Type = columnType,
                        Nullable = nullable
                    });
                }
            }

            return schema;
        }

        public Dictionary<string, TableSchema> GetSchemas()
        {
            return _schemas;
        }

        public string GetSchemaContext()
        {
            if (_schemas.Count == 0)
            {
                return "No tables loaded.";
            }

            var lines = new List<string> { "Available Tables:" };

            foreach (var schema in _schemas.Values)
            {
                var columns = string.Join(", ", schema.Columns.Select(c => $"{c.Name} {c.Type}"));
                lines.Add($"- {schema.TableName} ({columns})");
            }

            return string.Join("\n", lines);
        }
    }
}
