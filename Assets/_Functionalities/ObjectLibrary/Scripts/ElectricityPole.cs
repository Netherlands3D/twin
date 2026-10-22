using System.Collections.Generic;
using System.Linq;
using GogoGaga.OptimizedRopesAndCables;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    [RequireComponent(typeof(LayerGameObject))]
    public class ElectricityPole : MonoBehaviour, IVisualizationWithPropertyData
    {
        private LayerGameObject visualisation;
        private TransformLayerPropertyData transformPropertyData;
        private ConnectableHierarchicalLayerPropertyData connectedPropertyData;

        private ElectricityPole target;
        
        [SerializeField] private Transform[] Connectors;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualisation = GetComponent<LayerGameObject>();
            visualisation.InitProperty<ConnectableHierarchicalLayerPropertyData>(properties);
            transformPropertyData = visualisation.LayerData.GetProperty<TransformLayerPropertyData>();
            connectedPropertyData = visualisation.LayerData.GetProperty<ConnectableHierarchicalLayerPropertyData>();
            
            AddListeners();
            ConnectToSibling();
        }

        private void AddListeners()
        {
           transformPropertyData.OnPositionChanged.AddListener(UpdateCables);
           visualisation.LayerData.ParentOrSiblingIndexChanged.AddListener(ConnectToSibling);
        }

        private void RemoveListeners()
        {
            transformPropertyData.OnPositionChanged.RemoveListener(UpdateCables);
            visualisation.LayerData.ParentOrSiblingIndexChanged.RemoveListener(ConnectToSibling);
        }

        private void UpdateCables(Coordinate arg0)
        {
            
        }

        private void Update()
        {
            if(transformPropertyData != null && connectedPropertyData != null && connectedPropertyData.Target != null && connectedPropertyData.Target.GetProperty<TransformLayerPropertyData>() != null)
                Debug.DrawLine(transformPropertyData.Position.ToUnity(), connectedPropertyData.Target.GetProperty<TransformLayerPropertyData>().Position.ToUnity(), Color.red);
        }

        private void ConnectToSibling()
        {
            if (visualisation.LayerData.ParentLayer is not RootLayer)
            {
                List<LayerData> children = visualisation.LayerData.ParentLayer.ChildrenLayers;
                for (int i = 0; i < children.Count - 1; i++)
                {
                    ConnectableHierarchicalLayerPropertyData current = children[i].GetProperty<ConnectableHierarchicalLayerPropertyData>();
                    current.Target = null;
                    ConnectableHierarchicalLayerPropertyData connected = children[i + 1].GetProperty<ConnectableHierarchicalLayerPropertyData>();
                    if (connected != null)
                    {
                        current.Target = children[i + 1];
                    }
                }
            }
        }
        
        public void SetTarget(ElectricityPole target)
        {
            this.target = target;
            if (target != null)
            {
                gameObject.AddComponent<RopeMesh>();
            }
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }
    }
}