# 🗄️ PostgreSQL MCP Server

A powerful Model Context Protocol (MCP) server for PostgreSQL database operations, built with ASP.NET Core 8.0. Search and query your PostgreSQL database just like DBeaver, but with MCP integration for VS Code and Copilot!

## ✨ Features

### 🔍 Search Capabilities (DBeaver-like)
- **DB Metadata Search** - Search tables, columns, functions, procedures, constraints, data types
- **Full-Text Search** - Search within table data content
- **Function Source Search** - Search within function/procedure source code
- **Advanced Search** - Multi-criteria search with fine-grained control

### 📊 Database Management
- List schemas, tables, functions
- Get table structure and column details
- View table DDL definitions
- Get foreign key relationships
- Execute custom SQL queries

---

## 🚀 Quick Start

### 1️⃣ Prerequisites
- .NET 8.0 SDK or later
- PostgreSQL 16.10 or compatible
- Visual Studio 2022 / VS Code

### 2️⃣ Configuration Setup

#### **Option A: Using `launchSettings.json` (Recommended for Development)**

Open `mcp-db-server/Properties/launchSettings.json` and update the environment variables:

```json
{
  "profiles": {
    "mcp-db-server": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DATABASE_URL": "Server=YOUR_HOST;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD;Timeout=900;CommandTimeout=900",
        "SERVER_URL": "http://localhost:5004"
      },
      "applicationUrl": "http://localhost:5004"
    }
  }
}
```

**Replace with your database details:**
- `YOUR_HOST` - PostgreSQL server address (e.g., `localhost` or `34.173.168.133`)
- `YOUR_DB` - Database name (e.g., `staging`, `postgres`)
- `YOUR_USER` - Database username (e.g., `postgres`)
- `YOUR_PASSWORD` - Database password

#### **Option B: Using `.env` File**

Create `.env` file in the project root:

```env
DATABASE_URL=postgresql://user:password@localhost:5432/database_name
```

Or use individual parameters:

```env
DB_HOST=localhost
DB_PORT=5432
DB_NAME=postgres
DB_USER=postgres
DB_PASSWORD=yourpassword
```

### 3️⃣ Run the Server

**From Visual Studio:**
- Press `F5` or click "Run"

**From Terminal:**
```bash
cd mcp-db-server
dotnet run
```

Server will start on: `http://localhost:5004`

### 4️⃣ Access Swagger UI

Open in browser: `http://localhost:5004/swagger`

---

## 🔌 Using with VS Code & Copilot (MCP Integration)

### Step 1: Open MCP Configuration File

1. Open **User Settings** in VS Code:
   - Press `Ctrl+Shift+P`
   - Search for `Preferences: Open User Settings (JSON)`

2. Or manually navigate to: `C:\Users\[YourUsername]\AppData\Roaming\Code\User\settings.json`

3. Find or create the `"mcp"` section:

```json
{
  "mcp": {
    "servers": {
      "postgres-mcp": {
        "url": "http://localhost:5004/",
        "type": "http"
      }
    }
  }
}
```

### Step 2: Edit MCP Configuration File (mcp.json)

⚠️ **Important:** You need to edit the **`mcp.json` file**, not the settings.json!

1. Press `Ctrl+Shift+P` again
2. Search for `MCP: Edit MCP Configuration`
3. This will open the file: `C:\Users\[YourUsername]\AppData\Roaming\Code\User\mcp.json`

4. **Find your server name** in the file - look for the server name you created:
   - In this example: `"postgres-mcp"` ← This is the name of your MCP server
   - Your server name might be different!

5. Make sure your `mcp.json` contains:

```json
{
  "servers": {
    "postgres-mcp": {
      "url": "http://localhost:5004/",
      "type": "http"
    }
  },
  "inputs": [
    {
      "id": "Authorization",
      "type": "promptString",
      "description": "Authentication token (PAT or App token)",
      "password": true
    }
  ]
}
```

**Note:** Replace `"postgres-mcp"` with your actual server name if it's different!

### Step 3: Restart VS Code

- Close and reopen VS Code completely (or press `Ctrl+Shift+P` → `Developer: Reload Window`)

### Step 4: Verify Connection in MCP Servers Panel

