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

        protected virtual void OnEnable()
        {
            LayerManager.AllLayers.Add(this);
        }

        protected virtual void OnDisable()
        {
            LayerManager.AllLayers.Remove(this);
        }
    }
}
