using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON;
using static Netherlands3D.WMSTiles3D.TileSetsConfigLoader.Config;
using Netherlands3D.Coordinates;
using NetTopologySuite.Index.Quadtree;
using Unity.Mathematics;


namespace Netherlands3D.WMSTiles3D
{
    public enum QuadtreeQuadrant
        {
            Topleft=0,
            Topright = 1,
            BottomLeft=2,
            BottomRight=3


        }
    public static class ParseTileset
    {
        
        public static bool DebugLog = false;
        
        private static readonly Dictionary<string, BoundingVolumeType> boundingVolumeTypes = new()
        {
            { "region", BoundingVolumeType.Region },
            { "box", BoundingVolumeType.Box },
            { "sphere", BoundingVolumeType.Sphere }
        };
        public static readonly string[] SupportedExtensions = Array.Empty<string>(); //currently no extensions are supported
        
        internal static ReadSubtree subtreeReader;
        internal static Tile ReadTileset( Read3DTileset tileset)
        {
            Tile root = new Tile();
            root.tileSet = tileset;
            root.boundingVolume = new BoundingVolume();
            root.boundingVolume.boundingVolumeType = BoundingVolumeType.Box;
            root.refine="REPLACE";
            Coordinate center = tileset.wmsLayerProperties.boundingBox.Center;
            double boxWidth = tileset.wmsLayerProperties.boundingBox.TopRight.easting - tileset.wmsLayerProperties.boundingBox.BottomLeft.easting;
            double boxHeight = tileset.wmsLayerProperties.boundingBox.TopRight.northing - tileset.wmsLayerProperties.boundingBox.BottomLeft.northing;
            double tileSize = math.max(boxWidth,boxHeight);

            double centerElevation = tileset.heightmap.GetHeight(center);
            center.height=centerElevation;
            double elevationMax = centerElevation+10;
            double elevationMin = centerElevation-10;
            double averageElevation = (elevationMax + elevationMin)/2;
            double halfElevation = elevationMax - averageElevation;

            root.boundingVolume.values = new double[12]{center.easting,center.northing,averageElevation,tileSize/2,0,0,0,tileSize/2,0,0,0,halfElevation};

            double[] transformValues = new double[16] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 };
            root.tileTransform = new TileTransform(transformValues);
            root.level = 0;
            root.X=0;
            root.Y=0;
            root.contentType = TileContentType.WMS;
            root.geometricError = tileSize / tileset.wmsLayerProperties.maxWidth;
            double maximumBoxWidth = tileset.wmsLayerProperties.maxScaleDenominator*0.00028d*tileset.wmsLayerProperties.maxWidth;
            if (maximumBoxWidth<tileSize)
            {
                root.contentType = TileContentType.None;
            }
            root.children.Add(CreateChildTile(root,QuadtreeQuadrant.Topleft));
            root.children.Add(CreateChildTile(root,QuadtreeQuadrant.Topright));
            root.children.Add(CreateChildTile(root,QuadtreeQuadrant.BottomLeft));
            root.children.Add(CreateChildTile(root,QuadtreeQuadrant.BottomRight));
            return root;
        }