1. Open the **MCP Servers** panel in VS Code from ***mcp.json***
2. Search for your server name (e.g., `postgres-mcp`)
3. Click the **"▶️ Start"** button next to it

   ![MCP Start Button](images/mcp-start-btn.PNG)
   
   *Screenshot: The Start button is located at the top of the MCP server entry in the servers list*

  Tip: If the image doesn't appear in the preview, make sure you saved the screenshot to:
  `mcp-db-server/images/mcp-start-btn.PNG` (relative to this README). This file path is what the README references.

4. You should see a ✅ **Connected** status with **12 tools** available

### Step 5: Use with Copilot

Now you can ask Copilot questions about your database!

**Examples:**

1. **Search for database objects:**
   ```
   @postgres-mcp search for tables named user
   ```

2. **Query data:**
   ```
   @postgres-mcp execute: SELECT * FROM users LIMIT 5
   ```

3. **Find specific columns:**
   ```
   @postgres-mcp find all columns containing email
   ```

4. **Search in function source:**
   ```
   @postgres-mcp search functions that contain calculate
   ```

---

## 📡 API Endpoints

### Authentication
⚠️ **Currently no authentication** - Add your own security layer before production!

### Health Check
```
GET /health
```

### Search Endpoints

#### Metadata Search
```
GET /api/search/metadata?keyword=user&schema=public
```

#### Full-Text Search
```
POST /api/search/fulltext
Content-Type: application/json

{
  "keyword": "john@example.com",
  "schema": "public",
  "max_rows_per_table": 100
}
```

#### Function Source Search
```
GET /api/search/functions/source?keyword=calculate&schema=public
```

#### Advanced Search
```
POST /api/search/advanced
Content-Type: application/json

{
  "keyword": "payment",
  "schema": "public",
  "case_sensitive": false,
  "object_types": ["table", "function"]
}
```

### Database Management Endpoints

#### Get Schemas
```
GET /api/database/schemas
```

#### Get Tables
```
GET /api/database/tables?schema=public
```

#### Get Table Structure
```
GET /api/database/tables/{tableName}?schema=public
```

#### Get Table DDL
```
GET /api/database/tables/{tableName}/definition?schema=public
```

#### Get Functions
```
GET /api/database/functions?schema=public
```

#### Get Function Definition
```
GET /api/database/functions/{functionName}?schema=public
```

#### Execute Custom Query
```
POST /api/database/query
Content-Type: application/json

{
  "sql": "SELECT * FROM users WHERE user_id < 10 LIMIT 5"
}
```

#### Get Foreign Keys
```
GET /api/database/tables/{tableName}/foreignkeys?schema=public
```

---

## 🔧 Configuration Details

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DATABASE_URL` | Full PostgreSQL connection string | (required) |
| `DB_HOST` | Database host | `localhost` |
| `DB_PORT` | Database port | `5432` |
| `DB_NAME` | Database name | `postgres` |
| `DB_USER` | Database username | `postgres` |
| `DB_PASSWORD` | Database password | (empty) |
| `SERVER_URL` | Server URL for MCP | `http://localhost:5004` |
| `ASPNETCORE_ENVIRONMENT` | Environment | `Development` |

### Priority Order
1. `DATABASE_URL` environment variable (if set, uses this)
2. Individual `DB_*` variables (if DATABASE_URL not set)
3. Falls back to defaults

---

## 🧪 Testing

### Using Swagger UI (Easy)
1. Go to `http://localhost:5004/swagger`
2. Click on any endpoint
3. Click "Try it out"
4. Enter parameters and click "Execute"

### Using PowerShell
```powershell
# Search metadata
$response = Invoke-RestMethod -Uri "http://localhost:5004/api/search/metadata?keyword=user" `
  -Method Get

# Full-text search
$body = @{ keyword = "john"; schema = "public" } | ConvertTo-Json
$response = Invoke-RestMethod -Uri "http://localhost:5004/api/search/fulltext" `
  -Method Post `
  -Body $body `
  -ContentType "application/json"

# Execute query
$body = @{ sql = "SELECT COUNT(*) FROM information_schema.tables" } | ConvertTo-Json
$response = Invoke-RestMethod -Uri "http://localhost:5004/api/database/query" `
  -Method Post `
  -Body $body `
  -ContentType "application/json"
```

### Using cURL
```bash
# Metadata search
curl "http://localhost:5004/api/search/metadata?keyword=user&schema=public"

