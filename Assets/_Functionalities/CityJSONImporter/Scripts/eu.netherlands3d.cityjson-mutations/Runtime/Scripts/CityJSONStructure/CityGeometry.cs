using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace Netherlands3D.CityJson.Structure
{
    /// <summary>
    /// 3. A Geometry object is a JSON object for which the type member’s value is one of the following
    /// </summary>
    public enum GeometryType
    {
        MultiPoint = 00,
        MultiLineString = 10,
        MultiSurface = 20,
        CompositeSurface = 21,
        Solid = 30,
        MultiSolid = 40,
        CompositeSolid = 41,
    }

    /// <summary>
    /// A CityGeometry Object has the same structure as described in the specs: https://www.cityjson.org/specs/1.0.3/#geometry-objects
    /// </summary>
    public class CityGeometry
    {
        public GeometryType Type { get; private set; }
        public int Lod { get; private set; }
        public CityBoundary BoundaryObject { get; private set; }
        public bool UseSingleMaterialForEntireGeometry => !IncludeMaterials || MaterialIndicesForFullGeometry.Count > 0;
        public Dictionary<string, int> MaterialThemes { get; private set; } = new();
        public List<int> MaterialIndicesForFullGeometry { get; private set; } = new();
        public bool IncludeSemantics { get; set; }
        public bool IncludeMaterials { get; set; }
        public bool IncludeTextures { get; set; } //todo: Textures currently not implemented yet

        private static readonly Dictionary<CityObjectType, GeometryType[]> ValidGeometryTypes1_0 = new()
        {
            { CityObjectType.Building, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingPart, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingInstallation, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.Bridge, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgePart, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgeInstallation, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgeConstructionElement, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.Tunnel, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.TunnelPart, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.TunnelInstallation, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.CityFurniture, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.GenericCityObject, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.SolitaryVegetationObject, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.LandUse, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.PlantCover, new[] { GeometryType.MultiSurface, GeometryType.MultiSolid } },
            { CityObjectType.Railway, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.Road, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.TransportSquare, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.TINRelief, new[] { GeometryType.CompositeSurface } },
            { CityObjectType.WaterBody, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
        };

        private static readonly Dictionary<CityObjectType, GeometryType[]> ValidGeometryTypes1_1 = new()
        {
            { CityObjectType.Building, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingPart, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingInstallation, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.Bridge, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgePart, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgeInstallation, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgeConstructiveElement, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.Tunnel, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.TunnelPart, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.TunnelInstallation, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.CityFurniture, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.GenericCityObject, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.OtherConstruction, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.SolitaryVegetationObject, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.LandUse, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.PlantCover, new[] { GeometryType.MultiSurface, GeometryType.MultiSolid } },
            { CityObjectType.Railway, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.Road, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.TransportSquare, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.TINRelief, new[] { GeometryType.CompositeSurface } },
            { CityObjectType.WaterBody, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.Waterway, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
        };

        private static readonly Dictionary<CityObjectType, GeometryType[]> ValidGeometryTypes2_0 = new()
        {
            { CityObjectType.Building, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingPart, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingInstallation, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingConstructiveElement, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingFurniture, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingStorey, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingRoom, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BuildingUnit, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.Bridge, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgePart, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgeInstallation, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgeConstructiveElement, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgeRoom, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.BridgeFurniture, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.Tunnel, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.TunnelPart, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.TunnelInstallation, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.TunnelConstructiveElement, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.TunnelHollowSpace, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.TunnelFurniture, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.CityFurniture, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.OtherConstruction, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.SolitaryVegetationObject, new[] { GeometryType.MultiPoint, GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.LandUse, new[] { GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.PlantCover, new[] { GeometryType.MultiSurface, GeometryType.MultiSolid } },
            { CityObjectType.Railway, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.Road, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.TransportSquare, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
            { CityObjectType.TINRelief, new[] { GeometryType.CompositeSurface } },
            { CityObjectType.WaterBody, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface, GeometryType.Solid, GeometryType.CompositeSolid } },
            { CityObjectType.Waterway, new[] { GeometryType.MultiLineString, GeometryType.MultiSurface, GeometryType.CompositeSurface } },
        };

        //Certain CityObjectTypes can only have certain types of geometry. This is described in the specs.
        public static bool IsValidType(CityObjectType cityObjectType, GeometryType geometryType, string version)
        {
            GeometryType[] validTypes;
            if (!version.StartsWith("1.0"))
                validTypes = ValidGeometryTypes1_0[cityObjectType];
            else if (version.StartsWith("1.1"))
                validTypes = ValidGeometryTypes1_1[cityObjectType];
            else
                validTypes = ValidGeometryTypes2_0[cityObjectType];
            return Array.IndexOf(validTypes, geometryType) >= 0;
        }

        public CityGeometry(GeometryType geometryType, int lod, bool includeSemantics, bool includeMaterials, bool includeTextures)
        {
            //mandatory
            Type = geometryType;
            Lod = lod;
            BoundaryObject = CreateBoundaryObject(geometryType);

            //optional
            IncludeSemantics = includeSemantics;
            IncludeMaterials = includeMaterials;
            IncludeTextures = includeTextures;
        }

        //create a Boundary object based on the geometry type.
        private CityBoundary CreateBoundaryObject(GeometryType geometryType)
        {
            switch (geometryType)
            {
                case GeometryType.MultiPoint:
                    return new CityMultiPoint();
                case GeometryType.MultiLineString:
                    return new CityMultiLineString();
                case GeometryType.MultiSurface:
                    return new CityMultiOrCompositeSurface();
                case GeometryType.CompositeSurface:
                    return new CityMultiOrCompositeSurface();
                case GeometryType.Solid:
                    return new CitySolid();
                case GeometryType.MultiSolid:
                    return new CityMultiOrCompositeSolid();
                case GeometryType.CompositeSolid:
                    return new CityMultiOrCompositeSolid();
                default:
                    return null;
            }
        }

        //Exports the Geometry as a JSONObject, and renumbers the boundaries to fit the the vertices provided in currentCityJSONVertices. Missing vertices will be added to currentCityJSONVertices
        public virtual JSONObject GetGeometryNodeAndAddVertices(Dictionary<Vector3Double, int> currentCityJSONVertices)
        {
            var geometryNode = new JSONObject();
            geometryNode["type"] = Type.ToString();
            geometryNode["lod"] = Lod;
            geometryNode["boundaries"] = BoundaryObject.GetBoundariesAndAddNewVertices(currentCityJSONVertices);

            if (IncludeSemantics)
                geometryNode["semantics"] = CityGeometrySemantics.GetSemanticObject(BoundaryObject);
            //if (IncludeMaterials)
            //geometryNode["material"] = GetMaterials();
            if (IncludeTextures)
                geometryNode["texture"] = GetTextures();

            return geometryNode;
        }

        public IEnumerable<Vector3Double> GetUncombinedVertices()
        {
            return BoundaryObject.GetUncombinedVertices();
        }

        //parse a Geometry JSON into a CityGeometry object
        public static CityGeometry FromJSONNode(JSONNode geometryNode, List<Vector3Double> combinedVertices)
        {
            var type = (GeometryType)Enum.Parse(typeof(GeometryType), geometryNode["type"]);
            var lod = geometryNode["lod"].AsInt;

            //private CityBoundary boundaryObject;
            var semanticsNode = geometryNode["semantics"];
            var materialsNode = geometryNode["material"];
            var texturesNode = geometryNode["texture"];

            var includeSemantics = semanticsNode.Count > 0;
            var includeMaterials = materialsNode.Count > 0;
            var includeTextures = texturesNode.Count > 0;

            var geometry = new CityGeometry(type, lod, includeSemantics, includeMaterials, includeTextures);
            geometry.BoundaryObject.FromJSONNode(geometryNode["boundaries"].AsArray, combinedVertices);
            if (includeSemantics)
                CityGeometrySemantics.FromJSONNode(semanticsNode, geometry.BoundaryObject);

            if (includeMaterials)
            {
                // geometry.MaterialIndices = GetMaterialsFromJSONNode(materialsNode["default_theme"]);
                CityMaterial.FromJSONNode(materialsNode, geometry);
            }

            return geometry;
        }

        private JSONNode GetTextures()
        {
            throw new System.NotImplementedException();
        }

        public void CreateBoundaryObjectFromMesh(Mesh mesh)
        {
            var multiSurface = new CityMultiOrCompositeSurface();
            multiSurface.FromMesh(mesh);
            BoundaryObject = multiSurface;
        }

        public void AddMaterialTheme(string themeName, int themeIndex)
        {
            MaterialThemes.Add(themeName, themeIndex);
        }

        public void AddMaterialIndex(int themeIndex, int materialIndex)
        {
            MaterialIndicesForFullGeometry.Insert(themeIndex, materialIndex);
        }

        public List<int> GetMaterialIndices(string themeName)
        {
            if (MaterialThemes.TryGetValue(themeName, out int themeIndex))
            {
                return new List<int> { MaterialIndicesForFullGeometry[themeIndex] };
            }

            //theme not found, return the first theme if there are themes defined
            if (MaterialIndicesForFullGeometry.Count > 0)
            {
                return new List<int>() { MaterialIndicesForFullGeometry[0] };
            }

            return new List<int>(); //no materials defined
        }

        public int GetThemeIndex(string themeName)
        {
            return MaterialThemes.GetValueOrDefault(themeName, 0); //return the first available theme index when the theme name is not found
        }
    }
}