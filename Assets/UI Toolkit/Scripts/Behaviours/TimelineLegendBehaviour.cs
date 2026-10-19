using System;
using UnityEngine;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Panels;
using UnityEngine.UIElements;

namespace Netherlands3D.Timeline
{
    public class TimelineLegendBehaviour : MonoBehaviour
    {
        [SerializeField] private bool log = false;

        private TimelineLegendPanel legendPanel;

        private void Awake()
        {
            legendPanel = App.UIRoot.Root.Q<TimelineLegendPanel>();
        }

        private void Start()
        {
            legendPanel.SetVisible(false);
        }
        
        /// <summary>
        /// Shows or hides the legend panel. When showing, sets the container for
        /// the given WMS URL as active and downloads any missing images.
        /// </summary>
        public void ShowLegend(LayerData layerData, bool show)
        {
            legendPanel.SetVisible(show);

            if (!show)
            {
                if (log) Debug.Log("[Timeline] Legend hidden.");
                return;
            }
            
            legendPanel.SetLegend(layerData);
        }
    }
}
