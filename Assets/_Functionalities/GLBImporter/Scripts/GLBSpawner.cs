using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTFast;
using Netherlands3D.Coordinates;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.GLBImporter
{
    public class GLBSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;
        private GLBPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;
        private GameObject importedObject;

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.OfType<GLBPropertyData>().FirstOrDefault();
            if (propertyData == null) return;

            // Property data is set here, and the parsing and loading of the actual data is done
            // in the start method, there a coroutine is started to load the data in a streaming fashion.
            // If we do that here, then this may conflict with the loading of the project file and it would
            // cause duplication when adding a layer manually instead of through the loading mechanism
            this.propertyData = propertyData;
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

            var consoleLogger = new GLTFast.Logging.ConsoleLogger();
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
            var holgo = GetComponent<HierarchicalObjectLayerGameObject>();

            var isGeoReferenced = false;
            if (returnedGameObject.transform.childCount > 0)
            {
                //GLB stores coordinates as 32 bit floats, and therefore cannot accurately be georeferenced.
                //However, we will still do a check to ensure at least the model will appear roughly where it should if it is still georeferenced despite this.
                var referencePosition = returnedGameObject.transform.GetChild(0).localPosition;
                if (EPSG7415.IsValid(referencePosition.x, referencePosition.y, referencePosition.z, out var origin))
                {
                    PositionGeoReferencedGlb(returnedGameObject, holgo, origin);
                }
            }
            
            if(!isGeoReferenced)
                PositionNonGeoReferencedGlb(returnedGameObject, holgo);

            importedObject = returnedGameObject;
            foreach (var meshFilter in returnedGameObject.GetComponentsInChildren<MeshFilter>())
            {
                meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            // Object is loaded / replaced - trigger the application of styling
            holgo.ApplyStyling();
        }

        private void PositionNonGeoReferencedGlb(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo)
        {
            //if we have saved transform data, we will use that position, otherwise we will use this object's current position.
            if (holgo.TransformIsSetFromProperty)
            {
                //apply any transformation if present in the data
                var transformPropterty = (TransformLayerPropertyData)((ILayerWithPropertyData)holgo).PropertyData;
                transform.position = transformPropterty.Position.ToUnity();
                returnedGameObject.transform.SetParent(transform, false); // imported object should move to saved (parent's) position
            }
            else
            {
                //no transform property or georeference present, this object should just take on the parent's position
                returnedGameObject.transform.SetParent(transform, false); // imported object should move to saved (parent's) position
            }
        }

        private void PositionGeoReferencedGlb(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo, Coordinate origin)
        {
            if (!holgo.TransformIsSetFromProperty) //move the camera only if this is is a user imported object, not if this is a project import. We know this because a project import has its Transform property set.
            {
                var cameraMover = Camera.main.GetComponent<MoveCameraToCoordinate>();
                cameraMover.LookAtTarget(origin, cameraDistanceFromGeoReferencedObject); //move the camera to the georeferenced position, this also shifts the origin if needed.
            }
            
            holgo.WorldTransform.MoveToCoordinate(origin); //set this object to the georeferenced position, since this is the correct position.
            returnedGameObject.transform.SetParent(transform, false); // we set the parent and reset its localPosition, since the origin might have changed.
            returnedGameObject.transform.localPosition = Vector3.zero;
            foreach (Transform t in returnedGameObject.transform)
            {
                t.localPosition -= origin.ToUnity();
            }

            // imported object should stay where it is initially, and only then apply any user transformations if present.
            if (holgo.TransformIsSetFromProperty)
            {
                var transformPropterty = (TransformLayerPropertyData)((ILayerWithPropertyData)holgo).PropertyData;
                holgo.WorldTransform.MoveToCoordinate(transformPropterty.Position); //apply saved user changes to position.
            }
        }

        public void SetGlbPathInPropertyData(string fullPath)
        {
            var propertyData = PropertyData as GLBPropertyData;
            propertyData.GlbFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }

        private string GetGlbPathFromPropertyData()
        {
            if (propertyData.GlbFile == null)
                return "";

            var localPath = AssetUriFactory.GetLocalPath(propertyData.GlbFile);
            return localPath;
        }
    }
}