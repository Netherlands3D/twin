using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public abstract class ReferencedLayer : MonoBehaviour
    {
        public abstract bool IsActiveInScene { get; set; }
        public ReferencedProxyLayer ReferencedProxy { get; set; }

        protected virtual void Awake()
        {
            LayerData.AddReferenceLayer(this);
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
            if (ReferencedProxy)
                Destroy(ReferencedProxy.gameObject);
        }
    }
}