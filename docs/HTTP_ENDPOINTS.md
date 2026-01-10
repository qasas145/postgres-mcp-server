# HTTP Endpoints Documentation

## Overview

The PostgreSQL Database MCP Server provides both JSON-RPC stdio interface and HTTP REST endpoints for database exploration and CRUD operations. The HTTP server listens on `http://localhost:5000`.

## Connection Pooling

The server uses **Npgsql connection pooling** with the following configuration:
- **Min Pool Size**: 1 connection (always available)
- **Max Pool Size**: 20 concurrent connections
- **Connection Reuse**: Automatic (connections returned to pool after use)
- **Pooling**: Enabled by default

This ensures optimal performance for concurrent requests while maintaining resource efficiency.

## Starting the Server

```bash
# Build the project
dotnet build

# Run the server
dotnet run
```

The server will:
1. Connect to PostgreSQL using `DATABASE_URL` or individual DB_* environment variables
2. Start the HTTP API on `http://localhost:5000`
3. Accept JSON-RPC requests on stdin for Cursor integration

## Base URL

```
http://localhost:5000
```

## API Endpoints

### 1. Health Check

Check server status and connectivity.

**Endpoint**: `GET /health`

**Parameters**: None

**Response**:
```json
{
  "status": "ok",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

**cURL Example**:
```bash
curl http://localhost:5000/health
```

---

### 2. Search Database Objects

Search for tables, columns, functions, and other database objects by keyword.

**Endpoint**: `GET /api/search`

**Query Parameters**:
- `keyword` (required): Search term to find in object names (e.g., `payment`, `user`, `order`)

**Response**:
```json
{
  "tables": [
    {
      "tableName": "payment_methods",
      "tableSchema": "public"
    },
    {
      "tableName": "payments",
      "tableSchema": "public"
    }
  ],
  "columns": [
    {
      "columnName": "payment_amount",
      "dataType": "numeric",
      "isNullable": false
    }
  ],
  "functions": [
    {
      "routineName": "calculate_payment",
      "routineType": "FUNCTION",
      "routineSchema": "public"
    }
  ]
}
```

**cURL Examples**:

Search for "payment" objects:
```bash
curl "http://localhost:5000/api/search?keyword=payment"
```

Search for "user" objects:
```bash
curl "http://localhost:5000/api/search?keyword=user"
```

---

### 3. List Tables in Schema

List all tables in a specific schema.

**Endpoint**: `GET /api/tables`

**Query Parameters**:
- `schema` (optional): Schema name. Default: `public`

**Response**:
```json
[
  {
    "tableName": "users",
    "tableSchema": "public"
  },
  {
    "tableName": "orders",
    "tableSchema": "public"
  },
  {
    "tableName": "payments",
    "tableSchema": "public"
  }
]
```

**cURL Examples**:

List all tables in public schema:
```bash
curl "http://localhost:5000/api/tables"
```

List all tables in a specific schema:
```bash
curl "http://localhost:5000/api/tables?schema=public"
```

---

### 4. Get Table Columns

Get all columns and their metadata for a specific table.

**Endpoint**: `GET /api/table/{name}`

**Path Parameters**:
- `name` (required): Table name

**Query Parameters**:
- `schema` (optional): Schema name. Default: `public`

**Response**:
```json
[
  {
    "columnName": "id",
    "dataType": "integer",
    "isNullable": false,
    "columnDefault": "nextval('users_id_seq'::regclass)"
  },
  {
    "columnName": "email",
    "dataType": "character varying",
    "isNullable": false,
    "columnDefault": null
  },
  {
    "columnName": "created_at",
    "dataType": "timestamp without time zone",
    "isNullable": true,
    "columnDefault": "CURRENT_TIMESTAMP"
  }
]
```

**cURL Examples**:

Get columns of "users" table:
```bash
curl "http://localhost:5000/api/table/users"
```

Get columns of a table in specific schema:
```bash
curl "http://localhost:5000/api/table/users?schema=public"
```

---

### 5. List Functions in Schema

List all database functions in a schema.

**Endpoint**: `GET /api/functions`

**Query Parameters**:
- `schema` (optional): Schema name. Default: `public`

**Response**:
```json
[
  {
    "routineName": "get_user_orders",
    "routineType": "FUNCTION",
    "routineSchema": "public",
    "dataType": "TABLE"
  },
  {
    "routineName": "calculate_total",
    "routineType": "FUNCTION",
    "routineSchema": "public",
    "dataType": "numeric"
  }
]
```

**cURL Examples**:

List all functions in public schema:
```bash
curl "http://localhost:5000/api/functions"
```

List all functions in a specific schema:
```bash
curl "http://localhost:5000/api/functions?schema=staging"
```

---

### 6. Execute SQL Query

Execute a SELECT query and retrieve results.

**Endpoint**: `POST /api/query`

**Content-Type**: `application/json`

**Body**:
```json
{
  "sql": "SELECT id, email FROM users LIMIT 10;"
}
```

**Response**:
```json
{
  "columns": [
    {
      "columnName": "id",
      "dataType": "integer"
    },
    {
      "columnName": "email",
      "dataType": "character varying"
    }
  ],
  "rows": [
    {
      "id": 1,
      "email": "user1@example.com"
    },
    {
      "id": 2,
      "email": "user2@example.com"
    }
  ]
}
```

**cURL Examples**:

Select all from a table:
```bash
curl -X POST http://localhost:5000/api/query \
  -H "Content-Type: application/json" \
  -d '{"sql":"SELECT * FROM users LIMIT 5;"}'
