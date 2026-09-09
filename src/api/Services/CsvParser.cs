using System.Text;

namespace WatchTracker.Api.Services;

internal static class CsvParser
{
    public static List<string[]> Parse(string content)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < content.Length)
        {
            var c = content[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    field.Append(c);
                    i++;
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
                i++;
            }
            else if (c == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
                i++;
            }
            else if (c is '\r' or '\n')
            {
                fields.Add(field.ToString());
                field.Clear();
                if (fields.Any(value => value.Length > 0))
                    rows.Add(fields.ToArray());
                fields.Clear();
                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    i++;
                i++;
            }
            else
            {
                field.Append(c);
                i++;
            }
        }

        if (inQuotes)
            throw new InvalidDataException("CSV contains an unterminated quoted field.");

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            if (fields.Any(value => value.Length > 0))
                rows.Add(fields.ToArray());
        }

        return rows;
    }
}
