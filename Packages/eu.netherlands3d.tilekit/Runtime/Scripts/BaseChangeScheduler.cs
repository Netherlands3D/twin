using System;
using System.Collections;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit.TileMappers;
using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseChangeScheduler : MonoBehaviour
    {
        protected readonly ChangePlan changePlan = new();
        
        public virtual void Schedule(BaseTileMapper tileMapper, Change change)
        {
            change.UsingAction(changeToBePerformed => ApplyChange(tileMapper, changeToBePerformed));
        
            changePlan.Plan(change);
        }

        private Promise ApplyChange(BaseTileMapper tileMapper, Change changeToBePerformed)
        {
            if (tileMapper is BaseEventBasedTileMapper eventBasedTileMapper)
            {
                return eventBasedTileMapper.EventChannel.RaiseChangeApply(eventBasedTileMapper.EventSource, changeToBePerformed);
            }

            return Promise.Rejected(new Exception("Unsupported tile mapper")) as Promise;
        }

        public abstract IEnumerator Apply();
    }
}