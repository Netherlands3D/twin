using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public class LayerData
    {
        [SerializeField, DataMember] protected Guid UUID = Guid.NewGuid();
        [SerializeField, DataMember] protected string name;
        [SerializeField, DataMember] protected bool activeSelf = true;
        [SerializeField, DataMember] protected Color color = new Color(86f / 256f, 160f / 256f, 227f / 255f);
        [SerializeField, DataMember] protected List<LayerData> children = new();
        [JsonIgnore] protected LayerData parent; //not serialized to avoid a circular reference
        [SerializeField, DataMember] protected List<LayerPropertyData> layerProperties = new();
        [SerializeField, DataMember] protected List<LayerStyle> styles = new();
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
                NameChanged.Invoke(value);
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
                if (layerProperties == null)
                {
                    layerProperties = new();
                }

                return layerProperties;
            }
        }

        [JsonIgnore] public bool HasProperties => layerProperties.Count > 0;

        [JsonIgnore] public readonly UnityEvent<string> NameChanged = new();
        [JsonIgnore] public readonly UnityEvent<bool> LayerActiveInHierarchyChanged = new();
        [JsonIgnore] public readonly UnityEvent<Color> ColorChanged = new();
        [JsonIgnore] public readonly UnityEvent LayerDestroyed = new();

        [JsonIgnore] public readonly UnityEvent<LayerData> LayerSelected = new();
        [JsonIgnore] public readonly UnityEvent<LayerData> LayerDeselected = new();

        [JsonIgnore] public readonly UnityEvent ParentChanged = new();
        [JsonIgnore] public readonly UnityEvent ChildrenChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> ParentOrSiblingIndexChanged = new();
        [JsonIgnore] public readonly UnityEvent<LayerPropertyData> PropertyAdded = new();
        [JsonIgnore] public readonly UnityEvent<LayerPropertyData> PropertyRemoved = new();

        public void InitializeParent(LayerData initialParent = null)
        { 
            parent = initialParent;
            
            if (initialParent == null)
            {
                parent = Root;
            }
        }

        public virtual void SelectLayer(bool deselectOthers = false)
        {
            if (deselectOthers)
                Root.DeselectAllLayers();

            Root.AddLayerToSelection(this);
            LayerSelected.Invoke(this);
        }

        public virtual void DeselectLayer()
        {
            Root.RemoveLayerFromSelection(this);
            LayerDeselected.Invoke(this);
        }

        public LayerData(string name) //initialize without layer properties, needed when creating an object at runtime.
        {
            Name = name;
            if(this is not RootLayer) //todo: maybe move to inherited classes so this check is not needed?
                InitializeParent();
        }

        [JsonConstructor]
        public LayerData(string name, List<LayerPropertyData> layerProperties) //initialize with explicit layer properties, needed when deserializing an object that already has properties.
        {
            Name = name;
            if(this is not RootLayer) //todo: maybe move to inherited classes so this check is not needed?
                InitializeParent();
            this.layerProperties = layerProperties;
        }

        public void SetParent(LayerData newParent, int siblingIndex = -1)
        {
            if (newParent == null)
                newParent = Root;

            if (newParent == this)
                return;

            var parentChanged = ParentLayer != newParent;
            var oldSiblingIndex = SiblingIndex;

            parent.children.Remove(this);
            if (!parentChanged && siblingIndex > oldSiblingIndex) //if the parent did not change, and the new sibling index is larger than the old sibling index, we need to decrease the new siblingIndex by 1 because we previously removed one item from the children list
                siblingIndex--;
            parent.ChildrenChanged.Invoke(); //call event on old parent

            if (siblingIndex < 0)
                siblingIndex = newParent.children.Count;

            parent = newParent;

            newParent.children.Insert(siblingIndex, this);

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

        public virtual void DestroyLayer()
        {
            DeselectLayer();

            foreach (var child in ChildrenLayers.ToList()) //use ToList to make a copy and avoid a CollectionWasModified error
            {
                child.DestroyLayer();
            }

            ParentLayer.ChildrenLayers.Remove(this);
            parent.ChildrenChanged.Invoke(); //call event on old parent

            ProjectData.Current.RemoveLayer(this);
            LayerDestroyed.Invoke();
        }

        public void AddProperty(LayerPropertyData propertyData)
        {
            var existingProperty = layerProperties.FirstOrDefault(prop => prop.GetType() == propertyData.GetType());
            if (existingProperty != null)
            {
                Debug.Log("A property of type" +propertyData.GetType() + " already exists for " + Name + ". Overwriting the old PropertyData");
                int index = layerProperties.IndexOf(existingProperty);
                layerProperties[index] = propertyData;
            }
            
            layerProperties.Add(propertyData);
            PropertyAdded.Invoke(propertyData);
        }

        public void RemoveProperty(LayerPropertyData propertyData)
        {
            layerProperties.Remove(propertyData);
            PropertyRemoved.Invoke(propertyData);
        }

        /// <summary>
        /// Recursively collect all assets from each of the property data elements for loading and saving
        /// purposes. 
        /// </summary>
        /// <returns>A list of assets on disk</returns>
        public IEnumerable<LayerAsset> GetAssets()
        {
            IEnumerable<LayerAsset> assetsOfCurrentLayer = new List<LayerAsset>();
            if (layerProperties != null)
            {
                assetsOfCurrentLayer = layerProperties
                    .OfType<ILayerPropertyDataWithAssets>()
                    .SelectMany(p => p.GetAssets());
            }

            var assetsOfAllChildLayers = children
                .SelectMany(l => l.GetAssets());

            return assetsOfCurrentLayer.Concat(assetsOfAllChildLayers);
        }
    }
}