using System.ComponentModel;
using System.Text.Json;
using MCPDatabaseServer.Database;
using ModelContextProtocol.Server;
var builder = WebApplication.CreateBuilder(args);

// Add MCP Server with HTTP transport
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();


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

// Add database manager as singleton
builder.Services.AddSingleton<DatabaseManager>();

var app = builder.Build();

// Initialize database connection
var db = app.Services.GetRequiredService<DatabaseManager>();
Console.WriteLine("🚀 Starting PostgreSQL Database MCP Server...\n");

try
{
    await db.ConnectAsync();
    Console.WriteLine("✅ Database connection established successfully!\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to database: {ex.Message}");
    Environment.Exit(1);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PostgreSQL Database MCP Server API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors();

app.MapControllers();
app.MapMcp();

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("🎯 PostgreSQL Database MCP Server is running!");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine($"📍 MCP over HTTP: http://localhost:5004/mcp");
Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

app.Run();
