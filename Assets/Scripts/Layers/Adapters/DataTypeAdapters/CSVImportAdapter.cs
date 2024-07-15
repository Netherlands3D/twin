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
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CSVImportAdapter", fileName = "CSVImportAdapter", order = 0)]
    public class CSVImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private UnityEvent<string> csvReplacedMessageEvent = new();
        [SerializeField] private UnityEvent<float> progressEvent = new();
        public int maxParsesPerFrame = 100;
        private static DatasetLayer activeDatasetLayer; //todo: allow multiple datasets to exist

        readonly CsvConfiguration config = new(CultureInfo.CurrentCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ";"
        };

        public bool Supports(LocalFile localFile)
        {
            if(!localFile.LocalFilePath.ToLower().EndsWith(".csv"))
                return false;

            // Check if we can read the CVS using our expected config
            using var reader = new StreamReader(localFile.LocalFilePath);
            using var csv = new CsvReader(reader, config);

            return csv.Read();
        }

        public void Execute(LocalFile localFile)
        {
            ParseCSVFile(localFile.LocalFilePath);
        }

        public void ParseCSVFile(string fullPath)
        {
            if (activeDatasetLayer != null) //todo: temp fix to allow only 1 dataset layer
            {
                activeDatasetLayer.RemoveCustomColorSet(); //remove before destroying because otherwise the Start() function of the new colorset will apply the new colors before the OnDestroy function can clean up the old colorset. 

                activeDatasetLayer.DestroyLayer();
                csvReplacedMessageEvent.Invoke("Het oude CSV bestand is vervangen door het nieuw gekozen CSV bestand.");
            }

            var fileName = Path.GetFileName(fullPath);
            var datasetLayer = new GameObject(fileName).AddComponent<DatasetLayer>();
            datasetLayer.StartCoroutine(StreamReadCSV(fullPath, datasetLayer, maxParsesPerFrame));

            activeDatasetLayer = datasetLayer;
        }

        private IEnumerator StreamReadCSV(string path, DatasetLayer layer, int maxParsesPerFrame)
        {
            yield return null; //wait a frame for the created layer to be reparented and set up correctly to ensure the correct priority index
                  
            var dictionary = new Dictionary<string, Color>();
            var fileSize = new FileInfo(path).Length;
            var progress = 0f;

            using var streamReader = new StreamReader(path);
            using var csvReader = new CsvReader(streamReader, config);

            csvReader.Read();
            csvReader.ReadHeader();
            var count = 0;

            while (csvReader.Read())
            {
                var id = csvReader.GetField<string>("BagId");
                var hexColor = csvReader.GetField<string>("HexColor");
                dictionary[id] = ParseHexColor(hexColor);
                count++;

                if (count >= maxParsesPerFrame)
                {
                    progress = (float)streamReader.BaseStream.Position / fileSize;
                    progressEvent.Invoke(progress);
                    count = 0;
                    yield return null;
                }
            }

            //return the remaining elements of the part not divisible by maxParsesPerFrame 
            if (dictionary.Count > 0)
            {
                var cl = GeometryColorizer.AddAndMergeCustomColorSet(layer.PriorityIndex, dictionary);
                layer.SetColorSetLayer(cl, false);
                progressEvent.Invoke(1f);
            }
        }

        public static Color ParseHexColor(string hex)
        {
            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            var canParse = ColorUtility.TryParseHtmlString(hex, out var color);
            return canParse ? color : Interaction.NO_OVERRIDE_COLOR;
        }
    }
}