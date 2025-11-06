# Mallard Web Service - Implementation Plan

**Transformation**: CLI Application → Web Service with Browser UI

**Hosting Model**: Server executes all queries, browser provides only UI

## Executive Summary

Convert Mallard from a command-line tool to a web-based service where:
- **Server**: Executes DuckDB queries, calls Claude API, manages sessions
- **Browser**: Pure UI - no direct database access, no API keys, no local execution
- **Benefits**: Multi-user, centralized management, accessible anywhere, better collaboration

## Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Browser (UI Only)                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Chat    │  │  Schema  │  │  Results │  │  Export  │   │
│  │  Panel   │  │  Browser │  │  Grid    │  │  Manager │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
│                         ↓ HTTP/WebSocket ↓                  │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    ASP.NET Core Web API                     │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              API Controllers                          │  │
│  │  /api/chat | /api/query | /api/export | /api/schema  │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              Services Layer                           │  │
│  │  ConversationManager | QueryExecutor | ExcelService  │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              Core Components (Reused)                 │  │
│  │  ClaudeApiClient | DuckDbExecutor | SchemaLoader     │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    External Resources                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ DuckDB   │  │ Parquet  │  │ Claude   │  │  Export  │   │
│  │ Database │  │  Files   │  │   API    │  │  Files   │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

**Browser (Frontend)**:
- Display chat interface
- Show schema tree
- Render query results in grid
- Handle file downloads
- Manage UI state only
- No business logic
- No API keys or credentials

**Web Server (Backend)**:
- Execute all DuckDB queries
- Call Claude API with server-side key
- Manage user sessions and conversations
- Generate exports (Excel, .m files)
- Authenticate users
- Authorize access to data
- Store conversation history
- Cache schemas

**DuckDB/Parquet**:
- Query execution
- Schema metadata
- Data storage

**Claude API**:
- Natural language → SQL
- Teaching explanations
- Query optimization

## Technology Stack

### Backend: ASP.NET Core 8.0 Web API

**Why ASP.NET Core:**
- ✅ Already using .NET 8.0 for Mallard
- ✅ Reuse existing code (80%+ can be reused)
- ✅ High performance
- ✅ Built-in authentication/authorization
- ✅ WebSocket support (SignalR)
- ✅ Easy deployment to Windows Server/IIS

**NuGet Packages (Additional)**:
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="8.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.*" />
```

### Frontend Options

#### Option 1: Blazor Server (Recommended for Internal Use)
**Pros:**
- Pure C# - no JavaScript needed
- Reuse .NET models
- Built-in SignalR
- Minimal latency on intranet
- Easy authentication integration
- No REST API needed (direct C# calls)

**Cons:**
- Stateful server connections
- Not great for high concurrent users (>100)
- Requires constant connection

**Best for:** Internal intranet deployment, <50 concurrent users

#### Option 2: Blazor WebAssembly + ASP.NET Core API
**Pros:**
- Still C# for frontend
- Client-side execution (after load)
- Scalable to many users
- Works offline (after initial load)

**Cons:**
- Larger initial download
- Need REST API for data
- More complex deployment

**Best for:** Broader deployment, many concurrent users

#### Option 3: React/Vue + ASP.NET Core API
**Pros:**
- Modern, rich UI libraries
- Great developer tools
- Very responsive
- Mobile-friendly

**Cons:**
- Need JavaScript/TypeScript skills
- Two separate codebases
- More complex to maintain

**Best for:** Modern web experience, mobile access important

**Recommendation**: Start with **Blazor Server** for quickest time-to-value, migrate to Blazor WASM if needed later.

### Database for Sessions/Users

**Options:**
1. **SQLite** - Simple, file-based (good for small scale)
2. **SQL Server** - Enterprise-ready (you already have it)
3. **PostgreSQL** - Open-source alternative

**Recommendation**: Start with **SQLite** for simplicity, upgrade to SQL Server if needed.

Schema:
```sql
-- Users
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY,
    Username TEXT UNIQUE,
    Email TEXT,
    PasswordHash TEXT,
    Role TEXT,
    CreatedAt DATETIME
);

