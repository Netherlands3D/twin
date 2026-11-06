using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.UI.Panels;
using UnityEngine;

namespace Netherlands3D.Functionalities.GeoJSON.LayerPresets
{
    [LayerPreset("geojson-electricitypole-layer")]
    public sealed class GeoJSONElectricityPolePreset : ILayerPreset<GeoJSONElectricityPolePreset.Args>
    {
        public sealed class Args : LayerPresetArgs<GeoJSONElectricityPolePreset>
        {
            public string ObjectId { get; set; }
            public string Type { get; set; }
            
            public Coordinate Coordinate { get; set; }
            
            public GeoJSONPresetPrefabMapper PrefabMapper { get; set; }

            public Args(GeoJSONPresetPrefabMapper mapper, Coordinate coordinate, string objectId, string type)
            {
                this.Coordinate = coordinate;
                this.ObjectId = objectId;
                Type = type;
                PrefabMapper = mapper;
            }
        }

        public ILayerBuilder Apply(
            ILayerBuilder builder,
            Args args)
        {
            if(args.PrefabMapper == null || !args.PrefabMapper.TryGetPrefabIdentifier(args.Type, out  var prefabIdentifier))
            {
                Debug.LogError($"Cannot find prefab identifier for electricity pole prefab: {args.Type}, or prefab mapper is missing on import adapter");
                return builder;
            }
            
            return builder
                .OfType(prefabIdentifier)
                .NamedAs(args.ObjectId)
                .AddProperty(new GeoJSONPropertyData(args.Coordinate));
        }

        public ILayerBuilder Apply(
            ILayerBuilder builder,
            LayerPresetArgs args)
        {
            return Apply(builder, (Args)args);
        }
    }
}