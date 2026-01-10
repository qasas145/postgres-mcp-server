using Microsoft.AspNetCore.Mvc;
using MCPDatabaseServer.Database;
using System.ComponentModel;

namespace MCPDatabaseServer.Controllers
{
    [ApiController]
    [Route("api/database")]
    [Produces("application/json")]
    public class DatabaseController : ControllerBase
    {
        private readonly DatabaseManager _db;
        private readonly ILogger<DatabaseController> _logger;

        public DatabaseController(DatabaseManager db, ILogger<DatabaseController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Get all available schemas
        /// </summary>
        [HttpGet("schemas")]
        [Description("List all database schemas")]
        public async Task<IActionResult> GetSchemas()
        {
            try
            {
                var schemas = await _db.GetSchemasAsync();
                return Ok(new { schemas, count = schemas.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schemas");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all tables in a schema
        /// </summary>
        [HttpGet("tables")]
        [Description("List all tables in a schema")]
        public async Task<IActionResult> GetTables([FromQuery] string? schema = "public")
        {
            try
            {
                var tables = await _db.GetTablesAsync(schema);
                return Ok(new
                {
                    schema,
                    tables = tables.Select(t => new
                    {
                        t.TableName,
                        t.TableSchema
                    }),
                    count = tables.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tables");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get table structure (columns)
        /// </summary>
        [HttpGet("tables/{tableName}")]
        [Description("Get table structure and columns")]
        public async Task<IActionResult> GetTableColumns(string tableName, [FromQuery] string? schema = "public")
        {
            try
            {
                var columns = await _db.GetTableColumnsAsync(schema, tableName);
                return Ok(new
                {
                    schema,
                    tableName,
                    columns = columns.Select(c => new
                    {
                        c.ColumnName,
                        c.DataType,
                        c.IsNullable,
                        c.ColumnDefault,
                        c.OrdinalPosition
                    }),
                    count = columns.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table columns");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get table DDL definition
        /// </summary>
        [HttpGet("tables/{tableName}/definition")]
        [Description("Get table CREATE statement")]
        public async Task<IActionResult> GetTableDefinition(string tableName, [FromQuery] string? schema = "public")
        {
            try
            {
                var definition = await _db.GetTableDefinitionAsync(schema, tableName);
                return Ok(new { schema, tableName, definition });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table definition");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all functions/procedures in a schema
        /// </summary>
        [HttpGet("functions")]
        [Description("List all functions and procedures in a schema")]
        public async Task<IActionResult> GetFunctions([FromQuery] string? schema = "public")
        {
            try
            {
                var functions = await _db.GetFunctionsAsync(schema);
                return Ok(new
                {
                    schema,
                    functions = functions.Select(f => new
                    {
                        f.RoutineName,
                        f.RoutineType,
                        f.RoutineSchema,
                        f.DataType
                    }),
                    count = functions.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting functions");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get function/procedure definition
        /// </summary>
        [HttpGet("functions/{functionName}")]
        [Description("Get function or procedure definition")]
        public async Task<IActionResult> GetFunctionDefinition(string functionName, [FromQuery] string? schema = "public")
        {
            try
            {
                var definition = await _db.GetFunctionDefinitionAsync(schema, functionName);
                return Ok(new { schema, functionName, definition });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting function definition");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Execute a custom SQL query
        /// </summary>
        [HttpPost("query")]
        [Description("Execute a custom SQL query and return results")]
        public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Sql))
                return BadRequest(new { error = "SQL query is required" });

            try
            {
                _logger.LogInformation("Executing query: {Sql}", request.Sql);

                var (columns, rows) = await _db.QueryWithSchemaAsync(request.Sql);

                return Ok(new
                {
                    columns = columns.Select(c => new { c.ColumnName, c.DataType }),
                    rows,
                    rowCount = rows.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query");
                return StatusCode(500, new { error = ex.Message, query = request.Sql });
            }
        }

        /// <summary>
        /// Get table foreign keys
        /// </summary>
        [HttpGet("tables/{tableName}/foreignkeys")]
        [Description("Get foreign key constraints for a table")]
        public async Task<IActionResult> GetForeignKeys(string tableName, [FromQuery] string? schema = "public")
        {
            try
            {
                var foreignKeys = await _db.GetTableForeignKeysAsync(schema, tableName);
                return Ok(new
                {
                    schema,
                    tableName,
                    foreignKeys = foreignKeys.Select(fk => new
                    {
                        fk.ConstraintName,
                        fk.ColumnName,
                        fk.ReferencedTableSchema,
                        fk.ReferencedTableName,
                        fk.ReferencedColumnName
                    }),
                    count = foreignKeys.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting foreign keys");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class QueryRequest
    {
        public string Sql { get; set; } = string.Empty;
    }
}
