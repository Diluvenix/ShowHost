using Serilog.Events;
using Serilog.Formatting;
using System.Globalization;

namespace Server
{
    internal class LoggingFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            output.Write($"[{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");

            string level = logEvent.Level switch
            {
                LogEventLevel.Verbose => "VERB ",
                LogEventLevel.Debug => "DEBUG",
                LogEventLevel.Information => "INFO ",
                LogEventLevel.Warning => "WARN ",
                LogEventLevel.Error => "ERROR",
                LogEventLevel.Fatal => "FATAL",
                _ => "?????",
            };
            output.Write($"[{level}] ");

            if (logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue? source) &&
                source is ScalarValue { Value: string sourceText})
                output.Write($"[{sourceText}] ");
            else
                output.Write("[Unknown] ");

            output.Write(SanitizeText(logEvent.RenderMessage()));

            if (logEvent.Exception != null)
                output.Write($" | Exception={FormatException(logEvent.Exception)}");

            if (logEvent.Properties.Any(p => p.Key != "SourceContext"))
                output.Write($" | {{ {string.Join(", ", logEvent.Properties.Where(p => p.Key != "SourceContext").Select(p => $"{p.Key}={FormatValue(p.Value)}"))} }}");

            output.WriteLine();
        }

        private static string FormatValue(LogEventPropertyValue value)
        {
            return value switch
            {
                ScalarValue scalar => FormatScalar(scalar.Value),

                SequenceValue sequence => $"[{string.Join(", ", sequence.Elements.Select(FormatValue))}]",

                StructureValue structure => $"{{ {string.Join(", ", structure.Properties.Select(p => $"{p.Name}={FormatValue(p.Value)}"))} }}",

                DictionaryValue dictionary => $"{{ {string.Join(", ", dictionary.Elements.Select(p => $"{FormatValue(p.Key)}={FormatValue(p.Value)}"))} }}",

                _ => $"\"{Escape(value.ToString())}\""
            };
        }

        private static string FormatScalar(object? value)
        {
            if (value == null) 
                return "null";

            return value switch
            {
                string s => $"\"{Escape(SanitizeText(s))}\"",
                char c => $"\"{Escape(SanitizeText(c.ToString()))}\"",
                bool b => b ? "true" : "false",
                DateTime dt => $"\"{dt:O}\"",
                DateTimeOffset dto => $"\"{dto:O}\"",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "null",
                _ => $"\"{Escape(SanitizeText(value.ToString() ?? string.Empty))}\""
            };
        }

        private static string SanitizeText(string value)
            => value.ReplaceLineEndings(" ");
        private static string Escape(string value)
            => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string FormatException(Exception exception) 
            => $"\"{Escape(exception.ToString().ReplaceLineEndings(" -> "))}\"";
    }
}
