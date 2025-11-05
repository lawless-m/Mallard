# Mallard TODO List

## Immediate (First Session in VS Code on Windows)

- [ ] **Test with real data**
  - Set ANTHROPIC_API_KEY environment variable
  - Point to actual Parquet files from your database extracts
  - Run several realistic queries
  - Export to Excel and verify all 3 files (.xlsx, .sql, .m)
  - Import .m file into Excel Power Query
  - Test refresh functionality
  - Compare performance vs existing CGI/JSON approach

- [ ] **Verify Power Query workflow**
  - Does .m file load successfully?
  - Does it find the .sql file correctly?
  - Does ODBC connection to DSN=DuckDB work?
  - Can non-technical users follow the instructions?
  - Is refresh faster than CGI approach?

- [ ] **Test edge cases**
  - Very large result sets (10k+ rows)
  - Wide tables (50+ columns)
  - Special characters in data (quotes, commas, newlines)
  - Long-running queries (>30 seconds)
  - Parquet files with unusual types

- [ ] **Document any issues found**
  - CSV parsing problems?
  - Power Query connection issues?
  - Claude API response quality?
  - Performance bottlenecks?

## High Priority (If Testing Reveals Need)

- [ ] **Better error handling**
  - More descriptive error messages
  - Suggestions for fixing common issues
  - Validate inputs before execution
  - Graceful handling of API rate limits

- [ ] **Result pagination**
  - Display only first N rows
  - Add "show more" command
  - Warning for very large result sets
  - Configurable display limit

- [ ] **SCP Upload Integration** (for shared query library)
  - Add SSH.NET NuGet package
  - Create ScpUploader class
  - Read config from environment variables:
    - SCP_SERVER
    - SCP_USER
    - SCP_PATH (remote directory for .m/.sql files)
    - SCP_KEY (optional: path to private key)
  - Auto-upload .m and .sql on export
  - Optional: Upload .xlsx too
  - Show upload progress/confirmation
  - Handle connection failures gracefully

## Medium Priority (Based on Usage Feedback)

- [ ] **ODBC Executor** (alternative to CLI)
  - Create DuckDbOdbcExecutor.cs
  - Use DSN=DuckDB as default
  - Better performance than CLI for large results
  - Proper data type handling (not CSV parsing)
  - Add to executor selection (--executor odbc)

- [ ] **Query library features**
  - List previously exported queries
  - Browse queries on web server
  - Load .m files from URL
  - Search/filter query catalog
  - Query versioning/history

- [ ] **Conversation persistence**
  - Save conversation to file (JSON)
  - Load previous conversations
  - Resume query sessions
  - Export conversation history

- [ ] **Configuration file**
  - appsettings.json support
  - Default paths for schema/database
  - API settings (model, temperature, etc.)
  - Export settings (auto-upload, paths)
  - Per-user or per-project configs

## Low Priority (Polish/Nice-to-Have)

- [ ] **Multi-line input**
  - Support pressing Enter to add new lines
  - Submit with Ctrl+Enter or special command
  - Better for complex natural language requests

- [ ] **Syntax highlighting**
  - Colorize SQL output in terminal
  - Use ANSI color codes
  - Make SQL more readable

- [ ] **Query templates**
  - Pre-made query templates
  - Variable substitution
  - Common patterns (aggregation, joining, etc.)

- [ ] **Export formats**
  - CSV export
  - JSON export
  - Parquet export (results back to Parquet)
  - HTML report generation

- [ ] **Schema cache**
  - Cache loaded schemas to disk
  - Skip re-scanning Parquet files if unchanged
  - Faster startup for large schema sets

- [ ] **DuckDB database creation**
  - Auto-create .duckdb file if it doesn't exist
  - Helpful for first-time users
  - Load Parquet files into database tables

- [ ] **Batch query execution**
  - Run multiple queries from a file
  - Export all results
  - Scheduled/automated queries

## Ideas from User Discussion

- [ ] **Web server integration**
  - Publish queries to centralized repository
  - Team can browse/import shared queries
  - Version control for query definitions
  - Access control (who can publish/use)

- [ ] **Hybrid approach**
  - Support both Power Query (for Excel) and JSON API (for web)
  - Generate both .m and API endpoint info
  - Use CGI for web displays, ODBC for Excel (best of both worlds)

- [ ] **Query library UI**
  - Simple web interface to browse queries
  - Show query metadata (who created, when, description)
  - Download .m files directly
  - Star/favorite queries

## Future: Web Service Version

**See WEB_SERVICE_PLAN.md for comprehensive implementation plan**

Transform Mallard from CLI to full web application:
- Server executes all queries (security, centralized)
- Browser provides UI only (no DB access, no API keys)
- Multi-user with authentication
- Real-time query execution via SignalR
- Shared conversations and query library
- Scheduled queries with email
- Mobile-responsive design
- 80%+ code reuse from current CLI version

**Estimated effort**: 6-7 weeks, 1 developer
**Tech stack**: ASP.NET Core 8.0 + Blazor Server
**Deployment**: IIS on Windows Server

This would replace the CGI/JSON approach with better:
- Security (server-side only)
- Performance (ODBC vs HTTP+JSON)
- Collaboration (shared queries)
- User experience (real-time updates)

## Won't Do (Decided Against)

- ❌ PgQuery integration - Different use case (PostgreSQL ad-hoc queries)
- ❌ Jupyter notebook integration - Out of scope
- ❌ Real-time query monitoring - Too complex for MVP
- ❌ Multi-database support - DuckDB only for now

## Questions to Resolve

- [ ] EPPlus license: NonCommercial setting OK for company use?
  - Alternative: Switch to ClosedXML?

- [ ] Power Query: UNC paths vs local paths for shared network drives?
  - Test both approaches

- [ ] CSV parsing: Are there edge cases we're missing?
  - More robust parsing needed?

- [ ] Performance: Is CLI fast enough or should we prioritize ODBC executor?
  - Measure query times

- [ ] Teaching mode: Are explanations helpful or too verbose?
  - Get user feedback

## Completed ✅

- [x] Project structure and .csproj
- [x] IQueryExecutor interface
- [x] DuckDB CLI executor with CSV parsing
- [x] Claude API client with conversation context
- [x] Schema loader for Parquet files
- [x] Main conversation loop (SqlAssistant)
- [x] Special commands (help, schema, history, explain, export, clear)
- [x] Excel export with EPPlus
- [x] Query results sheet with formatting
- [x] Query info metadata sheet
- [x] Power Query integration
- [x] External .sql file export
- [x] Power Query .m file export
- [x] Power Query Setup instruction sheet
- [x] .gitignore for .NET artifacts
- [x] Comprehensive README
- [x] Getting Started guide for VS Code
- [x] Continuation guide for Claude Code CLI
- [x] All code committed and pushed
- [x] Build verification (0 warnings, 0 errors)

## Notes

- **Target Framework**: .NET 8.0
- **Windows Path**: `Y:\Data Warehouse\duckdb\duckdb.exe`
- **ODBC DSN**: `DSN=DuckDB`
- **Query Timeout**: 60 seconds
- **License**: EPPlus NonCommercial (review if needed)

Last Updated: 2025-11-05
