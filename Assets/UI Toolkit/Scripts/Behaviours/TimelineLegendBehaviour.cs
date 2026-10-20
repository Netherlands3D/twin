using System;
using UnityEngine;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.UI.Panels;
using UnityEngine.UIElements;

namespace Netherlands3D.Timeline
{
    public class TimelineLegendBehaviour : MonoBehaviour
    {
        private TimelineLegendPanel legendPanel;

        private void Awake()
        {
            legendPanel = App.UIRoot.Root.Q<TimelineLegendPanel>();
        }

        private void Start()
        {
            legendPanel.SetVisible(false);
        }
        
        public void ShowLegend(LayerData layerData, bool show)
        {
            legendPanel.SetVisible(show);

            if (!show)
                return;
            
            legendPanel.SetLegend(layerData);
        }
    }
}
