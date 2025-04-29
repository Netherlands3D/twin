using System;
using System.Collections;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit.TileMappers;
using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseChangeScheduler : ScriptableObject
    {
        protected readonly ChangePlan changePlan = new();
        
        public virtual void Schedule(BaseTileMapper tileMapper, Change change)
        {
            // change.UsingAction(changeToBePerformed => ApplyChange(tileMapper, changeToBePerformed));
        
            changePlan.Plan(change);
        }

        private void ApplyChange(BaseTileMapper tileMapper, Change changeToBePerformed)
        {
            if (tileMapper is BaseEventBasedTileMapper eventBasedTileMapper)
            {
                eventBasedTileMapper.EventChannel.ChangeApply.Invoke(eventBasedTileMapper.TilekitEventSource, changeToBePerformed);
            }
        }

        public abstract IEnumerator Apply();
    }
}