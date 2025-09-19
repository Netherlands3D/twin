using System;
using System.Collections.Generic;
using SimpleJSON;

namespace Netherlands3D.CityJson.Structure
{
    public static class CityMaterial
    {
        // public string theme;
        // public List<CityMaterial> Materials { get; private set; } = null;
        // public int MaterialIndex { get; private set; } = -1;

        public static void FromJSONNode(JSONNode materialNode, CityGeometry geometry)
        {
            int themeIndex = 0;
            foreach (var theme in materialNode)
            {
                geometry.AddMaterialTheme(theme.Key, themeIndex);
                // MaterialIndicesFromJSONNode(theme.Value);

                // The theme's value object is one JSON object that must contain either:
                // one member "value": each geometry has a defined material, and we can apply it to all surfaces of the geometry. We will store this in the Geometry to avoid duplicate data being stored in each surface.
                // one member "values": each surface has a defined material, and we will store the material index in the surface.
                if (theme.Value.HasKey("value")) //single value for the entire geometry
                {
                    ParseMaterialValueNode(geometry, theme, themeIndex);
                }
                else //each surface has its own material
                {
                    ParseMaterialValuesNode(geometry, themeIndex, theme);
                }

                themeIndex++;
            }
        }

        private static void ParseMaterialValueNode(CityGeometry geometry, KeyValuePair<string, JSONNode> theme, int themeIndex)
        {
            int materialIndex = -1;
            var materialIndexNode = theme.Value["value"];
            if (!materialIndexNode.IsNull)
            {
                materialIndex = materialIndexNode.AsInt;
            }

            geometry.AddMaterialIndex(themeIndex, materialIndex);
        }

        private static void ParseMaterialValuesNode(CityGeometry geometry, int themeIndex, KeyValuePair<string, JSONNode> theme)
        {
            var boundary = geometry.BoundaryObject;

            switch (boundary)
            {
                case CityMultiPoint:
                    throw new NotSupportedException("Boundary of type " + typeof(CityMultiPoint) + " does not have material support.");
                case CityMultiLineString:
                    throw new NotSupportedException("Boundary of type " + typeof(CityMultiLineString) + " does not have material support.");
                case CitySurface surface:
                    AddMaterial(surface, themeIndex, theme.Value["values"]);
                    break;
                case CityMultiOrCompositeSurface surface:
                    AddMaterial(surface, themeIndex, theme.Value["values"].AsArray);
                    break;
                case CitySolid solid:
                    AddMaterial(solid, themeIndex, theme.Value["values"].AsArray);
                    break;
                case CityMultiOrCompositeSolid solid:
                    AddMaterial(solid, themeIndex, theme.Value["values"].AsArray);
                    break;
                default:
                    throw new ArgumentException("Unknown boundary type: " + boundary.GetType() + " is not supported to convert to mesh");
            }
        }

        private static void AddMaterial(CitySurface boundary, int themeIndex, JSONNode materialIndexNode)
        {
            int materialIndex = -1;
            if (!materialIndexNode.IsNull)
            {
                materialIndex = materialIndexNode.AsInt;
            }
            boundary.AddMaterialIndex(themeIndex, materialIndex);
        }

        private static void AddMaterial(CityMultiOrCompositeSurface boundary, int themeIndex, JSONArray valuesArray)
        {
            for (var i = 0; i < boundary.Surfaces.Count; i++)
            {
                var surface = boundary.Surfaces[i];
                AddMaterial(surface, themeIndex, valuesArray[i]);
            }
        }

        private static void AddMaterial(CitySolid boundary, int themeIndex, JSONArray valuesArray)
        {
            for (var i = 0; i < boundary.Shells.Count; i++)
            {
                var shell = boundary.Shells[i];
                var materialIndexArray = valuesArray[i].AsArray;
                AddMaterial(shell, themeIndex, materialIndexArray);
            }
        }

        private static void AddMaterial(CityMultiOrCompositeSolid boundary, int themeIndex, JSONArray valuesArray)
        {
            for (var i = 0; i < boundary.Solids.Count; i++)
            {
                var solid = boundary.Solids[i];
                var materialIndexArray = valuesArray[i].AsArray;
                AddMaterial(solid, themeIndex, materialIndexArray);
            }
        }

        // public static int GetMaterialValuesDepth(CityBoundary boundaryObject)
        // {
        //     if (boundaryObject is CityMultiOrCompositeSurface)
        //         return 1;
        //     if (boundaryObject is CitySolid)
        //         return 2;
        //     if (boundaryObject is CityMultiOrCompositeSolid)
        //         return 3;
        //
        //     return 0; // All other boundary types get one index per theme
        // }
    }
}