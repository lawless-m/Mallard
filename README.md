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
- `export [filename]` - Export last query results to Excel (coming in Phase 5)
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

Execute this query? (y/n/e for explain):
```

## Architecture

The project follows a modular architecture with clear separation of concerns:

- **Program.cs**: Entry point and configuration
- **SqlAssistant.cs**: Main conversation loop
- **ClaudeApiClient.cs**: Anthropic API integration
- **IQueryExecutor.cs**: Abstract interface for query execution
- **Executors/DuckDbCliExecutor.cs**: CLI-based query execution
- **SchemaLoader.cs**: Parquet metadata extraction
- **Models/**: Data models (QueryResult, TableSchema, ConversationContext)

## Implementation Status

### ✅ Phase 1-4 Complete (MVP Core)
- [x] Basic project structure
- [x] DuckDB CLI executor
- [x] Claude API integration
- [x] Schema awareness
- [x] Conversation loop
- [x] Teaching features
- [x] Special commands

### 🚧 Phase 5: Excel Export (Pending)
- [ ] ExcelExporter implementation
- [ ] Export to .xlsx with formatting
- [ ] Metadata sheet with query info

### 📋 Phase 6: Polish (Future)
- [ ] Better error handling
- [ ] Result pagination
- [ ] Save/load conversation sessions
- [ ] ODBC executor
- [ ] Native executor
- [ ] Configuration file support

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
