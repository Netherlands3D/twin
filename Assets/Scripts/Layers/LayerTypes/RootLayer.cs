using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public class RootLayer : LayerNL3DBase
    {
        [JsonIgnore] public List<LayerNL3DBase> SelectedLayers { get; private set; } = new();

        public RootLayer(string name) : base(name)
        {
        }

        public void AddLayerToSelection(LayerNL3DBase layer)
        {
            if (!SelectedLayers.Contains(layer))
                SelectedLayers.Add(layer);
        }

        public void RemoveLayerFromSelection(LayerNL3DBase layer)
        {
            if (SelectedLayers.Contains(layer))
                SelectedLayers.Remove(layer);
        }

        public void DeselectAllLayers()
        {
            // Make a copy of the SelectedLayers list because the Deselect function removes
            // the selected layer from this list; and the enumeration fails without a copy
            foreach (var selectedLayer in SelectedLayers.ToList())
            {
                selectedLayer.DeselectLayer();
            }
        }

        public override void DestroyLayer()
        {
            foreach (var child in ChildrenLayers.ToList()) //use ToList to make a copy and avoid a CollectionWasModified error
            {
                child.DestroyLayer();
            }
            
            ProjectData.Current.RemoveLayer(this);
            LayerDestroyed.Invoke();
        }
    }
}