using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Netherlands3D.Twin;
using UnityEngine;
using Netherlands3D.SubObjects;

namespace Netherlands3D.Twin
{
    public class IDColor
    {
        [Index(0)] public string Id { get; set; }
        [Index(1)] public string HexColor { get; set; }

        public Color Color
        {
            get
            {
                var hex = HexColor;
                if (!hex.StartsWith("#"))
                    hex = "#" + hex;

                var canParse = ColorUtility.TryParseHtmlString(hex, out var color);
                return canParse ? color : Interaction.NO_OVERRIDE_COLOR; 
            }
        }
    }

    public class CSVToColors : MonoBehaviour
    {
        [SerializeField] private string path;
        [SerializeField] private int maxParsesPerFrame = 100;

        private void Start()
        {
            StartCoroutine(StreamReadCSV(maxParsesPerFrame));
        }

        public IEnumerator StreamReadCSV(int maxParsesPerFrame)
        {
            var dictionaries = ReadCSVColors(path, maxParsesPerFrame).GetEnumerator();

            while (dictionaries.MoveNext())
            {
                print("frame: " + Time.frameCount);
                var dictionary = dictionaries.Current;
                print(dictionary.Count);
                GeometryColorizer.AddAndMergeCustomColorSet(GeometryColorizer.GetLowestPriorityIndex(), dictionary);
                yield return null;
            }
        }

        public IEnumerable<Dictionary<string, Color>> ReadCSVColors(string path, int maxParsesPerFrame)
        {
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ";"
            };

            using var reader = new StreamReader(path);
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<IDColor>().GetEnumerator();
                var dictionary = new Dictionary<string, Color>();

                while (records.MoveNext())
                {
                    var record = records.Current;
                    dictionary[record.Id] = record.Color;

                    if (dictionary.Count >= maxParsesPerFrame)
                    {
                        yield return dictionary;
                        dictionary.Clear();
                    }
                }

                //return the remaining elements of the part not divisible by maxParsesPerFrame 
                if (dictionary.Count > 0)
                {
                    yield return dictionary;
                }
                // return records.ToDictionary(record => record.Id, record => record.Color); //don't return like this, because it will stop the parsing from being spread over multiple frames
            }
        }
    }
}