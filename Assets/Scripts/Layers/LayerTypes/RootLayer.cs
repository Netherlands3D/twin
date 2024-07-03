using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public class RootLayer //: LayerNL3DBase //todo: make an extension of base and make functions protected
    {
        public List<LayerNL3DBase> SelectedLayers { get; set; } = new();
        
        public void AddLayerToSelection(LayerNL3DBase layer) //todo: make protected once this is an extension of base
        {
            if (!SelectedLayers.Contains(layer))
                SelectedLayers.Add(layer);
        }

        public void RemoveLayerFromSelection(LayerNL3DBase layer) //todo: make protected once this is an extension of base
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
    }
}
