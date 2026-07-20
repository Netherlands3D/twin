using System;
using System.Collections.Generic;
using Netherlands3D.Functionalities.OGC3DTiles;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.FirstPersonViewer.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using UnityEngine;
using UnityEngine.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit.Scripts;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    [Serializable]
    public class LayerSpriteCollection
    {
        public Sprite PrimarySprite;
        public Sprite SecondarySprite;
    }

    [CreateAssetMenu(fileName = "LayerTypeSpriteLibrary", menuName = "ScriptableObjects/LayerTypeSpriteLibrary", order = 1)]
    public class LayerTypeSpriteLibrary : ScriptableObject
    {
        //todo UI Toolkit in the future we hope to refactor this class away, and have a presets for sprites which we can use also for prefabassetentries
        //the refactor will be dependent on the icon refactor story https://gemeente-amsterdam.atlassian.net/browse/S3DA-2101

        [SerializeField] private List<LayerSpriteCollection> layerTypeSprites;


        //todo UI Toolkit this can be removed when layerpanel story is merged
        public LayerSpriteCollection GetLayerTypeSprite(LayerData layer)
        {
            if (layer.HasProperty<FolderPropertyData>() || layer.HasProperty<ScenarioPropertyData>())
                return layerTypeSprites[2];

            LayerGameObject template = ProjectData.Current.PrefabLibrary.GetPrefabById(layer.PrefabIdentifier);
            if (template != null)
            {
                return GetProxyLayerSprite(template, layer);
            }

            Debug.LogError("layer type of " + layer.Name + " is not specified");
            return layerTypeSprites[0];
        }

        //todo UI Toolkit can also be removed after layerpanel story is merged
        private LayerSpriteCollection GetProxyLayerSprite(LayerGameObject template, LayerData data)
        {
            switch (template)
            {
                case WMSLayerGameObject _:
                case GeoJsonLayerGameObject _:
                    return layerTypeSprites[8];
                case CartesianTileLayerGameObject _:
                case Tile3DLayerGameObject _:
                case GroundPlaneLayerGameObject:
                    return layerTypeSprites[1];
                case WorldAnnotationLayerGameObject _:
                    return layerTypeSprites[10];
                case CameraPositionLayerGameObject _:
                    return layerTypeSprites[11];
                case FirstPersonCameraLayerGameObject _:
                    return layerTypeSprites[13];
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
                case PolygonSelectionLayerGameObject _:
                    {

                        if (data == null)
                            return layerTypeSprites[6];

                        PolygonSelectionLayerPropertyData propertyData = data.GetProperty<PolygonSelectionLayerPropertyData>();
                        if (propertyData.ShapeType == ShapeType.Line)
                            return layerTypeSprites[7];
                        else if (propertyData.ShapeType == ShapeType.Grid)
                            return layerTypeSprites[12];
                        return layerTypeSprites[6];
                    }
                default:
                    Debug.LogError($"layer type of {template.GetType()} is not specified");
                    return layerTypeSprites[0];
            }
        }


        public static string GetIconImage(LayerData layer)
        {
            if (layer.HasProperty<FolderPropertyData>() || layer.HasProperty<ScenarioPropertyData>())
                return IconImage.FOLDER;

            var template = ProjectData.Current.PrefabLibrary.GetPrefabById(layer.PrefabIdentifier);
            switch (template)
            {
                case WMSLayerGameObject _:
                case GeoJsonLayerGameObject _:
                    return IconImage.MAP;
                case CartesianTileLayerGameObject _:
                case Tile3DLayerGameObject _:
                case GroundPlaneLayerGameObject:
                    return IconImage.TILE;
                case WorldAnnotationLayerGameObject _:
                    return IconImage.ANNOTATION;
                case CameraPositionLayerGameObject _:
                    return IconImage.VIDEO_CAMERA;
                case FirstPersonCameraLayerGameObject _:
                    return IconImage.FPV;
                case HierarchicalObjectLayerGameObject _:
                    return IconImage.OBJECT;
                case ObjectScatterLayerGameObject _:
                    return IconImage.SCATTER_OBJECT;
                case CartesianTileSubObjectColorLayerGameObject _:
                    return IconImage.CSV;
                case GeoJSONPolygonLayer _:
                    return IconImage.POLYGON;
                case GeoJSONLineLayer _:
                    return IconImage.LINE;
                case GeoJSONPointLayer _:
                    return IconImage.DOT;
                case PolygonSelectionLayerGameObject _:
                    {
                        PolygonSelectionLayerPropertyData propertyData = layer.GetProperty<PolygonSelectionLayerPropertyData>();
                        if (propertyData.ShapeType == ShapeType.Line)
                            return IconImage.LINE;
                        else if (propertyData.ShapeType == ShapeType.Grid)
                            return IconImage.ORTHOGONAL_VIEW;
                        return IconImage.POLYGON;
                    }
                default:
                    Debug.LogError($"layer type of {template.GetType()} is not specified");
                    return IconImage.HELP;
            }
        }

        //todo UI Toolkit this is begging to be refactored and merged with the method above

        public static string GetIconImage(string prefabId)
        {
            var template = ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId);
            switch (template)
            {
                case WMSLayerGameObject _:
                case GeoJsonLayerGameObject _:
                    return IconImage.MAP;
                case CartesianTileLayerGameObject _:
                case Tile3DLayerGameObject _:
                case GroundPlaneLayerGameObject:
                    return IconImage.TILE;
                case WorldAnnotationLayerGameObject _:
                    return IconImage.ANNOTATION;
                case CameraPositionLayerGameObject _:
                    return IconImage.VIDEO_CAMERA;
                case FirstPersonCameraLayerGameObject _:
                    return IconImage.FPV;
                case HierarchicalObjectLayerGameObject _:
                    return IconImage.OBJECT;
                case ObjectScatterLayerGameObject _:
                    return IconImage.SCATTER_OBJECT;
                case CartesianTileSubObjectColorLayerGameObject _:
                    return IconImage.CSV;
                case GeoJSONPolygonLayer _:
                    return IconImage.POLYGON;
                case GeoJSONLineLayer _:
                    return IconImage.LINE;
                case GeoJSONPointLayer _:
                    return IconImage.DOT;
                case PolygonSelectionLayerGameObject _:
                     return IconImage.POLYGON;
                default:
                    Debug.LogError($"layer type of {template.GetType()} is not specified");
                    return IconImage.HELP;
            }
        }
    }
}
