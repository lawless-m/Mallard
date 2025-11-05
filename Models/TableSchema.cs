using System.Collections.Generic;

namespace Mallard.Models
{
    public class TableSchema
    {
        public string TableName { get; set; } = string.Empty;
        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
        public string SourceFile { get; set; } = string.Empty;
    }

    public class ColumnInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Nullable { get; set; }
    }
}
