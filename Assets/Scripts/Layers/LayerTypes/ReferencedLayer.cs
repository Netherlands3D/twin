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
        [field: SerializeField] public Sprite LayerTypeSprite { get; set; }

        protected virtual void Start()
        {
            LayerData.AddReferenceLayer(this);
        }

        protected virtual void OnDestroy()
        {
            if (ReferencedProxy)
                Destroy(ReferencedProxy.gameObject);
        }
    }
}