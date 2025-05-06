using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseTransitionPlanner : ScriptableObject, ITransitionPlanner
    {
        public abstract List<Change> CreateTransition(HashSet<Tile> tilesInView, HashSet<Tile> stagedTiles);
    }
}