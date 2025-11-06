# Claude Code CLI - Continuation Guide

This document helps you (or Claude) pick up where we left off when continuing development in VS Code.

## Session Summary - What We Built

**Date**: 2025-11-05
**Session Duration**: Full implementation from scratch to complete MVP
**Status**: ✅ All phases complete, ready for testing

### Completed Implementation

1. **Core Application** (Phases 1-4)
   - Natural language to SQL using Anthropic API
   - DuckDB CLI executor with CSV parsing
   - Parquet schema loading via DuckDB DESCRIBE
   - Interactive conversation loop with teaching mode
   - Special commands (help, schema, history, explain, export, clear)

2. **Excel Export** (Phase 5)
   - EPPlus-based export with formatting
   - Two-sheet format: Results + Query Info
   - Smart data type detection
   - Auto-fit columns with max width

3. **Power Query Integration** (Enhancement)
   - Three-file export: .xlsx, .sql, .m
   - External SQL file for runtime loading
   - Power Query M code for Excel import
   - Instructions sheet in Excel
   - Designed for intranet query library sharing

### Key Decisions Made

1. **Target Framework**: .NET 8.0 (not .NET 6)
   - Reason: Better performance, still widely supported
   - `ImplicitUsings=disable` to avoid conflicts with explicit Main

2. **DuckDB Integration**: CLI executor first (ODBC/native deferred)
   - Default path: `Y:\Data Warehouse\duckdb\duckdb.exe`
   - 60-second timeout for long queries
   - CSV output parsing for structured results

3. **Power Query Design**: External SQL files, not embedded
   - Reason: User wants to edit SQL separately and host on web server
   - M code loads SQL from .sql file at runtime
   - Enables shared query library on intranet

4. **No PgQuery Integration**: Different use case (ad-hoc PostgreSQL queries)

### Files Created (12 total)

```
✅ Program.cs              - Entry point, CLI args
✅ SqlAssistant.cs         - Conversation loop
✅ ClaudeApiClient.cs      - Anthropic API
✅ SchemaLoader.cs         - Parquet metadata
✅ ExcelExporter.cs        - Excel + Power Query
✅ IQueryExecutor.cs       - Executor interface
✅ Executors/DuckDbCliExecutor.cs
✅ Models/QueryResult.cs
✅ Models/TableSchema.cs
✅ Models/ConversationContext.cs
✅ Mallard.csproj
✅ README.md
✅ GETTING_STARTED.md
✅ .gitignore
```

## What's NOT Done Yet (Phase 6 Ideas)

These came up during discussion but haven't been implemented:

1. **SCP Upload to Web Server**
   - Auto-upload .m and .sql files to intranet on export
   - Would enable centralized query library
   - Needs SSH.NET NuGet package

2. **ODBC Executor**
   - Alternative to CLI for better performance
   - Connection string: `DSN=DuckDB` as default
   - Referenced in skills but not implemented

3. **Query Library Browser**
   - List/search previously exported queries
   - Load .m files from web server
   - Team collaboration features

4. **Configuration File**
   - appsettings.json for defaults
   - Mentioned in plan but deferred

## Next Steps for Tomorrow

### First Thing: Test It!

**What to do:**
```powershell
# 1. Set API key
$env:ANTHROPIC_API_KEY = "your_key"

# 2. Create test data (if needed)
duckdb test.duckdb
CREATE TABLE customers AS SELECT * FROM range(100);
COPY customers TO 'test-data/customers.parquet' (FORMAT PARQUET);

# 3. Run Mallard
dotnet run -- --schema-path "./test-data" --db-path "./test.duckdb"

# 4. Try commands
> schema
> Show me all customers
> export test.xlsx

# 5. Test Power Query in Excel
# Open test.m in Excel's Power Query Editor
```

### Questions to Ask Claude Code

When you resume in VS Code tomorrow:

**For Testing:**
- "Help me create more realistic test data with multiple Parquet files"
- "The query failed with [error], what's wrong?"
- "How do I debug the CSV parsing in DuckDbCliExecutor?"

**For Enhancements:**
- "Add SCP upload feature to auto-publish .m files to my web server"
- "Implement the ODBC executor using DSN=DuckDB"
- "Add a command to browse previously exported queries"
- "Help me add better error messages when [scenario]"

**For Understanding:**
- "Explain how the Power Query M code loads the external SQL file"
- "Walk me through what happens when I type 'export results.xlsx'"
- "Show me how to modify the Excel formatting"

### If Something Breaks

**Build errors:**
```
Ask: "The build is failing with [error message], how do I fix it?"
```

**Runtime errors:**
```
Ask: "When I run [command], I get [error]. Here's the stack trace: [paste]"
```

**API issues:**
```
Ask: "Claude API is returning [error], is it my prompt or configuration?"
```

## Important Context for Claude Code

### User's Environment

