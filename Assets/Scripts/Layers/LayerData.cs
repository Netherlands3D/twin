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
    public class LayerData : MonoBehaviour
    {
        public static LayerData Instance { get; private set; }
        // public static HashSet<LayerNL3DBase> AllLayers { get; set; } = new HashSet<LayerNL3DBase>();

        [SerializeField] private ProjectData currentProject;
        private static ProjectData projectData;
            
        private void Awake()
        {
            if (Instance)
                Debug.LogError("Another LayerData Object already exists, there should be only one LayerData object. The existing object will be overwritten", Instance.gameObject);

            Instance = this;
            projectData = currentProject;
        }
        
        public static void AddReferenceLayer(ReferencedLayer referencedLayer)
        {
            var referenceName = referencedLayer.name.Replace("(Clone)", "").Trim();

            var referenceLayerObject = new GameObject(referenceName);
            var proxyLayer = referenceLayerObject.AddComponent<ReferencedProxyLayer>(); 
            proxyLayer.CONSTRUCTOR(referenceName);
            proxyLayer.Reference = referencedLayer;
            referencedLayer.ReferencedProxy = proxyLayer;
        }
    }
}