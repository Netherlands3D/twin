using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Twin.Layers
{
    public abstract class LayerGameObject : MonoBehaviour
    {
        [SerializeField] private string prefabIdentifier;
        public string PrefabIdentifier => prefabIdentifier;

        public string Name
        {
            get => LayerData.Name;
            set => LayerData.Name = value;
        }

        private ReferencedLayerData layerData;

        public ReferencedLayerData LayerData
        {
            get
            {
                if (layerData == null)
                {
                    CreateProxy();
                }

                return layerData;
            }
            set
            {
                layerData = value;

                foreach (var layer in GetComponents<ILayerWithPropertyData>())
                {
                    layer.LoadProperties(layerData.LayerProperties); //initial load
                }
            }
        }

        [Space] public UnityEvent onShow = new();
        public UnityEvent onHide = new();

        public abstract BoundingBox Bounds { get; }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(prefabIdentifier) || prefabIdentifier == "00000000000000000000000000000000")
            {
                var pathToPrefab = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(pathToPrefab))
                {
                    var metaID = AssetDatabase.GUIDFromAssetPath(pathToPrefab);
                    prefabIdentifier = metaID.ToString();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif

        protected virtual void Start()
        {
            InitializeVisualisation();
        }

        protected virtual void InitializeVisualisation()
        {
            if (LayerData == null) //if the layer data object was not initialized when creating this object, create a new LayerDataObject
                CreateProxy();

            OnLayerActiveInHierarchyChanged(LayerData.ActiveInHierarchy); //initialize the visualizations with the correct visibility

            InitializeStyling();
        }

        private void CreateProxy()
        {
            ProjectData.AddReferenceLayer(this);
        }

        protected virtual void OnEnable()
        {
            onShow.Invoke();
        }

        protected virtual void OnDisable()
        {
            onHide.Invoke();
        }

        public virtual void OnSelect()
        {
        }

        public virtual void OnDeselect()
        {
        }

        public void DestroyLayer()
        {
            layerData.DestroyLayer();
        }

        public virtual void DestroyLayerGameObject()
        {
            Destroy(gameObject);
        }

        public virtual void OnProxyTransformChildrenChanged()
        {
            //called when the Proxy's children change            
        }

        public virtual void OnProxyTransformParentChanged()
        {
            //called when the Proxy's parent changes            
        }

        public virtual void OnSiblingIndexOrParentChanged(int newSiblingIndex)
        {
            //called when the Proxy's sibling index changes. Also called when the parent changes but the sibling index stays the same.            
        }

        public virtual void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            //called when the Proxy's active state changes.          
        }

        public virtual void InitializeStyling()
        {
            //initialize the layer's style        
        }

        public void CenterInView()
        {
            //move the camera to the center of the bounds, and move it back by the size of the bounds (2x the extents)
            var center = Bounds.Center;
            var sizeMagnitude = Bounds.GetSizeMagnitude(); //sizeMagnitude returns 2x the extents

            if (sizeMagnitude > 20000) // if the size of the bounds is larger than 20km, we don't move the camera
            {
                Debug.LogWarning("Extents too large, not moving camera");
                return;
            }

            var mainCamera = Camera.main;
            // Keep the current camera orientation
            Vector3 cameraDirection = mainCamera.transform.forward;

            var distance = 300d;

            //if the object is smaller than 2km in diameter, we will center the object in the view.
            //if the size of the bounds is larger than 2 km, we will center on the object with a fixed distance instead of trying to fit the object in the view
            if (sizeMagnitude < 2000)
            {
                // Compute the necessary distance to fit the entire object in view
                var fovRadians = mainCamera.fieldOfView * Mathf.Deg2Rad;
                distance = sizeMagnitude / (2 * Mathf.Tan(fovRadians / 2));
            }

            var currentCameraPosition = mainCamera.GetComponent<WorldTransform>().Coordinate;
            var difference = (currentCameraPosition - center).Convert(CoordinateSystem.RD); //use RD since this expresses the difference in meters, so we can use the SqrDistanceBeforeShifting to check if we need to shift.
            ulong sqDist = (ulong)(difference.easting * difference.easting + difference.northing * difference.northing);
            if (sqDist > Origin.current.SqrDistanceBeforeShifting)
            {
                // move the origin to the bounds center with height 0, to assure large jumps do not result in errors when centering.
                var newOrigin = center.Convert(CoordinateSystem.WGS84); //2d coord system to get rid of height.
                Origin.current.MoveOriginTo(newOrigin);
            }

            mainCamera.transform.position = center.ToUnity() - cameraDirection * (float)distance; //todo: do the final offset after origin shift for precision.
        }
    }
}