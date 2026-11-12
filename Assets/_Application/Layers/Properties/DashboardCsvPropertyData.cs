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
            Data = data;
        }

        internal void LoadFromProjectAsset()
        {
            using var reader = File.OpenText(AssetUriFactory.GetLocalPath(Url));
            Data = DashboardCsv.Read(reader);
        }
    }
}
