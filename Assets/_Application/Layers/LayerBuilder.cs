using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public class LayerBuilder : ILayerBuilder
    {
        internal string Type { get; private set; }
        internal Vector3? Position { get; private set; }
        internal Quaternion? Rotation { get; private set; }
        internal Uri Url { get; private set; }
        private string Name { get; set; }
        private Color? Color { get; set; }
        private LayerData Parent { get; set; }
        internal StoredAuthorization Credentials { get; set; }
        internal List<LayerPropertyData> Properties { get; } = new();
        [CanBeNull] private Symbolizer DefaultSymbolizer { get; set; }
        //private List<LayerStyle> Styles { get; } = new();
        private Action<LayerData> whenBuilt;
        
        internal LayerBuilder()
        {
        }

        public static ILayerBuilder Create() => new LayerBuilder();

        public static ILayerBuilder Create(LayerPresetArgs args) => LayerPresetRegistry.Create(args);

        public ILayerBuilder FromUrl(Uri url) => OfType("url").At(url).WithCredentials(new Public(url));

        public ILayerBuilder FromUrl(string url) => FromUrl(new Uri(url));

        public ILayerBuilder FromFile(Uri url) => OfType("file").At(url);

        public ILayerBuilder OfType(string type)
        {
            Type = type;
            
            return this;
        }

        public ILayerBuilder NamedAs(string name)
        {
            Name = name;
            
            return this;
        }

        public ILayerBuilder WithColor(Color color)
        {
            Color = color;
            
            return this;
        }

        public ILayerBuilder At(Uri url)
        {
            Url = url;

            return this;
        }

        public ILayerBuilder ChildOf(LayerData parent)
        {
            Parent = parent;
            
            return this;
        }

        public ILayerBuilder WithCredentials(StoredAuthorization creds)
        {
            Credentials = creds;
            
            return this;
        }

        public ILayerBuilder AddProperty(LayerPropertyData property)
        {
            Properties.Add(property);
            
            return this;
        }

        public ILayerBuilder AddProperties(params LayerPropertyData[] properties)
        {
            Properties.AddRange(properties);
            
            return this;
        }

        public ILayerBuilder PositionedAt(Vector3 position)
        {
            Position = position;
            
            return this;
        }

        public ILayerBuilder Rotated(Quaternion rotation)
        {
            Rotation = rotation;
            
            return this;
        }

        public ILayerBuilder SetDefaultStyling(Symbolizer symbolizer)
        {
            DefaultSymbolizer = symbolizer;
            
            return this;
        }

        // public ILayerBuilder AddStyle(LayerStyle style)
        // {
        //     Styles.Add(style);
        //
        //     return this;
        // }

        public ILayerBuilder WhenBuilt(Action<LayerData> callback)
        {
            this.whenBuilt = callback;

            return this;
        }

        public LayerData Build()
        {            
            LayerData layerData = new LayerData(Name, Type);
            layerData.InitializeParent();
            ProjectData.Current.RootLayer.AddChild(layerData, 0); //todo: this should not depend on projectData here, but we must set the new layer as child of the rootLayer.

            if (!string.IsNullOrEmpty(Name)) layerData.Name = Name;
            if (Color.HasValue) layerData.Color = Color.Value;
            if (Parent != null) layerData.SetParent(Parent);

            foreach (var property in Properties)
            {
                layerData.SetProperty(property);
            }

            
            
            //TODO if you add more styles with the layerbuilder then you have to add more stylingpropertydata's
            // StylingPropertyData stylingProperty = Properties.Get<StylingPropertyData>();
            // if (stylingProperty != null)
            // {
            //     if (DefaultSymbolizer != null)
            //     {
            //         stylingProperty.DefaultStyle.AnyFeature.Symbolizer = DefaultSymbolizer;
            //     }
            //     foreach (var style in Styles)
            //     {
            //         stylingProperty.AddStyle(style);
            //     }
            // }

            whenBuilt?.Invoke(layerData);

            return layerData;
        }
    }
}