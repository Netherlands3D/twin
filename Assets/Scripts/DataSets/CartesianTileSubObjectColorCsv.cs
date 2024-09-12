using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Netherlands3D.SubObjects;
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

            this.path = Path.Combine(Application.persistentDataPath, uri.LocalPath.TrimStart('/', '\\'));
            this.maxParsesPerFrame = maxParsesPerFrame;
        }

        public CartesianTileSubObjectColorCsv(string path, int maxParsesPerFrame = 100)
        {
            this.path = path;
            this.maxParsesPerFrame = maxParsesPerFrame;
        }

        public bool IsValid()
        {
            if(!path.ToLower().EndsWith(".csv"))
                return false;

            //Streamread first line to check if all required headers are present, and the config delimiter is used
            using var streamReader = new StreamReader(path);
            using var csv = new CsvReader(streamReader, config);

            bool canReadWithConfig = csv.Read();
            bool hasHeader = csv.ReadHeader();
            
            if (!canReadWithConfig || !hasHeader) return false;

            return requiredHeaders.All(header => csv.Context.HeaderRecord.Contains(header));
        }

        public Dictionary<string, Color> Read()
        {
            // Not yet needed for this Dataset
            throw new NotImplementedException();
        }

        public IEnumerator ReadAsync(Action<float> onProgress, Action<Dictionary<string, Color>> onComplete) 
        {
            var dictionary = new Dictionary<string, Color>();
            var fileSize = new FileInfo(path).Length;
            onProgress(0f);

            using var streamReader = new StreamReader(path);
            using var csvReader = new CsvReader(streamReader, config);

            csvReader.Read();
            csvReader.ReadHeader();
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

        private static void ParseRecord(IReaderRow csvReader, IDictionary<string, Color> dictionary)
        {
            var id = csvReader.GetField<string>("BagId");
            var hexColor = csvReader.GetField<string>("HexColor");
            dictionary[id] = ParseHexColor(hexColor);
        }

        private static Color ParseHexColor(string hex)
        {
            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            var canParse = ColorUtility.TryParseHtmlString(hex, out var color);
            return canParse ? color : Interaction.NO_OVERRIDE_COLOR;
        }
    }
}