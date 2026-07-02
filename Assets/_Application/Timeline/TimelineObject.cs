using System;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using UnityEngine;

namespace Netherlands3D
{
    public enum BuildState
    {
        PreBuild,
        Building,
        Normal,
        Demolishing,
        PostDemolish
    }

    [RequireComponent(typeof(LayerGameObject))]
    public class TimelineObject : MonoBehaviour
    {
        LayerGameObject layerGameObject;
        private TimelineLayerPropertyData timelineLayerPropertyData;
        ColorPropertyData stylingPropertyData;

        void Start()
        {
            layerGameObject = GetComponent<LayerGameObject>();
            layerGameObject.InitProperty<TimelineLayerPropertyData>(layerGameObject.LayerData.LayerProperties);
            timelineLayerPropertyData = layerGameObject.LayerData.GetProperty<TimelineLayerPropertyData>();

            
            stylingPropertyData = layerGameObject.LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();

            if (stylingPropertyData == null) return;

            ServiceLocator.GetService<SunTime>().timeOfDayChanged.AddListener(OnTimeChanged);
        }

        private void OnTimeChanged(DateTime currentTime)
        {
            var currentState = GetBuildState(currentTime);
            Debug.Log(currentState);
            SetVisibility(currentState == BuildState.Building || currentState == BuildState.Normal || currentState == BuildState.Demolishing);
            
            switch (currentState)
            {
                // case BuildState.PreBuild:
                    // stylingPropertyData.SetDefaultSymbolizerColor(new Color(0,0,0,0));
                    // break;
                case BuildState.Building:
                    stylingPropertyData.SetDefaultSymbolizerColor(new Color(0,1,0,1));
                    break;
                case BuildState.Normal:
                    stylingPropertyData.SetDefaultSymbolizerColor(Color.white);
                    break;
                case BuildState.Demolishing:
                    stylingPropertyData.SetDefaultSymbolizerColor(new Color(1,0,0,1));
                    break;
                // case BuildState.PostDemolish:
                    // stylingPropertyData.SetDefaultSymbolizerColor(new Color(0,0,0, 0));
                    // break;
            }
        }

        private void SetVisibility(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                r.enabled = visible;
            }
        }

        private BuildState GetBuildState(DateTime currentTime)
        {
            var state = BuildState.Normal;
            if (timelineLayerPropertyData.BuildStart.HasValue && currentTime < timelineLayerPropertyData.BuildStart.Value)
                state = BuildState.PreBuild;
            else if (timelineLayerPropertyData.BuildEnd.HasValue && currentTime < timelineLayerPropertyData.BuildEnd.Value)
                state = BuildState.Building;
            else if (timelineLayerPropertyData.DemolishEnd.HasValue && currentTime > timelineLayerPropertyData.DemolishEnd.Value)
                state = BuildState.PostDemolish;
            else if (timelineLayerPropertyData.DemolishStart.HasValue && currentTime > timelineLayerPropertyData.DemolishStart.Value)
                state = BuildState.Demolishing;
            
            return state;
        }
    }
}