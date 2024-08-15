using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
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
                    Debug.Log("ReferencedProxy is null, creating new layer");
                    CreateProxy();
                }
                    
                return layerData;
            }
            set
            {
                if (layerData != null)
                {
                    foreach (var layer in GetComponents<ILayerWithPropertyData>())
                    {
                        layerData.PropertiesChanged.RemoveListener(layer.LoadProperties); //remove old listeners
                    }
                }
                
                layerData = value;
                
                foreach (var layer in GetComponents<ILayerWithPropertyData>())
                {
                    layer.LoadProperties(layerData.LayerProperties); //initial load
                    layerData.PropertiesChanged.AddListener(layer.LoadProperties); //subscribe to changes
                }
            }
        }


        [Space] 
        public UnityEvent onShow = new();
        public UnityEvent onHide = new();

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
            if (LayerData == null) //if the layer data object was not initialized when creating this object, create a new LayerDataObject
                CreateProxy();

            OnLayerActiveInHierarchyChanged(LayerData.ActiveInHierarchy); //initialize the visualizations with the correct visibility
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

        public virtual void DestroyLayer()
        {
            layerData.DestroyLayer();
        }

        public void DestroyLayerGameObject()
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
    }
}