        internal static Tile CreateChildTile(Tile parentTile, QuadtreeQuadrant quadtreeQuadrant)
        {
            Tile newTile = new Tile();
            newTile.parent = parentTile;
            newTile.level = parentTile.level+1;
            newTile.tileSet = parentTile.tileSet;
            newTile.X = 2*parentTile.X + ((int)quadtreeQuadrant%2);
            newTile.Y = 2*parentTile.Y + (int)math.floor((int)quadtreeQuadrant/2);
            newTile.refine = "REPLACE";
            double boxWidth = parentTile.boundingVolume.values[3];
            newTile.contentType = parentTile.contentType;
            if(parentTile.contentType != TileContentType.WMS)
            {
                double maximumBoxWidth = parentTile.tileSet.wmsLayerProperties.maxScaleDenominator*0.00028d*parentTile.tileSet.wmsLayerProperties.maxWidth;
                if (maximumBoxWidth>boxWidth)
                {
                    newTile.contentType = TileContentType.WMS;
                }
            }
            newTile.geometricError = parentTile.geometricError*0.5;
            BoundingVolume newBoundingVolume = parentTile.boundingVolume.GetChildBoundingVolume((int)quadtreeQuadrant,SubdivisionScheme.Quadtree);
            Coordinate center = new Coordinate(CoordinateSystem.RD,newBoundingVolume.values[0],newBoundingVolume.values[1]);
            float centerelevation = parentTile.tileSet.heightmap.GetHeight(center);
            newBoundingVolume.values[2]=centerelevation;
            newTile.boundingVolume = newBoundingVolume;
            if (newTile.tileSet.wmsLayerProperties.maximumZoomlevel==newTile.level) return newTile;

            if(newTile.level%newTile.tileSet.wmsLayerProperties.MaxTreeDepth==0)
                {
                newTile.hasSubtree=true;
                return newTile;
                }
            newTile.children.Add(CreateChildTile(newTile,QuadtreeQuadrant.Topleft));
            newTile.children.Add(CreateChildTile(newTile,QuadtreeQuadrant.Topright));
            newTile.children.Add(CreateChildTile(newTile,QuadtreeQuadrant.BottomLeft));
            newTile.children.Add(CreateChildTile(newTile,QuadtreeQuadrant.BottomRight));

           return newTile; 
        }

        /// <summary>
        /// Recursive reading of tile nodes to build the tiles tree
        /// </summary>
        internal static Tile ReadExplicitNode(JSONNode node, Tile tile)
        {
            JSONNode transformNode = node["transform"];

            double[] transformValues = new double[16] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 };
            if (transformNode!=null)
            {
                for (int i = 0; i < 16; i++)
                {
                    transformValues[i] = transformNode[i].AsDouble;
                }
            }
            TileTransform myTileTransform = new TileTransform(transformValues);
            if (tile.parent!=null)
            {
                tile.tileTransform = tile.parent.tileTransform * myTileTransform;
            }
            else
            {
                tile.tileTransform = myTileTransform;
            }
           
            tile.boundingVolume = new BoundingVolume();
            JSONNode boundingVolumeNode = node["boundingVolume"];
            tile.boundingVolume = ParseBoundingVolume( boundingVolumeNode);
            tile.boundingVolume.ApplyTileTransform(tile.tileTransform);
            tile.CalculateUnitBounds();
            tile.geometricError = double.Parse(node["geometricError"].Value);
            if (node["refine"] !=null)
            {
                tile.refine = node["refine"].Value;
            }
            else
            {
                tile.refine = tile.parent.refine;
            }
            
            JSONNode childrenNode = node["children"];

            tile.children = new List<Tile>();
            if (childrenNode != null)
            {
                for (int i = 0; i < childrenNode.Count; i++)
                {
                    var childTile = new Tile();
                    childTile.tileSet = tile.tileSet;
                    childTile.parent = tile;
                    tile.children.Add(ReadExplicitNode(childrenNode[i], childTile));
                }
            }
            JSONNode contentNode = node["content"];
            if (contentNode != null)
            {
                tile.hascontent = true;
                tile.contentUri = contentNode["uri"].Value;
            }

