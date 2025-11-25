using System;
using System.Collections.Generic;
using Netherlands3D.Tilekit.WriteModel;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.ColdStorageMaterializers
{
    public partial class Ogc3DTilesMaterializer
    {
        private sealed class TilesetDto
        {
            [JsonProperty("asset")] public AssetDto Asset { get; set; }

            [JsonProperty("geometricError")] public double GeometricError { get; set; }

            [JsonProperty("root")] public TileDto Root { get; set; }
        }

        private sealed class AssetDto
        {
            [JsonProperty("version")] public string Version { get; set; }

            [JsonProperty("tilesetVersion")] public string TilesetVersion { get; set; }
        }

        private sealed class TileDto
        {
            [JsonProperty("boundingVolume")] public BoundingVolumeDto BoundingVolume { get; set; }
            [JsonProperty("geometricError")] public double GeometricError { get; set; }
            [JsonProperty("refine")] public string Refine { get; set; } // "ADD" or "REPLACE"
            [JsonProperty("transform")] public double[] Transform { get; set; } // optional
            [JsonProperty("children")] public List<TileDto> Children { get; set; } = new();
            [JsonProperty("content")] public ContentDto Content { get; set; }
            [JsonProperty("contents")] public List<ContentDto> Contents { get; set; } = new();

            public bool IntersectsAoi { get; set; }
            // For now we ignore implicit tiling here; add later if needed.
        }

        public sealed class BoundingVolumeDto
        {
            // 3D Tiles fields.
            // For 'box': [cx, cy, cz, hx0, hx1, hx2, hy0, hy1, hy2, hz0, hz1, hz2]
            public double[] Box { get; set; }

            // For 'region': [west, south, east, north, minHeight, maxHeight]
            public double[] Region { get; set; }

            // For 'sphere': [x, y, z, radius]
            public double[] Sphere { get; set; }

            /// <summary>
            /// Returns true if this bounding volume intersects the given AOI box
            /// on the X/Y plane. Height (Z) is ignored.
            /// </summary>
            public bool Intersects2D(BoxBoundingVolume aoi)
            {
                var box = ToBoxBoundingVolume();
                return !box.HasValue || Intersects2D(box.Value, aoi);
            }

            public BoxBoundingVolume? ToBoxBoundingVolume()
            {
                BoxBoundingVolume? box = null;
                // Prefer box if present
                if (Box is { Length: 12 })
                {
                    box = CreateBoxFrom3DTilesArray(Box);
                }

                // Region → approximate as axis-aligned box in same space
                if (Region is { Length: 6 })
                {
                    box = CreateRegionBoxFrom3DTilesArray(Region);
                }

                // Sphere → approximate as axis-aligned box in same space
                if (Sphere is { Length: 4 })
                {
                    box = CreateSphereBoxFrom3DTilesArray(Sphere);
                }

                return box;
            }

            /// <summary>
            /// Converts a 3D Tiles box array [cx,cy,cz, hx0,hx1,hx2, hy0,hy1,hy2, hz0,hz1,hz2]
            /// into your BoxBoundingVolume.
            /// </summary>
            private static BoxBoundingVolume CreateBoxFrom3DTilesArray(double[] a)
            {
                if (a.Length != 12)
                    throw new ArgumentException("3D Tiles box must have 12 elements.", nameof(a));

                var center = new double3(a[0], a[1], a[2]);
                var halfAxisX = new double3(a[3], a[4], a[5]);
                var halfAxisY = new double3(a[6], a[7], a[8]);
                var halfAxisZ = new double3(a[9], a[10], a[11]);

                return new BoxBoundingVolume(center, halfAxisX, halfAxisY, halfAxisZ);
            }

            /// <summary>
            /// Converts a 3D Tiles region array [west, south, east, north, minHeight, maxHeight]
            /// into a BoxBoundingVolume, then we use its X/Y extents for AOI intersection.
            ///
            /// Assumes the region coordinates are in the same projected space as the AOI.
            /// If they're lon/lat in radians, you should reproject before calling this.
            /// </summary>
            private static BoxBoundingVolume CreateRegionBoxFrom3DTilesArray(double[] a)
            {
                if (a.Length != 6)
                    throw new ArgumentException("3D Tiles region must have 6 elements.", nameof(a));

                double west = a[0];
                double south = a[1];
                double east = a[2];
                double north = a[3];
                double minHeight = a[4];
                double maxHeight = a[5];

                // Min/max corners in whatever space you're using.
                var min = new double3(west, south, minHeight);
                var max = new double3(east, north, maxHeight);

                return BoxBoundingVolume.FromTopLeftAndBottomRight(min, max);
            }

            /// <summary>
            /// Converts a 3D Tiles sphere array [x, y, z, radius] into a BoxBoundingVolume
            /// by creating an axis-aligned box that encloses the sphere.
            /// </summary>
            private static BoxBoundingVolume CreateSphereBoxFrom3DTilesArray(double[] a)
            {
                if (a.Length != 4)
                    throw new ArgumentException("3D Tiles sphere must have 4 elements.", nameof(a));

                var center = new double3(a[0], a[1], a[2]);
                double r = a[3];

                var min = center - new double3(r, r, r);
                var max = center + new double3(r, r, r);

                return BoxBoundingVolume.FromTopLeftAndBottomRight(min, max);
            }

            /// <summary>
            /// Axis-aligned 2D box intersection on X/Y, ignoring Z.
            /// Uses TopLeft/BottomRight, which in your struct represent min/max corners.
            /// </summary>
            private static bool Intersects2D(BoxBoundingVolume lhs, BoxBoundingVolume rhs)
            {
                var minA = lhs.TopLeft;
                var maxA = lhs.BottomRight;
                var minB = rhs.TopLeft;
                var maxB = rhs.BottomRight;

                bool overlapX = minA.x <= maxB.x && maxA.x >= minB.x;
                bool overlapY = minA.y <= maxB.y && maxA.y >= minB.y;

                return overlapX && overlapY;
            }
        }

        private sealed class ContentDto
        {
            [JsonProperty("uri")] public string Uri { get; set; }
            [JsonProperty("boundingVolume")] public BoundingVolumeDto BoundingVolume { get; set; }
        }

        private sealed class TileDtoAoiConverter : JsonConverter<TileDto>
        {
            private readonly BoxBoundingVolume areaOfInterest;

            // You’d typically inject AOI via constructor:
            public TileDtoAoiConverter(BoxBoundingVolume areaOfInterest)
            {
                this.areaOfInterest = areaOfInterest;
            }

            public override TileDto ReadJson(
                JsonReader reader,
                Type objectType,
                TileDto existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                Debug.Log("Reading tiledto");
                if (reader.TokenType == JsonToken.Null)
                    return null;

                // Expect start object
                if (reader.TokenType != JsonToken.StartObject)
                    throw new JsonException($"Expected StartObject for tile, got {reader.TokenType}");

                var tile = new TileDto();
                bool aoiChecked = false;
                bool insideAoi = true; // default = keep if something goes odd

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject) break;
                    if (reader.TokenType != JsonToken.PropertyName) continue;

                    string propName = (string)reader.Value!;

                    switch (propName)
                    {
                        // TODO: UGLY ASSUMPTION: bounding volume must be first property!
                        case "boundingVolume":
                            // Move to value and let serializer do the nested object
                            reader.Read();
                            tile.BoundingVolume = serializer.Deserialize<BoundingVolumeDto>(reader);

                            aoiChecked = true;
                            // TODO: remove AoI check for this moment to see if it works - restore it later 
                            // insideAoi = tile.BoundingVolume.Intersects2D(areaOfInterest);
                            insideAoi = true;
                            tile.IntersectsAoi = insideAoi;
                            break;

                        case "geometricError":
                            reader.Read();
                            tile.GeometricError = Convert.ToDouble(reader.Value);
                            break;

                        case "refine":
                            reader.Read();
                            tile.Refine = (string)reader.Value!;
                            break;

                        case "transform":
                            reader.Read();
                            tile.Transform = serializer.Deserialize<double[]>(reader);
                            break;

                        case "children":
                            // Outside of the AoI or boundingVolume comes after this - do not descend
                            // TODO: This is not ideal, but it's the best we can do for now.
                            if (aoiChecked && !insideAoi)
                            {
                                // Parent is outside AOI → skip entire children array
                                SkipValue(reader);
                            }
                            else
                            {
                                reader.Read(); // move to array
                                tile.Children = serializer.Deserialize<List<TileDto>>(reader);
                            }

                            break;

                        case "content":
                            reader.Read();
                            tile.Content = serializer.Deserialize<ContentDto>(reader);

                            break;

                        case "contents":
                            reader.Read();
                            tile.Contents = serializer.Deserialize<List<ContentDto>>(reader);

                            break;

                        default:
                            // Unknown property: skip
                            SkipValue(reader);
                            break;
                    }
                }

                // Outside of Area of Interest, so ignore the tile
                if (aoiChecked && !insideAoi) return null;

                return tile;
            }

            public override void WriteJson(JsonWriter writer, TileDto value, JsonSerializer serializer)
                => throw new NotImplementedException();

            private static void SkipValue(JsonReader reader)
            {
                // Similar to the helper you already wrote
                if (!reader.Read()) return;

                int depth = 0;
                do
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.StartObject:
                        case JsonToken.StartArray: depth++; break;
                        case JsonToken.EndObject:
                        case JsonToken.EndArray: depth--; break;
                    }
                } while (depth > 0 && reader.Read());
            }
        }
    }
}