using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPDatabaseServer.Models;

namespace MCPDatabaseServer.Database
{
    public class DatabaseManager
    {
        private NpgsqlDataSource _dataSource;

        public DatabaseManager()
        {
            var connectionString = BuildConnectionString();
            _dataSource = NpgsqlDataSource.Create(connectionString);
        }

        private string BuildConnectionString()
        {
            // Try to get from DATABASE_URL env variable first
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrEmpty(databaseUrl))
            {
                return databaseUrl;
            }

            // Fallback to individual environment variables
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
            var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

            var connectionString = $"Server={host};Port={port};Database={database};Username={username};Password={password};Timeout=900;CommandTimeout=900";
            return connectionString;
        }

        public async Task ConnectAsync()
        {
            try
            {
                await using var connection = await _dataSource.OpenConnectionAsync();
                Console.WriteLine("✓ Connected to PostgreSQL database");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to connect to database: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Dictionary<string, object>>> QueryAsync(string sql, params object[] parameters)
        {
            try
            {
                await using var command = _dataSource.CreateCommand(sql);
                for (int i = 0; i < parameters.Length; i++)
                {
                    command.Parameters.AddWithValue($"@p{i + 1}", parameters[i] ?? DBNull.Value);
                    command.CommandText = command.CommandText.Replace($"${i + 1}", $"@p{i + 1}");
                }

                await using var reader = await command.ExecuteReaderAsync();
                var result = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    result.Add(row);
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Query error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetSchemasAsync()
        {
            const string sql = @"
                SELECT schema_name 
                FROM information_schema.schemata 
                WHERE schema_name NOT IN ('pg_catalog', 'information_schema')
                ORDER BY schema_name";

            var rows = await QueryAsync(sql);
            return rows.Select(r => r["schema_name"].ToString()).ToList();
        }

        public async Task<List<Table>> GetTablesAsync(string schema = "public")
        {
            const string sql = @"
                SELECT table_name, table_schema
                FROM information_schema.tables
                WHERE table_schema = $1
                ORDER BY table_name";

            var rows = await QueryAsync(sql, schema);
            return rows.Select(r => new Table
            {
                TableName = r["table_name"].ToString(),
                TableSchema = r["table_schema"].ToString()
            }).ToList();
        }

        public async Task<List<Column>> GetTableColumnsAsync(string schema, string tableName)
        {
            const string sql = @"
                SELECT column_name, data_type, is_nullable, column_default, ordinal_position
                FROM information_schema.columns
                WHERE table_schema = $1 AND table_name = $2
                ORDER BY ordinal_position";

            var rows = await QueryAsync(sql, schema, tableName);
            return rows.Select(r => new Column
            {
                ColumnName = r["column_name"].ToString(),
                DataType = r["data_type"].ToString(),
                IsNullable = r["is_nullable"].ToString(),
                ColumnDefault = r["column_default"]?.ToString(),
                OrdinalPosition = Convert.ToInt32(r["ordinal_position"])
            }).ToList();
        }

        public async Task<string> GetTableDefinitionAsync(string schema, string tableName)
        {
            const string sql = @"
                SELECT 
                    'CREATE TABLE ' || $1 || '.' || $2 || ' (' || 
                    array_to_string(
                        array_agg(
                            '  ' || column_name || ' ' || data_type || 
                            CASE WHEN is_nullable = 'NO' THEN ' NOT NULL' ELSE '' END
                        ), ',\n'
                    ) || '\n);' as definition
                FROM information_schema.columns
                WHERE table_schema = $1 AND table_name = $2";

            var rows = await QueryAsync(sql, schema, tableName);
            return rows.FirstOrDefault()?["definition"]?.ToString() ?? "";
        }

        public async Task<List<Function>> GetFunctionsAsync(string schema = "public")
        {
            const string sql = @"
                SELECT routine_name, routine_type, routine_schema, data_type
                FROM information_schema.routines
                WHERE routine_schema = $1
                ORDER BY routine_name";

            var rows = await QueryAsync(sql, schema);
            return rows.Select(r => new Function
            {
                RoutineName = r["routine_name"]?.ToString(),
                RoutineType = r["routine_type"]?.ToString(),
                RoutineSchema = r["routine_schema"]?.ToString(),
                DataType = r["data_type"]?.ToString()
            }).ToList();
        }

        public async Task<string> GetFunctionDefinitionAsync(string schema, string functionName)
        {
            const string sql = @"
                SELECT pg_get_functiondef(p.oid) as definition
                FROM pg_proc p
                JOIN pg_namespace n ON p.pronamespace = n.oid
                WHERE n.nspname = $1 AND p.proname = $2
                LIMIT 1";

            var rows = await QueryAsync(sql, schema, functionName);
            return rows.FirstOrDefault()?["definition"]?.ToString() ?? "";
        }

        public async Task<List<ForeignKey>> GetTableForeignKeysAsync(string schema, string tableName)
        {
            const string sql = @"
                SELECT
                    tc.constraint_name,
                    kcu.column_name,
                    ccu.table_schema AS referenced_table_schema,
                    ccu.table_name AS referenced_table_name,
                    ccu.column_name AS referenced_column_name
                FROM information_schema.table_constraints AS tc
                JOIN information_schema.key_column_usage AS kcu
                    ON tc.constraint_name = kcu.constraint_name
                JOIN information_schema.constraint_column_usage AS ccu
                    ON ccu.constraint_name = tc.constraint_name
                WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_schema = $1 AND tc.table_name = $2";

            var rows = await QueryAsync(sql, schema, tableName);
            return rows.Select(r => new ForeignKey
            {
                ConstraintName = r["constraint_name"].ToString(),
                ColumnName = r["column_name"].ToString(),
                ReferencedTableSchema = r["referenced_table_schema"].ToString(),
                ReferencedTableName = r["referenced_table_name"].ToString(),
                ReferencedColumnName = r["referenced_column_name"].ToString()
            }).ToList();
        }

        public async Task<SearchResult> SearchObjectsAsync(string keyword, string schema = "public")
        {
            var result = new SearchResult { Keyword = keyword };

            // Search tables
            const string tablesSql = @"
                SELECT table_name, table_schema
                FROM information_schema.tables
                WHERE table_schema = $1 AND table_name ILIKE $2";
            var tableRows = await QueryAsync(tablesSql, schema, $"%{keyword}%");
            result.Tables = tableRows.Select(r => new Table
            {
                TableName = r["table_name"].ToString(),
                TableSchema = r["table_schema"].ToString()
            }).ToList();

            // Search columns
            const string columnsSql = @"
                SELECT column_name, table_name, table_schema, data_type, is_nullable, column_default, ordinal_position
                FROM information_schema.columns
                WHERE table_schema = $1 AND column_name ILIKE $2";
            var columnRows = await QueryAsync(columnsSql, schema, $"%{keyword}%");
            result.Columns = columnRows.Select(r => new Column
            {
                ColumnName = r["column_name"].ToString(),
                DataType = r["data_type"].ToString(),
                IsNullable = r["is_nullable"].ToString(),
                ColumnDefault = r["column_default"]?.ToString()
            }).ToList();

            // Search functions (including body)
            const string functionsSql = @"
                SELECT 
                    p.proname AS routine_name,
                    CASE 
                        WHEN p.prokind = 'f' THEN 'FUNCTION'
                        WHEN p.prokind = 'p' THEN 'PROCEDURE'
                        WHEN p.prokind = 'w' THEN 'WINDOW'
                        ELSE 'FUNCTION'
                    END AS routine_type,
                    n.nspname AS routine_schema,
                    pg_get_functiondef(p.oid) AS definition,
                    p.prosrc AS source_code
                FROM pg_proc p
                JOIN pg_namespace n ON p.pronamespace = n.oid
                WHERE n.nspname = $1 AND (p.proname ILIKE $2 OR p.prosrc ILIKE $2)";
            var functionRows = await QueryAsync(functionsSql, schema, $"%{keyword}%");
            result.Functions = functionRows.Select(r => new Function
            {
                RoutineName = r["routine_name"].ToString(),
                RoutineType = r["routine_type"].ToString(),
                RoutineSchema = r["routine_schema"].ToString(),
                DataType = "function",
                SourceCode = r["source_code"]?.ToString() ?? "",
                Definition = r["definition"]?.ToString() ?? ""
            }).ToList();

            return result;
        }

        /// <summary>
        /// Enhanced metadata search with all database objects
        /// Similar to DBeaver's DB Metadata search
        /// </summary>
        public async Task<SearchResult> SearchMetadataAsync(string keyword, string schema, SearchOptions options)
        {
            var result = new SearchResult { Keyword = keyword };

            // Search tables
            if (options.SearchTables)
            {
                const string tablesSql = @"
                    SELECT DISTINCT t.table_name, t.table_schema
                    FROM information_schema.tables t
                    WHERE t.table_schema = $1 AND t.table_name ILIKE $2
                    ORDER BY t.table_name";
                var tableRows = await QueryAsync(tablesSql, schema, $"%{keyword}%");
                result.Tables = tableRows.Select(r => new Table
                {
                    TableName = r["table_name"].ToString(),
                    TableSchema = r["table_schema"].ToString()
                }).ToList();
            }

            // Search columns
            if (options.SearchColumns)
            {
                const string columnsSql = @"
                    SELECT DISTINCT 
                        c.column_name, 
                        c.table_name,
                        c.table_schema,
                        c.data_type, 
                        c.is_nullable, 
                        c.column_default, 
                        c.ordinal_position
                    FROM information_schema.columns c
                    WHERE c.table_schema = $1 AND c.column_name ILIKE $2
                    ORDER BY c.table_name, c.ordinal_position";
                var columnRows = await QueryAsync(columnsSql, schema, $"%{keyword}%");
                result.Columns = columnRows.Select(r => new Column
                {
                    ColumnName = r["column_name"].ToString(),
                    TableName = r["table_name"].ToString(),
                    TableSchema = r["table_schema"].ToString(),
                    DataType = r["data_type"].ToString(),
                    IsNullable = r["is_nullable"].ToString(),
                    ColumnDefault = r["column_default"]?.ToString(),
                    OrdinalPosition = Convert.ToInt32(r["ordinal_position"])
                }).ToList();
            }

            // Search functions and procedures
            if (options.SearchFunctions)
            {
                const string functionsSql = @"
                    SELECT 
                        p.proname AS routine_name,
                        CASE 
                            WHEN p.prokind = 'f' THEN 'FUNCTION'
                            WHEN p.prokind = 'p' THEN 'PROCEDURE'
                            WHEN p.prokind = 'a' THEN 'AGGREGATE'
                            WHEN p.prokind = 'w' THEN 'WINDOW'
                            ELSE 'FUNCTION'
                        END AS routine_type,
                        n.nspname AS routine_schema,
                        pg_catalog.pg_get_function_result(p.oid) AS data_type
                    FROM pg_proc p
                    JOIN pg_namespace n ON p.pronamespace = n.oid
                    WHERE n.nspname = $1 AND p.proname ILIKE $2
                    ORDER BY p.proname";
                var functionRows = await QueryAsync(functionsSql, schema, $"%{keyword}%");
                result.Functions = functionRows.Select(r => new Function
                {
                    RoutineName = r["routine_name"].ToString(),
                    RoutineType = r["routine_type"].ToString(),
                    RoutineSchema = r["routine_schema"].ToString(),
                    DataType = r["data_type"]?.ToString()
                }).ToList();
            }

            // Search constraints
            if (options.SearchConstraints)
            {
                const string constraintsSql = @"
                    SELECT 
                        tc.constraint_name,
                        tc.constraint_type,
                        tc.table_name,
                        tc.table_schema,
                        kcu.column_name
                    FROM information_schema.table_constraints tc
                    LEFT JOIN information_schema.key_column_usage kcu 
                        ON tc.constraint_name = kcu.constraint_name 
                        AND tc.table_schema = kcu.table_schema
                    WHERE tc.table_schema = $1 AND tc.constraint_name ILIKE $2
                    ORDER BY tc.constraint_name";
                var constraintRows = await QueryAsync(constraintsSql, schema, $"%{keyword}%");
                result.Constraints = constraintRows.Select(r => new Constraint
                {
                    ConstraintName = r["constraint_name"].ToString(),
                    ConstraintType = r["constraint_type"].ToString(),
                    TableName = r["table_name"].ToString(),
                    TableSchema = r["table_schema"].ToString(),
                    ColumnName = r["column_name"]?.ToString()
                }).ToList();
            }

            // Search data types
            if (options.SearchDataTypes)
            {
                const string dataTypesSql = @"
                    SELECT 
                        t.typname AS type_name,
                        n.nspname AS type_schema,
                        CASE 
                            WHEN t.typcategory = 'A' THEN 'Array'
                            WHEN t.typcategory = 'B' THEN 'Boolean'
                            WHEN t.typcategory = 'C' THEN 'Composite'
                            WHEN t.typcategory = 'D' THEN 'Date/Time'
                            WHEN t.typcategory = 'E' THEN 'Enum'
                            WHEN t.typcategory = 'G' THEN 'Geometric'
                            WHEN t.typcategory = 'I' THEN 'Network Address'
                            WHEN t.typcategory = 'N' THEN 'Numeric'
                            WHEN t.typcategory = 'P' THEN 'Pseudo-types'
                            WHEN t.typcategory = 'R' THEN 'Range'
                            WHEN t.typcategory = 'S' THEN 'String'
                            WHEN t.typcategory = 'T' THEN 'Timespan'
                            WHEN t.typcategory = 'U' THEN 'User-defined'
                            WHEN t.typcategory = 'V' THEN 'Bit-string'
                            WHEN t.typcategory = 'X' THEN 'Unknown'
                            ELSE 'Other'
                        END AS type_category
                    FROM pg_type t
                    JOIN pg_namespace n ON t.typnamespace = n.oid
                    WHERE n.nspname = $1 AND t.typname ILIKE $2
                    ORDER BY t.typname";
                var dataTypeRows = await QueryAsync(dataTypesSql, schema, $"%{keyword}%");
                result.DataTypes = dataTypeRows.Select(r => new DataType
                {
                    TypeName = r["type_name"].ToString(),
                    TypeSchema = r["type_schema"].ToString(),
                    TypeCategory = r["type_category"]?.ToString()
                }).ToList();
            }

            return result;
        }

        /// <summary>
        /// Full-text search within table data
        /// Similar to DBeaver's DB Full-Text search
        /// </summary>
        public async Task<List<FullTextSearchResult>> FullTextSearchAsync(
            string keyword, 
            string schema, 
            List<string>? specificTables = null,
            List<string>? specificColumns = null,
            int maxRowsPerTable = 100)
        {
            var results = new List<FullTextSearchResult>();

            // Get tables to search
            var tables = specificTables != null && specificTables.Any()
                ? await GetTablesAsync(schema)
                    .ContinueWith(t => t.Result.Where(tbl => specificTables.Contains(tbl.TableName)).ToList())
                : await GetTablesAsync(schema);

            foreach (var table in tables)
            {
                try
                {
                    // Get text/varchar columns for this table
                    var columns = await GetTableColumnsAsync(schema, table.TableName);
                    var searchableColumns = columns
                        .Where(c => c.DataType.Contains("char") || c.DataType.Contains("text"))
                        .Where(c => specificColumns == null || specificColumns.Contains(c.ColumnName))
                        .ToList();

                    if (!searchableColumns.Any())
                        continue;

                    // Build dynamic WHERE clause for each text column
                    var whereClauses = searchableColumns
                        .Select((c, i) => $"{c.ColumnName}::text ILIKE $1")
                        .ToList();

                    if (!whereClauses.Any())
                        continue;

                    var sql = $@"
                        SELECT *
                        FROM {schema}.{table.TableName}
                        WHERE {string.Join(" OR ", whereClauses)}
                        LIMIT {maxRowsPerTable}";

                    var rows = await QueryAsync(sql, $"%{keyword}%");

                    if (rows.Any())
                    {
                        results.Add(new FullTextSearchResult
                        {
                            TableName = table.TableName,
                            TableSchema = schema,
                            Matches = rows,
                            MatchCount = rows.Count
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error searching table {table.TableName}: {ex.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// Search within function/procedure source code
        /// </summary>
        public async Task<List<Function>> SearchFunctionSourceAsync(string keyword, string schema)
        {
            const string sql = @"
                SELECT 
                    p.proname AS routine_name,
                    CASE 
                        WHEN p.prokind = 'f' THEN 'FUNCTION'
                        WHEN p.prokind = 'p' THEN 'PROCEDURE'
                        WHEN p.prokind = 'a' THEN 'AGGREGATE'
                        WHEN p.prokind = 'w' THEN 'WINDOW'
                        ELSE 'FUNCTION'
                    END AS routine_type,
                    n.nspname AS routine_schema,
                    pg_catalog.pg_get_function_result(p.oid) AS data_type,
                    pg_get_functiondef(p.oid) AS definition,
                    p.prosrc AS source_code
                FROM pg_proc p
                JOIN pg_namespace n ON p.pronamespace = n.oid
                WHERE n.nspname = $1 
                    AND (p.prosrc ILIKE $2 OR pg_get_functiondef(p.oid) ILIKE $2)
                ORDER BY p.proname";

            var rows = await QueryAsync(sql, schema, $"%{keyword}%");
            return rows.Select(r => new Function
            {
                RoutineName = r["routine_name"].ToString(),
                RoutineType = r["routine_type"].ToString(),
                RoutineSchema = r["routine_schema"].ToString(),
                DataType = r["data_type"]?.ToString(),
                Definition = r["definition"]?.ToString() ?? "",
                SourceCode = r["source_code"]?.ToString() ?? ""
            }).ToList();
        }

        /// <summary>
        /// Advanced search with multiple criteria
        /// </summary>
        public async Task<SearchResult> AdvancedSearchAsync(AdvancedSearchRequest request)
        {
            var result = new SearchResult { Keyword = request.Keyword };
            var pattern = request.CaseSensitive ? $"%{request.Keyword}%" : $"%{request.Keyword}%";
            var comparison = request.CaseSensitive ? "LIKE" : "ILIKE";

            // Determine which object types to search
            var searchAll = request.ObjectTypes == null || !request.ObjectTypes.Any();
            var searchTables = searchAll || request.ObjectTypes.Contains("table");
            var searchColumns = searchAll || request.ObjectTypes.Contains("column");
            var searchFunctions = searchAll || request.ObjectTypes.Contains("function");
            var searchConstraints = searchAll || request.ObjectTypes.Contains("constraint");

            if (request.SearchInNames && searchTables)
            {
                var tablesSql = $@"
                    SELECT table_name, table_schema
                    FROM information_schema.tables
                    WHERE table_schema = $1 AND table_name {comparison} $2
                    ORDER BY table_name";
                var tableRows = await QueryAsync(tablesSql, request.Schema, pattern);
                result.Tables = tableRows.Select(r => new Table
                {
                    TableName = r["table_name"].ToString(),
                    TableSchema = r["table_schema"].ToString()
                }).ToList();
            }

            if (request.SearchInNames && searchColumns)
            {
                var columnsSql = $@"
                    SELECT DISTINCT 
                        column_name, 
                        table_name,
                        table_schema,
                        data_type, 
                        is_nullable, 
                        column_default, 
                        ordinal_position
                    FROM information_schema.columns
                    WHERE table_schema = $1 AND column_name {comparison} $2
                    ORDER BY table_name, ordinal_position";
                var columnRows = await QueryAsync(columnsSql, request.Schema, pattern);
                result.Columns = columnRows.Select(r => new Column
                {
                    ColumnName = r["column_name"].ToString(),
                    TableName = r["table_name"].ToString(),
                    TableSchema = r["table_schema"].ToString(),
                    DataType = r["data_type"].ToString(),
                    IsNullable = r["is_nullable"].ToString(),
                    ColumnDefault = r["column_default"]?.ToString(),
                    OrdinalPosition = Convert.ToInt32(r["ordinal_position"])
                }).ToList();
            }

            if (searchFunctions && (request.SearchInNames || request.SearchInDefinitions))
            {
                var whereClause = request.SearchInNames && request.SearchInDefinitions
                    ? $"(p.proname {comparison} $2 OR p.prosrc {comparison} $2)"
                    : request.SearchInNames
                        ? $"p.proname {comparison} $2"
                        : $"p.prosrc {comparison} $2";

                var functionsSql = $@"
                    SELECT 
                        p.proname AS routine_name,
                        CASE 
                            WHEN p.prokind = 'f' THEN 'FUNCTION'
                            WHEN p.prokind = 'p' THEN 'PROCEDURE'
                            WHEN p.prokind = 'a' THEN 'AGGREGATE'
                            WHEN p.prokind = 'w' THEN 'WINDOW'
                            ELSE 'FUNCTION'
                        END AS routine_type,
                        n.nspname AS routine_schema,
                        pg_catalog.pg_get_function_result(p.oid) AS data_type,
                        pg_get_functiondef(p.oid) AS definition
                    FROM pg_proc p
                    JOIN pg_namespace n ON p.pronamespace = n.oid
                    WHERE n.nspname = $1 AND {whereClause}
                    ORDER BY p.proname";
                var functionRows = await QueryAsync(functionsSql, request.Schema, pattern);
                result.Functions = functionRows.Select(r => new Function
                {
                    RoutineName = r["routine_name"].ToString(),
                    RoutineType = r["routine_type"].ToString(),
                    RoutineSchema = r["routine_schema"].ToString(),
                    DataType = r["data_type"]?.ToString(),
                    Definition = r["definition"]?.ToString()
                }).ToList();
            }

            return result;
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, params object[] parameters)
        {
            try
            {
                await using var command = _dataSource.CreateCommand(sql);
                for (int i = 0; i < parameters.Length; i++)
                {
                    command.Parameters.AddWithValue($"@p{i + 1}", parameters[i] ?? DBNull.Value);
                    command.CommandText = command.CommandText.Replace($"${i + 1}", $"@p{i + 1}");
                }

                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ExecuteNonQuery error: {ex.Message}");
                throw;
            }
        }

        public async Task<(List<(string ColumnName, string DataType)> Columns, List<Dictionary<string, object>> Rows)> QueryWithSchemaAsync(string sql)
        {
            try
            {
                await using var command = _dataSource.CreateCommand(sql);
                await using var reader = await command.ExecuteReaderAsync();

                var columns = new List<(string ColumnName, string DataType)>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var typeName = reader.GetDataTypeName(i);
                    columns.Add((name, typeName));
                }

                var rows = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    rows.Add(row);
                }

                return (columns, rows);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QueryWithSchema error: {ex.Message}");
                throw;
            }
        }

        public async Task CloseAsync()
        {
            if (_dataSource != null)
            {
                await _dataSource.DisposeAsync();
                Console.WriteLine("✓ Database connection closed");
            }
        }
    }
}
