# PostgreSQL Database MCP Server

A professional Model Context Protocol (MCP) server for PostgreSQL database operations with advanced search capabilities similar to DBeaver.

## 🌟 Features

### Database Search Capabilities

1. **DB Metadata Search** - Search across all database objects:
   - Tables
   - Columns
   - Functions/Procedures
   - Constraints (PRIMARY KEY, FOREIGN KEY, CHECK, UNIQUE)
   - Data Types

2. **DB Full-Text Search** - Search within table data:
   - Search across all text/varchar columns
   - Filter by specific tables or columns
   - Configurable result limits

3. **Function Source Search** - Search within stored procedures and functions:
   - Search in function bodies
   - Search in function definitions
   - Supports all PostgreSQL function types

4. **Advanced Search** - Multi-criteria search:
   - Search in names and/or definitions
   - Case-sensitive/insensitive options
   - Filter by object types

### Database Management

- List schemas, tables, and functions
- Get table structures and definitions
- Execute custom SQL queries
- View foreign key relationships
- Get function/procedure definitions

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 or higher
- PostgreSQL 16.x (tested on 16.10)
- Connection to a PostgreSQL database

### Installation

1. Clone the repository
2. Copy `.env.example` to `.env` and configure your database connection:

```bash
cp .env.example .env
```

3. Edit `.env` with your PostgreSQL credentials:

```env
DATABASE_URL=postgresql://username:password@localhost:5432/dbname
```

Or use individual parameters:

```env
DB_HOST=localhost
DB_PORT=5432
DB_NAME=postgres
DB_USER=postgres
DB_PASSWORD=your_password
```

### Run the Server

```bash
cd mcp-db-server
dotnet restore
dotnet run
```

The server will start on `http://localhost:5004`

### Access API Documentation

Open your browser and navigate to:
- **Swagger UI**: http://localhost:5004/swagger
- **Health Check**: http://localhost:5004/health

## 📚 API Endpoints

### Search Endpoints

#### 1. Metadata Search
Search database objects (tables, columns, functions, etc.)

```http
GET /api/search/metadata?keyword=user&schema=public
```

Query Parameters:
- `keyword` (required): Search term
- `schema` (optional, default: "public"): Schema name
- `searchTables` (optional, default: true)
- `searchColumns` (optional, default: true)
- `searchFunctions` (optional, default: true)
- `searchConstraints` (optional, default: true)
- `searchDataTypes` (optional, default: true)

#### 2. Full-Text Search
Search within table data content

```http
POST /api/search/fulltext
Content-Type: application/json

{
  "keyword": "john",
  "schema": "public",
  "tables": ["users", "customers"],
  "columns": ["name", "email"],
  "max_rows_per_table": 100
}
```

#### 3. Function Source Search
Search within function/procedure code

```http
GET /api/search/functions/source?keyword=calculate&schema=public
```

#### 4. Advanced Search
Multi-criteria search with fine control

```http
POST /api/search/advanced
Content-Type: application/json

{
  "keyword": "order",
  "schema": "public",
  "search_in_names": true,
  "search_in_definitions": true,
  "search_in_comments": false,
  "case_sensitive": false,
  "object_types": ["table", "function"]
}
```

### Database Management Endpoints

#### List Schemas
```http
GET /api/database/schemas
```

#### List Tables
```http
GET /api/database/tables?schema=public
```

#### Get Table Structure
```http
GET /api/database/tables/users?schema=public
```

#### Get Table Definition (DDL)
```http
GET /api/database/tables/users/definition?schema=public
```

#### List Functions
```http
GET /api/database/functions?schema=public
```

#### Get Function Definition
```http
GET /api/database/functions/calculate_total?schema=public
```

#### Execute Custom Query
```http
POST /api/database/query
Content-Type: application/json

{
  "sql": "SELECT * FROM users WHERE active = true LIMIT 10"
}
```

#### Get Foreign Keys
```http
GET /api/database/tables/orders/foreignkeys?schema=public
```

## 🔧 Configuration as MCP Server

### For VS Code with GitHub Copilot

Create or update `.vscode/mcp.json`:

```json
{
  "postgres-mcp": {
			"url": "http://localhost:5004/",
			"type": "http"
	}
}
```

## 🎯 Use Cases

### 1. Find all tables related to "user"
```http
GET /api/search/metadata?keyword=user&searchTables=true&searchColumns=false
```

### 2. Find all columns containing "email"
```http
GET /api/search/metadata?keyword=email&searchTables=false&searchColumns=true
```

### 3. Search for text in table data
```http
POST /api/search/fulltext
{
  "keyword": "john@example.com",
  "schema": "public"
}
```

### 4. Find functions that calculate totals
```http
GET /api/search/functions/source?keyword=total
```

### 5. Search for constraints
```http
GET /api/search/metadata?keyword=fk_user&searchConstraints=true
```

## 🔍 PostgreSQL Queries Used

The server uses optimized PostgreSQL queries compatible with PostgreSQL 16.x:

- `information_schema` views for metadata
- `pg_catalog` for system information
- `pg_proc` and `pg_namespace` for functions
- Full-text search with `ILIKE` for text matching
- `pg_get_functiondef()` for function definitions

## 🛠️ Development

### Project Structure

```
mcp-db-server/
├── Program.cs                          # Main application entry
├── mcp-db-server.csproj               # Project file
└── src/
    ├── Controllers/
    │   ├── DatabaseSearchController.cs # Search endpoints
    │   └── DatabaseController.cs       # Database management
    ├── Database/
    │   └── DatabaseManager.cs          # Database operations
    └── Models/
        └── MCPModels.cs                # Data models
```

### Technologies Used

- ASP.NET Core 8.0
- Npgsql 10.0.1 (PostgreSQL driver)
- Swashbuckle.AspNetCore (Swagger/OpenAPI)
- Model Context Protocol (MCP)

### Adding New Search Features

1. Add new methods to `DatabaseManager.cs`
2. Create endpoints in controllers
3. Update models in `MCPModels.cs`
4. Test with Swagger UI

## 📝 Examples

### Search for all user-related objects
```bash
curl "http://localhost:5004/api/search/metadata?keyword=user&schema=public"
```

### Full-text search in users table
```bash
curl -X POST "http://localhost:5004/api/search/fulltext" \
  -H "Content-Type: application/json" \
  -d '{
    "keyword": "john",
    "schema": "public",
    "tables": ["users"]
  }'
```

### Execute a custom query
```bash
curl -X POST "http://localhost:5004/api/database/query" \
  -H "Content-Type: application/json" \
  -d '{
    "sql": "SELECT table_name FROM information_schema.tables WHERE table_schema = '\''public'\'' LIMIT 5"
  }'
```

## 🔒 Security Notes

- **Never expose this server directly to the internet without authentication**
- Use environment variables for database credentials
- Consider adding authentication middleware for production
- Validate and sanitize all SQL inputs
- Use connection pooling for better performance
- Monitor database connections

## 🚧 Roadmap

- [ ] Add authentication and authorization
- [ ] Support for multiple database connections
- [ ] Query result caching
- [ ] Export results to CSV/JSON
- [ ] Advanced query builder
- [ ] Database schema visualization
- [ ] Support for other databases (MySQL, SQL Server)

## 📄 License

MIT License

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📧 Support

For issues and questions, please use the GitHub issue tracker.

---

**Built with ❤️ for the MCP community**

PostgreSQL 16.10 Compatible | ASP.NET Core 8.0 | Model Context Protocol
