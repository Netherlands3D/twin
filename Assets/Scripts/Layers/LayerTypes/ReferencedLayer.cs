using System;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Twin.Layers
{
    public abstract class ReferencedLayer : MonoBehaviour
    {
        [SerializeField] private string prefabIdentifier;
        public string PrefabIdentifier => prefabIdentifier;

        public string Name
        {
            get => ReferencedProxy.Name;
            set => ReferencedProxy.Name = value;
        }

        private ReferencedProxyLayer referencedProxy;
        public ReferencedProxyLayer ReferencedProxy
        {
            get
            {
                if (referencedProxy == null) //todo: this should never be true
                {
                    Debug.Log("ReferencedProxy is null, creating new layer");
                    CreateProxy();
                }
                    
                return referencedProxy;
            }
            set => referencedProxy = value;
        }

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
                    // print("setting prefab id to : " + prefabIdentifier);
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
        private void Start()
        {
            if (ReferencedProxy == null) //if the layer data object was not initialized when creating this object, create a new LayerDataObject
                CreateProxy();

            ReferencedProxy.LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged); //todo: move this to referencedProxy
            OnLayerActiveInHierarchyChanged(ReferencedProxy.ActiveInHierarchy); //initialize the visualizations with the correct visibility
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

        protected virtual void OnLayerActiveInHierarchyChanged(bool isActive)
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
            Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            ReferencedProxy.LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged); //add in Awake and remove in OnDestroy, so that the Event function is called even if the gameObject is disabled
            DestroyProxy();
        }

        public virtual void DestroyProxy()
        {
            if (ReferencedProxy != null)
            {
                ReferencedProxy.DestroyLayer();
            }
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
    }
}