using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    // public class LayerData : LayerNL3DBase //todo delete this class and replace all references with RootLayer
    // {
    //     public static LayerData Instance { get; private set; }
    //     // public static HashSet<LayerNL3DBase> AllLayers { get; set; } = new HashSet<LayerNL3DBase>();
    //     
    //     private void Awake()
    //     {
    //         if (Instance)
    //             Debug.LogError("Another LayerData Object already exists, there should be only one LayerData object. The existing object will be overwritten", Instance.gameObject);
    //
    //         Instance = this;
    //     }
    //
    //     protected override void Start()
    //     {// don't call base start because this is not needed for this temp rootlayer
    //     }
    // }
}