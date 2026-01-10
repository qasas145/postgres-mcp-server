using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MCPDatabaseServer.Models
{
    // Database Models
    public class Table
    {
        [JsonPropertyName("table_name")]
        public string TableName { get; set; }

        [JsonPropertyName("table_schema")]
        public string TableSchema { get; set; }
    }

    public class Column
    {
        [JsonPropertyName("column_name")]
        public string ColumnName { get; set; }

        [JsonPropertyName("data_type")]
        public string DataType { get; set; }

        [JsonPropertyName("is_nullable")]
        public string IsNullable { get; set; }

        [JsonPropertyName("column_default")]
        public string? ColumnDefault { get; set; }

        [JsonPropertyName("ordinal_position")]
        public int OrdinalPosition { get; set; }

        [JsonPropertyName("table_name")]
        public string? TableName { get; set; }

        [JsonPropertyName("table_schema")]
        public string? TableSchema { get; set; }
    }

    public class Function
    {
        [JsonPropertyName("routine_name")]
        public string RoutineName { get; set; }

        [JsonPropertyName("routine_type")]
        public string RoutineType { get; set; }

        [JsonPropertyName("routine_schema")]
        public string RoutineSchema { get; set; }

        [JsonPropertyName("data_type")]
        public string? DataType { get; set; }

        [JsonPropertyName("definition")]
        public string? Definition { get; set; }

        [JsonPropertyName("source_code")]
        public string? SourceCode { get; set; }
    }

    public class ForeignKey
    {
        [JsonPropertyName("constraint_name")]
        public string ConstraintName { get; set; }

        [JsonPropertyName("column_name")]
        public string ColumnName { get; set; }

        [JsonPropertyName("referenced_table_schema")]
        public string ReferencedTableSchema { get; set; }

        [JsonPropertyName("referenced_table_name")]
        public string ReferencedTableName { get; set; }

        [JsonPropertyName("referenced_column_name")]
        public string ReferencedColumnName { get; set; }
    }

    public class Constraint
    {
        [JsonPropertyName("constraint_name")]
        public string ConstraintName { get; set; }

        [JsonPropertyName("constraint_type")]
        public string ConstraintType { get; set; }

        [JsonPropertyName("table_name")]
        public string TableName { get; set; }

        [JsonPropertyName("table_schema")]
        public string TableSchema { get; set; }

        [JsonPropertyName("column_name")]
        public string? ColumnName { get; set; }
    }

    public class DataType
    {
        [JsonPropertyName("type_name")]
        public string TypeName { get; set; }

        [JsonPropertyName("type_schema")]
        public string TypeSchema { get; set; }

        [JsonPropertyName("type_category")]
        public string? TypeCategory { get; set; }
    }

    public class SearchResult
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; }

        [JsonPropertyName("tables")]
        public List<Table> Tables { get; set; } = new();

        [JsonPropertyName("columns")]
        public List<Column> Columns { get; set; } = new();

        [JsonPropertyName("functions")]
        public List<Function> Functions { get; set; } = new();

        [JsonPropertyName("constraints")]
        public List<Constraint> Constraints { get; set; } = new();

        [JsonPropertyName("data_types")]
        public List<DataType> DataTypes { get; set; } = new();
    }

    public class SearchOptions
    {
        public bool SearchTables { get; set; } = true;
        public bool SearchColumns { get; set; } = true;
        public bool SearchFunctions { get; set; } = true;
        public bool SearchConstraints { get; set; } = true;
        public bool SearchDataTypes { get; set; } = true;
    }

    public class FullTextSearchRequest
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = string.Empty;

        [JsonPropertyName("schema")]
        public string? Schema { get; set; } = "public";

        [JsonPropertyName("tables")]
        public List<string>? Tables { get; set; }

        [JsonPropertyName("columns")]
        public List<string>? Columns { get; set; }

        [JsonPropertyName("max_rows_per_table")]
        public int? MaxRowsPerTable { get; set; } = 100;
    }

    public class FullTextSearchResult
    {
        [JsonPropertyName("table_name")]
        public string TableName { get; set; }

        [JsonPropertyName("table_schema")]
        public string TableSchema { get; set; }

        [JsonPropertyName("matches")]
        public List<Dictionary<string, object>> Matches { get; set; } = new();

        [JsonPropertyName("match_count")]
        public int MatchCount { get; set; }
    }

    public class AdvancedSearchRequest
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = string.Empty;

        [JsonPropertyName("schema")]
        public string? Schema { get; set; } = "public";

        [JsonPropertyName("search_in_names")]
        public bool SearchInNames { get; set; } = true;

        [JsonPropertyName("search_in_definitions")]
        public bool SearchInDefinitions { get; set; } = true;

        [JsonPropertyName("search_in_comments")]
        public bool SearchInComments { get; set; } = true;

        [JsonPropertyName("search_in_data")]
        public bool SearchInData { get; set; } = false;

        [JsonPropertyName("case_sensitive")]
        public bool CaseSensitive { get; set; } = false;

        [JsonPropertyName("object_types")]
        public List<string>? ObjectTypes { get; set; }
    }
}
