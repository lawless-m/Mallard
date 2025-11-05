# Mallard - DuckDB SQL Assistant - MVP Implementation Plan

## IMPORTANT: Skills Available
**Before starting implementation, please read the relevant skill files in the repository:**
- `/mnt/skills/public/xlsx/SKILL.md` - For Excel export implementation guidance
- Any other relevant skills in `/mnt/skills/` that may help with best practices

These skills contain battle-tested approaches and will help you avoid common pitfalls.

## Project Overview
A C# CLI tool that acts as an intelligent SQL assistant for DuckDB, helping users write queries through natural language. Built to help SQL users transition from limited systems (DBISAM) to DuckDB's richer feature set, with teaching and explanation capabilities.

Named after the Mallard steam locomotive - the fastest steam train ever, just as this tool helps you query faster than ever before.

## Target User Profile
- Understands basic SQL (joins, WHERE IN clauses)
- Never used CTEs, subqueries, or window functions
- Working with daily Parquet extracts of database tables
- Needs to learn DuckDB features while being productive

## Core Design Principles
1. **Swappable execution layer** - Easy to switch between CLI, ODBC, or native DuckDB connection
2. **Teaching mode** - Explain new SQL concepts as they're used
3. **Iterative refinement** - Maintain conversation context for query improvements
4. **Schema awareness** - Know all tables and columns from Parquet metadata

## Architecture

### Project Structure
```
Mallard/
├── Mallard.csproj
├── Program.cs                      # Entry point
├── SqlAssistant.cs                 # Main conversation loop
├── IQueryExecutor.cs               # Executor interface
├── Executors/
│   ├── DuckDbCliExecutor.cs       # Shell out to duckdb CLI
│   ├── DuckDbOdbcExecutor.cs      # Use ODBC connection (future)
│   └── DuckDbNativeExecutor.cs    # Use DuckDB.NET.Data (future)
├── SchemaLoader.cs                 # Load Parquet metadata
├── ClaudeApiClient.cs              # Anthropic API integration
├── ExcelExporter.cs                # Export results to Excel
├── Models/
│   ├── QueryResult.cs
│   ├── TableSchema.cs
│   └── ConversationContext.cs
└── README.md
```

## Component Specifications

### 1. Program.cs
**Purpose**: Entry point and configuration

**Responsibilities**:
- Parse command line arguments
- Load configuration (API key, paths)
- Initialize components
- Start the assistant

**Example skeleton**:
```csharp
namespace Mallard
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Mallard - DuckDB SQL Assistant");
            // ... initialization
        }
    }
}
```

**Arguments**:
- `--schema-path` or `-s`: Path to directory containing Parquet files (default: `./schemas`)
- `--db-path` or `-d`: Path to DuckDB database file (default: `./data.duckdb`)
- `--executor` or `-e`: Executor type: `cli`, `odbc`, `native` (default: `cli`)
- `--duckdb-exe`: Path to duckdb executable (default: `duckdb` from PATH)

**Environment Variables**:
- `ANTHROPIC_API_KEY`: Required for Claude API access

### 2. IQueryExecutor.cs
**Purpose**: Abstract interface for query execution

```csharp
public interface IQueryExecutor
{
    Task<QueryResult> ExecuteAsync(string sql);
    Task<bool> TestConnectionAsync();
    string GetExecutorType();
}

public class QueryResult
{
    public bool Success { get; set; }
    public string Output { get; set; }        // Raw output
    public string Error { get; set; }
    public int RowCount { get; set; }
    public double ExecutionTimeMs { get; set; }
    public List<Dictionary<string, object>> Rows { get; set; }  // Parsed results
}
```

### 3. DuckDbCliExecutor.cs
**Purpose**: Execute queries via CLI

**Implementation Details**:
- Use `Process.Start()` to call duckdb executable
- Execute with: `duckdb [dbpath] -csv -c "[SQL]"`
- Parse CSV output into structured results
- Capture stderr for errors
- Set reasonable timeout (30 seconds default, configurable)

**Key Methods**:
- `ExecuteAsync(string sql)`: Run query and return results
- `TestConnectionAsync()`: Verify duckdb CLI is available
- Parse CSV output to populate `QueryResult.Rows`

