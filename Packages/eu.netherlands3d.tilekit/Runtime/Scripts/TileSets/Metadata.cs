using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets", Name = "Metadata")]
    public class Metadata : Dictionary<string, string>
    {
        
    }
}