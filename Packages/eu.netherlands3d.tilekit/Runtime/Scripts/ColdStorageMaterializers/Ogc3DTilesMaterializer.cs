using System;
using System.IO;
using Netherlands3D.Tilekit.WriteModel;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.ColdStorageMaterializers
{
    public partial class Ogc3DTilesMaterializer : IColdStorageMaterializer<Ogc3DTilesPopulatorSettings>
    {
        public void Materialize(ColdStorage storage, Ogc3DTilesPopulatorSettings settings)
        {
            Debug.Log("1. Building tileset from");
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double,
                Formatting = Formatting.None,
                Converters = { new TileDtoAoiConverter(storage.AreaOfInterest) },
                NullValueHandling = NullValueHandling.Ignore,
            };

            Debug.Log("2. Building tileset from");
            var serializer = JsonSerializer.Create(jsonSerializerSettings);

            using var sr = new StreamReader(settings.Stream);
            using var reader = new JsonTextReader(sr)
            {
                CloseInput = false,
            };
            Debug.Log("3. Building tileset from");

            TilesetDto tileset;
            try
            {
                // This will stream-deserialize through your converter
                tileset = serializer.Deserialize<TilesetDto>(reader);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }

            Debug.Log("4. Building tileset from");
            
            if (tileset == null)
                throw new InvalidOperationException("Failed to deserialize tileset.json");
            Debug.Log("5. Building tileset from");

            storage.Clear();
            try
            {
                HydrateTile(storage, tileset.Root);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private int HydrateTile(ColdStorage storage, TileDto tile)
        {
            if (tile == null)
            {
                Debug.Log("No tile");
                return -1;
            }
            Debug.Log("Hydrating tile with geometric error " + tile.GeometricError);
            var boxBoundingVolume = tile.BoundingVolume.ToBoxBoundingVolume();
            if (!boxBoundingVolume.HasValue)
            {
                Debug.Log("Could not decipher bounding volume of tile");
                // Invalid tile - so return a -1 to indicate that it is invalid
                return -1;
            }

            // We know how many children we have, so we can pre-allocate the array - and after adding the children we know their indices
            // and update the store.
            var childIndices = new NativeArray<int>(tile.Children.Count, Allocator.Temp);

            int tileIndex = storage.AddTile(
                boxBoundingVolume.Value,
                tile.GeometricError,
                ReadOnlySpan<TileContentData>.Empty, // TODO: populate contents
                childIndices,
                tile.Refine == "REPLACE" ? MethodOfRefinement.Replace : MethodOfRefinement.Add,
                float4x4.identity
            );

            Debug.Log("Hydrating tile with id " + tileIndex + " and children: " + tile.Children.Count);
            // Create the children and capture their tile ids
            for (var index = 0; index < tile.Children.Count; index++)
            {
                childIndices[index] = HydrateTile(storage, tile.Children[index]);
            }

            // Update the tile's children
            storage.Children[tileIndex].Replace(childIndices);

            childIndices.Dispose();

            return tileIndex;
        }
    }

    public struct Ogc3DTilesPopulatorSettings
    {
        public Stream Stream;
    }
}