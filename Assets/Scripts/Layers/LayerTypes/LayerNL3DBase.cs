using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public abstract class LayerNL3DBase : MonoBehaviour
    {
        public LayerUI UI { get; set; }

        public abstract bool IsActiveInScene { get; set; }

        public virtual void OnSelect(){}
        public virtual void OnDeselect(){}

        protected virtual void Awake()
        {
            if (!LayerData.AllLayers.Contains(this))
                LayerData.AddLayer(this);
        }

        protected virtual void OnDestroy()
        {
            LayerData.RemoveLayer(this);
            UI.DestroyUI();
        }
    }
}
