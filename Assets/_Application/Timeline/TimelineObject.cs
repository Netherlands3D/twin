using System;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Timeline
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
        private TimelineStylingLayerPropertyData timelineStylingLayerPropertyData;
        ColorPropertyData stylingPropertyData;

        void Start()
        {
            layerGameObject = GetComponent<LayerGameObject>();
            layerGameObject.InitProperty<TimelineStylingLayerPropertyData>(layerGameObject.LayerData.LayerProperties);
            timelineStylingLayerPropertyData = layerGameObject.LayerData.GetProperty<TimelineStylingLayerPropertyData>();

            
            stylingPropertyData = layerGameObject.LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();

            if (stylingPropertyData == null) return;

            ServiceLocator.GetService<SunTime>().timeOfDayChanged.AddListener(OnTimeChanged);
        }

        private void OnTimeChanged(DateTime currentTime)
        {
            var currentState = GetBuildState(currentTime);
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
            // if (timelineStylingLayerPropertyData.BuildStart.HasValue && currentTime < timelineStylingLayerPropertyData.BuildStart.Value)
            //     state = BuildState.PreBuild;
            // else if (timelineStylingLayerPropertyData.BuildEnd.HasValue && currentTime < timelineStylingLayerPropertyData.BuildEnd.Value)
            //     state = BuildState.Building;
            // else if (timelineStylingLayerPropertyData.DemolishEnd.HasValue && currentTime > timelineStylingLayerPropertyData.DemolishEnd.Value)
            //     state = BuildState.PostDemolish;
            // else if (timelineStylingLayerPropertyData.DemolishStart.HasValue && currentTime > timelineStylingLayerPropertyData.DemolishStart.Value)
            //     state = BuildState.Demolishing;
            
            return state;
        }
    }
}