using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
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

        //Use this function to initialize anything that has to be done after either:
        // 1. Instantiating prefab -> creating new LayerData, or
        // 2. Creating LayerData (from project), Instantiating prefab, coupling that LayerData to this LayerGameObject
        protected virtual void InitializeVisualisation()
        {
            if (layerData == null) //if the layer data object was not initialized when creating this object, create a new LayerDataObject
                CreateProxy();

            layerData.LayerDoubleClicked.AddListener(CenterInView); //only subscribe to this event once the layerData component has been initialized
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

        private void OnDestroy()
        {
            //don't unsubscribe in OnDisable, because we still want to be able to center to a 
            layerData.LayerDoubleClicked.RemoveListener(CenterInView);
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

        public void CenterInView(LayerData layer)
        {
            if (Bounds == null)
            {
                Debug.LogError("Bounds object is null, no bounds specified to center to.");
                return;
            }

            if(Bounds.BottomLeft.PointsLength > 2)
                Bounds.Convert(CoordinateSystem.RDNAP); //todo: make this CRS independent
            else
                Bounds.Convert(CoordinateSystem.RD);
            
            //move the camera to the center of the bounds, and move it back by the size of the bounds (2x the extents)
            Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget(Bounds.Center, Bounds.GetSizeMagnitude());//sizeMagnitude returns 2x the extents
        }
    }
}