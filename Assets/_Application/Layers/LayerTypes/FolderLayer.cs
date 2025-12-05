using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "Folder")] //todo: make this parsable in the new mehtod
    [Obsolete("delete this class once the parser supports the old data into the new structure")]
    public class FolderLayer : LayerData
    {
        public FolderLayer(string name) : base(name)
        {
            // TODO: Move this out of the constructor but to a factory method or even better, the caller. This
            // introduces side-effects
            // ProjectData.Current.AddStandardLayer(this);
        }
        
        [JsonConstructor]
        public FolderLayer(string name, List<LayerPropertyData> layerProperties) : base(name, layerProperties)
        {
            // when deserializing - do not call ProjectData.Current.AddStandardLayer
        }
    }
}