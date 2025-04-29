using System.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit.Changes.ChangeSchedulers
{
    public class ImmediateChangeScheduler : BaseChangeScheduler
    {
        public override IEnumerator Apply()
        {
            foreach (var change in changePlan)
            {
                change.Value.Trigger();
                yield return new WaitForEndOfFrame();
            }

            yield break;
        }
    }
}