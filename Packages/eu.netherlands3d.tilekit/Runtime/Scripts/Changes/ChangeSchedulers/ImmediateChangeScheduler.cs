using System.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit.Changes.ChangeSchedulers
{
    [CreateAssetMenu(menuName = "Netherlands3D/Tilekit/Schedulers/Immediate", fileName = "ImmediateChangeScheduler", order = 0)]
    public class ImmediateChangeScheduler : ChangeScheduler
    {
        public override IEnumerator Apply()
        {
            foreach (var change in changePlan)
            {
                change.Value.Trigger();
            }

            yield break;
        }
    }
}