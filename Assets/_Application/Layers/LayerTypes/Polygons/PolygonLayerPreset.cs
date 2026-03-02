using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    [LayerPreset("polygon")]
    public sealed class PolygonLayerPreset : ILayerPreset<PolygonLayerPreset.Args>
    {
        private const string PrefabIdentifier = "0dd48855510674827b667fa4abd5cf60";

        public sealed class Args : LayerPresetArgs<PolygonLayerPreset>
        {
            public string Name { get; }
            public ShapeType ShapeType { get; }
            public List<Coordinate> Coordinates { get; }
            public float LineWidth { get; }

            public Args(string name, ShapeType shapeType, List<Coordinate> coordinates, float lineWidth = 10f)
            {
                Name = name;
                ShapeType = shapeType;
                Coordinates = coordinates ?? new List<Coordinate>();
                LineWidth = lineWidth;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(args.Name)
                .AddProperty(new PolygonSelectionLayerPropertyData()
                {
                    ShapeType = args.ShapeType,
                    OriginalPolygon = args.Coordinates,
                    LineWidth = args.LineWidth
                });
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}