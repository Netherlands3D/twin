using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;

namespace Netherlands3D.Tilekit
{
    public interface ITransitionPlanner
    {
        public List<Change> CreateTransition(HashSet<Tile> tilesInView, HashSet<Tile> stagedTiles);
    }
}