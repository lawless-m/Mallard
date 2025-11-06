using System;
using System.Collections.Generic;

namespace Mallard.Models
{
    public class QueryResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public double ExecutionTimeMs { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();
        public List<string> ColumnNames { get; set; } = new List<string>();
    }
}
