using System.Collections;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSetRenderers;
using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class ChangeScheduler : ScriptableObject
    {
        protected readonly ChangePlan changePlan = new();
        
        public virtual void Schedule(ComposableTileSetRenderer tileSetRenderer, Change change)
        {
                switch (change.Type)
                {
                    case TypeOfChange.Add:
                        change.UsingAction(changeToBePerformed => AddChange(tileSetRenderer, changeToBePerformed as Change));
                        break;
                    case TypeOfChange.Remove:
                        change.UsingAction(changeToBePerformed => RemoveChange(tileSetRenderer, changeToBePerformed as Change));
                        break;
                    case TypeOfChange.Replace:
                        // TODO: I am not 100% sure how to do replaces - so revisit this later
                        change.UsingAction(tileSetRenderer.TileRenderer.Replace);
                        break;
                }

            changePlan.Plan(change);
        }

        protected virtual Promise AddChange(ComposableTileSetRenderer tileSetRenderer, Change change)
        {
            var tile = change.Tile;
            var promise = tileSetRenderer.TileRenderer.Add(change);

            return promise
                .Then( () =>
                {
                    tileSetRenderer.TilesInView.Add(tile);
                    return promise;
                })
                .Catch(Debug.LogException) 
                as Promise;
        }

        protected virtual Promise RemoveChange(ComposableTileSetRenderer tileSetRenderer, Change change)
        {
            var tile = change.Tile;
            var promise = tileSetRenderer.TileRenderer.Remove(change);

            return promise
                .Then( () =>
                {
                    tileSetRenderer.TilesInView.Remove(tile);
                    return promise;
                })
                .Catch(Debug.LogException)
                as Promise;
        }

        public abstract IEnumerator Apply();
    }
}