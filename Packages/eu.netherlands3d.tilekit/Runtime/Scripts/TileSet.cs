using System.Runtime.Serialization;
using Netherlands3D.Tilekit.TileSets;

namespace Netherlands3D.Tilekit
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit", Name = "TileSet")]
    public class TileSet
    {
        public Tile Root { get; } // Root tile of the tileSet

        public TileSet(Tile root)
        {
            Root = root;
        }
    }
}