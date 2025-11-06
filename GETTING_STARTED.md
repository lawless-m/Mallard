# Getting Started with Mallard Development

This guide will help you set up and start developing Mallard in VS Code on Windows.

## Prerequisites

### Required
- ✅ .NET 8.0 SDK - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ Visual Studio Code - [Download](https://code.visualstudio.com/)
- ✅ DuckDB CLI - [Download](https://duckdb.org/docs/installation/)
- ✅ Anthropic API Key - [Get one](https://console.anthropic.com/)

### Optional but Recommended
- DuckDB ODBC Driver (for Power Query testing)
- Git for Windows (for version control)

## First-Time Setup

### 1. Clone and Open Project

```powershell
# Clone the repository
git clone <your-repo-url>
cd Mallard

# Open in VS Code
code .
```

### 2. Install VS Code Extensions

VS Code will prompt you to install recommended extensions. Accept, or install manually:

**Essential:**
- **C# Dev Kit** (Microsoft) - Provides IntelliSense, debugging, testing
- **C#** (Microsoft) - Language support

**Recommended:**
- **NuGet Gallery** - Browse and install NuGet packages
- **.NET Core Test Explorer** - Run tests from VS Code
- **GitLens** - Enhanced git integration
- **Markdown All in One** - Edit documentation

### 3. Set Environment Variables

Create a `.env` file in the project root (already in .gitignore):

```env
ANTHROPIC_API_KEY=your_actual_key_here
```

Or set system-wide:
```powershell
# PowerShell
$env:ANTHROPIC_API_KEY = "your_actual_key_here"

# To persist:
[System.Environment]::SetEnvironmentVariable('ANTHROPIC_API_KEY', 'your_key', 'User')
```

### 4. Restore and Build

```powershell
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Should see: Build succeeded. 0 Warning(s) 0 Error(s)
```

## Running Mallard

### From VS Code Terminal

```powershell
# Basic run (default paths)
dotnet run

# With custom paths
dotnet run -- --schema-path "C:\Data\Parquets" --db-path "C:\Data\queries.duckdb"

# With all options
dotnet run -- `
  --schema-path "C:\Data\Parquets" `
  --db-path "C:\Data\queries.duckdb" `
  --duckdb-exe "C:\Tools\duckdb.exe"
```

### Using VS Code Debug/Run

1. Press `F5` or click **Run > Start Debugging**
2. VS Code will create `.vscode/launch.json` automatically
3. Modify `args` in launch.json for different scenarios

Example `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Mallard - Default",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/net8.0/Mallard.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "console": "integratedTerminal",
      "stopAtEntry": false
    },
    {
      "name": "Mallard - Custom Paths",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/net8.0/Mallard.dll",
      "args": [
        "--schema-path", "C:\\Data\\Parquets",
        "--db-path", "C:\\Data\\queries.duckdb"
      ],
      "cwd": "${workspaceFolder}",
      "console": "integratedTerminal",
      "stopAtEntry": false
    }
  ]
}
```

## Testing Your First Query

### 1. Prepare Test Data

Create a simple test Parquet file:

```sql
-- Using DuckDB CLI
duckdb test.duckdb

-- Create sample data
CREATE TABLE customers AS
SELECT
  row_number() OVER () as customer_id,
  'Customer ' || row_number() OVER () as name,
  'customer' || row_number() OVER () || '@example.com' as email
FROM range(100);

-- Export to Parquet
COPY customers TO 'customers.parquet' (FORMAT PARQUET);

.quit
```

### 2. Create Test Directory Structure

```
Mallard/
├── test-data/
│   └── customers.parquet
└── test.duckdb
```

### 3. Run Mallard

```powershell
dotnet run -- --schema-path "./test-data" --db-path "./test.duckdb"
```

### 4. Try These Queries

```
> schema
# Shows your loaded tables

> Show me all customers

> Show me the first 10 customers ordered by name

> export test_results.xlsx
# Creates 3 files: .xlsx, .sql, .m
```

## Debugging Tips

### Setting Breakpoints

1. Click in the gutter (left of line numbers) to set breakpoints
2. Press `F5` to start debugging
3. Use Debug Console to inspect variables

**Useful places to set breakpoints:**
- `ClaudeApiClient.cs:45` - Before API call
- `DuckDbCliExecutor.cs:38` - Before executing SQL
- `SqlAssistant.cs:135` - When handling user input
- `ExcelExporter.cs:23` - Before creating Excel file

### Viewing Variables

- **Watch Window**: Add expressions to monitor
- **Locals**: See all local variables in current scope
- **Call Stack**: Trace execution path

### Common Issues

**Issue**: "ANTHROPIC_API_KEY not set"
- **Solution**: Set environment variable (see step 3 above)
- Restart VS Code after setting system variables

**Issue**: "Could not connect to DuckDB"
- **Solution**: Verify DuckDB path with `where duckdb` in PowerShell
- Or specify path: `--duckdb-exe "C:\path\to\duckdb.exe"`

**Issue**: "No Parquet files found"
- **Solution**: Check `--schema-path` points to directory with .parquet files
- Use absolute paths to avoid confusion

## Development Workflow

### 1. Making Changes

```powershell
# Make code changes in VS Code

