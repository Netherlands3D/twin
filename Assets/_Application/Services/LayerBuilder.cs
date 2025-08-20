using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin.Services
{
    public class LayerBuilder : ILayerBuilder
    {
        internal string Type { get; private set; }
        internal Uri Url { get; private set;}
        internal string Name { get; private set; }
        internal Vector3? Position { get; private set; }
        internal Quaternion? Rotation { get; private set; }
        internal Color? Color { get; private set; }
        internal LayerData Parent { get; private set; }
        internal StoredAuthorization Credentials { get; private set; }
        internal List<LayerPropertyData> Properties { get; } = new();
        [CanBeNull] internal Symbolizer DefaultSymbolizer { get; private set; }
        internal List<LayerStyle> Styles { get; } = new();
        
        private LayerBuilder()
        {
        }

        public static ILayerBuilder Start => new LayerBuilder();

        public ILayerBuilder OfType(string type)
        {
            Type = type;
            
            return this;
        }

        public ILayerBuilder FromUrl(Uri url) => OfType("url").At(url).WithCredentials(new Public(url));

        public ILayerBuilder FromUrl(string url) => FromUrl(new Uri(url));
        public ILayerBuilder FromFile(Uri url) => OfType("file").At(url);

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

        public ILayerBuilder ParentUnder(LayerData parent)
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

        public ILayerBuilder AddStyle(LayerStyle style)
        {
            Styles.Add(style);

            return this;
        }

        public LayerData Build(LayerGameObject placeholderPrefab)
        {
            var placeholder = Object.Instantiate(
                placeholderPrefab, 
                Position ?? Vector3.zero, 
                Rotation ?? Quaternion.identity
            );
            var layerData = new ReferencedLayerData(Name, Type, placeholder);
            
            if (!string.IsNullOrEmpty(this.Name))
                layerData.Name = this.Name;

            if (this.Color.HasValue)
                layerData.Color = this.Color.Value;

            if (this.Parent != null)
                layerData.SetParent(this.Parent);

            foreach (var property in this.Properties)
            {
                layerData.AddProperty(property);
            }

            if (this.DefaultSymbolizer != null)
            {
                layerData.DefaultStyle.AnyFeature.Symbolizer = this.DefaultSymbolizer;
            }
            
            foreach (var style in this.Styles)
            {
                layerData.AddStyle(style);
            }

            return layerData;
        }
    }
}