using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit.TilesTransitionPlanners
{
    [CreateAssetMenu(menuName = "Tilekit/Transitions/Planner", fileName = "TransitionPlanner", order = 0)]
    public class TransitionPlanner : BaseTransitionPlanner
    {
        public override List<Change> CreateTransition(HashSet<Tile> tilesInView, HashSet<Tile> stagedTiles)
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