# Full-text search
curl -X POST "http://localhost:5004/api/search/fulltext" \
  -H "Content-Type: application/json" \
  -d '{"keyword":"john@example.com","schema":"public"}'

# Execute query
curl -X POST "http://localhost:5004/api/database/query" \
  -H "Content-Type: application/json" \
  -d '{"sql":"SELECT COUNT(*) FROM pg_tables"}'
```

---

## 📁 Project Structure

```
mcp-db-server/
├── Program.cs                          # Application entry point
├── mcp-db-server.csproj               # Project file
├── Properties/
│   └── launchSettings.json            # ⚡ Configuration (set DB URL here!)
├── src/
│   ├── Controllers/
│   │   ├── DatabaseController.cs       # DB operations endpoints
│   │   └── DatabaseSearchController.cs # Search endpoints
│   ├── Database/
│   │   └── DatabaseManager.cs          # PostgreSQL connection & queries
│   ├── Models/
│   │   └── MCPModels.cs               # Data models
│   ├── Tools/                          # MCP tools
│   └── Utilities/                      # Helper utilities
└── bin/
    └── Debug/                          # Build output (ignored by git)
```

---

## 🔒 Security Notes

⚠️ **Before Production:**

1. **Add Authentication**
   - Implement JWT or API Key authentication
   - Add authorization middleware

2. **Validate Inputs**
   - SQL injection protection
   - Input sanitization

3. **Use Secrets**
   - Never commit `.env` or `launchSettings.json` with real credentials
   - Use Azure Key Vault or similar in production

4. **Network Security**
   - Don't expose to the internet without HTTPS
   - Use VPN or private networks
   - Add firewall rules

5. **Database Security**
   - Use strong passwords
   - Limit user permissions
   - Enable SSL for database connections

---

## 🐛 Troubleshooting

### "Connection refused" Error

**Problem:** Can't connect to PostgreSQL
- ✅ Check if PostgreSQL is running
- ✅ Verify host/port in configuration
- ✅ Check firewall rules
- ✅ Verify username and password

### "Database not found" Error

**Problem:** Specified database doesn't exist
- ✅ Check database name spelling
- ✅ Verify database exists in PostgreSQL
- ✅ Check user permissions

### MCP Server Not Connecting

**Problem:** VS Code can't find the MCP server
- ✅ Make sure server is running (`dotnet run`)
- ✅ Check URL in `mcp.json` is correct
- ✅ Restart VS Code
- ✅ Check firewall (port 5004 must be open)

### Port Already in Use

**Problem:** Port 5004 is already in use
- Option 1: Change `SERVER_URL` in `launchSettings.json`
- Option 2: Kill the process using port 5004:
  ```powershell
  Get-Process -Id (Get-NetTCPConnection -LocalPort 5004).OwningProcess | Stop-Process
  ```

---

## 📚 Additional Resources

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [Npgsql Documentation](https://www.npgsql.org/doc/)

---

## 📝 API Response Format

### Success Response
```json
{
  "data": {...},
  "status": 200,
  "message": "Success"
}
```

### Error Response
```json
{
  "error": "Error description",
  "status": 400,
  "details": "Additional error information"
}
```

---

## 🎯 Example Workflows

### Example 1: Finding a Specific Table

1. Use Swagger UI: `/api/search/metadata?keyword=users`
2. Get table information
3. Use `/api/database/tables/users` to see columns
4. Use `/api/database/tables/users/foreignkeys` to see relationships

### Example 2: Searching Table Data

1. POST to `/api/search/fulltext`
2. Search for email addresses or phone numbers
3. Get matching rows from tables
4. Use results for data analysis

### Example 3: Automating with Copilot

1. Start MCP server
2. Configure in VS Code
3. Ask Copilot: "@postgres-mcp show me all customers from New York"
4. Copilot uses the MCP endpoints to query and analyze
5. Get AI-assisted insights

---

## 📞 Support

For issues or questions:
1. Check this README
2. Review error messages
3. Check PostgreSQL connection
4. Verify database permissions
5. Check VS Code MCP configuration

---

## 📄 License

MIT License - Feel free to use and modify!

---

## 🎉 Happy Database Querying!

Built with ❤️ using **ASP.NET Core 8.0**  
PostgreSQL 16.10 Compatible | MCP Protocol Ready

---

**Last Updated:** January 11, 2026
