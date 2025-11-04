using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Services;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Twin.Layers
{
    public enum SpawnLocation
    {
        Auto = -1, // Do not pass a spawn location - similar to Instantiate without position and rotation properties
        OpticalCenter = 0, //Center of the screen calculated through the optical Raycaster, keep the prefab rotation
        CameraPosition = 1, //Position and rotation of the main camera
        PrefabPosition = 2 //keep the original prefab position and rotation
    }
    
    public abstract class LayerGameObject : MonoBehaviour, IStylable
    {
        public const int DEFAULT_MASK_BIT_MASK = 16777215; //(2^24)-1; 
        
        [SerializeField] private string prefabIdentifier;
        [SerializeField] private SpriteState thumbnail;
        [SerializeField] private SpawnLocation spawnLocation;
        public string PrefabIdentifier => prefabIdentifier;
        public SpriteState Thumbnail => thumbnail;
        public SpawnLocation SpawnLocation => spawnLocation;
        public virtual bool IsMaskable => true; // Can we mask this layer? Usually yes, but not in case of projections

        public virtual IStyler Styler => styler;
        protected IStyler styler;

        public string Name
        {
            get => LayerData.Name;
            set => LayerData.Name = value;
        }

        public bool HasLayerData => LayerData != null;

        private ReferencedLayerData layerData;
        public ReferencedLayerData LayerData => layerData;
        //public ReferencedLayerData LayerData
        //{
        //    get => layerData;
        //    set
        //    {
        //        // Make idempotent - only continue if layerData actually changes
        //        if (value == layerData) return;

        //        layerData = value;
        //        OnLayerInitialize();
        //        onLayerInitialized.Invoke();
        //    }
        //}

        public Dictionary<object, LayerFeature> LayerFeatures { get; private set; } = new();
        Dictionary<string, LayerStyle> IStylable.Styles => LayerData.Styles;

        [Space] 
        public UnityEvent onShow = new();
        public UnityEvent onHide = new();
        public UnityEvent onLayerInitialized = new();
        public UnityEvent onLayerReady = new();
        public UnityEvent OnStylingApplied = new();

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

        [Obsolete("Do not use Awake in subclasses, use OnLayerInitialize instead", true)]
        private void Awake()
        {
            // Technically not possible, but is a safeguard.
            if (LayerData != null) return;
            
            // No parent, definitely no placeholder to replace
            if (!transform.parent) return;
            
            // Immediately return if there is no Placeholder as parent - no action needed
            if (transform.parent.GetComponent<PlaceholderLayerGameObject>() is not { } placeholder) return;

            // Replacing the placeholder will change the Reference property of the LayerData,
            // and that will trigger the LayerData property of this LayerGameObject to be set.
            // Since the LayerData is responsible for initializing the Layer, it will trigger the
            // real OnLayerInitialize method below
            // By doing it this way, we could directly instantiate LayerGameObjects and defer their
            // initialisation until a LayerData is present, as is done in the PlaceholderLayerGameObject.
            placeholder.ReplaceWith(this);
        }

        public void SetData(LayerData layerData)
        {
            if (this.LayerData == layerData) return;

            //remove the old listeners from previous layerdata
            if (this.layerData != null)
                UnregisterEventListeners();

            this.layerData = layerData as ReferencedLayerData; //TODO referencedlayerdata will be removed later
            
            RegisterEventListeners();
            OnLayerInitialize();
            onLayerInitialized.Invoke();
            // Call a template method that children are free to play with - this way we can avoid using
            // the start method directly and prevent forgetting to call the base.Start() from children
            LoadPropertiesInVisualisations();
            OnLayerReady();
            // Event invocation is separate from template method on purpose to ensure child classes complete their
            // readiness before external classes get to act - it also prevents forgetting calling the base method
            // when overriding OnLayerReady
            onLayerReady.Invoke();
            InitializeVisualisation();
        }

        private LayerGameObject Object() => this;

        private void RegisterEventListeners()
        {
            layerData.ParentChanged.AddListener(OnProxyTransformParentChanged);
            layerData.ChildrenChanged.AddListener(OnProxyTransformChildrenChanged);
            layerData.ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
            layerData.LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
            layerData.OnVisualizationRequested += Object;
            layerData.LayerDoubleClicked.AddListener(OnDoubleClick);
        }

        private void UnregisterEventListeners()
        {
            layerData.ParentChanged.RemoveListener(OnProxyTransformParentChanged);
            layerData.ChildrenChanged.RemoveListener(OnProxyTransformChildrenChanged);
            layerData.ParentOrSiblingIndexChanged.RemoveListener(OnSiblingIndexOrParentChanged);
            layerData.LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged);
            layerData.OnVisualizationRequested -= Object;
            layerData.LayerDoubleClicked.RemoveListener(OnDoubleClick);
        }

        /// <summary>
        /// Called when the Layer needs to be (re)initialised once a new LayerData is provided.
        ///
        /// As soon as a LayerData property is assigned to this LayerGameObject, this method is called to initialize
        /// this layer game object. Keep in mind that this could be called from the Awake function, it is recommended
        /// to use this as little as possible.
        ///
        /// When using the LayerSpawner, the OnLayerReady will be called once after this method is fired the first time.
        /// When you have a custom instantiation flow this is not guaranteed.
        /// </summary>
        protected virtual void OnLayerInitialize()
        {
            // Intentionally left blank as it is a template method and child classes should not have to
            // call `base.OnLayerInitialize` (and possibly forget to do that)
        }

        [Obsolete("Do not use Awake in subclasses, use OnLayerReady instead", true)]
        private void Start()
        {
           
        }

        protected virtual void OnLayerReady()
        {
            // Intentionally left blank as it is a template method and child classes should not have to
            // call `base.OnLayerReady` (and possibly forget to do that)
        }

        //Use this function to initialize anything that has to be done after either:
        // 1. Instantiating prefab -> creating new LayerData, or
        // 2. Creating LayerData (from project), Instantiating prefab, coupling that LayerData to this LayerGameObject
        protected virtual void InitializeVisualisation()
        {            
            OnLayerActiveInHierarchyChanged(LayerData.ActiveInHierarchy); //initialize the visualizations with the correct visibility

            ApplyStyling();
        }

        private void LoadPropertiesInVisualisations()
        {
            foreach (var visualisation in GetComponents<ILayerWithPropertyData>())
            {
                visualisation.LoadProperties(LayerData.LayerProperties);
            }
        }

        protected virtual void OnEnable()
        {
            onShow.Invoke();
            if(IsMaskable)
                PolygonSelectionLayer.MaskDestroyed.AddListener(ResetMask);
        }

        protected virtual void OnDisable()
        {
            onHide.Invoke();
            if(IsMaskable)
                PolygonSelectionLayer.MaskDestroyed.RemoveListener(ResetMask);
        }

        private void ResetMask(int maskBitIndex)
        {
            SetMaskBit(maskBitIndex, true); //reset accepting masks
        }
        
        protected virtual void OnDestroy()
        {
            //don't unsubscribe in OnDisable, because we still want to be able to center to a 
          
            UnregisterEventListeners();
        }

        public virtual void OnConvert(string previousId)
        {
        }

        public virtual void OnSelect()
        {
        }

        public virtual void OnDeselect()
        {
        }

        public void DestroyLayer()
        {
            LayerData.DestroyLayer();
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

        protected virtual void OnDoubleClick(LayerData layer)
        {
            CenterInView();
        }
        
        public void CenterInView()
        {
            if (Bounds == null)
            {
                Debug.LogError("Bounds object is null, no bounds specified to center to.");
                return;
            }
            
            Coordinate targetCoordinate = Bounds.Center;
            if (targetCoordinate.PointsLength == 2) //2D CRS, use the heigtmap to estimate the height.
            {
                targetCoordinate = targetCoordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
                float height = ServiceLocator.GetService<HeightMap>().GetHeight(targetCoordinate);
                targetCoordinate.height = height;
            }

            var convertedBounds = new BoundingBox(Bounds.BottomLeft, Bounds.TopRight);
            convertedBounds.Convert(CoordinateSystems.connectedCoordinateSystem); //convert the bounds so to the connected 
            var targetDistance = convertedBounds.GetSizeMagnitude();

            // !IMPORTANT: we deselect the layer, because if we don't do this, the TransformHandles might be connected to this LayerGameObject
            // This causes conflicts between the transformHandles and the Origin Shifter system, because the Transform handles will try to move the gameObject to the old (pre-shift) position.
            LayerData.DeselectLayer();
            //move the camera to the center of the bounds, and move it back by the size of the bounds (2x the extents)
            Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget(targetCoordinate, targetDistance); //sizeMagnitude returns 2x the extents
        }

#region Styling
        protected Symbolizer GetStyling(LayerFeature feature)
        {
            return StyleResolver.Instance.GetStyling(feature, LayerData.Styles);
        }

        public virtual void ApplyStyling()
        {
            var bitMask = GetBitMask();
            UpdateMaskBitMask(bitMask);
            // var mask = LayerStyler.GetMaskLayerMask(this); todo?
            //initialize the layer's style and emit an event for other services and/or UI to update
            OnStylingApplied.Invoke();
        }

        protected int GetBitMask()
        {
            int? bitMask = LayerData.DefaultSymbolizer.GetMaskLayerMask();
            if (bitMask == null)
                bitMask = DEFAULT_MASK_BIT_MASK;
            
            return bitMask.Value;
        }

        public virtual void UpdateMaskBitMask(int bitmask)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                UpdateBitMaskForMaterials(bitmask, r.materials);
            }
        }

        protected void UpdateBitMaskForMaterials(int bitmask, IEnumerable<Material> materials)
        {
            foreach (var m in materials)
            {
                m.SetFloat("_MaskingChannelBitmask", bitmask);
            }
        }

        /// <summary>
        /// Sets a bitmask to the layer to determine which masks affect the provided LayerGameObject
        /// </summary>
        public void SetMaskLayerMask(int bitMask)
        {
            LayerData.DefaultStyle.AnyFeature.Symbolizer.SetMaskLayerMask(bitMask);
            ApplyStyling();
        }

        public void SetMaskBit(int bitIndex, bool enableBit)
        {
            var currentLayerMask = GetMaskLayerMask();
            int maskBitToSet = 1 << bitIndex;
                
            if (enableBit)
            {
                currentLayerMask |= maskBitToSet; // set bit to 1
            }
            else
            {
                currentLayerMask &= ~maskBitToSet; // set bit to 0
            }

            SetMaskLayerMask(currentLayerMask);
        }

        /// <summary>
        /// Retrieves the bitmask for masking of the LayerGameObject.
        /// </summary>
        public int GetMaskLayerMask()
        {
            int? bitMask = LayerData.DefaultStyle.AnyFeature.Symbolizer.GetMaskLayerMask();
            if (bitMask == null)
                bitMask = LayerGameObject.DEFAULT_MASK_BIT_MASK;

            return bitMask.Value;
        }
#endregion

#region Features

        /// <summary>
        /// Creates a list of features for each component of type T on this game object. This list is not automatically
        /// recorded in the local list of features to allow streaming services to request a list per tile, or to perform
        /// actions or filtering before registering these features.
        /// </summary>
        protected List<LayerFeature> CreateFeaturesByType<T>() where T : Component
        {
            var cachedFeatures = new List<LayerFeature>();

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
            LayerFeature feature = LayerFeature.Create(this, geometry);
            AddAttributesToLayerFeature(feature);

            return feature;
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
        
        protected T GetAndCacheComponent<T>(ref T cache) where T : class
        {
            // 'is' works both on interfaces and Unity's lifecycle check because is is overridden
            if (cache is null)
            {
                cache = GetComponent<T>();
            }

            return cache;
        }
    }
}