### 4. SchemaLoader.cs
**Purpose**: Extract and cache table schemas from Parquet files

**Responsibilities**:
- Scan directory for .parquet files
- Extract schema metadata (columns, types)
- Cache schema information
- Generate schema summary for Claude context

**Key Methods**:
- `LoadSchemasAsync(string directory)`: Scan all Parquet files
- `GetSchemaContext()`: Return formatted schema for Claude API
- Use DuckDB to query schema: `DESCRIBE SELECT * FROM 'file.parquet'`

**Schema Format for Claude**:
```
Available Tables:
- customers (customer_id INT, name VARCHAR, email VARCHAR, created_date DATE)
- orders (order_id INT, customer_id INT, order_date DATE, total DECIMAL(10,2))
- order_items (item_id INT, order_id INT, product_id INT, quantity INT, price DECIMAL(10,2))
```

### 5. ClaudeApiClient.cs
**Purpose**: Handle Anthropic API communication

**Responsibilities**:
- Manage conversation history
- Send messages to Claude API
- Parse responses to extract SQL and explanations
- Handle API errors gracefully

**Key Methods**:
- `SendMessageAsync(string userMessage, ConversationContext context)`: Send user query
- `ExtractSqlFromResponse(string response)`: Parse SQL from Claude's response
- Handle streaming for better UX (optional for MVP)

**System Prompt Structure**:
```
You are a SQL assistant helping users write DuckDB queries. The user understands 
basic SQL (joins, WHERE clauses) but is new to CTEs, subqueries, and window functions.

Your responses should:
1. Generate valid DuckDB SQL
2. Explain new concepts when you use them
3. Suggest optimizations when relevant
4. Format SQL clearly with proper indentation

When responding, use this format:
<explanation>
Brief explanation of the approach
</explanation>

<sql>
-- Your SQL query here
SELECT ...
</sql>

<teaching_note>
Optional: Explain any new SQL features used (CTEs, window functions, etc.)
</teaching_note>

Available schema:
{schema_context}
```

### 6. SqlAssistant.cs
**Purpose**: Main conversation loop

**Responsibilities**:
- Display welcome message and instructions
- Accept user input
- Send to Claude API
- Display generated SQL
- Prompt user to execute (y/n) or refine
- Execute queries via IQueryExecutor
- Display results
- Maintain conversation context

**Conversation Flow**:
1. User enters natural language query
2. Send to Claude with schema context + history
3. Display generated SQL with explanation
4. Ask: "Execute this query? (y/n/e for explain): "
5. If yes: execute and show results
6. If explain: show detailed breakdown
7. If no: ask for refinement
8. Loop

**Special Commands**:
- `exit` or `quit`: Exit the program
- `schema`: Display available tables
- `history`: Show query history
- `clear`: Clear conversation history
- `explain [query]`: Get detailed explanation of a query
- `export [filename]`: Export last query results to Excel (default: results_TIMESTAMP.xlsx)
- `help`: Show available commands

### 7. ExcelExporter.cs
**Purpose**: Export query results to Excel format

**Responsibilities**:
- Take QueryResult and export to .xlsx file
- Format with headers (column names)
- Auto-fit columns for readability
- Add metadata sheet with query SQL and execution info
- Handle data type formatting (dates, numbers, text)

**Key Methods**:
- `ExportToExcel(QueryResult result, string filename, string sql)`: Main export method
- `FormatWorksheet(worksheet)`: Apply formatting (bold headers, freeze panes, etc.)
- Returns full path to created file

**Implementation Notes**:
- Use EPPlus or ClosedXML (EPPlus recommended for simplicity)
- Create two worksheets: "Results" (data) and "Query Info" (metadata)
- Query Info sheet should include: SQL, execution time, row count, timestamp
- Handle null values gracefully
- Set appropriate Excel data types based on DuckDB types

**Example Output**:
- Sheet 1 "Results": Formatted table with query results
- Sheet 2 "Query Info": 
  - SQL: [the query]
  - Executed: 2025-11-05 14:30:22
  - Rows: 1,234
  - Execution Time: 0.45s

