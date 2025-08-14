using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D._Application._Twin.SDK
{
    public record Layer
    {
        internal string Type { get; }
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

        private Layer(string type)
        {
            this.Type = type;
        }
        
        public static Layer OfType(string type) => new(type);
        public static Layer FromUrl(Uri url) => new Layer("url").At(url).WithCredentials(new Public(url));
        public static Layer FromUrl(string url) => FromUrl(new Uri(url));
        public static Layer FromFile(Uri url) => new Layer("file").At(url);

        public Layer NamedAs(string name)
        {
            Name = name;
            return this;
        }

        public Layer WithColor(Color color)
        {
            Color = color;
            return this;
        }

        public Layer At(Uri url)
        {
            Url = url;

            return this;
        }

        public Layer ParentUnder(LayerData parent)
        {
            Parent = parent;
            return this;
        }

        public Layer WithCredentials(StoredAuthorization creds)
        {
            Credentials = creds;
            return this;
        }

        public Layer AddProperty(LayerPropertyData property)
        {
            Properties.Add(property);
            return this;
        }

        public Layer AddProperties(params LayerPropertyData[] properties)
        {
            Properties.AddRange(properties);
            return this;
        }

        public Layer PositionedAt(Vector3 position)
        {
            Position = position;
            
            return this;
        }

        public Layer Rotated(Quaternion rotation)
        {
            Rotation = rotation;
            
            return this;
        }

        public Layer SetDefaultStyling(Symbolizer symbolizer)
        {
            DefaultSymbolizer = symbolizer;
            
            return this;
        }

        public Layer AddStyle(LayerStyle style)
        {
            Styles.Add(style);

            return this;
        }
    }
}