-- Conversations
CREATE TABLE Conversations (
    Id INTEGER PRIMARY KEY,
    UserId INTEGER,
    Title TEXT,
    CreatedAt DATETIME,
    UpdatedAt DATETIME,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Messages (conversation history)
CREATE TABLE Messages (
    Id INTEGER PRIMARY KEY,
    ConversationId INTEGER,
    Role TEXT,
    Content TEXT,
    Timestamp DATETIME,
    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id)
);

-- Queries (executed queries)
CREATE TABLE Queries (
    Id INTEGER PRIMARY KEY,
    ConversationId INTEGER,
    Sql TEXT,
    ExecutedAt DATETIME,
    Success BOOLEAN,
    RowCount INTEGER,
    ExecutionTimeMs REAL,
    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id)
);

-- Exports
CREATE TABLE Exports (
    Id INTEGER PRIMARY KEY,
    QueryId INTEGER,
    Filename TEXT,
    FilePath TEXT,
    ExportedAt DATETIME,
    FileSize INTEGER,
    FOREIGN KEY (QueryId) REFERENCES Queries(Id)
);
```

## API Design

### REST Endpoints

#### Authentication
```
POST   /api/auth/login
POST   /api/auth/logout
GET    /api/auth/user
```

#### Conversations
```
GET    /api/conversations              # List user's conversations
POST   /api/conversations              # Create new conversation
GET    /api/conversations/{id}         # Get conversation details
DELETE /api/conversations/{id}         # Delete conversation
GET    /api/conversations/{id}/messages # Get conversation history
```

#### Chat (Main interaction)
```
POST   /api/chat/{conversationId}     # Send message, get SQL
       Request: { message: "Show me all customers" }
       Response: {
         explanation: "...",
         sql: "SELECT * FROM customers",
         teachingNote: "..."
       }
```

#### Query Execution
```
POST   /api/query/execute
       Request: { conversationId, sql }
       Response: {
         success: true,
         columns: [...],
         rows: [...],
         rowCount: 100,
         executionTimeMs: 45.2
       }

GET    /api/query/{id}                # Get previous query results
```

#### Schema
```
GET    /api/schema                    # Get all available schemas
GET    /api/schema/{tableName}        # Get specific table schema
POST   /api/schema/reload             # Reload schemas from Parquet
```

#### Export
```
POST   /api/export/excel
       Request: { queryId, filename }
       Response: {
         downloadUrl: "/downloads/{guid}/results.xlsx",
         files: ["results.xlsx", "results.sql", "results.m"]
       }

GET    /downloads/{guid}/{filename}   # Download exported file
```

#### Admin (optional)
```
GET    /api/admin/users               # List users
POST   /api/admin/users               # Create user
GET    /api/admin/queries             # View all queries
GET    /api/admin/stats               # System stats
```

### SignalR Hub (Real-time)

For long-running queries and live updates:

```csharp
public class QueryHub : Hub
{
    // Client connects
    public async Task StartQuery(string conversationId, string sql)
    {
        // Execute query
        // Send progress updates
        await Clients.Caller.SendAsync("QueryProgress", new {
            status = "executing",
            progress = 50
        });

        // Send final result
        await Clients.Caller.SendAsync("QueryComplete", result);
    }

    // Cancel running query
    public async Task CancelQuery(string queryId)
    {
        // Cancel execution
    }
}
```

**Client usage:**
```javascript
// Connect to hub
connection = new signalR.HubConnectionBuilder()
    .withUrl("/queryHub")
    .build();

// Listen for updates
connection.on("QueryProgress", (data) => {
    updateProgressBar(data.progress);
});

connection.on("QueryComplete", (result) => {
    displayResults(result);
});

// Start query
connection.invoke("StartQuery", conversationId, sql);
```

## Frontend Design (Blazor Server)

### Page Structure

```
/
├── Index.razor              # Dashboard/conversation list
├── Chat/{id}.razor          # Main chat interface
├── Schema.razor             # Schema browser
├── History.razor            # Query history
├── Admin/
│   ├── Users.razor
│   └── Statistics.razor
└── Shared/
    ├── MainLayout.razor
    ├── NavMenu.razor
    └── LoginDisplay.razor