# Build to check for errors
dotnet build

# Run to test
dotnet run

# Or use F5 to debug
```

### 2. Adding New Features

Example: Adding a new special command

1. **Edit SqlAssistant.cs**
   ```csharp
   // In HandleSpecialCommands method
   if (command == "mycommand")
   {
       await MyNewFeature();
       return true;
   }
   ```

2. **Add the method**
   ```csharp
   private async Task MyNewFeature()
   {
       Console.WriteLine("My new feature!");
       await Task.CompletedTask;
   }
   ```

3. **Update help**
   ```csharp
   // In ShowHelp method
   Console.WriteLine("  mycommand - Description of my command");
   ```

4. **Test it**
   ```powershell
   dotnet run
   > mycommand
   ```

### 3. Testing Excel Export

After running a query:
```
> export test_output.xlsx
```

You'll get 3 files:
- `test_output.xlsx` - Open in Excel
- `test_output.sql` - Open in any text editor
- `test_output.m` - Import into Power Query

**Test Power Query:**
1. Open Excel
2. Data > Get Data > Launch Power Query Editor
3. Home > New Source > Blank Query
4. View > Advanced Editor
5. Paste contents of `test_output.m`
6. Click Done
7. Click Close & Load

## Project Structure Reference

```
Mallard/
├── Program.cs              ← Entry point, command-line args
├── SqlAssistant.cs         ← Main conversation loop, commands
├── ClaudeApiClient.cs      ← Anthropic API calls
├── SchemaLoader.cs         ← Load Parquet metadata
├── ExcelExporter.cs        ← Excel + Power Query export
├── IQueryExecutor.cs       ← Executor interface
├── Executors/
│   └── DuckDbCliExecutor.cs ← DuckDB CLI execution
├── Models/
│   ├── QueryResult.cs       ← Query results data
│   ├── TableSchema.cs       ← Schema metadata
│   └── ConversationContext.cs ← Conversation state
├── Mallard.csproj          ← Project configuration
├── .gitignore              ← Git ignore rules
├── README.md               ← User documentation
├── GETTING_STARTED.md      ← This file!
└── mallard.md              ← Original implementation plan
```

## Next Steps for Development

### Immediate Testing

- [ ] Test with your real Parquet files
- [ ] Try complex queries that need CTEs/window functions
- [ ] Export to Excel and test Power Query refresh
- [ ] Compare performance vs your CGI/JSON approach
- [ ] Test with colleagues - can they use it?

### Phase 6 Enhancements (Prioritize Based on Usage)

**High Priority** (if needed after testing):
1. **Better error handling** - More helpful error messages
2. **Result pagination** - Handle large result sets better
3. **Query history persistence** - Save/load previous sessions

**Medium Priority** (team collaboration):
4. **SCP upload** - Auto-upload .m/.sql to web server
5. **Query library UI** - Browse shared queries
6. **ODBC executor** - Alternative to CLI for better performance

**Low Priority** (polish):
7. **Configuration file** - appsettings.json support
8. **Multi-line input** - Better query editing
9. **Syntax highlighting** - Pretty SQL display

### Adding SCP Upload (Example Enhancement)

If you want to auto-upload .m files to your web server:

1. **Add SSH.NET NuGet package**
   ```powershell
   dotnet add package SSH.NET
   ```

2. **Create ScpUploader.cs**
   ```csharp
   public class ScpUploader
   {
       public async Task UploadAsync(string localFile, string remoteUrl)
       {
           // Implementation using Renci.SshNet
       }
   }
   ```

3. **Integrate in SqlAssistant.cs**
   ```csharp
   // After export
   if (autoUpload)
   {
       await _scpUploader.UploadAsync(mFile, "server:/path/queries/");
   }
   ```

## Useful Commands

```powershell
# Build
dotnet build

# Run with arguments
dotnet run -- --help

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore

# Create release build
dotnet build -c Release

# Publish standalone executable
dotnet publish -c Release -r win-x64 --self-contained

# Check for package updates
dotnet list package --outdated

# Format code
dotnet format
```

## VS Code Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Build | `Ctrl+Shift+B` |
| Debug | `F5` |
| Run without debug | `Ctrl+F5` |
| Toggle breakpoint | `F9` |
| Step over | `F10` |
| Step into | `F11` |
| Continue | `F5` |
| Go to definition | `F12` |
| Find references | `Shift+F12` |
| Rename symbol | `F2` |
| Format document | `Shift+Alt+F` |

## Getting Help

- **Documentation**: See README.md
- **Original Plan**: See mallard.md
- **Issues**: Track in GitHub issues
- **API Reference**:
  - [Anthropic API Docs](https://docs.anthropic.com/)
  - [DuckDB Docs](https://duckdb.org/docs/)
  - [EPPlus Docs](https://epplussoftware.com/docs/)

## Tips for Success

1. **Start Small**: Test with simple queries first
2. **Real Data**: Use your actual Parquet files early
3. **Iterate**: Don't over-engineer - see what's actually needed
4. **Document**: Add comments as you learn what works
5. **Share**: Get feedback from colleagues who will use it

Happy coding! 🚀
