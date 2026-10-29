using System.Collections.Generic;
using System.Linq;
using GG.Extensions;
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

        private LayerData previousParent;
        private float ropeLengthMp = 1.1f;

        [SerializeField] private Material ropeMaterial;
        
        //quality 10, width 0.1, stiff 350, damp 15, len calc, mid 0.5, influence 1, 
        
        [SerializeField] private Transform[] Connectors;
        private Rope[] ropes;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualisation = GetComponent<LayerGameObject>();
            visualisation.InitProperty<ConnectableHierarchicalLayerPropertyData>(properties, null, null);
            transformPropertyData = visualisation.LayerData.GetProperty<TransformLayerPropertyData>();
            connectedPropertyData = visualisation.LayerData.GetProperty<ConnectableHierarchicalLayerPropertyData>();
            connectedPropertyData.Connectors = Connectors;
            
            AddListeners();
            ConnectToSibling();
        }

        private void AddListeners()
        {
           connectedPropertyData.OnTargetChanged.AddListener(UpdateCables);
           visualisation.LayerData.ParentOrSiblingIndexChanged.AddListener(ConnectToSibling);
           transformPropertyData.OnPositionChanged.AddListener(UpdateCableLengths);
        }

        private void RemoveListeners()
        {
            connectedPropertyData.OnTargetChanged.RemoveListener(UpdateCables);
            visualisation.LayerData.ParentOrSiblingIndexChanged.RemoveListener(ConnectToSibling);
            transformPropertyData.OnPositionChanged.RemoveListener(UpdateCableLengths);
        }

        private void UpdateCableLengths(Coordinate coord)
        {
            if (connectedPropertyData.Target != null)
            {
                if (ropes != null)
                {
                    for (int i = 0; i < Connectors.Length; i++)
                    {
                        if (ropes[i].StartPoint != null && ropes[i].EndPoint != null)
                        {
                            float dist = Vector3.Distance(ropes[i].StartPoint.position, ropes[i].EndPoint.position);
                            float prevDist = ropes[i].ropeLength;
                            ropes[i].ropeLength = dist * ropeLengthMp;
                            if (prevDist != dist)
                                ropes[i].RecalculateRope();
                        }
                        else
                        {
                            float prevDist = ropes[i].ropeLength;
                            ropes[i].ropeLength = 0;
                            if (prevDist != 0)
                                ropes[i].RecalculateRope();
                        }
                    }
                }
            }
            else
            {
                if (ropes != null)
                {
                    for (int i = 0; i < Connectors.Length; i++)
                    {
                        float prevDist = ropes[i].ropeLength;
                        ropes[i].ropeLength = 0;
                        if (prevDist != 0)
                            ropes[i].RecalculateRope();
                    }
                }
            }

            if (visualisation.LayerData.ParentLayer is not RootLayer)
            {
                //also update the cable lengths for the pole this pole as target
                List<LayerData> children = visualisation.LayerData.ParentLayer.ChildrenLayers;
                for (int i = 0; i < children.Count; i++)
                {
                    if (i > 0 && children[i] == visualisation.LayerData)
                    {
                        TransformLayerPropertyData fromTargetPropertyData = children[i - 1].GetProperty<TransformLayerPropertyData>();
                        fromTargetPropertyData.OnPositionChanged.Invoke(fromTargetPropertyData.Position);
                        return;
                    }
                }
            }
        }

        private void UpdateCables(LayerData target)
        {
            if (ropes == null)
            {
                ropes = new Rope[Connectors.Length];
                for (int i = 0; i < Connectors.Length; i++)
                {
                    ropes[i] = Connectors[i].AddComponent<Rope>();
                    ropes[i].Init();
                    Connectors[i].GetComponent<LineRenderer>().material = ropeMaterial;
                }
            }
            
            if (target == null)
            {
                for (int i = 0; i < Connectors.Length; i++)
                {
                    ropes[i].SetStartPoint(null);
                    ropes[i].SetEndPoint(null);
                   
                }
                UpdateCableLengths(new Coordinate());
                return;
            }
            
            Transform[] targetConnectors = target.GetProperty<ConnectableHierarchicalLayerPropertyData>().Connectors;
            for (int i = 0; i < Connectors.Length; i++)
            {
                ropes[i].SetStartPoint(Connectors[i]);
                ropes[i].SetEndPoint(targetConnectors[i]);
            }
            UpdateCableLengths(new Coordinate());
        }

        private void Update()
        {
            if (transformPropertyData != null && connectedPropertyData != null &&
                connectedPropertyData.Target != null &&
                connectedPropertyData.Target.GetProperty<TransformLayerPropertyData>() != null)
            {
                Debug.DrawLine(transformPropertyData.Position.ToUnity(), connectedPropertyData.Target.GetProperty<TransformLayerPropertyData>().Position.ToUnity(), Color.red);

                Transform[] targetConnectors = connectedPropertyData.Target.GetProperty<ConnectableHierarchicalLayerPropertyData>().Connectors;
                for(int i = 0; i < Connectors.Length; i++)
                    Debug.DrawLine(Connectors[i].position, targetConnectors[i].position, Color.green);
            }
        }

        private void ConnectToSibling()
        {
            if (visualisation.LayerData.ParentLayer is not RootLayer)
            {
                OrderAllChildrenOfParent(visualisation.LayerData.ParentLayer);
                previousParent = visualisation.LayerData.ParentLayer;
            }
            else
            {
                if (previousParent != null)
                {
                    OrderAllChildrenOfParent(previousParent);
                    previousParent = null;
                }
            }
        }

        private void OrderAllChildrenOfParent(LayerData parent)
        {
            List<LayerData> children = parent.ChildrenLayers;
            for (int i = 0; i < children.Count - 1; i++)
            {
                ConnectableHierarchicalLayerPropertyData current = children[i].GetProperty<ConnectableHierarchicalLayerPropertyData>();
                current.Target = null;
                LayerData target = children[i + 1];
                ConnectableHierarchicalLayerPropertyData connected = target.GetProperty<ConnectableHierarchicalLayerPropertyData>();
                if (connected != null)
                {
                    current.Target = target;
                }
            }
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }
    }
}