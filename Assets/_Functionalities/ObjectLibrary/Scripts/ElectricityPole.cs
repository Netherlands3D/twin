using System.Collections.Generic;
using System.Linq;
using GG.Extensions;
using GogoGaga.OptimizedRopesAndCables;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Samplers;
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
        private int previousSiblingIndex = -1;
        private float ropeLengthMp = 1.1f;
        public bool DebugTarget = false;

        [SerializeField] private Material ropeMaterial;
        
        //quality 10, width 0.1, stiff 350, damp 15, len calc, mid 0.5, influence 1, 
        
        [SerializeField] private Transform[] Connectors;
        private Rope[] ropes;
        
        private HeightMap heightMap;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            heightMap = ServiceLocator.GetService<HeightMap>();
            visualisation = GetComponent<LayerGameObject>();
            visualisation.InitProperty<ConnectableHierarchicalLayerPropertyData>(properties, null, null);
            transformPropertyData = visualisation.LayerData.GetProperty<TransformLayerPropertyData>();
            GeoJSONPropertyData geoJsonPropertyData  = visualisation.LayerData.GetProperty<GeoJSONPropertyData>();
            if(geoJsonPropertyData != null)
            {
                Coordinate targetCoordinate = geoJsonPropertyData.geojsonCoordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
                targetCoordinate = targetCoordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
                float height = heightMap.GetHeight(targetCoordinate);
                targetCoordinate.height = height;
                transformPropertyData.Position = targetCoordinate;
            }
            
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
           visualisation.OnVisibilityChanged.AddListener(UpdateVisiblity);
        }

        private void RemoveListeners()
        {
            connectedPropertyData.OnTargetChanged.RemoveListener(UpdateCables);
            visualisation.LayerData.ParentOrSiblingIndexChanged.RemoveListener(ConnectToSibling);
            transformPropertyData.OnPositionChanged.RemoveListener(UpdateCableLengths);
            visualisation.OnVisibilityChanged.RemoveListener(UpdateVisiblity);
        }

        private void UpdateVisiblity(bool visible)
        {
            if (ropes != null)
            {
                for (int i = 0; i < Connectors.Length; i++)
                {
                    ropes[i].enabled = visible;
                }
            }
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
                            float prevDist = ropes[i].ropeLength;
                            float dist = Vector3.Distance(ropes[i].StartPoint.position, ropes[i].EndPoint.position);
                            float groundY = heightMap.GetHeight(transformPropertyData.Position);
                            float poleY = (ropes[i].StartPoint.position.y + ropes[i].EndPoint.position.y) * 0.5f;
                            float maxSag = poleY - groundY - 0.75f;
                            float sag = Mathf.Min(dist * 0.1f, maxSag);
                            float newLength = dist + sag;
                            ropes[i].ropeLength = newLength;
                            if (prevDist != newLength)
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
                        if(fromTargetPropertyData == null)
                            return;
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
            if(!DebugTarget) return;
            
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
            LayerData currentParent = visualisation.LayerData.ParentLayer;

            int currentIndex = currentParent is RootLayer
                ? -1
                : currentParent.ChildrenLayers.IndexOf(visualisation.LayerData);

            // Nothing changed.
            if (previousParent == currentParent &&
                previousSiblingIndex == currentIndex)
                return;

            // Same parent: only update connections around the old
            // and new positions.
            if (previousParent == currentParent &&
                currentParent is not RootLayer)
            {
                UpdateConnectionAt(currentParent, previousSiblingIndex - 1);
                UpdateConnectionAt(currentParent, previousSiblingIndex);

                UpdateConnectionAt(currentParent, currentIndex - 1);
                UpdateConnectionAt(currentParent, currentIndex);
            }
            else
            {
                // Update the old parent's connection.
                if (previousParent != null &&
                    previousParent is not RootLayer)
                {
                    UpdateConnectionAt(previousParent, previousSiblingIndex - 1);
                }

                // Update the new parent's connections.
                if (currentParent is not RootLayer)
                {
                    UpdateConnectionAt(currentParent, currentIndex - 1);
                    UpdateConnectionAt(currentParent, currentIndex);
                }
            }

            previousParent = currentParent is RootLayer ? null : currentParent;
            previousSiblingIndex = currentIndex;
        }

        private void UpdateConnectionAt(LayerData parent, int index)
        {
            if (parent == null || parent is RootLayer)
                return;

            List<LayerData> children = parent.ChildrenLayers;

            // Include the last child: it must have no target.
            if (index < 0 || index >= children.Count)
                return;

            ConnectableHierarchicalLayerPropertyData current =
                children[index].GetProperty<ConnectableHierarchicalLayerPropertyData>();

            if (current == null)
                return;

            LayerData desiredTarget = null;

            // Only connect to the next child if one exists and is connectable.
            if (index < children.Count - 1)
            {
                LayerData nextChild = children[index + 1];

                if (nextChild.GetProperty<ConnectableHierarchicalLayerPropertyData>() != null)
                    desiredTarget = nextChild;
            }

            // Avoid triggering OnTargetChanged unnecessarily.
            if (current.Target == desiredTarget)
                return;

            current.Target = desiredTarget;
        }


        private void OrderAllChildrenOfParent(LayerData parent)
        {
            List<LayerData> children = parent.ChildrenLayers;
            for (int i = 0; i < children.Count - 1; i++)
            {
                ConnectableHierarchicalLayerPropertyData current = children[i].GetProperty<ConnectableHierarchicalLayerPropertyData>();
                if (current == null) break;
                 
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