using System.Collections;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.Functionalities.LASImporter;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.LASImporter
{
    /// <summary>
    /// Debug-friendly LAS spawner.
    /// Logs every step so we can see if the layer system actually sent us LASPropertyData.
    /// </summary>
    [RequireComponent(typeof(HierarchicalObjectLayerGameObject))]
    public class LASSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        [SerializeField] private IncrementalPointCloud pointCloud;
        [SerializeField] private int pointsPerFrame = 5000;

        // will be replaced in LoadProperties(...)
        private LASPropertyData propertyData = null;
        private bool gotProperties = false;

        public LayerPropertyData PropertyData => propertyData;

        private void Awake()
        {
            Debug.Log($"[LASSpawner] Awake on {name}. Waiting for LoadProperties...");
        }

        private void Start()
        {
            Debug.Log($"[LASSpawner] Start on {name}. gotProperties={gotProperties}");
            // we don’t try to load immediately here, because sometimes Start is called
            // before the layer system injected properties. So we start a small routine.
            StartCoroutine(TryStartLoading());
        }

        /// <summary>
        /// Called by the layer system with all properties that were attached
        /// in the LayerPreset (LasPreset).
        /// </summary>
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            Debug.Log($"[LASSpawner] LoadProperties called on {name}. properties.Count={properties?.Count}");

            if (properties != null)
            {
                // log what we actually got
                for (int i = 0; i < properties.Count; i++)
                {
                    var p = properties[i];
                    Debug.Log($"[LASSpawner]   property[{i}] = {p?.GetType().Name}");
                }

                var lasData = properties.Get<LASPropertyData>();
                if (lasData != null)
                {
                    propertyData = lasData;
                    gotProperties = true;
                    Debug.Log($"[LASSpawner] Got LASPropertyData with URI={propertyData.LasFile}");
                }
                else
                {
                    Debug.LogWarning("[LASSpawner] LoadProperties: LASPropertyData NOT found in list.");
                }
            }
            else
            {
                Debug.LogWarning("[LASSpawner] LoadProperties: properties list was NULL.");
            }
        }

        /// <summary>
        /// We wait a few frames to give the layer system time to call LoadProperties.
        /// </summary>
        private IEnumerator TryStartLoading()
        {
            // wait up to 30 frames (~0.5 s) for properties to arrive
            const int maxFrames = 30;
            int frames = 0;

            while (!gotProperties && frames < maxFrames)
            {
                frames++;
                yield return null;
            }

            if (!gotProperties)
            {
                Debug.LogError("[LASSpawner] Gave up waiting for LASPropertyData. " +
                               "Most likely the prefab identifier in LasPreset.cs does not point to THIS prefab, " +
                               "or the layer system is not calling LoadProperties for this layer type.");
                yield break;
            }

            if (propertyData == null)
            {
                Debug.LogError("[LASSpawner] propertyData is still null after waiting.");
                yield break;
            }

            if (propertyData.LasFile == null)
            {
                Debug.LogError("[LASSpawner] LASPropertyData found, but LasFile is NULL.");
                yield break;
            }

            // turn URI into local path
            string localPath = AssetUriFactory.GetLocalPath(propertyData.LasFile);
            Debug.Log($"[LASSpawner] Resolved URI '{propertyData.LasFile}' to local path '{localPath}'");

            if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
            {
                Debug.LogError($"[LASSpawner] File does not exist at '{localPath}'. " +
                               "This means the filebrowser didn’t copy it or the URI is wrong.");
                yield break;
            }

            byte[] bytes = File.ReadAllBytes(localPath);
            Debug.Log($"[LASSpawner] Read {bytes.Length} bytes from LAS file. Starting streaming...");

            var parser = new LasStreamingParser(bytes);
            StartCoroutine(StreamPoints(parser));
        }

        private IEnumerator StreamPoints(LasStreamingParser parser)
        {
            while (!parser.Finished)
            {
                var chunk = parser.ReadNextPoints(pointsPerFrame);
                pointCloud.AddPoints(chunk);
                Debug.Log($"[LASSpawner] streamed {chunk.Count} points. totalRead={parser.PointsRead}");
                yield return null;
            }

            parser.Dispose();
            Debug.Log("[LASSpawner] finished streaming LAS");
        }
    }
}
