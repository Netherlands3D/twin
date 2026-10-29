using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;

namespace Netherlands3D.Functionalities.GeoJSON.LayerPresets
{
    [LayerPreset("geojson-electricitypole-layer")]
    public sealed class GeoJSONElectricityPolePreset : ILayerPreset<GeoJSONElectricityPolePreset.Args>
    {
        private const string PrefabIdentifier = "353ba723424e3d0469d4c770da8fc061";

        public sealed class Args : LayerPresetArgs<GeoJSONElectricityPolePreset>
        {
            public string ObjectId { get; set; }
            public string Type { get; set; }
            
            public Coordinate Coordinate { get; set; }

            public Args(Coordinate coordinate, string objectId, string type)
            {
                this.Coordinate = coordinate;
                this.ObjectId = objectId;
                Type = type;
            }
        }

        public ILayerBuilder Apply(
            ILayerBuilder builder,
            Args args)
        {
            return builder
                .OfType(PrefabIdentifier)
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