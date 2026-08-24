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
using UnityEngine;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit.Scripts;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    public static class LayerTypeSpriteLibrary
    {
        //todo UI Toolkit in the future we hope to refactor this class away, and have a presets for sprites which we can use also for prefabassetentries
        //the refactor will be dependent on the icon refactor story https://gemeente-amsterdam.atlassian.net/browse/S3DA-2101

        public static string GetIconImage(LayerData layer)
        {
            if (layer.HasProperty<ScenarioPropertyData>())
                return IconImage.SCENARIO;

            if (layer.HasProperty<FolderPropertyData>())
                return IconImage.FOLDER;

            if (layer.HasProperty<PolygonSelectionLayerPropertyData>()) // special cases for polygon layers that have a defined shape type
            {
                PolygonSelectionLayerPropertyData propertyData = layer.GetProperty<PolygonSelectionLayerPropertyData>(); 
                if (propertyData.ShapeType == ShapeType.Line)
                    return IconImage.LINE;
                else if (propertyData.ShapeType == ShapeType.Grid)
                    return IconImage.ORTHOGONAL_VIEW;
                return IconImage.POLYGON;
            }

            return GetIconImage(layer.PrefabIdentifier);
        }
        
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