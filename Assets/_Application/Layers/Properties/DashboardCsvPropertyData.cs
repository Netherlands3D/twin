using System;
using System.IO;
using System.Runtime.Serialization;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "DashboardCsv")]
    public sealed class DashboardCsvPropertyData : LayerURLPropertyData
    {
        [JsonIgnore] public DashboardData Data { get; private set; }

        public DashboardCsvPropertyData()
        {
        }

        public DashboardCsvPropertyData(Uri url, DashboardData data) : base(url)
        {
            SetData(data);
        }

        internal void LoadFromProjectAsset()
        {
            using var reader = File.OpenText(AssetUriFactory.GetLocalPath(Url));
            SetData(DashboardCsv.Read(reader));
        }

        private void SetData(DashboardData data)
        {
            Data = data;
#if UNITY_EDITOR
            if (data == null || Url == null)
                return;

            var skippedLines = data.SkippedRowCount > 0 ? $" (regels {string.Join(", ", data.SkippedLineNumbers)})" : string.Empty;
            UnityEngine.Debug.Log($"Dashboard-CSV '{Path.GetFileName(Url.LocalPath)}' bevat {data.SourceRowCount} bronrijen, " +
                $"{data.Housing.Count} woonrijen, {data.NonHousing.Count} niet-woonrijen, {data.ProgramChanges.Count} programmawijzigingen " +
                $"en {data.SkippedRowCount} overgeslagen rijen{skippedLines}.");
#endif
        }
    }
}
