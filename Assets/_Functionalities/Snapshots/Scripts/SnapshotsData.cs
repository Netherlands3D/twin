using System.Collections.Generic;
using UnityEngine;
using static Netherlands3D.Snapshots.PeriodicSnapshots;

namespace Netherlands3D.Functionalities.Snapshots
{
    [CreateAssetMenu(menuName = "Netherlands3D/Data/SnapshotsData", fileName = "SnapshotsData", order = 0)]
    public class SnapshotsData : ScriptableObject
    {
        [SerializeField] public List<Moment> Moments = new List<Moment>();
    }
}
