namespace CloudRestaurent.Application.Common.Imports;

/// <summary>
/// Result of a CSV import operation. We always import in best-effort mode:
/// good rows commit, bad rows are reported back. Caller can re-upload a corrected
/// CSV containing only the failed rows. This is the same model UltimatePOS uses
/// for its bulk imports (and what users intuitively expect).
/// </summary>
public sealed record ImportResultDto(
    int TotalRows,
    int ImportedRows,
    int SkippedRows,
    IReadOnlyList<ImportRowError> Errors);

public sealed record ImportRowError(int Row, string Field, string Message);

/// <summary>
/// Minimal CSV parser tuned to the format Excel/Sheets exports — comma-separated,
/// double-quote escaping for fields containing commas/quotes/newlines. Handles
/// CRLF and LF. We intentionally don't pull in CsvHelper for this — the input is
/// trusted (only TenantAdmin can upload), and a 3-rule parser is easier to audit
/// than a dependency.
/// </summary>
public static class CsvParser
{
    public static IReadOnlyList<IReadOnlyList<string>> Parse(string content)
    {
        var rows = new List<IReadOnlyList<string>>();
        var currentRow = new List<string>();
        var field = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    // Escaped quote inside a quoted field.
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(ch);
                }
            }
            else
            {
                if (ch == '"' && field.Length == 0)
                {
                    inQuotes = true;
                }
                else if (ch == ',')
                {
                    currentRow.Add(field.ToString());
                    field.Clear();
                }
                else if (ch == '\r')
                {
                    // skip — CRLF line endings; consumed by \n branch
                }
                else if (ch == '\n')
                {
                    currentRow.Add(field.ToString());
                    field.Clear();
                    if (currentRow.Count > 0 && !(currentRow.Count == 1 && string.IsNullOrEmpty(currentRow[0])))
                        rows.Add(currentRow);
                    currentRow = new List<string>();
                }
                else
                {
                    field.Append(ch);
                }
            }
        }

        // Trailing field without newline.
        if (field.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(field.ToString());
            if (!(currentRow.Count == 1 && string.IsNullOrEmpty(currentRow[0])))
                rows.Add(currentRow);
        }

        return rows;
    }
}
