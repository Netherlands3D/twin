using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.DataSets
{
    public class CartesianTileSubObjectColorCsv : DataSet<Dictionary<string, Color>>
    {
        private readonly string path;
        private readonly int maxParsesPerFrame;
        private readonly string[] requiredHeaders = { "BagId", "HexColor" };

        private readonly CsvConfiguration config = new(CultureInfo.CurrentCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ";"
        };

        public CartesianTileSubObjectColorCsv(Uri uri, int maxParsesPerFrame = 100)
        {
            // TODO: This should be moved into a URI extension method
            if (uri.Scheme != "project")
            {
                throw new NotSupportedException(
                    "The given type of URI is not supported, only project files are supported"
                );
            }

            this.path = AssetUriFactory.GetLocalPath(uri);
            this.maxParsesPerFrame = maxParsesPerFrame;
        }

        public CartesianTileSubObjectColorCsv(string path, int maxParsesPerFrame = 100)
        {
            this.path = path;
            this.maxParsesPerFrame = maxParsesPerFrame;
        }

        public bool IsValid()
        {
            if (!path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return false;

            //Streamread first line to check if all required headers are present, and the config delimiter is used
            using var streamReader = new StreamReader(path);
            using var csv = new CsvReader(streamReader, config);

            return ReadHeader(csv);
        }

        public Dictionary<string, Color> Read()
        {
            using var streamReader = new StreamReader(path);
            using var csv = new CsvReader(streamReader, config);
            if (!ReadHeader(csv))
                throw new FormatException("The building color CSV must contain BagId and HexColor columns.");

            var colors = new Dictionary<string, Color>();
            while (csv.Read())
                ParseRecord(csv, colors);

            return colors;
        }

        public IEnumerator ReadAsync(Action<float> onProgress, Action<Dictionary<string, Color>> onComplete) 
        {
            var dictionary = new Dictionary<string, Color>();
            var fileSize = new FileInfo(path).Length;
            onProgress(0f);

            using var streamReader = new StreamReader(path);
            using var csvReader = new CsvReader(streamReader, config);

            if (!ReadHeader(csvReader))
                throw new FormatException("The building color CSV must contain BagId and HexColor columns.");
            var count = 0;

            while (csvReader.Read())
            {
                ParseRecord(csvReader, dictionary);
                count++;

                if (count < maxParsesPerFrame) continue;
                
                onProgress((float)streamReader.BaseStream.Position / fileSize);
                count = 0;
                yield return null;
            }

            onComplete(dictionary);
            onProgress(1f);
        }

        private bool ReadHeader(CsvReader csv)
        {
            return csv.Read() && csv.ReadHeader() && requiredHeaders.All(header => csv.Context.HeaderRecord.Contains(header));
        }

        private static void ParseRecord(IReaderRow csvReader, IDictionary<string, Color> dictionary)
        {
            var id = csvReader.GetField<string>("BagId")?.Trim();
            var hexColor = csvReader.GetField<string>("HexColor")?.Trim();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(hexColor))
                return;

            if (!hexColor.StartsWith("#"))
                hexColor = "#" + hexColor;

            if (ColorUtility.TryParseHtmlString(hexColor, out var color))
                dictionary[id] = color;
        }
    }
}