```

### Main Chat Interface (`Chat.razor`)

**Layout:**
```
┌─────────────────────────────────────────────────────────────┐
│ Mallard - Conversation: "Sales Analysis"           [Export] │
├─────────────────┬───────────────────────────────────────────┤
│                 │                                           │
│  Schema         │  Chat Panel                               │
│  ┌──────────┐   │  ┌─────────────────────────────────────┐ │
│  │ ▼ Tables │   │  │ You: Show me all customers          │ │
│  │   ├ customers│  │ ┌─────────────────────────────────┐ │ │
│  │   ├ orders   │  │ │ Mallard: I'll select all        │ │ │
│  │   └ products │  │ │ customers from the table        │ │ │
│  │              │   │ │                                 │ │ │
│  │ ▼ History   │   │ │ SQL:                            │ │ │
│  │   ├ Recent   │   │ │ SELECT * FROM customers;        │ │ │
│  │   └ Saved    │   │ │                                 │ │ │
│  └──────────┘   │  │ │ [Execute] [Explain] [Refine]    │ │ │
│                 │  │ └─────────────────────────────────┘ │ │
│                 │  └─────────────────────────────────────┘ │
│                 │                                           │
│                 │  Results Grid                             │
│                 │  ┌─────────────────────────────────────┐ │
│                 │  │ id │ name         │ email           │ │
│                 │  ├────┼──────────────┼─────────────────┤ │
│                 │  │ 1  │ Acme Corp    │ acme@...        │ │
│                 │  │ 2  │ Tech Ltd     │ tech@...        │ │
│                 │  │... │ ...          │ ...             │ │
│                 │  └─────────────────────────────────────┘ │
│                 │  [Download Excel] [Download .m]           │
├─────────────────┴───────────────────────────────────────────┤
│ Type your question... [Send]                         [Help] │
└─────────────────────────────────────────────────────────────┘
```

**Key Components:**

1. **Schema Browser** (Left panel)
   - Tree view of tables
   - Expandable columns
   - Click to insert into query
   - Recent queries
   - Saved queries

2. **Chat Panel** (Top right)
   - Conversation history
   - User messages
   - Claude responses with SQL
   - Teaching notes (collapsible)
   - Action buttons (Execute/Explain/Refine)

3. **Results Grid** (Bottom right)
   - Sortable columns
   - Pagination
   - Export buttons
   - Row count/execution time

4. **Input Box** (Bottom)
   - Natural language input
   - Send button
   - Help/suggestions

### Example Blazor Component

```razor
@page "/chat/{id:int}"
@inject IConversationService ConversationService
@inject IQueryService QueryService
@inject NavigationManager Navigation

<div class="chat-container">
    <div class="chat-header">
        <h2>@conversation.Title</h2>
        <button @onclick="ExportResults">Export</button>
    </div>

    <div class="chat-main">
        <div class="schema-panel">
            <SchemaTree Schemas="@schemas" OnTableClick="InsertTableName" />
        </div>

        <div class="chat-panel">
            @foreach (var msg in messages)
            {
                <ChatMessage Message="@msg" OnExecute="ExecuteQuery" />
            }

            @if (currentResult != null)
            {
                <ResultsGrid Data="@currentResult" />
            }
        </div>
    </div>

    <div class="chat-input">
        <input @bind="userInput" @onkeypress="HandleKeyPress" />
        <button @onclick="SendMessage">Send</button>
    </div>
</div>

@code {
    [Parameter]
    public int Id { get; set; }

    private Conversation conversation;
    private List<Message> messages = new();
    private List<TableSchema> schemas = new();
    private QueryResult currentResult;
    private string userInput;

    protected override async Task OnInitializedAsync()
    {
        conversation = await ConversationService.GetAsync(Id);
        messages = await ConversationService.GetMessagesAsync(Id);
        schemas = await QueryService.GetSchemasAsync();
    }

    private async Task SendMessage()
    {
        var response = await ConversationService.SendMessageAsync(Id, userInput);
        messages.Add(new Message { Role = "user", Content = userInput });
        messages.Add(new Message { Role = "assistant", Content = response.RawResponse });
        userInput = "";
        StateHasChanged();
    }

    private async Task ExecuteQuery(string sql)
    {
        currentResult = await QueryService.ExecuteAsync(Id, sql);
        StateHasChanged();
    }
}
```

## Security Implementation

### Authentication

**ASP.NET Core Identity** with JWT tokens:

```csharp
// Program.cs
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
        };
    });
