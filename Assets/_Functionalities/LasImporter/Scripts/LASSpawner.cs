using System.Collections;
using System.IO;
using Netherlands3D.Functionalities.LASImporter;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.LASImporter
{
    public class LASSpawner : MonoBehaviour
    {
        [SerializeField] private IncrementalPointCloud pointCloud;
        [SerializeField] private int pointsPerFrame = 5000;

        private LASPropertyData propertyData;

        private void Awake()
        {
            // the layer system injects the property component, we just grab it
            propertyData = GetComponent<LASPropertyData>();
        }

        private void Start()
        {
            if (propertyData == null || propertyData.LasFile == null)
            {
                Debug.LogError("LASSpawner: no LAS property data / file set.");
                return;
            }

            // convert URI back to local path (this works with your filebrowser /
            // IndexedDB flow, because the filebrowser copied the file locally)
            var localPath = AssetUriFactory.GetLocalPath(propertyData.LasFile);

            if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
            {
                Debug.LogError($"LASSpawner: file not found at {localPath}");
                return;
            }

            // read all bytes once – for huge files you could stream from file instead
            var data = File.ReadAllBytes(localPath);

            var parser = new LasStreamingParser(data);
            StartCoroutine(StreamPoints(parser));
        }

        private IEnumerator StreamPoints(LasStreamingParser parser)
        {
            while (!parser.Finished)
            {
                var chunk = parser.ReadNextPoints(pointsPerFrame);
                pointCloud.AddPoints(chunk);
                yield return null;
            }

            parser.Dispose();
            Debug.Log("LASSpawner: finished streaming LAS");
        }
    }
}
