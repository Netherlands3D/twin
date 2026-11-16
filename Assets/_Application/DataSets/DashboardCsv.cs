using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

            var headers = csv.Context.HeaderRecord;
            return !CartesianTileSubObjectColorCsv.HasColorColumns(headers) && RequiredHeaders.All(header => headers.Contains(header));
        }

        public static DashboardData Read(TextReader reader)
        {
            using var csv = Open(reader);
            if (!ReadNextRecord(csv, out _) || !csv.ReadHeader())
                throw new FormatException("The dashboard CSV has no header row.");

            var columnCount = csv.Context.HeaderRecord.Length;
            var headers = new HashSet<string>(csv.Context.HeaderRecord, StringComparer.Ordinal);
            if (headers.Count != columnCount || !RequiredHeaders.All(headers.Contains))
                throw new FormatException("The dashboard CSV has invalid column headers.");

            var housing = new List<DashboardData.HousingRow>();
            var nonHousing = new List<DashboardData.NonHousingRow>();
            var programChanges = new List<DashboardData.ProgramChangeRow>();
            var skippedLineNumbers = new List<int>();

            while (ReadNextRecord(csv, out var lineNumber))
            {
                if (csv.Context.Record.Length != columnCount)
                {
                    skippedLineNumbers.Add(lineNumber);
                    continue;
                }

                try
                {
                    var block = ReadText(csv, "Blok");
                    var phase = ReadText(csv, "Planproduct");

                    switch (ReadText(csv, "Type"))
                    {
                        case "Wonen":
                            housing.Add(new DashboardData.HousingRow(block, phase, ReadDate(csv, "Datum indiening"), ReadText(csv, "Segment"),
                                ReadInteger(csv, "Kamers"), ReadDecimal(csv, "Min GBO"), ReadInteger(csv, "Aantal woningen")));
                            break;
                        case "Niet-wonen":
                            nonHousing.Add(new DashboardData.NonHousingRow(block, phase, ReadText(csv, "Categorie"), ReadDecimal(csv, "BVO")));
                            break;
                        case "Programmawijziging":
                            programChanges.Add(new DashboardData.ProgramChangeRow(block, phase, ReadText(csv, "Programma"), ReadText(csv, "Afwijking")));
                            break;
                        default:
                            skippedLineNumbers.Add(lineNumber);
                            break;
                    }
                }
                catch (Exception exception) when (exception is FormatException or OverflowException)
                {
                    skippedLineNumbers.Add(lineNumber);
                }
            }

            return new DashboardData(housing, nonHousing, programChanges, skippedLineNumbers);
        }

        private static CsvReader Open(TextReader reader)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                IgnoreBlankLines = false
            };

            return new CsvReader(reader, configuration, leaveOpen: true);
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

        private static string ReadText(IReaderRow csv, string header) => csv.GetField<string>(header)?.Trim();

        private static DateTime? ReadDate(IReaderRow csv, string header)
        {
            var value = ReadText(csv, header);
            return string.IsNullOrEmpty(value) ? null : DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static int? ReadInteger(IReaderRow csv, string header)
        {
            var value = ReadText(csv, header);
            return string.IsNullOrEmpty(value) ? null : int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
        }

        private static decimal? ReadDecimal(IReaderRow csv, string header)
        {
            var value = ReadText(csv, header);
            return string.IsNullOrEmpty(value) ? null : decimal.Parse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        }
    }
}
