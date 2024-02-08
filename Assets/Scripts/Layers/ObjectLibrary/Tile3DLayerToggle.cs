using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class Tile3DLayerToggle : LayerToggle
    {
        protected override void Awake()
        {
            base.Awake();
            layerParent = GameObject.FindWithTag("3DTileParent").transform;
            layer = layerParent.GetComponentsInChildren<Tile3DLayer>().FirstOrDefault(l => l.name == prefab.name);
        }
    }
}