using System.Collections;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileMappers;
using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class ChangeScheduler : ScriptableObject
    {
        protected readonly ChangePlan changePlan = new();
        
        public virtual void Schedule(ComposableTileMapper tileMapper, Change change)
        {
                switch (change.Type)
                {
                    case TypeOfChange.Add:
                        change.UsingAction(changeToBePerformed => AddChange(tileMapper, changeToBePerformed as Change));
                        break;
                    case TypeOfChange.Remove:
                        change.UsingAction(changeToBePerformed => RemoveChange(tileMapper, changeToBePerformed as Change));
                        break;
                }

            changePlan.Plan(change);
        }

        protected virtual Promise AddChange(ComposableTileMapper tileMapper, Change change)
        {
            var tile = change.Tile;
            var promise = tileMapper.TileRenderer.Add(change);

            return promise
                .Then( () =>
                {
                    tileMapper.TilesInView.Add(tile);
                    return promise;
                })
                .Catch(Debug.LogException) 
                as Promise;
        }

        protected virtual Promise RemoveChange(ComposableTileMapper tileMapper, Change change)
        {
            var tile = change.Tile;
            var promise = tileMapper.TileRenderer.Remove(change);

            return promise
                .Then( () =>
                {
                    tileMapper.TilesInView.Remove(tile);
                    return promise;
                })
                .Catch(Debug.LogException)
                as Promise;
        }

        public abstract IEnumerator Apply();
    }
}