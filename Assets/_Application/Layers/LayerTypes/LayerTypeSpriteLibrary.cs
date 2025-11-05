using System.Collections.Generic;
using Netherlands3D.Functionalities.OGC3DTiles;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    [CreateAssetMenu(fileName = "LayerTypeSpriteLibrary", menuName = "ScriptableObjects/LayerTypeSpriteLibrary", order = 1)]
    public class LayerTypeSpriteLibrary : ScriptableObject
    {
        [SerializeField] private List<Sprite> layerTypeSprites;

        //TODO the data.Visualisation reference needs to be refactored with the ui toolkit integration
        public Sprite GetLayerTypeSprite(LayerData layer)
        {
            switch (layer)
            {
                case PolygonSelectionLayer selectionLayer:
                    if (selectionLayer.ShapeType == ShapeType.Line)
                        return layerTypeSprites[7];
                    else if (selectionLayer.ShapeType == ShapeType.Grid)
                        return layerTypeSprites[12];
                    return layerTypeSprites[6];
                case ReferencedLayerData data:
                    var reference = data.Visualization;
                    return reference == null ? layerTypeSprites[0] : GetProxyLayerSprite(reference);
                case FolderLayer _:
                    return layerTypeSprites[2];
                default:
                    Debug.LogError("layer type of " + layer.Name + " is not specified");
                    return layerTypeSprites[0];
            }
        }

        private Sprite GetProxyLayerSprite(LayerGameObject layer)
        {
            switch (layer)
            {
                case WMSLayerGameObject _:
                case GeoJsonLayerGameObject _:
                    return layerTypeSprites[8];
                case CartesianTileLayerGameObject _:
                case Tile3DLayerGameObject _:
                    return layerTypeSprites[1];
                case WorldAnnotationLayerGameObject _:
                    return layerTypeSprites[10];
                case CameraPositionLayerGameObject _:
                    return layerTypeSprites[11];
                case HierarchicalObjectLayerGameObject _:
                    return layerTypeSprites[3];
                case ObjectScatterLayerGameObject _:
                    return layerTypeSprites[4];
                case CartesianTileSubObjectColorLayerGameObject _:
                    return layerTypeSprites[5];
                case GeoJSONPolygonLayer _:
                    return layerTypeSprites[6];
                case GeoJSONLineLayer _:
                    return layerTypeSprites[7];                
                case GeoJSONPointLayer _:
                    return layerTypeSprites[9];               
                default:
                    Debug.LogError($"layer type of {layer.GetType()} is not specified");
                    return layerTypeSprites[0];
            }
        }
    }
}
