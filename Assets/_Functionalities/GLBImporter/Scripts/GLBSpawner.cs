using System.Collections;
using System.Collections.Generic;
using System.IO;
using GLTFast;
using GLTFast.Logging;
using Netherlands3D.Coordinates;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.GLBImporter
{
    [RequireComponent(typeof(HierarchicalObjectLayerGameObject))]
    public class GLBSpawner : MonoBehaviour, IVisualizationWithPropertyData, IImportedObject
    {
        private GLBPropertyData propertyData = new();
        private GameObject importedObject;
        private HierarchicalObjectLayerGameObject layerGameObject;
        private MoveCameraToCoordinate cameraMover;
        
        [Header("Settings")]
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;

        public UnityEvent<GameObject> ObjectVisualized { get; } = new();

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.Get<GLBPropertyData>();
            if (propertyData == null) return;

            // Property data is set here, and the parsing and loading of the actual data is done
            // in the start method, there a coroutine is started to load the data in a streaming fashion.
            // If we do that here, then this may conflict with the loading of the project file and it would
            // cause duplication when adding a layer manually instead of through the loading mechanism
            this.propertyData = propertyData;
        }

        private void Awake()
        {
            cameraMover = Camera.main.GetComponent<MoveCameraToCoordinate>();
            layerGameObject = GetComponent<HierarchicalObjectLayerGameObject>();
        }

        private void Start()
        {
            StartImport(); //called after loading properties or after setting the file path through the import adapter
        }

        private void StartImport()
        {
            if (importedObject)
                Destroy(importedObject);

            var objPath = GetGlbPathFromPropertyData();
            ImportGlb(objPath);
        }

        private void ImportGlb(string glbPath)
        {
            StartCoroutine(LoadGlb(glbPath));
        }

        private IEnumerator LoadGlb(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                Debug.LogError("Invalid file path.");
                yield break;
            }

            var consoleLogger = new ConsoleLogger();
            var materialGenerator = new NL3DMaterialGenerator();
            GltfImport gltf = new GltfImport(null, null, materialGenerator, consoleLogger);

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            byte[] data = new byte[fileStream.Length];
            int totalRead = 0;
            fileStream.Read(data, 0, data.Length);
            fileStream.Close();

            var loadTask = gltf.Load(data);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (!loadTask.Result)
            {
                Debug.LogError("Failed to load GLB file.");
                yield break;
            }

            var root = new GameObject("GLBRoot");
            var instantiateTask = gltf.InstantiateMainSceneAsync(root.transform);
            while (!instantiateTask.IsCompleted)
            {
                yield return null;
            }

            if (!instantiateTask.Result)
            {
                Debug.LogError("Failed to instantiate GLB.");
                yield break;
            }
            root.transform.Rotate(Vector3.up, 180); // compensate for 180 degree rotation
            OnObjImported(root);
        }

        private void OnObjImported(GameObject returnedGameObject)
        {
            PositionImportedGameObject(returnedGameObject);

            importedObject = returnedGameObject;
            foreach (var meshFilter in returnedGameObject.GetComponentsInChildren<MeshFilter>())
            {
                meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            ObjectVisualized.Invoke(returnedGameObject);
        }

        private void PositionImportedGameObject(GameObject returnedGameObject)
        {
            var rootObject = GetRootObject(returnedGameObject);
            if (rootObject)
            {
                //GLB stores coordinates as 32 bit floats, and therefore cannot accurately be georeferenced.
                //However, we will still do a check to ensure at least the model will appear roughly where it should if it is still georeferenced despite this.
                var isGeoReferenced = IsGeoReferenced(
                    rootObject.localPosition.x, 
                    rootObject.localPosition.y, 
                    rootObject.localPosition.z, 
                    out var geoReferencedOrigin
                );

                if (isGeoReferenced)
                {
                    PositionGeoReferencedGlb(geoReferencedOrigin);
                    ParentImportedGameObject(returnedGameObject, geoReferencedOrigin);
                    return;
                }
            }
            
            if (layerGameObject.LayerData.IsNew)
            {
                GetComponent<WorldTransform>().MoveToCoordinate(new Coordinate(transform.position));
            }

            returnedGameObject.transform.SetParent(transform, false); // imported object should move to saved (parent's) position
        }

        private Transform GetRootObject(GameObject returnedGameObject)
        {
            if (returnedGameObject.transform.childCount <= 0) return null;

            return returnedGameObject.transform.GetChild(0);
        }

        private bool IsGeoReferenced(double x, double y, double z, out Coordinate rdOrigin)
        {
            rdOrigin = new Coordinate(CoordinateSystem.RDNAP, x, z, 0);
            if (  rdOrigin.IsValid())
            {
                rdOrigin = new Coordinate(CoordinateSystem.RDNAP, x, z, 0); //don't offset the height
                return true;
            }

            rdOrigin = new Coordinate(CoordinateSystem.RDNAP, x, y, 0);
            if (rdOrigin.IsValid())
            {
                rdOrigin = new Coordinate(CoordinateSystem.RDNAP, x, y, 0); //don't offset the height
                return true;
            }

            rdOrigin = new Coordinate();
            return false;
        }
        
        private void PositionGeoReferencedGlb(Coordinate origin)
        {
            if (layerGameObject.LayerData.IsNew)
            {
                cameraMover.LookAtTarget(origin, cameraDistanceFromGeoReferencedObject); //move the camera to the georeferenced position, this also shifts the origin if needed.
            }
            
            Coordinate position = origin;
            if (!layerGameObject.LayerData.IsNew)
            {
                position = layerGameObject.LayerData.GetProperty<TransformLayerPropertyData>().Position;
            }
            
            layerGameObject.WorldTransform.MoveToCoordinate(position);
        }

        private void ParentImportedGameObject(GameObject returnedGameObject, Coordinate origin)
        {
            returnedGameObject.transform.SetParent(transform, false); // we set the parent and reset its localPosition, since the origin might have changed.
            returnedGameObject.transform.localPosition = Vector3.zero;
            foreach (Transform t in returnedGameObject.transform)
            {
                t.localPosition -= origin.ToUnity();
            }
        }

        private string GetGlbPathFromPropertyData()
        {
            if (propertyData.GlbFile == null) return string.Empty;

            return AssetUriFactory.GetLocalPath(propertyData.GlbFile);
        }
    }
}