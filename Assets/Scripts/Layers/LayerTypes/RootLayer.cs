using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "Root")]
    public class RootLayer : LayerData
    {
        [JsonIgnore] public List<LayerData> SelectedLayers { get; private set; } = new();

        public RootLayer(string name) : base(name)
        {
        }

        public void AddLayerToSelection(LayerData layer)
        {
            if (!SelectedLayers.Contains(layer))
                SelectedLayers.Add(layer);
        }

        public void RemoveLayerFromSelection(LayerData layer)
        {
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

        public void ReconstructParentsRecursive()
        {
            foreach (var layer in ChildrenLayers)
            {
                ReconstructParentsRecursive(layer, this);
            }
        }

        private void ReconstructParentsRecursive(LayerData layer, LayerData parent)
        {
            layer.InitializeParent(parent);
            foreach (var child in layer.ChildrenLayers)
            {
                ReconstructParentsRecursive(child, layer);
            }
        }

        public void AddChild(LayerData layer, int siblingIndex = -1)
        {
            if (!ChildrenLayers.Contains(layer))
            {
                if (siblingIndex >= 0 && siblingIndex < ChildrenLayers.Count)
                    ChildrenLayers.Insert(siblingIndex, layer);
                else
                    ChildrenLayers.Add(layer);
                ChildrenChanged.Invoke();
            }
        }
    }
}