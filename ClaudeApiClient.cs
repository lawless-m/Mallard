using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mallard.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mallard
{
    public class ClaudeApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly int _maxTokens;

        public ClaudeApiClient(string apiKey, string model = "claude-sonnet-4-5-20250929", int maxTokens = 4096)
        {
            _apiKey = apiKey;
            _model = model;
            _maxTokens = maxTokens;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<ClaudeResponse> SendMessageAsync(string userMessage, ConversationContext context)
        {
            var systemPrompt = BuildSystemPrompt(context.Schemas);
            var messages = BuildMessages(context.History, userMessage);

            var requestBody = new
            {
                model = _model,
                max_tokens = _maxTokens,
                temperature = 0.0,
                system = systemPrompt,
                messages = messages
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ClaudeResponse
                    {
                        Success = false,
                        Error = $"API Error: {response.StatusCode} - {responseText}"
                    };
                }

                var responseData = JObject.Parse(responseText);
                var textContent = responseData["content"]?[0]?["text"]?.ToString() ?? string.Empty;

                return new ClaudeResponse
                {
                    Success = true,
                    RawResponse = textContent,
                    Sql = ExtractSql(textContent),
                    Explanation = ExtractExplanation(textContent),
                    TeachingNote = ExtractTeachingNote(textContent)
                };
            }
            catch (Exception ex)
            {
                return new ClaudeResponse
                {
                    Success = false,
                    Error = $"Exception: {ex.Message}"
                };
            }
        }

        private string BuildSystemPrompt(Dictionary<string, TableSchema> schemas)
        {
            var schemaContext = BuildSchemaContext(schemas);

            return $@"You are a SQL assistant helping users write DuckDB queries. The user understands
basic SQL (joins, WHERE clauses) but is new to CTEs, subqueries, and window functions.

Your responses should:
1. Generate valid DuckDB SQL
2. Explain new concepts when you use them
3. Suggest optimizations when relevant
4. Format SQL clearly with proper indentation

When responding, use this format:
<explanation>
Brief explanation of the approach
</explanation>

<sql>
-- Your SQL query here
SELECT ...
</sql>

<teaching_note>
Optional: Explain any new SQL features used (CTEs, window functions, etc.)
</teaching_note>

Available schema:
{schemaContext}";
        }

        private string BuildSchemaContext(Dictionary<string, TableSchema> schemas)
        {
            if (schemas.Count == 0)
            {
                return "No schema information available yet.";
            }

            var sb = new StringBuilder();
            sb.AppendLine("Available Tables:");

            foreach (var schema in schemas.Values)
            {
                var columns = string.Join(", ", schema.Columns.Select(c => $"{c.Name} {c.Type}"));
                sb.AppendLine($"- {schema.TableName} ({columns})");
            }

            return sb.ToString();
        }

        private List<object> BuildMessages(List<Message> history, string newUserMessage)
        {
            var messages = new List<object>();

            foreach (var msg in history)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            messages.Add(new { role = "user", content = newUserMessage });

            return messages;
        }

        private string ExtractSql(string response)
        {
            var sqlStart = response.IndexOf("<sql>");
            var sqlEnd = response.IndexOf("</sql>");

            if (sqlStart >= 0 && sqlEnd > sqlStart)
            {
                return response.Substring(sqlStart + 5, sqlEnd - sqlStart - 5).Trim();
            }

            return string.Empty;
        }

        private string ExtractExplanation(string response)
        {
            var start = response.IndexOf("<explanation>");
            var end = response.IndexOf("</explanation>");

            if (start >= 0 && end > start)
            {
                return response.Substring(start + 13, end - start - 13).Trim();
            }

            return string.Empty;
        }

        private string ExtractTeachingNote(string response)
        {
            var start = response.IndexOf("<teaching_note>");
            var end = response.IndexOf("</teaching_note>");

            if (start >= 0 && end > start)
            {
                return response.Substring(start + 15, end - start - 15).Trim();
            }

            return string.Empty;
        }
    }

    public class ClaudeResponse
    {
        public bool Success { get; set; }
        public string RawResponse { get; set; } = string.Empty;
        public string Sql { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string TeachingNote { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
