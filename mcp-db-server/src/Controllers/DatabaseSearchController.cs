using Microsoft.AspNetCore.Mvc;
using MCPDatabaseServer.Database;
using MCPDatabaseServer.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCPDatabaseServer.Controllers
{
    [ApiController]
    [Route("api/search")]
    [Produces("application/json")]
    [McpServerToolType]
    public class DatabaseSearchController : ControllerBase
    {
        private readonly DatabaseManager _db;
        private readonly ILogger<DatabaseSearchController> _logger;

        public DatabaseSearchController(DatabaseManager db, ILogger<DatabaseSearchController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Search database objects (tables, columns, functions, procedures, constraints, data types)
        /// Similar to DBeaver's DB Metadata search
        /// </summary>
        [HttpGet("metadata")]
        [McpServerTool]
        [Description("Search for database objects: tables, columns, functions, procedures, constraints, and data types")]
        public async Task<IActionResult> SearchMetadata(
            [FromQuery] string keyword,
            [FromQuery] string? schema = "public",
            [FromQuery] bool searchTables = true,
            [FromQuery] bool searchColumns = true,
            [FromQuery] bool searchFunctions = true,
            [FromQuery] bool searchConstraints = true,
            [FromQuery] bool searchDataTypes = true)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { error = "Keyword is required" });

            try
            {
                _logger.LogInformation("Searching metadata for keyword: {Keyword} in schema: {Schema}", keyword, schema);
                
                var result = await _db.SearchMetadataAsync(keyword, schema, new SearchOptions
                {
                    SearchTables = searchTables,
                    SearchColumns = searchColumns,
                    SearchFunctions = searchFunctions,
                    SearchConstraints = searchConstraints,
                    SearchDataTypes = searchDataTypes
                });

                return Ok(new
                {
                    keyword,
                    schema,
                    timestamp = DateTime.UtcNow,
                    results = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching metadata");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Full-text search within database content (table data)
        /// Similar to DBeaver's DB Full-Text search
        /// </summary>
        [HttpPost("fulltext")]
        [McpServerTool]
        [Description("Search for text within database table content using PostgreSQL full-text search")]
        public async Task<IActionResult> SearchFullText(
            [FromBody] FullTextSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Keyword))
                return BadRequest(new { error = "Keyword is required" });

            try
            {
                _logger.LogInformation("Full-text search for: {Keyword}", request.Keyword);

                var results = await _db.FullTextSearchAsync(
                    request.Keyword,
                    request.Schema ?? "public",
                    request.Tables,
                    request.Columns,
                    request.MaxRowsPerTable ?? 100);

                return Ok(new
                {
                    keyword = request.Keyword,
                    schema = request.Schema,
                    timestamp = DateTime.UtcNow,
                    results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in full-text search");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Search within function/procedure source code
        /// Similar to searching within stored procedure bodies in DBeaver
        /// </summary>
        [HttpGet("functions/source")]
        [McpServerTool]
        [Description("Search within function and procedure source code")]
        public async Task<IActionResult> SearchFunctionSource(
            [FromQuery] string keyword,
            [FromQuery] string? schema = "public")
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { error = "Keyword is required" });

            try
            {
                _logger.LogInformation("Searching function source for: {Keyword}", keyword);

                var results = await _db.SearchFunctionSourceAsync(keyword, schema);

                return Ok(new
                {
                    keyword,
                    schema,
                    timestamp = DateTime.UtcNow,
                    results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching function source");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Advanced search combining multiple criteria
        /// </summary>
        [HttpPost("advanced")]
        [McpServerTool]
        [Description("Advanced search with multiple criteria across all database objects")]
        public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Keyword))
                return BadRequest(new { error = "Keyword is required" });

            try
            {
                _logger.LogInformation("Advanced search for: {Keyword}", request.Keyword);

                var results = await _db.AdvancedSearchAsync(request);

                return Ok(new
                {
                    request.Keyword,
                    request.Schema,
                    timestamp = DateTime.UtcNow,
                    results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced search");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