```

**Login Flow:**
1. User enters credentials
2. Server validates against database
3. Server generates JWT token
4. Client stores token (HttpOnly cookie or localStorage)
5. Client includes token in all API requests
6. Server validates token on each request

### Authorization

**Role-based access:**

```csharp
[Authorize(Roles = "User")]
public class ChatController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SendMessage(...)
    {
        // Only authenticated users can chat
    }
}

[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        // Only admins can view users
    }
}
```

**Row-level security** (if needed):
```csharp
// Limit users to specific schemas/tables
public class SchemaAccessPolicy
{
    public bool CanAccess(User user, string tableName)
    {
        // Check user permissions
        return user.AllowedTables.Contains(tableName);
    }
}
```

### SQL Injection Prevention

**Critical**: Even though users provide natural language, the generated SQL must be validated:

```csharp
public class SqlValidator
{
    private static readonly string[] DangerousKeywords =
    {
        "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE",
        "EXEC", "EXECUTE", "xp_", "sp_"
    };

    public bool IsSafe(string sql)
    {
        // Allow only SELECT statements
        if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            return false;

        // Check for dangerous keywords
        foreach (var keyword in DangerousKeywords)
        {
            if (sql.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
```

**Additional protections:**
1. Use read-only DuckDB connection
2. Limit query execution time (60s timeout)
3. Limit result set size (max 10,000 rows)
4. Log all queries with user attribution
5. Rate limiting per user

### Secrets Management

**Never expose in frontend:**
- Anthropic API key
- Database connection strings
- JWT signing key

**Store in:**
```json
// appsettings.json (development)
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=mallard.db"
  },
  "Anthropic": {
    "ApiKey": "sk-ant-..." // From environment variable
  },
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "Mallard",
    "Audience": "MallardUsers"
  }
}
```

**Production**: Use environment variables or Azure Key Vault:
```csharp
builder.Configuration.AddEnvironmentVariables();
// ANTHROPIC_API_KEY environment variable
```

## Implementation Phases

### Phase 1: Backend API (Week 1-2)

**Goal**: Create working REST API that reuses existing Mallard code

**Tasks:**
- [ ] Create ASP.NET Core Web API project
- [ ] Copy existing Mallard code to Services/ folder
- [ ] Refactor for dependency injection
- [ ] Implement ConversationService
- [ ] Implement QueryService
- [ ] Implement ExportService
- [ ] Add SQLite database for sessions
- [ ] Create API controllers
- [ ] Add Swagger/OpenAPI documentation
- [ ] Test with Postman/curl

**Deliverables:**
- Working REST API
- Swagger UI at `/swagger`
- Postman collection

### Phase 2: Authentication & Security (Week 2-3)

**Goal**: Secure the API

**Tasks:**
- [ ] Add ASP.NET Core Identity
- [ ] Implement JWT authentication
- [ ] Create login/register endpoints
- [ ] Add role-based authorization
- [ ] Implement SQL validation
- [ ] Add rate limiting
- [ ] Configure CORS
- [ ] Add audit logging

**Deliverables:**
- Secured API
- User registration/login working
- Admin capabilities

### Phase 3: Basic Frontend (Week 3-4)

**Goal**: Create minimal working UI

**Tasks:**
- [ ] Create Blazor Server project
- [ ] Design main layout
- [ ] Implement login page
- [ ] Create chat interface
- [ ] Add schema browser component
- [ ] Implement results grid
- [ ] Add export functionality
- [ ] Style with CSS (Bootstrap or Tailwind)

**Deliverables:**
- Working web UI
- Can create conversations
- Can execute queries
- Can export results

### Phase 4: Real-time Features (Week 4-5)

**Goal**: Add SignalR for better UX

**Tasks:**
- [ ] Add SignalR to backend
- [ ] Create QueryHub
- [ ] Implement progress updates
- [ ] Add query cancellation
- [ ] Add live collaboration (optional)
- [ ] Update frontend to use SignalR

**Deliverables:**
- Real-time query execution
- Progress indicators
- Ability to cancel queries

### Phase 5: Advanced Features (Week 5-6)

**Goal**: Polish and extend

**Tasks:**
- [ ] Query history and search
- [ ] Saved queries/templates
- [ ] Query sharing between users
- [ ] Admin dashboard
- [ ] Usage statistics
- [ ] Export queue management
- [ ] SCP upload integration
- [ ] Power Query library hosting

**Deliverables:**
- Full-featured application
- Admin tools
- Collaboration features

### Phase 6: Deployment (Week 6-7)

**Goal**: Production-ready deployment

**Tasks:**
- [ ] Performance optimization
- [ ] Error handling and logging (Serilog)
- [ ] Health checks
- [ ] Create deployment package
- [ ] IIS configuration
- [ ] SSL certificate setup
- [ ] Backup strategy
- [ ] Monitoring setup
- [ ] User documentation
- [ ] Admin documentation

**Deliverables:**
- Production deployment
- Monitoring in place
- Documentation complete

## Project Structure

```
Mallard.Web/
├── Mallard.Web.API/                 # Backend API
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ChatController.cs
│   │   ├── QueryController.cs
│   │   ├── SchemaController.cs
│   │   ├── ExportController.cs
│   │   └── AdminController.cs
│   ├── Services/
│   │   ├── ConversationService.cs
│   │   ├── QueryService.cs
│   │   ├── ExportService.cs
│   │   └── UserService.cs
│   ├── Hubs/
│   │   └── QueryHub.cs
│   ├── Models/
│   │   ├── DTOs/                    # Data transfer objects
│   │   └── Entities/                # Database entities
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Core/                        # Reused from Mallard CLI
│   │   ├── ClaudeApiClient.cs
│   │   ├── DuckDbCliExecutor.cs
│   │   ├── SchemaLoader.cs
│   │   └── ExcelExporter.cs
│   ├── Middleware/
│   │   ├── ErrorHandlingMiddleware.cs
│   │   └── RateLimitingMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── Mallard.Web.UI/                  # Frontend (Blazor Server)
│   ├── Pages/
│   │   ├── Index.razor
│   │   ├── Login.razor
│   │   ├── Chat.razor
│   │   ├── Schema.razor
│   │   └── History.razor
│   ├── Components/
│   │   ├── SchemaTree.razor
│   │   ├── ChatMessage.razor
│   │   ├── ResultsGrid.razor
│   │   └── ExportDialog.razor
│   ├── Services/
│   │   ├── ApiClient.cs
│   │   └── StateContainer.cs
│   ├── wwwroot/
│   │   ├── css/
│   │   └── js/
│   ├── Program.cs
│   └── appsettings.json
│
├── Mallard.Shared/                  # Shared models
│   ├── Models/
│   │   ├── QueryResult.cs
│   │   ├── TableSchema.cs
│   │   └── Message.cs
│   └── DTOs/
│       ├── ChatRequest.cs
│       ├── ChatResponse.cs
│       └── QueryRequest.cs
│
└── Mallard.Tests/
    ├── API.Tests/
    ├── Services.Tests/
    └── UI.Tests/
```

## Migration from CLI

### Code Reuse Strategy

**Directly reusable (minimal changes):**
- ✅ ClaudeApiClient.cs
- ✅ IQueryExecutor.cs
- ✅ DuckDbCliExecutor.cs
- ✅ SchemaLoader.cs
- ✅ ExcelExporter.cs
- ✅ All Models/*.cs

**Needs refactoring:**
- ⚠️ SqlAssistant.cs → ConversationService.cs
  - Remove Console.WriteLine
  - Return data instead of displaying
  - Add database persistence

**Not needed in web version:**
- ❌ Program.cs (CLI-specific)
- ❌ Command-line argument parsing

### Refactoring Example

**Before (CLI):**
```csharp
// SqlAssistant.cs
public async Task RunAsync()
{
    Console.WriteLine("Welcome to Mallard!");

    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();

        var response = await _claudeClient.SendMessageAsync(input, _context);
        Console.WriteLine(response.Explanation);
        Console.WriteLine(response.Sql);
    }
}
```

**After (Web Service):**
```csharp
// ConversationService.cs
public async Task<ChatResponse> SendMessageAsync(int conversationId, string message)
{
    var conversation = await _db.Conversations
        .Include(c => c.Messages)
        .FirstOrDefaultAsync(c => c.Id == conversationId);

    var context = BuildContext(conversation);
    var response = await _claudeClient.SendMessageAsync(message, context);

    // Save to database
    conversation.Messages.Add(new Message
    {
        Role = "user",
        Content = message,
        Timestamp = DateTime.UtcNow
    });
    conversation.Messages.Add(new Message
    {
        Role = "assistant",
        Content = response.RawResponse,
        Timestamp = DateTime.UtcNow
    });

    await _db.SaveChangesAsync();

    return new ChatResponse
    {
        Explanation = response.Explanation,
        Sql = response.Sql,
        TeachingNote = response.TeachingNote
    };
}
```

## Deployment

### IIS Deployment (Windows Server)

**Requirements:**
- Windows Server 2019+
- IIS 10+
- .NET 8.0 Hosting Bundle
- URL Rewrite Module

**Steps:**

1. **Publish Application**
   ```powershell
   dotnet publish Mallard.Web.API -c Release -o ./publish/api
   dotnet publish Mallard.Web.UI -c Release -o ./publish/ui
   ```

2. **Create IIS Sites**
   ```powershell
   # API site
   New-WebAppPool -Name "MallardAPI"
   New-Website -Name "Mallard API" `
     -Port 5000 `
     -PhysicalPath "C:\inetpub\mallard\api" `
     -ApplicationPool "MallardAPI"

   # UI site
   New-WebAppPool -Name "MallardUI"
   New-Website -Name "Mallard UI" `
     -Port 443 `
     -PhysicalPath "C:\inetpub\mallard\ui" `
     -ApplicationPool "MallardUI"
   ```

3. **Configure SSL**
   - Bind SSL certificate to UI site
   - Redirect HTTP → HTTPS

4. **Set Environment Variables**
   ```powershell
   [System.Environment]::SetEnvironmentVariable(
     'ANTHROPIC_API_KEY',
     'your-key',
     'Machine'
   )
   ```

5. **Configure Firewall**
   ```powershell
   New-NetFirewallRule -DisplayName "Mallard HTTP" `
     -Direction Inbound -LocalPort 80 -Protocol TCP -Action Allow
   New-NetFirewallRule -DisplayName "Mallard HTTPS" `
     -Direction Inbound -LocalPort 443 -Protocol TCP -Action Allow
   ```

### Docker Deployment (Alternative)

```dockerfile
# Dockerfile for API
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./publish/api .
EXPOSE 5000
ENTRYPOINT ["dotnet", "Mallard.Web.API.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:5000"
    environment:
      - ANTHROPIC_API_KEY=${ANTHROPIC_API_KEY}
      - ConnectionStrings__DefaultConnection=/data/mallard.db
    volumes:
      - ./data:/data
      - ./parquet:/parquet

  ui:
    build: ./ui
    ports:
      - "443:443"
    depends_on:
      - api
```

## New Features Enabled by Web Architecture

### Multi-User Collaboration

**Shared conversations:**
- Multiple users can view same query results
- Comment on queries
- Share saved queries
- Team query library

**Implementation:**
```csharp
public class SharedConversation
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public List<int> SharedWithUserIds { get; set; }
    public bool AllowEdit { get; set; }
}
```

### Query Templates Library

**Centralized repository:**
- Pre-built query templates
- Parameter substitution
- Category organization
- Usage tracking

**UI:**
```
Templates > Sales > Monthly Revenue
  Parameters:
    - Start Date: [____]
    - End Date: [____]
  [Use This Template]
