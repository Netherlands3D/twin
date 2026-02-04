using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "Layer")]
    [DataContractAliases(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Names = new[] { "Folder", "Prefab", "PolygonSelection" })]
    [JsonConverter(typeof(LayerDataJsonConverter))]
    public class LayerData : IEquatable<LayerData>, IDisposable
    {
        [SerializeField, DataMember] protected Guid UUID = Guid.NewGuid();
        public Guid Id => UUID;

        [JsonIgnore] public bool IsNew { get; private set; } = true;
        [SerializeField, DataMember] protected string name;
        [SerializeField, DataMember] protected bool activeSelf = true;
        
        /// <summary>
        /// The default color of a layer.
        /// 
        /// This will influence how it is displayed in the layers side-panel, it does not automatically imply any
        /// coloring in the styling of the layer but can be used to tell layers apart from one another in the layer
        /// panel.
        ///
        /// Each type of layer could decide to use this value to influence the default styling by listening to the
        /// ColorChanged event and applying the color to the relevant color field in the default Style, such as fill
        /// for polygon vector layers, or stroke color for line polygon layers.
        /// </summary>
        [SerializeField, DataMember] protected Color color = new(86f / 256f, 160f / 256f, 227f / 255f);

        [SerializeField, DataMember] protected List<LayerData> children = new();
        [JsonIgnore] protected LayerData parent; //not serialized to avoid a circular reference
        [JsonIgnore] protected int rootIndex = -1;
        [SerializeField, DataMember] protected List<LayerPropertyData> layerProperties = new();        

        [JsonIgnore] private bool hasValidCredentials = true; //assume credentials are not needed. not serialized because we don't save credentials
        [JsonIgnore] public RootLayer Root => ProjectData.Current.RootLayer;
        [JsonIgnore] public LayerData ParentLayer => parent;

        [JsonIgnore] public List<LayerData> ChildrenLayers => children;
        [JsonIgnore] public bool IsSelected => Root.SelectedLayers.Contains(this);
        
        [JsonIgnore]
        public string Name
        {
            get => name;
            set
            {
                name = value;
                NameChanged.Invoke(this, value);
            }
        }

        [JsonIgnore]
        public bool ActiveSelf
        {
            get => activeSelf;
            set
            {
                activeSelf = value;
                foreach (var child in ChildrenLayers)
                {
                    child.ActiveSelf = child.ActiveSelf; //set the values again to recursively call the events.
                }

                LayerActiveInHierarchyChanged.Invoke(ActiveInHierarchy);
            }
        }

        [JsonIgnore]
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                ColorChanged.Invoke(value);
            }
        }

        [JsonIgnore] public int SiblingIndex => parent.ChildrenLayers.IndexOf(this);

        [JsonIgnore]
        public int RootIndex
        {
            get => rootIndex;
            set
            {
                if(value != rootIndex)
                    LayerOrderChanged.Invoke(value); 
                rootIndex = value;
            }
        }

        [JsonIgnore]
        public bool ActiveInHierarchy
        {
            get
            {
                if (this is RootLayer)
                    return activeSelf;

                return ParentLayer.ActiveInHierarchy && activeSelf;
            }
        }

        [JsonIgnore] public List<LayerPropertyData> LayerProperties
        {
            get
            {
                // When unserializing, and the layerproperties ain't there: make sure we have a valid list object.
                layerProperties ??= new List<LayerPropertyData>();

                return layerProperties;
            }
        }

        [JsonIgnore]
        public bool HasValidCredentials
        {
            get => hasValidCredentials;
            set
            {
                hasValidCredentials = value;
                HasValidCredentialsChanged.Invoke(value);
            }
        }

        [JsonIgnore] public bool HasProperties => LayerProperties.Count > 0;
        
        [DataMember] protected string prefabId;

        public string PrefabIdentifier //todo: this being settable is now very error sensitive. Will be refactored in ticket 3/4
        {
            get
            {
                return prefabId;
            }
            set
            {
                prefabId = value;
                OnPrefabIdChanged.Invoke();
            }
        }
        public UnityEvent OnPrefabIdChanged = new();        

        [JsonIgnore] public readonly UnityEvent<LayerData, string> NameChanged = new();
        [JsonIgnore] public readonly UnityEvent<bool> LayerActiveInHierarchyChanged = new();
        [JsonIgnore] public readonly UnityEvent<Color> ColorChanged = new();
        [JsonIgnore] public readonly UnityEvent LayerDestroyed = new();
        [JsonIgnore] public readonly UnityEvent<int> LayerOrderChanged = new();
        
        [JsonIgnore] public readonly UnityEvent<LayerData> LayerSelected = new();
        [JsonIgnore] public readonly UnityEvent<LayerData> LayerDeselected = new();
        [JsonIgnore] public UnityEvent<LayerData> LayerDoubleClicked = new();

        [JsonIgnore] public readonly UnityEvent ParentChanged = new();
        [JsonIgnore] public readonly UnityEvent ChildrenChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> ParentOrSiblingIndexChanged = new();
        [JsonIgnore] public readonly UnityEvent<LayerPropertyData> PropertySet = new();
        [JsonIgnore] public readonly UnityEvent<LayerPropertyData> PropertyRemoved = new();
       
        [JsonIgnore] public readonly UnityEvent<bool> HasValidCredentialsChanged = new();
        [JsonIgnore] public bool IsDisposed {get; private set;}

        /// <summary>
        /// Track whether this data object is new, in other words instantiated during this session, or whether it comes
        /// from persistence. If it was deserialized using Newtonsoft, we know it is not new. In which case we flip the
        /// flag.
        /// </summary>
        /// <param name="_"></param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext _)
        {
            IsNew = false;
        }

        /// <summary>
        /// This is needed because we cannot serialize both the parent and children since that would give a circular reference. Therefore, we need to initialize the parent value at runtime
        /// </summary>
        /// <param name="initialParent"></param>
        public void InitializeParent(LayerData initialParent)
        { 
            parent = initialParent;            
            if (initialParent == null)
            {
                parent = Root;
                ParentOrSiblingIndexChanged.AddListener(Root.UpdateLayerTreeOrder);
            }
        }

        public void SelectLayer(bool deselectOthers = false)
        {
            if (deselectOthers)
                Root.DeselectAllLayers();

            Root.AddLayerToSelection(this);
            LayerSelected.Invoke(this);
        }

        public void DeselectLayer()
        {
            Root.RemoveLayerFromSelection(this);
            LayerDeselected.Invoke(this);
        }

        public void DoubleClickLayer()
        {
            LayerDoubleClicked.Invoke(this);
        }

        public LayerData(string name, string prefabId) //TODO this should be refactored back in 3/4 of the layer stories
        {
            PrefabIdentifier = prefabId;
            Name = name;
        }

        public LayerData(string name) //initialize without layer properties, needed when creating an object at runtime.
        {
            Name = name;
        }

        [JsonConstructor]
        public LayerData(string name, List<LayerPropertyData> layerProperties) //initialize with explicit layer properties, needed when deserializing an object that already has properties.
        {
            Name = name;
            this.layerProperties = layerProperties ?? new List<LayerPropertyData>();
        }

        public void SetParent(LayerData newParent, int siblingIndex = -1)
        {
            if (newParent == null)
                newParent = Root;

            //in two cases we should not Set a new parent, since it would create a cyclical reference:
            //1. if you are trying to parent a layer to itself
            //2. if this LayerData is somewhere in the parent chain of the new parent.
            if (newParent == this || IsParentOrAncestor(newParent))
                return;
            
            var parentChanged = ParentLayer != newParent;
            var oldSiblingIndex = SiblingIndex;

            if (siblingIndex < 0)
                siblingIndex = newParent.children.Count;
            
            if (!parentChanged && siblingIndex > oldSiblingIndex) // moved down: insert first, remove after to keep the correct indices
            {
                parent = newParent;
                newParent.children.Insert(siblingIndex, this);
                
                parent.children.RemoveAt(oldSiblingIndex);
                parent.ChildrenChanged.Invoke(); //call event on old parent
            }
            else
            {
                parent.children.RemoveAt(oldSiblingIndex);

                parent = newParent;
                newParent.children.Insert(siblingIndex, this);
                
                parent.ChildrenChanged.Invoke(); //call event on old parent
            }
            
            if (parentChanged || siblingIndex != oldSiblingIndex)
            {
                LayerActiveInHierarchyChanged.Invoke(ActiveInHierarchy);
                ParentOrSiblingIndexChanged.Invoke(siblingIndex);
            }

            if (parentChanged)
            {
                ParentChanged.Invoke();
                newParent.ChildrenChanged.Invoke(); //call event on new parent
            }
        }
        
        //This function checks if this LayerData is somewhere in the parent chain of the layerToCheck
        private bool IsParentOrAncestor(LayerData layerToCheck)
        {
            var current = layerToCheck;
            while (current != null)
            {
                if (current == this)
                {
                    return true;
                }

                current = current.ParentLayer;
            }
            return false;
        }

        public virtual void Dispose()
        {
            DeselectLayer();

            foreach (var child in ChildrenLayers.ToList()) //use ToList to make a copy and avoid a CollectionWasModified error
            {
                child.Dispose();
            }

            ParentLayer.ChildrenLayers.Remove(this);
            IsDisposed = true;
            parent.ChildrenChanged.Invoke(); //call event on old parent
            ParentOrSiblingIndexChanged.RemoveListener(Root.UpdateLayerTreeOrder);
            LayerDestroyed.Invoke();
        }
        public bool HasProperty<T>() where T : LayerPropertyData
        {
            return LayerProperties.Contains<T>();
        }

        public T GetProperty<T>() where T : LayerPropertyData
        {
            return LayerProperties.Get<T>();
        }
        
        public IEnumerable<T> GetProperties<T>() where T : LayerPropertyData
        {
            return LayerProperties.OfType<T>();
        }

        public void SetProperty(LayerPropertyData propertyData)
        {
            if (LayerProperties.Set(propertyData))
            {
                PropertySet.Invoke(propertyData);
            }
        }

        public void RemoveProperty(LayerPropertyData propertyData)
        {
            if (LayerProperties.Remove(propertyData))
            {
                PropertyRemoved.Invoke(propertyData);
            }
        }
        
        /// <summary>
        /// Recursively collect all assets from each of the property data elements for loading and saving
        /// purposes. 
        /// </summary>
        /// <returns>A list of assets on disk</returns>
        public IEnumerable<LayerAsset> GetAssets()
        {
            IEnumerable<LayerAsset> assetsOfCurrentLayer = new List<LayerAsset>();
            assetsOfCurrentLayer = LayerProperties
                .OfType<ILayerPropertyDataWithAssets>()
                .SelectMany(p => p.GetAssets());

            var assetsOfAllChildLayers = children
                .SelectMany(l => l.GetAssets());

            return assetsOfCurrentLayer.Concat(assetsOfAllChildLayers);
        }

        /// <summary>
        /// Recursively get all layers within children
        /// </summary>
        /// <returns></returns>
        public List<LayerData> GetLayerDataTree()
        {
            List<LayerData> layerDataTree = new List<LayerData>();
            layerDataTree.Add(this);
            layerDataTree.AddRange(children.SelectMany(l => l.GetLayerDataTree()).ToList());
            return layerDataTree;
        }
        
        public bool Equals(LayerData other) => other is not null && other.Id == Id;
        public override bool Equals(object obj) => Equals(obj as LayerData);
        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(LayerData left, LayerData right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(LayerData left, LayerData right) => !(left == right);
    }
}