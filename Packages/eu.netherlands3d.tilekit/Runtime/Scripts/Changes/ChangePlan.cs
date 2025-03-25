using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Tilekit.TileSets;

namespace Netherlands3D.Tilekit.Changes
{
    // TODO: Add prioritization
    public class ChangePlan : IEnumerable<KeyValuePair<Tile, Change>>
    {
        /// <summary>
        /// Use a dictionary to make lookups based on Tile O(1) instead of O(n).
        /// </summary>
        private readonly Dictionary<Tile, Change> changes = new ();
        private readonly Dictionary<Change, HashSet<Tile>> lookupTable = new ();

        public void Plan(Change change)
        {
            Add(change);
            change.Plan();
        }

        public Change FindByTile(Tile tile)
        {
            return changes.GetValueOrDefault(tile);
        }

        public void Cancel(Change change)
        {
            change.Cancel();
        }

        private void Add(Change change)
        {
            // If a previous change is detected, perhaps we should wrap the new change in the old one as it will 
            // replace the previous? If the new change is pending and cancelled, it should 'unwrap' if the previous 
            // is still active?
            
            // TODO: check if the change conflicts with a pending or running change and adapt the change plan
            // TODO: Is there an error in my thinking? What happens if a Tile change is no longer pending but a new
            //   change is issued; shouldn't it stay in queue?
            var affectedTiles = change.AffectedTiles();
            foreach (var affectedTile in affectedTiles)
            {
                if (changes.ContainsKey(affectedTile)) continue;

                changes.Add(affectedTile, change);
                if (lookupTable.ContainsKey(change) == false)
                {
                    lookupTable.Add(change, new HashSet<Tile>());
                }

                lookupTable[change].Add(affectedTile);
            }
            
            // If a change is cancelled or has completed; remove it
            change.Completed += Remove;
            change.Cancelled += Remove;
        }

        private void Remove(Change change)
        {
            var tiles = lookupTable.GetValueOrDefault(change);
            foreach (var tile in tiles)
            {
                changes.Remove(tile);
            }

            lookupTable.Remove(change);

            // Clean up references
            change.Completed -= Remove;
            change.Cancelled -= Remove;
        }

        public IEnumerator<KeyValuePair<Tile, Change>> GetEnumerator()
        {
            return new Dictionary<Tile, Change>(changes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}