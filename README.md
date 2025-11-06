# Mallard - DuckDB SQL Assistant

An intelligent SQL assistant for DuckDB that helps users write queries through natural language. Named after the Mallard steam locomotive - the fastest steam train ever, just as this tool helps you query faster than ever before.

## Features

- 🤖 **Natural Language to SQL**: Describe what you want in plain English, get DuckDB SQL
- 📚 **Teaching Mode**: Learn new SQL concepts (CTEs, window functions) as you use them
- 🔄 **Iterative Refinement**: Refine queries through conversation
- 📊 **Schema Awareness**: Automatically loads table schemas from Parquet files
- 💬 **Conversation History**: Maintains context across multiple queries
- 🎯 **Smart Explanations**: Get detailed explanations of complex queries

## Prerequisites

- .NET 8.0 SDK or later
- DuckDB CLI (https://duckdb.org/docs/installation/)
- Anthropic API key (https://console.anthropic.com/)

## Installation

1. Clone this repository
2. Set your Anthropic API key:
   ```bash
   export ANTHROPIC_API_KEY=your_key_here
   ```
3. Build the project:
   ```bash
   dotnet build
   ```

## Usage

### Basic Usage

```bash
dotnet run -- --schema-path ./test-data --db-path ./test.duckdb
```

### Command Line Options

- `--schema-path`, `-s`: Path to directory containing Parquet files (default: `./schemas`)
- `--db-path`, `-d`: Path to DuckDB database file (default: `./data.duckdb`)
- `--executor`, `-e`: Executor type: `cli`, `odbc`, `native` (default: `cli`)
- `--duckdb-exe`: Path to duckdb executable (default: `duckdb` from PATH or `Y:\Data Warehouse\duckdb\duckdb.exe`)

### Interactive Commands

Once running, you can use these commands:

- `help` - Show available commands
- `schema` - Display available tables and columns
- `history` - Show query history
- `clear` - Clear conversation history
- `explain [query]` - Get detailed explanation of a query
- `export [filename]` - Export last query results to Excel (.xlsx)
- `exit` / `quit` - Exit the program

### Example Session

```
> Show me all customers

Generating query...

Explanation:
I'll select all records from the customers table.

SQL:
SELECT * FROM customers;

Execute this query? (y/n/e for explain): y

Executing...

Results (100 rows):
customer_id  name                  email                    created_date
-----------  --------------------  -----------------------  ------------
1            Acme Corporation      contact@acme.com         2023-01-15
2            Tech Solutions Ltd    info@techsolutions.com   2023-01-16
...

Executed in 45.23ms

> Show me total sales by customer for last month

Generating query...

Explanation:
I'll use a CTE to first aggregate order totals, then join with customers.
A CTE (Common Table Expression) is like a temporary named result set that
makes complex queries more readable.

💡 Teaching Note:
CTEs are defined using the WITH keyword and allow you to break down complex
queries into logical steps. Think of them as temporary views that exist only
for the duration of your query.

SQL:
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

> export sales_report.xlsx

Exporting to Excel with Power Query support...

✓ Created 3 files:
  📊 sales_report.xlsx
     - Results sheet: 15 rows, 3 columns
     - Query Info sheet: metadata and SQL
     - Power Query Setup sheet: instructions for refreshable connection
  📝 sales_report.sql - SQL query for editing/sharing
  🔄 sales_report.m - Power Query M code (import to Excel)

💡 Tip: Import the .m file in Excel for a refreshable connection to DuckDB!
```

## Power Query Integration

Mallard exports not just static Excel files, but **refreshable Power Query connections** that stay synchronized with your DuckDB database!

### What Gets Exported

When you export query results, Mallard creates **3 files**:

1. **results.xlsx** - Excel workbook with 3 sheets:
   - **Results**: Your query data with formatting
   - **Query Info**: Metadata (SQL, execution time, row count)
   - **Power Query Setup**: Step-by-step instructions

2. **results.sql** - The SQL query as a separate file
   - Edit the SQL without opening Excel
   - Version control friendly
   - Share across multiple workbooks

3. **results.m** - Power Query M code for direct import
   - Loads SQL from external .sql file at runtime
   - Connects to DuckDB via ODBC (DSN=DuckDB)
   - Import directly into Excel's Power Query Editor

### How to Use Power Query

**Option 1: Import the .m file (Easiest)**
1. Open Excel
2. Data > Get Data > Launch Power Query Editor
3. Home > New Source > Blank Query
4. Click Advanced Editor
5. Paste contents of the .m file
6. Click Done, then Close & Load
7. Click Refresh anytime to update data from DuckDB!

**Option 2: Manual ODBC setup**
1. Data > Get Data > From Other Sources > From ODBC
2. Select "DuckDB" DSN
3. Click Advanced options
4. Paste the SQL from results.sql
5. Click OK > Load

### Benefits

- **Live data**: Click Refresh in Excel to get latest data from DuckDB
- **No Mallard needed**: Users can refresh data without running Mallard
- **Edit queries**: Modify the .sql file and refresh Excel
- **Share queries**: Host .m files on your web server for team access
- **Schedule refreshes**: Use Excel's scheduled refresh features

### Intranet Deployment

If you host .m files on your intranet web server:
- Users can load queries directly from the web
- Centrally manage and update queries
- Everyone gets the latest query definitions
- Version control query logic separately from Excel

## Architecture

The project follows a modular architecture with clear separation of concerns:

- **Program.cs**: Entry point and configuration
- **SqlAssistant.cs**: Main conversation loop
- **ClaudeApiClient.cs**: Anthropic API integration
- **IQueryExecutor.cs**: Abstract interface for query execution
- **Executors/DuckDbCliExecutor.cs**: CLI-based query execution
- **SchemaLoader.cs**: Parquet metadata extraction
- **ExcelExporter.cs**: Excel export with EPPlus (two-sheet format)
- **Models/**: Data models (QueryResult, TableSchema, ConversationContext)

## Implementation Status

### ✅ Phase 1-5 Complete (Full MVP + Power Query)
- [x] Basic project structure
- [x] DuckDB CLI executor
- [x] Claude API integration
- [x] Schema awareness
- [x] Conversation loop
- [x] Teaching features
- [x] Special commands
- [x] Excel export with EPPlus
- [x] Three-sheet format (Results + Query Info + Power Query Setup)
- [x] Formatted output with auto-fit columns
- [x] Metadata tracking (SQL, timestamp, execution time)
- [x] **Power Query integration** - Refreshable Excel connections
- [x] External .sql file export for query editing
- [x] Power Query .m file export for direct import
- [x] Runtime SQL loading from external files

### 📋 Phase 6: Polish (Future)
- [ ] Better error handling
- [ ] Result pagination
- [ ] Save/load conversation sessions
- [ ] ODBC executor
- [ ] Native executor
- [ ] Configuration file support
- [ ] SCP upload: Automatically upload .m and .sql files to intranet web server
- [ ] Web server integration: Publish queries to centralized repository
- [ ] Query library: Browse and load previously saved queries

## Target User Profile

Mallard is designed for users who:
- Understand basic SQL (joins, WHERE clauses)
- Are new to CTEs, subqueries, or window functions
- Work with daily Parquet extracts of database tables
- Want to learn DuckDB features while being productive

## Design Principles

1. **Swappable execution layer** - Easy to switch between CLI, ODBC, or native DuckDB connection
2. **Teaching mode** - Explain new SQL concepts as they're used
3. **Iterative refinement** - Maintain conversation context for query improvements
4. **Schema awareness** - Know all tables and columns from Parquet metadata

## Contributing

This is an MVP implementation following the plan in `mallard.md`. Future enhancements welcome!

## License

(Add your license here)

## Acknowledgments

Named after the LNER Class A4 4468 Mallard, which set the world speed record for steam locomotives at 126 mph (203 km/h) in 1938.
