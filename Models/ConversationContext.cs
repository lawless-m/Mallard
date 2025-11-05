using System;
using System.Collections.Generic;

namespace Mallard.Models
{
    public class ConversationContext
    {
        public List<Message> History { get; set; } = new List<Message>();
        public Dictionary<string, TableSchema> Schemas { get; set; } = new Dictionary<string, TableSchema>();
        public List<ExecutedQuery> QueryHistory { get; set; } = new List<ExecutedQuery>();
    }

    public class Message
    {
        public string Role { get; set; } = string.Empty;  // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class ExecutedQuery
    {
        public string Sql { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
        public bool Success { get; set; }
        public int RowCount { get; set; }
    }
}
