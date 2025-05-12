using System;
using System.Collections;
using Netherlands3D.Tilekit.Changes;
using RSG;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseChangeScheduler : IChangeScheduler
    {
        protected readonly ChangePlan changePlan = new();
        
        public virtual void Schedule(Change change)
        {
            changePlan.Plan(change);
        }

        public abstract IEnumerator Apply();
    }
}