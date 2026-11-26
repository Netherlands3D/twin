using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace Netherlands3D.Twin.DataSets
{
    public static class DashboardCsv
    {
        private static readonly string[] RequiredHeaders =
        {
            "Type", "Blok", "Planproduct", "Datum indiening", "Segment", "Kamers", "Min GBO",
            "Aantal woningen", "Categorie", "BVO", "Programma", "Afwijking"
        };

        internal static bool HasDashboardColumns(TextReader reader)
        {
            using var csv = Open(reader);
            if (!ReadNextRecord(csv, out _) || !csv.ReadHeader())
                return false;

            return HasDashboardColumns(csv.Context.HeaderRecord);
        }

        internal static bool HasDashboardColumns(string[] headers) =>
            !CartesianTileSubObjectColorCsv.HasColorColumns(headers) && RequiredHeaders.All(headers.Contains);

        internal static DashboardData ReadFile(string path)
        {
            if (DashboardXlsx.IsWorkbook(path))
                return DashboardXlsx.Read(path);

            using var reader = File.OpenText(path);
            return Read(reader);
        }

        public static DashboardData Read(TextReader reader)
        {
            using var csv = Open(reader);
            return ReadRows(ReadHeaders(csv), ReadCsvRows(csv));
        }

        internal static void ValidateHeaders(TextReader reader)
        {
            using var csv = Open(reader);
            GetColumnIndices(ReadHeaders(csv));
        }

        private static string[] ReadHeaders(CsvReader csv)
        {
            if (!ReadNextRecord(csv, out _) || !csv.ReadHeader())
                throw new DashboardImportFormatException("De rij met kolomnamen ontbreekt.");

            return csv.Context.HeaderRecord;
        }

        private static IEnumerable<(int LineNumber, string[] Values)> ReadCsvRows(CsvReader csv)
        {
            while (ReadNextRecord(csv, out var lineNumber))
                yield return (lineNumber, csv.Context.Record);
        }

        private static Dictionary<string, int> GetColumnIndices(string[] headers)
        {
            var columnIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < headers.Length; index++)
            {
                if (!columnIndices.TryAdd(headers[index], index))
                    throw new DashboardImportFormatException($"Kolomnaam '{headers[index]}' komt dubbel voor.");
            }

            var missingHeaders = RequiredHeaders.Where(header => !columnIndices.ContainsKey(header)).ToArray();
            if (missingHeaders.Length > 0)
                throw new DashboardImportFormatException($"Ontbrekende kolommen: {string.Join(", ", missingHeaders)}.");

            return columnIndices;
        }

        internal static DashboardData ReadRows(string[] headers, IEnumerable<(int LineNumber, string[] Values)> rows)
        {
            var columnIndices = GetColumnIndices(headers);
            var housing = new List<DashboardData.HousingRow>();
            var nonHousing = new List<DashboardData.NonHousingRow>();
            var programChanges = new List<DashboardData.ProgramChangeRow>();
            var skippedRows = new List<(int, string)>();
            string projectTitle = null;

            foreach (var (lineNumber, values) in rows)
            {
                if (values.Length != headers.Length)
                {
                    var reason = values.Length < headers.Length
                        ? $"Te weinig velden. Vanaf kolom {values.Length + 1} ('{headers[values.Length]}') ontbreken velden."
                        : $"Te veel velden. Er staan {values.Length} velden in plaats van {headers.Length}.";
                    skippedRows.Add((lineNumber, reason));
                    continue;
                }

                if (columnIndices.TryGetValue("Projecttitel", out var titleColumn))
                {
                    var title = values[titleColumn]?.Trim();
                    if (!string.IsNullOrEmpty(title))
                    {
                        if (projectTitle != null && !string.Equals(projectTitle, title, StringComparison.Ordinal))
                            throw new DashboardImportFormatException($"Rij {lineNumber}: vul in de kolom 'Projecttitel' overal dezelfde titel in.");

                        projectTitle = title;
                    }
                }

                var issues = new List<string>();
                string ReadText(string header) => values[columnIndices[header]]?.Trim();
                void Require(params string[] columns)
                {
                    foreach (var column in columns)
                    {
                        if (string.IsNullOrEmpty(ReadText(column)))
                            issues.Add($"'{column}' is verplicht");
                    }
                }

                T? ReadValue<T>(string column, Func<string, T?> parse, string expected) where T : struct
                {
                    try
                    {
                        return parse(ReadText(column));
                    }
                    catch (Exception exception) when (exception is FormatException or OverflowException)
                    {
                        issues.Add($"'{column}': {expected}");
                        return null;
                    }
                }

                var block = ReadText("Blok");
                var phase = ReadText("Planproduct");
                switch (ReadText("Type"))
                {
                    case "Wonen":
                        Require("Blok", "Planproduct", "Segment", "Kamers");
                        var date = ReadValue<DateTime>("Datum indiening", ReadDate, "gebruik JJJJ-MM-DD");
                        var rooms = ReadValue<int>("Kamers", ReadInteger, "vul een geheel getal vanaf 0 in");
                        var area = ReadValue<decimal>("Min GBO", ReadDecimal, "vul een getal vanaf 0 in");
                        var count = ReadValue<int>("Aantal woningen", ReadInteger, "vul een geheel getal vanaf 0 in");
                        if (issues.Count == 0)
                            housing.Add(new DashboardData.HousingRow(block, phase, date, ReadText("Segment"), rooms, area, count));
                        break;
                    case "Niet-wonen":
                        Require("Blok", "Planproduct", "Categorie");
                        var grossArea = ReadValue<decimal>("BVO", ReadDecimal, "vul een getal vanaf 0 in");
                        if (issues.Count == 0)
                            nonHousing.Add(new DashboardData.NonHousingRow(block, phase, ReadText("Categorie"), grossArea));
                        break;
                    case "Programmawijziging":
                        Require("Blok", "Planproduct", "Afwijking");
                        if (issues.Count == 0)
                            programChanges.Add(new DashboardData.ProgramChangeRow(block, phase, ReadText("Programma"), ReadText("Afwijking")));
                        break;
                    default:
                        if (string.IsNullOrEmpty(ReadText("Type")))
                            Require("Type");
                        else
                            issues.Add("'Type': kies Wonen, Niet-wonen of Programmawijziging");
                        break;
                }

                if (issues.Count > 0)
                    skippedRows.Add((lineNumber, string.Join(", ", issues) + "."));
            }

            return new DashboardData(housing, nonHousing, programChanges, skippedRows, projectTitle);
        }

        private static CsvReader Open(TextReader reader)
        {
            // The dashboard header labels contain neither separator. Keep the
            // rest of the input streaming, including quoted multiline fields.
            var prefix = new StringBuilder();
            string header;
            do
            {
                header = reader.ReadLine();
                if (header == null)
                    break;
                if (prefix.Length == 0)
                    header = header.TrimStart('\uFEFF');
                prefix.Append(header).Append('\n');
            }
            while (header.Length == 0);

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = GetDelimiter(header),
                HasHeaderRecord = true,
                IgnoreBlankLines = false
            };

            return new CsvReader(new HeaderReplayReader(prefix.ToString(), reader), configuration);
        }

        private static string GetDelimiter(string header)
        {
            var quoted = false;
            var commas = 0;
            var semicolons = 0;
            foreach (var character in header ?? string.Empty)
            {
                if (character == '"')
                    quoted = !quoted;
                else if (!quoted && character == ',')
                    commas++;
                else if (!quoted && character == ';')
                    semicolons++;
            }

            return commas > semicolons ? "," : ";";
        }

        private sealed class HeaderReplayReader : TextReader
        {
            private readonly StringReader prefix;
            private readonly TextReader source;

            public HeaderReplayReader(string header, TextReader source)
            {
                prefix = new StringReader(header);
                this.source = source;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                var copied = prefix.Read(buffer, index, count);
                return copied == count ? copied : copied + source.Read(buffer, index + copied, count - copied);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    prefix.Dispose();
                base.Dispose(disposing);
            }
        }

        private static bool ReadNextRecord(CsvReader csv, out int lineNumber)
        {
            do
            {
                // RawRow points to the final physical line once CsvHelper reads a multiline record.
                lineNumber = csv.Context.RawRow + 1;
                if (!csv.Read())
                    return false;
            }
            while (csv.Context.Record.Length == 0);

            return true;
        }

        private static DateTime? ReadDate(string value)
        {
            return string.IsNullOrEmpty(value) ? null : DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static int? ReadInteger(string value)
        {
            return string.IsNullOrEmpty(value) ? null : int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
        }

        private static decimal? ReadDecimal(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (!value.Contains('.'))
                value = value.Replace(',', '.');

            return decimal.Parse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        }
    }

    internal sealed class DashboardImportFormatException : FormatException
    {
        internal DashboardImportFormatException(string message) : base(message)
        {
        }
    }
}
