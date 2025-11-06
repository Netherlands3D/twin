using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "Root")]
    public class RootLayer : LayerData
    {
        [JsonIgnore] public List<LayerData> SelectedLayers { get; private set; } = new();

        [JsonIgnore] private UnityAction<ProjectData> projectDataListener;

        public UnityEvent<LayerData> AddedSelectedLayer = new();
        public UnityEvent<LayerData> RemovedSelectedLayer = new();

        public RootLayer(string name) : base(name)
        {
           
        }

        public void AddLayerToSelection(LayerData layer)
        {
            if (!SelectedLayers.Contains(layer))
            {
                SelectedLayers.Add(layer);
                AddedSelectedLayer.Invoke(layer);
            }
        }

        public void RemoveLayerFromSelection(LayerData layer)
        {
            if (SelectedLayers.Remove(layer))
                RemovedSelectedLayer.Invoke(layer);
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
            Clear();
            ProjectData.Current.RemoveLayer(this);
            LayerDestroyed.Invoke();
        }

        public void Clear()
        {
            // use ToList to make a copy and avoid a CollectionWasModified error
            var childLayers = ChildrenLayers.ToList();

            // TODO (S3DA-1666) Due to a bug in the TileHandler, we need to ensure that the CartesianTileHandlers are
            // ordered based on the index in the TileHandler's "layers" property. The TileHandler's RemoveLayer() method
            // doesn't properly remove all pending or running changes. When the layers are not removed in order
            // of the layers field, the application crashes because a pending change will try to access a layer that
            // doesn't exist anymore.
            // This ordering can be removed as soon as we have our new tiling system, but for now it is needed.
            //var sortedChildLayers = childLayers
            //    // Create 2 buckets of layers - one with all types except CartesianTileLayers, and
            //    // one with only CartesianTileLayers. First order the first bucket, and then the second
            //    .OrderBy(l => l is ReferencedLayerData { Reference : CartesianTileLayerGameObject })
            //    // Within each bucket, pick the right key:
            //    .ThenBy(l =>
            //        l is ReferencedLayerData { Reference: CartesianTileLayerGameObject ct }
            //            ? ct.TileHandlerLayerIndex
            //            : l.RootIndex)
            //    // Optional deterministic tie-breakers:
            //    .ThenBy(l => l.RootIndex);

            var sortedChildLayers = childLayers
                .OrderBy(l =>
                    l.Visualization != null &&
                    l.Visualization is CartesianTileLayerGameObject)
                .ThenBy(l =>
                    l .Visualization != null &&
                    l.Visualization is CartesianTileLayerGameObject ct
                        ? ct.TileHandlerLayerIndex
                        : l.RootIndex)
                .ThenBy(l => l.RootIndex);

            foreach (var child in sortedChildLayers) 
            {
                child.DestroyLayer();
            }
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

        public void UpdateLayerTreeOrder(int index)
        {
            List<LayerData> children = GetLayerDataTree();
            int count = children.Count();
            for (int i = 0; i < count; i++)
            {
                children[i].RootIndex = i;
            }
        }
        
        public LayerData GetFirstLayerByLayerMask(LayerMask mask)
        {
            //TODO we need to identify if these layerdatas have a visualization
            return ChildrenLayers.FirstOrDefault(refData => refData.Visualization.gameObject.layer == mask); //TODO maybe we should cache this in layerdata
        }

        public List<LayerData> GetFlatHierarchy()
        {
            var list = new List<LayerData>();

            AddLayersRecursive(this, list);

            list.Remove(this); //remove rootLayer
            return list;
        }

        private void AddLayersRecursive(LayerData layer, List<LayerData> list)
        {
            list.Add(layer);
            foreach (var child in layer.ChildrenLayers)
            {
                AddLayersRecursive(child, list);
            }
        }
    }
}