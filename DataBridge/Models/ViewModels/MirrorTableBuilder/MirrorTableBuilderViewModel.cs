using System.ComponentModel.DataAnnotations;

namespace DataBridge.Models.ViewModels.MirrorTableBuilder
{
    // ── Table list item ───────────────────────────────────────────────────────
    public class TableInfoViewModel
    {
        public string TableName { get; set; } = string.Empty;
        public int ColumnCount { get; set; }
        public long RowCount { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    // ── Column definition (used in Create form) ───────────────────────────────
    public class ColumnDefinitionViewModel
    {
        [Required(ErrorMessage = "Column name is required.")]
        public string ColumnName { get; set; } = string.Empty;

        [Required]
        public string DataType { get; set; } = "NVARCHAR";

        // For NVARCHAR, VARCHAR, CHAR, BINARY, VARBINARY
        public int? Length { get; set; }

        // For DECIMAL / NUMERIC
        public int? Precision { get; set; }
        public int? Scale { get; set; }

        public bool IsNullable { get; set; } = true;
        public bool IsPrimaryKey { get; set; } = false;
        public bool IsIdentity { get; set; } = false;
        public string? DefaultValue { get; set; }
    }

    // ── Create table form ─────────────────────────────────────────────────────
    public class CreateTableViewModel
    {
        [Required(ErrorMessage = "Table name is required.")]
        [RegularExpression(@"^[A-Za-z_][A-Za-z0-9_]*$",
            ErrorMessage = "Table name must start with a letter or underscore and contain only alphanumeric characters.")]
        [MaxLength(128)]
        public string TableName { get; set; } = string.Empty;

        public string? Schema { get; set; } = "dbo";

        public string? Description { get; set; }
        public string? CreatedBy { get; set; }

        public List<ColumnDefinitionViewModel> Columns { get; set; } = new()
        {
            new ColumnDefinitionViewModel
            {
                ColumnName = "Id",
                DataType = "INT",
                IsNullable = false,
                IsPrimaryKey = true,
                IsIdentity = true
            }
        };

        // Available SQL data types for dropdown
        public static readonly List<string> AvailableTypes = new()
        {
            "INT", "BIGINT", "SMALLINT", "TINYINT",
            "BIT",
            "DECIMAL", "NUMERIC", "FLOAT", "REAL", "MONEY", "SMALLMONEY",
            "NVARCHAR", "VARCHAR", "NCHAR", "CHAR", "TEXT", "NTEXT",
            "DATETIME", "DATETIME2", "DATE", "TIME", "DATETIMEOFFSET", "SMALLDATETIME",
            "UNIQUEIDENTIFIER",
            "VARBINARY", "BINARY", "IMAGE",
            "XML"
        };

        // Types that require a length
        public static readonly List<string> LengthTypes = new()
        {
            "NVARCHAR", "VARCHAR", "NCHAR", "CHAR", "BINARY", "VARBINARY"
        };

        // Types that require precision/scale
        public static readonly List<string> PrecisionTypes = new()
        {
            "DECIMAL", "NUMERIC"
        };
    }

    // ── Column info for display ───────────────────────────────────────────────
    public class ColumnInfoViewModel
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string FullType { get; set; } = string.Empty;  // e.g. NVARCHAR(200)
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public string? DefaultValue { get; set; }
        public int OrdinalPosition { get; set; }
    }

    // ── Edit schema viewmodel ─────────────────────────────────────────────────
    public class EditSchemaViewModel
    {
        public string FullTableName { get; set; } = string.Empty;   // dbo.inventory
        public string Schema { get; set; } = "dbo";
        public string TableName { get; set; } = string.Empty;

        // Kolom yang sudah ada (read dari DB)
        public List<ExistingColumnViewModel> ExistingColumns { get; set; } = new();

        // Kolom baru yang mau ditambah
        public List<ColumnDefinitionViewModel> NewColumns { get; set; } = new();
    }

    public class ExistingColumnViewModel
    {
        public string ColumnName { get; set; } = string.Empty;
        public string FullType { get; set; } = string.Empty;   // e.g. NVARCHAR(200)
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }

        // Yang bisa diedit user
        public bool MarkedForDrop { get; set; } = false;
        public bool NewIsNullable { get; set; }                   // diisi dari IsNullable saat load
    }
}