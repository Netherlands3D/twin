using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;
using Application = UnityEngine.Application;

namespace Netherlands3D.Twin
{
    
    // public class IDColorMap : ClassMap<IDColor>
    // {
    //     public IDColorMap()
    //     {
    //         Map(m => m.Id).Name("id");
    //         Map(m => m.HexColor).Name("hexcolor");
    //     }
    // }
    
    public class IDColor
    {
        public string Id { get; set; }
        public string HexColor { get; set; }

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

        // public IDColor() //needed for CSVHelper to work in a build
        // {
        // }
        //
        // public IDColor(string id, string hexColor)
        // {
        //     Id = id;
        //     HexColor = hexColor;
        // }
    }


    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CSVImportAdapter", fileName = "CSVImportAdapter", order = 0)]
    public class CSVImportAdapter : ScriptableObject
    {
        [SerializeField] private UnityEvent<string> csvReplacedMessageEvent = new();
        public int maxParsesPerFrame = 100;
        private static DatasetLayer activeDatasetLayer; //todo: allow multiple datasets to exist

        public void ParseCSVFile(string file)
        {
            if (activeDatasetLayer) //todo: temp fix to allow only 1 dataset layer
            {
                activeDatasetLayer.RemoveCustomColorSet(); //remove before destroying because otherwise the Start() function of the new colorset will apply the new colors before the OnDestroy function can clean up the old colorset. 

                Destroy(activeDatasetLayer.gameObject);
                csvReplacedMessageEvent.Invoke("Het oude CSV bestand is vervangen door het nieuw gekozen CSV bestand.");
            }

            var datasetLayer = new GameObject(file).AddComponent<DatasetLayer>();
            var fullPath = Path.Combine(Application.persistentDataPath, file);
            datasetLayer.StartCoroutine(StreamReadCSV(fullPath, datasetLayer, maxParsesPerFrame));

            activeDatasetLayer = datasetLayer;
        }

        private IEnumerator StreamReadCSV(string path, DatasetLayer layer, int maxParsesPerFrame)
        {
            yield return null; //wait a frame for the created layer to be reparented and set up correctly to ensure the correct priority index
            var dictionaries = ReadCSVColors(path, maxParsesPerFrame).GetEnumerator();

            while (dictionaries.MoveNext())
            {
                var dictionary = dictionaries.Current;
                var cl = GeometryColorizer.AddAndMergeCustomColorSet(layer.PriorityIndex, dictionary);
                layer.SetColorSetLayer(cl);

                yield return null;
            }
        }

        private IEnumerable<Dictionary<string, Color>> ReadCSVColors(string path, int maxParsesPerFrame)
        {
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                // HasHeaderRecord = false,
                Delimiter = ";"
            };

            // IDColorMap valuesMap = new IDColorMap();
            // config.RegisterClassMap(valuesMap);
            using (var reader = new StreamReader(path))
            {
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
}
