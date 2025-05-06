using System;
using System.Collections;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Twin.Tilekit;
using Netherlands3D.Twin.Tilekit.Events;
using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseChangeScheduler : ScriptableObject, IChangeScheduler
    {
        protected readonly ChangePlan changePlan = new();
        
        public virtual void Schedule(ITileSetProvider tileSetProvider, Change change)
        {
            change.UsingAction(changeToBePerformed => ApplyChange(tileSetProvider, changeToBePerformed));
        
            changePlan.Plan(change);
        }

        private Promise ApplyChange(ITileSetProvider tileSetProvider, Change changeToBePerformed)
        {
            Promise promise = new Promise();
            if (tileSetProvider.TileSet.HasValue == false)
            {
                promise.Reject(
                    new Exception("TileSet is not loaded (yet), unable to apply a change where there is no tileset")
                );
                return promise;
            }
            
            var tileSetId = tileSetProvider.TileSetId;
            var eventStreamContext = new TileSetEventStreamContext(
                tileSetId,
                tileSetProvider.TileSet.Value
            );
            EventBus.Stream(tileSetId).ChangeApply.Invoke(eventStreamContext, changeToBePerformed);
            
            promise.Resolve();
            return promise;
        }

        public abstract IEnumerator Apply();
    }
}