using System;
using System.Collections.Generic;
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
        internal string Name { get; private set; }
        internal Vector3? Position { get; private set; }
        internal Quaternion? Rotation { get; private set; }
        internal Color? Color { get; private set; }
        internal LayerData Parent { get; private set; }
        internal StoredAuthorization Credentials { get; private set; }
        internal List<LayerPropertyData> Properties { get; } = new();
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
            AddProperty(new LayerURLPropertyData
            {
                Data = url
            });

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

        public Layer AddStyle(LayerStyle style)
        {
            Styles.Add(style);
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
    }
}