using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Panels;
using UnityEngine;

namespace Netherlands3D.Functionalities.GeoJSON.LayerPresets
{
    [LayerPreset("geojson-annotation-layer")]
    public sealed class GeoJSONAnnotationPreset : ILayerPreset<GeoJSONAnnotationPreset.Args>
    {
        private const string PrefabIdentifier = "292690ace10994ff5bd502be1f5a5bf2";

        public sealed class Args : LayerPresetArgs<GeoJSONAnnotationPreset>
        {
            public string Title { get; }
            public string AnnotationText { get; set; }
            public string ImageUrl { get; set; }
            public string ImageCaption { get; set; }
            public Color Color { get; set; }
            
            public bool ReadOnly { get; set; }

            public Args(string title, string annotationText, string imageUrl, string imageCaption, Color color, bool readOnly = false)
            {
                Title = title;
                AnnotationText = annotationText;
                ImageUrl = imageUrl;
                ImageCaption = imageCaption;
                Color = color;
                ReadOnly = readOnly;
            }
        }

        public ILayerBuilder Apply(
            ILayerBuilder builder,
            Args args)
        {
            return builder
                    .OfType(PrefabIdentifier)
                    .NamedAs(args.Title)
                    .AddProperty(new AnnotationPropertyData(args.Title, args.AnnotationText, args.ImageUrl, args.ImageCaption, args.ReadOnly))
                    .WithColor(args.Color);
                
        }

        public ILayerBuilder Apply(
            ILayerBuilder builder,
            LayerPresetArgs args)
        {
            return Apply(builder, (Args)args);
        }
    }
}