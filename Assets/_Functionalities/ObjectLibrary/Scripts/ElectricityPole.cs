using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    [RequireComponent(typeof(LayerGameObject))]
    public class ElectricityPole : MonoBehaviour, IVisualizationWithPropertyData
    {
        private WindmillPropertyData windmillPropertyData;
        private TransformLayerPropertyData transformPropertyData;

        public struct Connector
        {
            Transform front;
            Transform back;
        }
        
        [SerializeField] private Transform[] Connectors;

        private void Awake()
        {
            
        }

        private void Start()
        {
            
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var lgo = GetComponent<LayerGameObject>();
            
            //in case we cannot transform the object, we also should not be able to edit the Windmill property data, so we need to match the IsEditable state of the WindmillPropertyData to the TransformPropertyData
            transformPropertyData = lgo.LayerData.GetProperty<TransformLayerPropertyData>();
            windmillPropertyData.IsEditable = transformPropertyData.IsEditable;
            
            AddListeners();
        }

        private void AddListeners()
        {
           
        }

        private void RemoveListeners()
        {
       
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }
    }
}