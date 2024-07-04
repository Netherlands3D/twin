using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public abstract class ReferencedLayer : MonoBehaviour
    {
        public string Name
        {
            get => ReferencedProxy.Name;
            set => ReferencedProxy.Name = value;
        }

        public ReferencedProxyLayer ReferencedProxy { get; set; }

        public UnityEvent onShow = new();
        public UnityEvent onHide = new();

        protected virtual void Awake()
        {
            CreateProxy();
            ReferencedProxy.LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged); //add in Awake and remove in OnDestroy, so that the Event function is called even if the gameObject is disabled
        }

        protected virtual void OnEnable()
        {
            onShow.Invoke();
        }

        protected virtual  void OnDisable()
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

        public virtual void CreateProxy()
        {
            LayerData.AddReferenceLayer(this);
        }

        public virtual void DestroyProxy()
        {
            if (ReferencedProxy!=null)
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