### 8. Models

**TableSchema.cs**:
```csharp
public class TableSchema
{
    public string TableName { get; set; }
    public List<ColumnInfo> Columns { get; set; }
    public string SourceFile { get; set; }
}

public class ColumnInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool Nullable { get; set; }
}
```

**ConversationContext.cs**:
```csharp
public class ConversationContext
{
    public List<Message> History { get; set; }
    public Dictionary<string, TableSchema> Schemas { get; set; }
    public List<ExecutedQuery> QueryHistory { get; set; }
}

public class Message
{
    public string Role { get; set; }  // "user" or "assistant"
    public string Content { get; set; }
}

public class ExecutedQuery
{
    public string Sql { get; set; }
    public DateTime ExecutedAt { get; set; }
    public bool Success { get; set; }
    public int RowCount { get; set; }
}
```

## NuGet Dependencies

```xml
<PackageReference Include="Anthropic.SDK" Version="0.1.*" />
<!-- Or use RestSharp/HttpClient for API calls if SDK not suitable -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.*" />
<PackageReference Include="System.CommandLine" Version="2.0.*" />
<PackageReference Include="EPPlus" Version="7.0.*" />
<!-- EPPlus for Excel export - note: requires license for commercial use -->
<!-- Alternative: <PackageReference Include="ClosedXML" Version="0.102.*" /> -->
```

## Configuration File (Optional)

**appsettings.json**:
```json
{
  "DuckDb": {
    "DefaultExecutor": "cli",
    "DuckDbExePath": "duckdb",
    "QueryTimeout": 30000,
    "MaxResultRows": 100
  },
  "Claude": {
    "Model": "claude-sonnet-4-5-20250929",
    "MaxTokens": 4096,
    "Temperature": 0.0
  }
}
```

## Implementation Phases

### Phase 1: Basic Structure (MVP Core)
1. Create project structure
2. Implement IQueryExecutor interface
3. Implement DuckDbCliExecutor
4. Basic ClaudeApiClient (hardcoded prompt for now)
5. Simple Program.cs that ties it together
6. Test with a single hardcoded query

### Phase 2: Schema Awareness
1. Implement SchemaLoader
2. Integrate schema context into Claude prompts
3. Test with multiple tables

### Phase 3: Conversation Loop
1. Implement SqlAssistant conversation manager
2. Add conversation history tracking
3. Add special commands (exit, schema, help)
4. Implement query confirmation flow

### Phase 4: Teaching Features
1. Enhance system prompt for teaching
2. Parse and display teaching notes separately
3. Add "explain" command for query breakdown
4. Track and display query history

### Phase 5: Excel Export (MVP)
1. Implement ResultExporter interface
2. Add EPPlus or ClosedXML NuGet package
3. Add "export" command to save last results to Excel
4. Format Excel output with headers, auto-fit columns
5. Support exporting with query metadata (SQL, timestamp)

### Phase 6: Polish (Post-MVP)
1. Better error handling
2. Result formatting/pagination
3. Save/load conversation sessions
4. Add ODBC executor
5. Add native executor
6. Configuration file support

## Testing Strategy

### Manual Testing Scenarios
1. **Schema Loading**: Point to directory with test Parquet files, verify schema extraction
2. **Simple Query**: "Show me all customers"
3. **Join Query**: "Get orders with customer names"
4. **Aggregation**: "Total sales by month"
5. **Teaching Moment**: Ask for something requiring a CTE
6. **Refinement**: Generate query, say "no", ask for modification
7. **Error Handling**: Invalid SQL, missing table, timeout

### Test Data
Create sample Parquet files:
- `customers.parquet`: 100 sample customers
- `orders.parquet`: 500 sample orders
- `order_items.parquet`: 2000 sample line items

Script to generate these using DuckDB:
```sql
CREATE TABLE customers AS SELECT ...;
COPY customers TO 'customers.parquet' (FORMAT PARQUET);
```

## Example Usage Session

