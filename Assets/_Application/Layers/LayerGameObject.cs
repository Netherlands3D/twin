using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
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
    public abstract class LayerGameObject : MonoBehaviour, IStylable
    {
        [SerializeField] private string prefabIdentifier;
        public string PrefabIdentifier => prefabIdentifier;

        public string Name
        {
            get => LayerData.Name;
            set => LayerData.Name = value;
        }

        public bool HasLayerData => layerData != null;
        
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

        Dictionary<string, LayerStyle> IStylable.Styles => LayerData.Styles;
        private readonly List<LayerFeature> cachedFeatures = new();

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

            ApplyStyling();
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
            
            // !IMPORTANT: we deselect the layer, because if we don't do this, the TransformHandles might be connected to this LayerGameObject
            // This causes conflicts between the transformHandles and the Origin Shifter system, because the Transform handles will try to move the gameObject to the old (pre-shift) position.
            LayerData.DeselectLayer(); 
            //move the camera to the center of the bounds, and move it back by the size of the bounds (2x the extents)
            Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget(Bounds.Center, Bounds.GetSizeMagnitude());//sizeMagnitude returns 2x the extents
        }

#region Styling
        protected Symbolizer GetStyling(LayerFeature feature)
        {
            var symbolizer = new Symbolizer();
            foreach (var style in LayerData.Styles)
            {
                symbolizer = style.Value.ResolveSymbologyForFeature(symbolizer, feature);
            }

            return symbolizer;
        }

        public virtual void ApplyStyling()
        {
            //initialize the layer's style        
        }
#endregion

#region Features
        public List<LayerFeature> GetFeatures<T>() where T : Component
        {
            cachedFeatures.Clear();

            // By default, consider each Unity.Component of type T as a "Feature" and create an ExpressionContext to
            // select the correct styling Rule to apply to the given "Feature". 
            var components = GetComponentsInChildren<T>();

            foreach (var component in components)
            {
                cachedFeatures.Add(CreateFeature(component));
            }

            return cachedFeatures;
        }

        /// <summary>
        /// Create a Feature object from the given Component, this method is meant as an extension point
        /// for LayerGameObjects to add more information to the Attribute (ExpressionContext) of the given Feature.
        ///
        /// For example: to be able to match on material names you need to include the material names in the attributes.
        /// </summary>
        protected LayerFeature CreateFeature(object geometry)
        {
            return AddAttributesToLayerFeature(LayerFeature.Create(this, geometry));
        }

        /// <summary>
        /// Construct attributes onto the layer feature so that the styling system can
        /// use that as input to pick the correct style.
        ///
        /// This could result in, what seems, duplication if a layer has a native type that also produces a series of
        /// properties; however, this mechanism will ensure that all layers are treated equally for the styling system
        /// and that the properties are encoded as Expression types so that the expression system does not need to do
        /// ad hoc implicit conversions from a primitive to an Expression type. 
        /// </summary>
        protected virtual LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            return feature;
        }
        #endregion
    }
}