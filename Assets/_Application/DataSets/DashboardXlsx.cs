using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace Netherlands3D.Twin.DataSets
{
    internal static class DashboardXlsx
    {
        private const string WorkbookPath = "xl/workbook.xml";
        private const int MaxColumns = 16384;
        private const int MaxRows = 1048576;
        private const int MaxPartSize = 32 * 1024 * 1024;
        private static readonly Regex EscapedCharacter = new(@"_x([0-9A-Fa-f]{4})_");

        internal static bool IsWorkbook(string path) => string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase);

        internal static DashboardData Read(string path)
        {
            try
            {
                using var file = File.OpenRead(path);
                using var archive = new ZipFile(file);
                var workbook = ReadPart(archive, WorkbookPath);
                var ns = workbook.Name.Namespace;
                var sheet = workbook.Element(ns + "sheets")?.Elements(ns + "sheet")
                    .FirstOrDefault(element => (string)element.Attribute("name") == "Dashboard");
                var sheetId = sheet?.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id")?.Value;
                if (sheetId == null)
                    throw new DashboardImportFormatException("Het werkblad 'Dashboard' ontbreekt.");

                var relationships = ReadPart(archive, "xl/_rels/workbook.xml.rels").Elements();
                var sheetPart = relationships.FirstOrDefault(element => (string)element.Attribute("Id") == sheetId);
                var worksheet = ReadPart(archive, ResolvePartPath(sheetPart, "worksheet"));
                var sharedPart = relationships.FirstOrDefault(element => HasPartType(element, "sharedStrings"));
                var sharedStrings = sharedPart == null
                    ? Array.Empty<string>()
                    : ReadPart(archive, ResolvePartPath(sharedPart, "sharedStrings")).Elements().Select(ReadString).ToArray();

                var dateSystem = (string)workbook.Element(ns + "workbookPr")?.Attribute("date1904");
                return ReadWorksheet(worksheet, sharedStrings, dateSystem is "1" or "true");
            }
            catch (Exception exception) when (exception is ZipException or XmlException or InvalidDataException or UriFormatException)
            {
                throw new FormatException("The dashboard workbook could not be read.", exception);
            }
        }

        private static DashboardData ReadWorksheet(XElement worksheet, string[] sharedStrings, bool uses1904Dates)
        {
            var ns = worksheet.Name.Namespace;
            var sheetData = worksheet.Element(ns + "sheetData") ?? throw new DashboardImportFormatException("Het werkblad 'Dashboard' is leeg.");
            using var rows = sheetData.Elements(ns + "row").GetEnumerator();
            var rowNumber = 0;
            while (rows.MoveNext())
            {
                rowNumber = GetRowNumber(rows.Current, rowNumber);
                var headers = ReadCells(rows.Current, sharedStrings, -1, uses1904Dates);
                if (headers.Length == 0)
                    continue;
                var dateColumn = Array.IndexOf(headers, "Datum indiening");
                return DashboardCsv.ReadRows(headers, ReadRows(rows, rowNumber, headers.Length, dateColumn, sharedStrings, uses1904Dates));
            }

            throw new DashboardImportFormatException("De rij met kolomnamen ontbreekt op het werkblad 'Dashboard'.");
        }

        private static IEnumerable<(int LineNumber, string[] Values)> ReadRows(IEnumerator<XElement> rows, int rowNumber, int width, int dateColumn, string[] sharedStrings, bool uses1904Dates)
        {
            while (rows.MoveNext())
            {
                rowNumber = GetRowNumber(rows.Current, rowNumber);
                var values = ReadCells(rows.Current, sharedStrings, dateColumn, uses1904Dates);
                if (values.Length == 0)
                    continue;

                // Excel omits empty cells and may omit trailing columns entirely.
                if (values.Length < width)
                    Array.Resize(ref values, width);
                yield return (rowNumber, values);
            }
        }

        private static string[] ReadCells(XElement row, string[] sharedStrings, int dateColumn, bool uses1904Dates)
        {
            var ns = row.Name.Namespace;
            var values = new List<string>();
            var column = -1;
            foreach (var cell in row.Elements(ns + "c"))
            {
                column = GetColumnIndex((string)cell.Attribute("r"), column);
                var value = (string)cell.Element(ns + "v");
                var type = (string)cell.Attribute("t");
                if (type == "inlineStr")
                    value = ReadString(cell.Element(ns + "is"));
                else if (type == "s" && value != null)
                    value = ReadSharedString(value, sharedStrings);
                else if (type == "str" && value != null)
                    value = DecodeString(value);
                else if (type == "b")
                    value = value == "1" ? "TRUE" : "FALSE";
                else if (column == dateColumn && value != null)
                    value = NormalizeDate(value, type, uses1904Dates);

                if (string.IsNullOrEmpty(value))
                    continue;

                while (values.Count <= column)
                    values.Add(string.Empty);
                values[column] = value;
            }

            return values.ToArray();
        }

        private static string NormalizeDate(string value, string type, bool uses1904Dates)
        {
            if (type == "d" && DateTime.TryParseExact(value, new[] { "yyyy-MM-dd", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK" },
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
                return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if ((type == null || type == "n") &&
                double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) &&
                serial >= 0 && serial < (uses1904Dates ? 2957004 : 2958466))
            {
                // Excel's 1900 calendar includes a fictitious 29 February.
                if (!uses1904Dates && Math.Floor(serial) == 60)
                    return value;

                var epoch = uses1904Dates ? new DateTime(1904, 1, 1) : new DateTime(1899, 12, serial < 60 ? 31 : 30);
                return epoch.AddDays(Math.Floor(serial)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return value;
        }

        private static int GetRowNumber(XElement row, int previous)
        {
            var reference = (string)row.Attribute("r");
            var number = previous + 1;
            if ((reference != null && !int.TryParse(reference, NumberStyles.None, CultureInfo.InvariantCulture, out number)) ||
                number <= previous || number > MaxRows)
                throw new FormatException("The Dashboard worksheet has an invalid row number.");
            return number;
        }

        private static int GetColumnIndex(string reference, int previous)
        {
            var column = previous + 2;
            if (!string.IsNullOrEmpty(reference))
            {
                column = 0;
                foreach (var character in reference)
                {
                    if (character < 'A' || character > 'Z')
                        break;
                    column = column * 26 + character - 'A' + 1;
                    if (column > MaxColumns)
                        break;
                }
            }

            if (column <= previous + 1 || column > MaxColumns)
                throw new FormatException("The Dashboard worksheet has an invalid cell reference.");
            return column - 1;
        }

        private static string ReadSharedString(string value, string[] strings)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= strings.Length)
                throw new FormatException("The dashboard workbook has an invalid shared string reference.");
            return strings[index];
        }

        private static string ReadString(XElement element)
        {
            if (element == null)
                return string.Empty;
            var ns = element.Name.Namespace;
            return string.Concat(element.Elements(ns + "t").Concat(element.Elements(ns + "r").Elements(ns + "t")).Select(text => DecodeString(text.Value)));
        }

        private static string DecodeString(string value) =>
            EscapedCharacter.Replace(value, match => ((char)int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString());

        private static bool HasPartType(XElement relationship, string type) =>
            ((string)relationship?.Attribute("Type"))?.EndsWith("/" + type, StringComparison.Ordinal) == true;

        private static string ResolvePartPath(XElement relationship, string type)
        {
            var target = (string)relationship?.Attribute("Target");
            if (!HasPartType(relationship, type) || string.IsNullOrEmpty(target) || (string)relationship.Attribute("TargetMode") == "External")
                throw new FormatException("The dashboard workbook has an invalid part relationship.");

            var origin = new Uri("https://workbook.invalid/" + WorkbookPath);
            var part = new Uri(origin, target);
            if (part.Scheme != origin.Scheme || part.Authority != origin.Authority || part.Query.Length != 0 || part.Fragment.Length != 0)
                throw new FormatException("The dashboard workbook has an invalid part path.");
            return Uri.UnescapeDataString(part.AbsolutePath.TrimStart('/'));
        }

        private static XElement ReadPart(ZipFile archive, string path)
        {
            var entry = archive.GetEntry(path);
            if (entry == null || !entry.IsFile || entry.Size < 0 || entry.Size > MaxPartSize)
                throw new FormatException("The dashboard workbook has a missing or unsupported part.");

            using var stream = archive.GetInputStream(entry);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                MaxCharactersInDocument = MaxPartSize
            });
            return XElement.Load(reader, LoadOptions.PreserveWhitespace);
        }
    }
}