```

### Scheduled Queries

**Run queries on schedule:**
- Cron-like scheduling
- Email results
- Auto-export to network share
- Alert on threshold breaches

**Implementation:**
```csharp
public class ScheduledQuery
{
    public int Id { get; set; }
    public string CronExpression { get; set; }
    public string Sql { get; set; }
    public List<string> EmailRecipients { get; set; }
    public string ExportPath { get; set; }
}
```

### Usage Analytics

**Track:**
- Most-used queries
- Peak usage times
- Slow queries
- User activity
- Schema access patterns

**Dashboard:**
- Real-time query count
- Average execution time
- Popular tables
- Active users

### Mobile Access

**Responsive design:**
- View results on phone/tablet
- Quick query execution
- Push notifications for scheduled queries
- Mobile-optimized grid

## Performance Considerations

### Caching Strategy

```csharp
public class CacheService
{
    private readonly IMemoryCache _cache;

    // Cache schemas (refresh every hour)
    public async Task<List<TableSchema>> GetSchemasAsync()
    {
        return await _cache.GetOrCreateAsync("schemas", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _schemaLoader.LoadSchemasAsync();
        });
    }

    // Cache query results (5 minutes)
    public async Task<QueryResult> GetQueryResultAsync(string queryHash)
    {
        return await _cache.GetOrCreateAsync($"query:{queryHash}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _executor.ExecuteAsync(sql);
        });
    }
}
```

### Connection Pooling

```csharp
// Reuse DuckDB connections
public class DuckDbConnectionPool
{
    private readonly SemaphoreSlim _semaphore = new(10); // Max 10 concurrent

