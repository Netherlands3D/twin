using Netherlands3D.Snapshots;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Functionalities.Snapshots
{
    public class SnapshotsPanel : MonoBehaviour
    {
        private PeriodicSnapshots snapshots;
        [SerializeField] private Button downloadButton;

        private void Start()
        {
            snapshots = FindObjectOfType<PeriodicSnapshots>();
            downloadButton.onClick.AddListener(snapshots.DownloadSnapshots);
        }

        private void OnDestroy()
        {
            downloadButton.onClick.RemoveListener(snapshots.DownloadSnapshots);
        }
    }
}
