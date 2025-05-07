using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;

namespace Netherlands3D.Tilekit
{
    public class TransitionPlanner : ITransitionPlanner
    {
        public List<Change> CreateTransition(HashSet<Tile> tilesInView, HashSet<Tile> stagedTiles)
        {
            var transitions = new List<Change>();
            
            foreach (var tile in stagedTiles)
            {
                if (tilesInView.Contains(tile)) continue;

                transitions.Add(Change.Add(tile));
            }

            foreach (var tile in tilesInView)
            {
                if (stagedTiles.Contains(tile)) continue;

                transitions.Add(Change.Remove(tile));
            }

            return transitions;
        }
    }
}