- **Platform**: Windows (paths like `Y:\`, `C:\`)
- **Use Case**: Querying Parquet extracts from databases
- **Current System**: CGI program that returns JSON (has latency issues)
- **Goal**: Replace with faster ODBC/Power Query approach
- **Team Setup**: Want to share queries via intranet web server

### User's Expertise

- SQL: ✅ Strong (joins, WHERE IN)
- C#: ✅ Experienced (has skills for Parquet, Databases, etc.)
- DuckDB: ✅ Familiar (already using with ODBC)
- Power Query: ✅ Knows it (asked specifically for this integration)
- .NET: ✅ Experienced (has .NET 8+ projects)

### Design Preferences

- **Practical over perfect**: MVP first, polish later
- **Windows-first**: But keep cross-platform in mind
- **Real-world focus**: Wants to use with actual data, not toy examples
- **Team collaboration**: Query sharing is important
- **Performance matters**: That's why moving from CGI to ODBC

## Skills Available in Repository

The `.claude/skills/` directory has project-specific knowledge:

- **Parquet Files** - How to work with Parquet.Net library
- **Databases** - DuckDB, PostgreSQL, MySQL connection patterns
- **Dotnet 8 to 9** - Migration guide (used for .NET 8 setup)
- **Logging** - UTF-8 file logging patterns
- **CSharpener** - C# static analysis tools
- **Elasticsearch** - ES 5.2 operations (may not be relevant)

**When to reference:**
```
Ask: "Check the Parquet Files skill - does it have guidance on [topic]?"
Ask: "According to the Databases skill, what's the best way to [scenario]?"
```

## Code Patterns to Follow

Based on skills and existing implementation:

### 1. ODBC Connection Pattern
```csharp
// From Databases skill
using var connection = new OdbcConnection("DSN=DuckDB");
connection.Open();
var command = new OdbcCommand(sql, connection);
command.CommandTimeout = 60; // Important for DuckDB!
```

### 2. Parquet Schema Loading
```csharp
// Current approach using DuckDB
var sql = $"DESCRIBE SELECT * FROM '{parquetFile}' LIMIT 1";
// Parquet.Net library is available if needed for direct reading
```

### 3. Error Handling
```csharp
// Be specific and helpful
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("Try: [helpful suggestion]");
}
```

## Known Issues / Gotchas

1. **CSV Parsing**: DuckDB CLI returns CSV with quoted fields
   - Current parser handles escaped quotes
   - May need refinement for edge cases

2. **Path Separators**: Windows backslashes need conversion for DuckDB
   - Already handled in SchemaLoader: `filePath.Replace("\\", "/")`

3. **EPPlus License**: Set to NonCommercial in ExcelExporter
   - May need commercial license for company use
   - Alternative: ClosedXML

4. **Power Query File Paths**: M code has absolute paths to .sql files
   - Works for local use
   - May need UNC paths for network sharing

## Prompts for Common Tasks

### Adding a New Special Command

```
"Add a new command called 'save' that saves the conversation history to a JSON file.
The command should:
1. Prompt for filename
2. Serialize _context.History to JSON
3. Save to file
4. Confirm success"
```

### Implementing SCP Upload

```
"Implement SCP upload for exported files. When export is successful:
1. Check if environment variable SCP_SERVER is set
2. If set, upload .m and .sql files via SCP
3. Use SSH.NET library
4. Connection details: SCP_SERVER, SCP_USER, SCP_PATH
5. Show upload progress
6. Handle connection failures gracefully"
```

### Adding ODBC Executor

```
"Implement DuckDbOdbcExecutor.cs similar to DuckDbCliExecutor:
1. Use DSN=DuckDB as default connection string
2. Execute queries via ODBC command
3. Read results using OdbcDataReader
4. Map to QueryResult model
5. Handle errors with helpful messages
6. Keep same 60-second timeout"
```

## Testing Checklist

Before considering it "done":

- [ ] Runs on Windows with real Parquet files
- [ ] Claude generates useful SQL for real queries
- [ ] Teaching notes are actually helpful
- [ ] Export creates valid .xlsx files
- [ ] .m file imports successfully into Excel
- [ ] Power Query refresh works in Excel
- [ ] Faster than existing CGI/JSON approach
- [ ] Colleagues can use it without help
- [ ] Error messages are clear and actionable

## Performance Comparison Script

To test vs CGI approach:

```powershell
# Time Mallard export
Measure-Command {
  # Run query and export
  dotnet run -- --schema-path "..." --db-path "..."
  # Then: > [query]
  # Then: > export test.xlsx
}

# Time CGI approach
Measure-Command {
  curl "http://your-server/cgi-bin/query.cgi?report=sales"
}

# Compare refresh times in Excel
# Power Query refresh vs re-running CGI
```

## Git Workflow

Current branch: `claude/read-mallard-md-011CUqBUnbDMD7Q6Wr4GVnQ2`

**All changes committed and pushed:**
- ✅ Initial implementation
- ✅ .gitignore for build artifacts
- ✅ Excel export (Phase 5)
- ✅ Power Query integration

**Next commits might be:**
- Bug fixes from testing
- SCP upload feature
- ODBC executor
- Configuration file support

## Contact/Reference

- **Repository**: (Your GitHub URL)
- **Claude Code Version**: Sonnet 4.5
- **Session Date**: 2025-11-05
- **Skills Used**: Parquet Files, Databases, Dotnet 8 to 9

---

## Quick Start Tomorrow

```powershell
# 1. Open in VS Code
cd Mallard
code .

# 2. Restore/build
dotnet restore
dotnet build

# 3. Run with test data
dotnet run -- --schema-path "./test-data" --db-path "./test.duckdb"

# 4. In Mallard prompt:
> help
> schema
> Show me all customers
> export results.xlsx

# 5. In Excel:
# - Open results.xlsx
# - Data > Get Data > Launch Power Query Editor
# - Home > New Source > Blank Query
# - Advanced Editor > paste results.m
# - Done > Close & Load
# - Test Refresh!
```

**Then ask Claude:** "I've tested Mallard with real data. Here's what happened: [describe]. What should we improve first?"

Good luck! 🚀
