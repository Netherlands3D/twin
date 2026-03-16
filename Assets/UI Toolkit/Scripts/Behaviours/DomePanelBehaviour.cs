using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Tools;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class DomePanelBehaviour : FloatingPanelBehaviour
    {
        [SerializeField] private Tool domeTool;
        public override Dictionary<string, object> GetData()
        {
            var layers = ProjectData.Current.RootLayer.GetFlatHierarchy();
            return layers.ToDictionary(layer => layer.Id.ToString(), layer => (object)layer);
        }

        public override VisualElement SpawnFloatingPanelContent(Dictionary<string, object> data = null)
        {
            DomePanel panel = new DomePanel(data);
            return panel;
        }

        public override bool ShouldBeActive()
        {
            return domeTool.Open;
        }
    }
}