```

Join multiple tables:
```bash
curl -X POST http://localhost:5000/api/query \
  -H "Content-Type: application/json" \
  -d '{"sql":"SELECT u.id, u.email, o.id as order_id FROM users u LEFT JOIN orders o ON u.id = o.user_id LIMIT 10;"}'
```

Aggregate query:
```bash
curl -X POST http://localhost:5000/api/query \
  -H "Content-Type: application/json" \
  -d '{"sql":"SELECT status, COUNT(*) as count FROM orders GROUP BY status;"}'
```

---

## Configuration

### Environment Variables

The server supports both `DATABASE_URL` (preferred) and individual configuration variables:

#### Option 1: Using DATABASE_URL (Recommended for Production)

```bash
export DATABASE_URL="Server=34.173.168.133;Database=staging;Username=proximity-dev-user;Password=TrmOabTZ/nn)rqZ9;Timeout=900;CommandTimeout=900;"
```

#### Option 2: Using Individual Variables (Development)

```bash
export DB_HOST="34.173.168.133"
export DB_PORT="5432"
export DB_NAME="staging"
export DB_USER="proximity-dev-user"
export DB_PASSWORD="TrmOabTZ/nn)rqZ9"
```

### Example .env File

```env
# PostgreSQL Connection (Option 1 - Recommended)
DATABASE_URL=Server=34.173.168.133;Database=staging;Username=proximity-dev-user;Password=TrmOabTZ/nn)rqZ9;Timeout=900;CommandTimeout=900;

# Or PostgreSQL Connection (Option 2 - Individual Variables)
DB_HOST=34.173.168.133
DB_PORT=5432
DB_NAME=staging
DB_USER=proximity-dev-user
DB_PASSWORD=TrmOabTZ/nn)rqZ9
```

## Error Handling

All endpoints return consistent error responses:

**Error Response**:
```json
{
  "error": "Connection failed: could not connect to server: Name or service not known"
}
```

Common errors:
- **Connection Failed**: Database unreachable (check DATABASE_URL and network)
- **Invalid Schema**: Schema does not exist
- **Invalid SQL**: Syntax error in query
- **Timeout**: Query took too long (900s default)

## Performance Optimization

### Connection Pooling Benefits

- **Reusable Connections**: Reduces connection overhead
- **Concurrent Requests**: Up to 20 simultaneous database operations
- **Automatic Cleanup**: Idle connections are automatically returned to pool
- **Long Command Timeout**: 900 seconds (15 minutes) for long-running queries

### Best Practices

1. **Use Connection Pooling**: Always reuse connections (automatic in this server)
2. **Limit Results**: Use `LIMIT` in SELECT queries for large datasets
3. **Use Indexes**: Ensure table columns used in WHERE clauses are indexed
4. **Batch Operations**: Group multiple queries where possible
5. **Monitor Timeout**: Set appropriate CommandTimeout in DATABASE_URL

## Integration with Cursor

To use these HTTP endpoints in Cursor:

1. **Start the Server**:
   ```bash
   dotnet run
   ```

2. **Use in Cursor Tools**:
   - Call `http://localhost:5000/api/search?keyword=payment` to find payment-related objects
   - Call `http://localhost:5000/api/table/users` to explore table structure
   - Call `http://localhost:5000/api/query` with POST for complex data analysis

3. **Example Cursor Integration**:
   ```python
   import requests
   
   # Search for database objects
   response = requests.get("http://localhost:5000/api/search?keyword=payment")
   objects = response.json()
   
   # Get table structure
   response = requests.get("http://localhost:5000/api/table/users")
   columns = response.json()
   
   # Execute query
   response = requests.post(
       "http://localhost:5000/api/query",
       json={"sql": "SELECT * FROM users LIMIT 5;"}
   )
   results = response.json()
   ```

## Troubleshooting

### HTTP Server Won't Start

```
Error: Port 5000 already in use
```

Solution: Change port in Program.cs or kill process using port 5000:
```bash
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

### Database Connection Failed

```
Error: could not connect to server: No such file or directory
```

Solution: Check `DATABASE_URL` or individual environment variables are correctly set.

### Query Timeout

```
Error: Timeout expired. The timeout period elapsed prior to completion of the operation
```

Solution: Optimize query or increase CommandTimeout in DATABASE_URL.

## Summary

| Endpoint | Method | Purpose | Parameters |
|----------|--------|---------|-----------|
| `/health` | GET | Check server status | None |
| `/api/search` | GET | Search database objects | keyword |
| `/api/tables` | GET | List tables | schema (optional) |
| `/api/table/{name}` | GET | Get table columns | schema (optional) |
| `/api/functions` | GET | List functions | schema (optional) |
| `/api/query` | POST | Execute SQL query | sql (in body) |

All endpoints return JSON and support cross-origin requests for Cursor integration.