            return tile;
        }

        internal static BoundingVolume ParseBoundingVolume(JSONNode boundingVolumeNode)
        {
            if (boundingVolumeNode != null)
            {
                BoundingVolume boundingVolume = new BoundingVolume();
                foreach (KeyValuePair<string, BoundingVolumeType> kvp in boundingVolumeTypes)
                {
                    JSONNode volumeNode = boundingVolumeNode[kvp.Key];
                    if (volumeNode != null)
                    {
                        int length = GetBoundingVolumeLength(kvp.Value);
                        if (volumeNode.Count == length)
                        {
                            boundingVolume.values = new double[length];
                            for (int i = 0; i < length; i++)
                            {
                                boundingVolume.values[i] = volumeNode[i].AsDouble;
                            }
                            boundingVolume.boundingVolumeType = kvp.Value;

                            return boundingVolume;

                        }
                    }
                }
            }
            return null;
            
        }

        public static int GetBoundingVolumeLength(BoundingVolumeType type)
        {
            switch (type)
            {
                case BoundingVolumeType.Region:
                    return 6;
                case BoundingVolumeType.Box:
                    return 12;
                case BoundingVolumeType.Sphere:
                    return 4;
                default:
                    return 0;
            }
        }

        private static void ReadImplicitTiling(JSONNode rootnode, Tile parentTile)
        {
            return;
            ImplicitTilingSettings implicitTilingSettings = new ImplicitTilingSettings();
            string refine = rootnode["refine"].Value;
            switch (refine)
            {
                case "REPLACE":
                    implicitTilingSettings.refinementType = RefinementType.Replace;
                    break;
                case "ADD":
                    implicitTilingSettings.refinementType = RefinementType.Add;
                    break;
                default:
                    break;
            }
            implicitTilingSettings.geometricError = rootnode["geometricError"].AsFloat;
           
            
            implicitTilingSettings.contentUri = rootnode["content"]["uri"].Value;
            JSONNode implicitTilingNode = rootnode["implicitTiling"];
            string subdivisionScheme = implicitTilingNode["subdivisionScheme"].Value;
            switch (subdivisionScheme)
            {
                case "QUADTREE":
                    implicitTilingSettings.subdivisionScheme = SubdivisionScheme.Quadtree;
                    break;
                default:
                    implicitTilingSettings.subdivisionScheme = SubdivisionScheme.Octree;
                    break;
            }
            implicitTilingSettings.boundingVolume = ParseBoundingVolume(rootnode["boundingVolume"]);
            implicitTilingSettings.availableLevels = implicitTilingNode["availableLevels"];
            implicitTilingSettings.subtreeLevels = implicitTilingNode["subtreeLevels"];
            implicitTilingSettings.subtreeUri = implicitTilingNode["subtrees"]["uri"].Value;
            subtreeReader.settings = implicitTilingSettings;
            
            if(DebugLog)
                Debug.Log("Load subtree: " + "");
            if (parentTile.level==0)
            {
                parentTile.contentUri = implicitTilingSettings.subtreeUri.Replace("{level}", parentTile.level.ToString()).Replace("{x}", parentTile.X.ToString()).Replace("{y}", parentTile.Y.ToString());
            }
            subtreeReader.DownloadSubtree("", implicitTilingSettings,parentTile, null);
        }

        internal static (string[], string[]) GetUsedExtensions(JSONNode rootNode)
        {
            var extensionsUsedNode = rootNode["extensionsUsed"];

            if (extensionsUsedNode == null)
                return (Array.Empty<string>(), Array.Empty<string>());
            
            var extensionsUsedArray = extensionsUsedNode.AsArray;
            string[] extensionsUsed = new string[extensionsUsedArray.Count];
            for (var i = 0; i < extensionsUsedArray.Count; i++)
            {
                var item = extensionsUsedArray[i];
                extensionsUsed[i] = item.Value;
            }

            var unsupportedExtensions = GetUnsupportedExtensions(extensionsUsed);
            
            return (extensionsUsed, unsupportedExtensions);
        }

        internal static string[] GetUnsupportedExtensions(string[] extensionsUsed)
        {
            var list = new List<string>();
            foreach (var extension in extensionsUsed)
            {
                if (!SupportedExtensions.Contains(extension))
                {
                    list.Add(extension);
                }
            }

            return list.ToArray();
        }
    }
}
