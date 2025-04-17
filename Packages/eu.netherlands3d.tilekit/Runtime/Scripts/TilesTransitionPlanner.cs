using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class TilesTransitionPlanner : ScriptableObject
    {
        public abstract List<Change> CreateTransition(List<Tile> tilesInView, List<Tile> stagedTiles);
    }
}