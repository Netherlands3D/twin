using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON;
using static Netherlands3D.Tiles3D.TileSetsConfigLoader.Config;

namespace Netherlands3D.Tiles3D
{
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
        internal static Tile ReadTileset(JSONNode rootnode, Read3DTileset tileset)
        {
            Tile root = new Tile();
            root.tileSet = tileset;
            TilingMethod tilingMethod = TilingMethod.ExplicitTiling;
            double[] transformValues = new double[16] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 };
            JSONNode transformNode = rootnode["transform"];
            if (transformNode != null)
            {
                for (int i = 0; i < 16; i++)
                {
                    transformValues[i] = transformNode[i].AsDouble;
                }
            }
            root.tileTransform = new TileTransform(transformValues);
            root.transform = transformValues;
            JSONNode implicitTilingNode = rootnode["implicitTiling"];
            if (implicitTilingNode != null)
            {
                tilingMethod = TilingMethod.ImplicitTiling;
            }

            //setup location and rotation
            switch (tilingMethod)
            {
                case TilingMethod.ExplicitTiling:
                    if(DebugLog)
                        Debug.Log("Explicit tiling");
                    Tile rootTile = new Tile();
                    rootTile.tileSet = tileset;
                    rootTile.tileTransform = root.tileTransform;
                    root = ReadExplicitNode(rootnode, rootTile);
                    
                    rootTile.transform = root.transform;

                    if (rootTile.children.Count==0 )
                    {
                        Tile childTile = new Tile();
                        childTile.tileSet = tileset;
                        childTile = ReadExplicitNode(rootnode, childTile);
                        childTile.parent = rootTile;
                        rootTile.geometricError += 10;
                        childTile.parent.contentUri = "";
                        rootTile.children.Add(childTile);
                        
                    }
                    break;
                case TilingMethod.ImplicitTiling:
                    if(DebugLog)
                        Debug.Log("Implicit tiling"); 
                    rootTile = new Tile();
                    rootTile.tileSet = tileset;
                    rootTile = ReadExplicitNode(rootnode, rootTile);
                    rootTile.level = 0;
                    rootTile.X = 0;
                    rootTile.Y = 0;
                    rootTile.transform = root.transform;
                    rootTile.tileTransform = root.tileTransform;
                    ReadImplicitTiling(rootnode,rootTile);

                    rootTile.transform = root.transform;
                    root = rootTile;
                    break;
                default:
                    break;
            }
            return root;
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
                    childTile.transform = tile.transform;
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
