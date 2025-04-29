﻿using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;

namespace Netherlands3D.Tilekit.TilesTransitionPlanners
{
    public class CrudeTransitionPlanner : BaseTransitionPlanner
    {
        public override List<Change> CreateTransition(List<Tile> tilesInView, List<Tile> stagedTiles)
        {
            var transitions = new List<Change>();
            
            // Crude diff - if a staged tile is not in the active tiles array, add it
            foreach (var tile in stagedTiles)
            {
                if (tilesInView.Contains(tile)) continue;

                transitions.Add(Change.Add(tile));
            }

            // Crude diff - if an active tile is not in the staged array, remove it
            foreach (var tile in tilesInView)
            {
                if (stagedTiles.Contains(tile)) continue;

                transitions.Add(Change.Remove(tile));
            }

            return transitions;
        }
    }
}