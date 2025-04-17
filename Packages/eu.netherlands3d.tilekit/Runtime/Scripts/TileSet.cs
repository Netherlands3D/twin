using System.Runtime.Serialization;
using Netherlands3D.Tilekit.TileSets;

namespace Netherlands3D.Tilekit
{
    public struct TileSet
    {
        public Tile Root { get; }

        public TileSet(Tile root)
        {
            Root = root;
        }
    }
}