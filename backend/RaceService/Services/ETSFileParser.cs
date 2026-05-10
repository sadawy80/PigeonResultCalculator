using System.Data;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;

namespace PRC.RaceService.Services;

public class ETSParseResult
{
    public bool IsSuccess { get; set; }
    public List<ETSRow> Rows { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public int DuplicateRows { get; set; }
}

public class ETSRow
{
    public string RingNumber { get; set; } = string.Empty;
    public DateTime ArrivalTime { get; set; }
    public string? FancierName { get; set; }
    public string? PigeonName { get; set; }
    public string? Sex { get; set; }
    public int? YearOfBirth { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IETSFileParser
{
    Task<ETSParseResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}

public class ETSFileParser : IETSFileParser
{
    private static readonly string[] ValidTimestampFormats =
    {
        "HH:mm:ss", "H:mm:ss", "HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy HH:mm:ss",
        "MM/dd/yyyy HH:mm:ss", "dd-MM-yyyy HH:mm:ss"
    };

    public async Task<ETSParseResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".xlsx" or ".xls" => await ParseExcelAsync(fileStream, ct),
            ".csv" => await ParseCsvAsync(fileStream, ct),
            _ => new ETSParseResult
            {
                IsSuccess = false,
                Errors = new List<string> { $"Unsupported file type: {extension}" }
            }
        };
    }

    private Task<ETSParseResult> ParseExcelAsync(Stream stream, CancellationToken ct)
    {
        var result = new ETSParseResult();
        var rows = new List<ETSRow>();
        var errors = new List<string>();

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateReader(stream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        var table = dataSet.Tables[0];
        result.TotalRows = table.Rows.Count;

        var ringCol    = FindColumn(table, "ring", "ringnumber", "ring_number", "pigeon", "id");
        var timeCol    = FindColumn(table, "time", "arrival", "arrivaltime", "arrival_time", "timestamp");
        var fancierCol = FindColumn(table, "fancier", "fancier_name", "fanciername", "owner", "breeder");
        var nameCol    = FindColumn(table, "name", "pigeonname", "pigeon_name");
        var sexCol     = FindColumn(table, "sex", "gender");
        var yearCol    = FindColumn(table, "year", "yearofbirth", "year_of_birth", "born");

        if (ringCol == -1 || timeCol == -1)
        {
            result.IsSuccess = false;
            result.Errors.Add("Could not find required columns: Ring Number and Arrival Time.");
            return Task.FromResult(result);
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < table.Rows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var row = table.Rows[i];
            var rowNum = i + 2;

            var ring = row[ringCol]?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(ring))
            {
                errors.Add($"Row {rowNum}: Missing ring number.");
                result.FailedRows++;
                continue;
            }

            if (!TryParseDateTime(row[timeCol]?.ToString(), out var arrivalTime))
            {
                rows.Add(new ETSRow { RingNumber = ring, HasError = true, ErrorMessage = $"Row {rowNum}: Invalid arrival time '{row[timeCol]}'" });
                result.FailedRows++;
                continue;
            }

            if (seen.Contains(ring))
            {
                rows.Add(new ETSRow { RingNumber = ring, ArrivalTime = arrivalTime });
                result.DuplicateRows++;
                continue;
            }

            seen.Add(ring);

            var etRow = new ETSRow
            {
                RingNumber  = ring,
                ArrivalTime = arrivalTime,
                FancierName = fancierCol >= 0 ? row[fancierCol]?.ToString()?.Trim().NullIfEmpty() : null,
                PigeonName  = nameCol >= 0 ? row[nameCol]?.ToString()?.Trim().NullIfEmpty() : null,
                Sex         = sexCol >= 0 ? NormalizeSex(row[sexCol]?.ToString()) : null
            };

            if (yearCol >= 0 && int.TryParse(row[yearCol]?.ToString(), out var yr))
                etRow.YearOfBirth = yr;

            rows.Add(etRow);
            result.SuccessfulRows++;
        }

        result.Rows = rows;
        result.Errors = errors;
        result.IsSuccess = errors.Count == 0 || result.SuccessfulRows > 0;
        return Task.FromResult(result);
    }

    private async Task<ETSParseResult> ParseCsvAsync(Stream stream, CancellationToken ct)
    {
        var result = new ETSParseResult();
        var rows = new List<ETSRow>();
        var errors = new List<string>();

        using var reader = new StreamReader(stream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null, HeaderValidated = null
        };

        using var csv = new CsvReader(reader, config);
        await csv.ReadAsync();
        csv.ReadHeader();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int rowNum = 2;

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            result.TotalRows++;

            var ring = csv.TryGetField<string>("RingNumber", out var r) ? r?.Trim()
                     : csv.TryGetField<string>("Ring", out var r2) ? r2?.Trim() : null;

            if (string.IsNullOrWhiteSpace(ring))
            {
                errors.Add($"Row {rowNum}: Missing ring number.");
                result.FailedRows++;
                rowNum++;
                continue;
            }

            var timeStr = csv.TryGetField<string>("ArrivalTime", out var t) ? t
                        : csv.TryGetField<string>("Time", out var t2) ? t2 : null;

            if (!TryParseDateTime(timeStr, out var arrivalTime))
            {
                errors.Add($"Row {rowNum}: Invalid arrival time '{timeStr}'.");
                result.FailedRows++;
                rowNum++;
                continue;
            }

            if (seen.Contains(ring)) { result.DuplicateRows++; rowNum++; continue; }
            seen.Add(ring);

            string? fancierName = null;
            if (csv.TryGetField<string>("FancierName", out var fn) && !string.IsNullOrWhiteSpace(fn)) fancierName = fn.Trim();
            else if (csv.TryGetField<string>("Fancier", out var fn2) && !string.IsNullOrWhiteSpace(fn2)) fancierName = fn2.Trim();
            else if (csv.TryGetField<string>("Owner", out var fn3) && !string.IsNullOrWhiteSpace(fn3)) fancierName = fn3.Trim();

            var etRow = new ETSRow
            {
                RingNumber  = ring,
                ArrivalTime = arrivalTime,
                FancierName = fancierName,
                PigeonName  = csv.TryGetField<string>("Name", out var n) ? n?.Trim().NullIfEmpty() : null,
                Sex         = csv.TryGetField<string>("Sex", out var s) ? NormalizeSex(s) : null
            };

            if (csv.TryGetField<string>("Year", out var yr) && int.TryParse(yr, out var yrInt))
                etRow.YearOfBirth = yrInt;

            rows.Add(etRow);
            result.SuccessfulRows++;
            rowNum++;
        }

        result.Rows = rows;
        result.Errors = errors;
        result.IsSuccess = result.SuccessfulRows > 0;
        return result;
    }

    private static int FindColumn(DataTable table, params string[] names)
    {
        for (int i = 0; i < table.Columns.Count; i++)
        {
            var colName = table.Columns[i].ColumnName.ToLowerInvariant().Replace(" ", "").Replace("_", "");
            if (names.Any(n => colName.Contains(n.Replace("_", ""))))
                return i;
        }
        return -1;
    }

    private static bool TryParseDateTime(string? value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (DateTime.TryParseExact(value.Trim(), ValidTimestampFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            if (result.Year == 1) result = DateTime.Today.Add(result.TimeOfDay);
            return true;
        }
        return DateTime.TryParse(value.Trim(), out result);
    }

    private static string? NormalizeSex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToUpperInvariant() switch
        {
            "M" or "MALE" or "COCK" or "C" => "M",
            "F" or "FEMALE" or "HEN" or "H" => "F",
            _ => "U"
        };
    }
}

internal static class StringExtensions
{
    internal static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
