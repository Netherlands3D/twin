using System.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit.Changes.ChangeSchedulers
{
    [CreateAssetMenu(menuName = "Tilekit/Changes/ImmediateScheduler", fileName = "ImmediateChangeScheduler", order = 0)]
    public class ImmediateChangeScheduler : BaseChangeScheduler
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