using MCPDatabaseServer.Database;
using System.Text.Json;

// Load environment variables
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.UseUrls("http://localhost:5004");

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PostgreSQL Database MCP Server API",
        Version = "v1",
        Description = @"A Model Context Protocol (MCP) server for PostgreSQL database operations.
        
Features:
- **DB Metadata Search**: Search tables, columns, functions, procedures, constraints, and data types
- **DB Full-Text Search**: Search within table data content
- **Function Source Search**: Search within stored procedure/function code
- **Advanced Search**: Combine multiple search criteria
- **Database Management**: Query tables, schemas, functions, and execute SQL

Similar to DBeaver's search capabilities."
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register DatabaseManager as singleton
builder.Services.AddSingleton<DatabaseManager>();

var app = builder.Build();

// Initialize database connection
var dbManager = app.Services.GetRequiredService<DatabaseManager>();
Console.WriteLine("🚀 Starting PostgreSQL Database MCP Server...\n");

try
{
    await dbManager.ConnectAsync();
    Console.WriteLine("✅ Database connection established successfully!\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to database: {ex.Message}\n");
    Console.WriteLine("Please check your database configuration in environment variables:");
    Console.WriteLine("  - DATABASE_URL or");
    Console.WriteLine("  - DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD");
    Environment.Exit(1);
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PostgreSQL MCP Server API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at root
    });
}

app.UseCors();
app.UseRouting();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Json(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    database = "connected",
    server = "PostgreSQL Database MCP Server v1.0"
})).WithTags("Health");

// Root endpoint
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("🎯 PostgreSQL Database MCP Server is running!");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine($"📍 Server URL: http://localhost:5004");
Console.WriteLine($"📚 API Documentation: http://localhost:5004/swagger");
Console.WriteLine($"💚 Health Check: http://localhost:5004/health");
Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

Console.WriteLine("Available Search Endpoints:");
Console.WriteLine("  🔍 GET  /api/search/metadata        - Search database objects");
Console.WriteLine("  📝 POST /api/search/fulltext        - Full-text search in data");
Console.WriteLine("  🔧 GET  /api/search/functions/source- Search in function code");
Console.WriteLine("  🚀 POST /api/search/advanced        - Advanced multi-criteria search");
Console.WriteLine();
Console.WriteLine("Database Management Endpoints:");
Console.WriteLine("  📊 GET  /api/database/schemas       - List all schemas");
Console.WriteLine("  📋 GET  /api/database/tables        - List all tables");
Console.WriteLine("  🔢 GET  /api/database/tables/{name} - Get table structure");
Console.WriteLine("  ⚙️  GET  /api/database/functions    - List all functions");
Console.WriteLine("  💻 POST /api/database/query         - Execute SQL query");
Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

app.Run();
