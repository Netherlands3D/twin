using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "DashboardCsv")]
    public sealed class DashboardCsvPropertyData : LayerURLPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri logoFile;

        [JsonIgnore] public readonly UnityEvent<Uri> OnLogoFileChanged = new();
        [JsonIgnore] public DashboardData Data { get; private set; }
        [JsonIgnore] internal bool HasLocalSource { get; set; }
        [JsonIgnore] public readonly UnityEvent DataChanged = new();

        [JsonIgnore]
        public Uri LogoFile
        {
            get => logoFile;
            set
            {
                logoFile = value;
                OnLogoFileChanged.Invoke(value);
            }
        }

        public DashboardCsvPropertyData()
        {
        }

        public DashboardCsvPropertyData(Uri url, DashboardData data) : base(url)
        {
            HasLocalSource = data != null;
            SetData(data);
        }

        internal void LoadFromProjectAsset()
        {
            SetData(DashboardCsv.ReadFile(AssetUriFactory.GetLocalPath(Url)));
        }

        internal void ClearLoadedData() => SetData(null);

        IEnumerable<LayerAsset> ILayerPropertyDataWithAssets.GetAssets()
        {
            foreach (var asset in base.GetAssets())
                yield return asset;

            if (logoFile != null)
                yield return new LayerAsset(this, logoFile);
        }

        private void SetData(DashboardData data)
        {
            Data = data;
            DataChanged.Invoke();
#if UNITY_EDITOR
            if (data == null || Url == null)
                return;

            var skippedLines = data.SkippedRowCount > 0 ? $" (regels {string.Join(", ", data.SkippedLineNumbers)})" : string.Empty;
            UnityEngine.Debug.Log($"Dashboardbestand '{Path.GetFileName(Url.LocalPath)}' bevat {data.SourceRowCount} bronrijen, " +
                $"{data.Housing.Count} woonrijen, {data.NonHousing.Count} niet-woonrijen, {data.ProgramChanges.Count} programmawijzigingen " +
                $"en {data.SkippedRowCount} overgeslagen rijen{skippedLines}.");
#endif
        }
    }
}