```
$ mallard --schema-path ./test-data --db-path ./test.duckdb

Mallard - DuckDB SQL Assistant
===============================
Named after the fastest steam train - helping you query at record speed!

Loaded 3 tables: customers, orders, order_items
Type 'help' for commands or describe what you want to query.

> Show me total sales by customer for last month

Generating query...

Explanation:
I'll use a CTE to first aggregate order totals, then join with customers.
A CTE (Common Table Expression) is like a temporary named result set that
makes complex queries more readable.

SQL:
-- Get monthly sales totals per customer
WITH monthly_sales AS (
    SELECT 
        customer_id,
        SUM(total) as total_sales
    FROM orders
    WHERE order_date >= CURRENT_DATE - INTERVAL '1 month'
    GROUP BY customer_id
)
SELECT 
    c.name,
    c.email,
    ms.total_sales
FROM customers c
JOIN monthly_sales ms ON c.customer_id = ms.customer_id
ORDER BY ms.total_sales DESC;

Execute this query? (y/n/e for explain): y

Executing...

Results (15 rows):
┌─────────────────────┬──────────────────────────┬─────────────┐
│ name                │ email                    │ total_sales │
├─────────────────────┼──────────────────────────┼─────────────┤
│ Acme Corporation    │ contact@acme.com         │   15,847.50 │
│ Tech Solutions Ltd  │ info@techsolutions.co.uk │   12,340.00 │
│ ...                 │ ...                      │         ... │
└─────────────────────┴──────────────────────────┴─────────────┘

Executed in 0.23s

> export sales_report.xlsx

Exporting to Excel...
✓ Created: /home/user/sales_report.xlsx (2 sheets, 15 rows)

> 
```

## Error Handling

### Common Error Scenarios
1. **Missing DuckDB executable**: Clear message with installation instructions
2. **API key not set**: Clear message about environment variable
3. **Schema load failure**: Show which files failed and why
4. **Query timeout**: Allow user to increase timeout or cancel
5. **Invalid SQL**: Show DuckDB error message, ask Claude to fix
6. **API errors**: Retry logic for transient errors, clear messages for quota/auth issues

## Security Considerations

1. **SQL Injection**: Not applicable - user provides natural language, not SQL
2. **API Key Protection**: Load from environment, never log or display
3. **File Access**: Validate schema-path is a directory, exists, and is readable
4. **Query Limits**: Implement timeouts to prevent runaway queries

## Future Enhancements (Not MVP)

- Multi-line input support
- Syntax highlighting
- Integration with data visualization tools
- Query performance analysis
- Scheduled query execution
- Multiple database support
- Web UI version
- Export to other formats (CSV, JSON, PDF)

## Success Criteria for MVP

- [ ] Can load Parquet schemas from directory
- [ ] Can execute queries via DuckDB CLI
- [ ] Can generate SQL from natural language
- [ ] Shows explanations for new SQL concepts
- [ ] Allows iterative query refinement
- [ ] Handles basic error cases gracefully
- [ ] User can complete a full query session without crashes
- [ ] Can export query results to Excel with proper formatting
- [ ] Excel export includes both results and query metadata

## Notes for Claude Code

- **FIRST: Read `/mnt/skills/public/xlsx/SKILL.md` before implementing Excel export** - it contains essential best practices
- Check `/mnt/skills/` for any other relevant skills that might help
- Target .NET 6.0 or later
- Use async/await throughout
- Keep executor interface clean for easy swapping
- Add helpful comments for the teaching aspects
- Make sure error messages are user-friendly
- Consider the user is learning DuckDB features progressively
- The system should feel conversational, not just a SQL generator

## Open Questions to Resolve

1. Should we auto-execute simple SELECT queries or always ask?
2. How many rows to display by default? (suggestion: 10, with --more flag)
3. Should conversation history persist between sessions?
4. Do we need to handle very large schemas (100+ tables)?
5. Should we support ODBC connection strings or just file paths initially?

## Getting Started Checklist

- [ ] Create .NET console project
- [ ] Add NuGet packages
- [ ] Set up ANTHROPIC_API_KEY environment variable
- [ ] Create test Parquet files
- [ ] Verify duckdb CLI is in PATH
- [ ] Start with Phase 1 implementation
