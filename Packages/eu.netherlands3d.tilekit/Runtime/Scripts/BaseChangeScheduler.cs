using System;
using System.Collections;
using Netherlands3D.Tilekit.Changes;
using RSG;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseChangeScheduler : IChangeScheduler
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
            
            promise.Resolve();
            return promise;
        }

        public abstract IEnumerator Apply();
    }
}