    public async Task<QueryResult> ExecuteAsync(string sql)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _executor.ExecuteAsync(sql);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### Result Streaming

For large result sets:
```csharp
[HttpGet("stream")]
public async IAsyncEnumerable<object> StreamResults(string sql)
{
    await foreach (var row in _executor.ExecuteStreamAsync(sql))
    {
        yield return row;
    }
}
```

## Monitoring & Logging

### Serilog Configuration

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/mallard-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341") // Optional: Seq for log aggregation
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "Mallard")
    .CreateLogger();
```

### Application Insights (Azure)

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("database", () => CheckDatabase())
    .AddCheck("duckdb", () => CheckDuckDb())
    .AddCheck("claude_api", () => CheckClaudeApi());

app.MapHealthChecks("/health");
```

## Testing Strategy

### Unit Tests
```csharp
public class ConversationServiceTests
{
    [Fact]
    public async Task SendMessage_CreatesNewMessage()
    {
        // Arrange
        var service = CreateService();

        // Act
        var response = await service.SendMessageAsync(1, "test");

        // Assert
        Assert.NotNull(response.Sql);
    }
}
```

### Integration Tests
```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Chat_RequiresAuthentication()
    {
        var response = await _client.PostAsync("/api/chat/1", ...);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

### End-to-End Tests
Using Playwright or Selenium for UI testing.

## Cost Considerations

**Infrastructure:**
- Windows Server license (if not already owned)
- SSL certificate (Let's Encrypt free)
- Storage for SQLite database (minimal)

**Development Time:**
- ~6-7 weeks for full implementation
- 1 developer full-time

**Operational:**
- Claude API costs (same as CLI version)
- Hosting costs (minimal if internal server)
- Backup storage

## Success Metrics

**User Adoption:**
- Number of active users
- Queries per day
- Conversations created

**Performance:**
- Average query execution time
- API response time (<200ms)
- Page load time (<2s)

**Quality:**
- Error rate (<1%)
- User satisfaction
- Bugs reported vs resolved

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Concurrent users overload | High | Connection pooling, rate limiting |
| Long-running queries block UI | Medium | SignalR for async execution |
| Security breach | High | Authentication, SQL validation, audit logs |
| Claude API rate limits | Medium | Caching, queue system |
| Database corruption | High | Regular backups, transaction safety |

## Next Steps

1. **Review this plan** with team
2. **Prioritize features** based on needs
3. **Set up development environment** for web project
4. **Create proof-of-concept** (Phase 1 only)
5. **Test with small user group**
6. **Iterate based on feedback**
7. **Full deployment**

## Appendix: Sample Code

See `/examples/` directory for:
- Complete controller examples
- Service implementations
- Blazor component samples
- Authentication setup
- Deployment scripts

---

**Document Version**: 1.0
**Last Updated**: 2025-11-05
**Author**: Claude Code CLI
**Status**: Draft for Review
