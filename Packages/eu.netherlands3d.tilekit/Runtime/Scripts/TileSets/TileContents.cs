using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets", Name = "TileContents")]
    public class TileContents : List<TileContent>
    {
    }
}