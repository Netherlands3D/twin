using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace Netherlands3D.CityJson.Structure
{
    /// <summary>
    /// CityJSON Appearance object according to v1.0.3 spec
    /// https://www.cityjson.org/specs/1.0.3/#appearance-object
    /// </summary>
    [System.Serializable]
    public class CityAppearance
    {
        public List<MaterialInfo> Materials { get; private set; } = new List<MaterialInfo>();
        public List<TextureInfo> Textures { get; private set; } = new List<TextureInfo>();

        public static CityAppearance FromJSON(JSONNode node)
        {
            if (node == null || node.Count == 0) return null;

            var appearance = new CityAppearance();

            var materialsNode = node["materials"];
            if (materialsNode != null && materialsNode.IsArray)
            {
                foreach (var matNode in materialsNode.AsArray)
                {
                    appearance.Materials.Add(MaterialInfo.FromJSON(matNode));
                }
            }

            var texturesNode = node["textures"];
            if (texturesNode != null && texturesNode.IsArray)
            {
                foreach (var texNode in texturesNode.AsArray)
                {
                    appearance.Textures.Add(TextureInfo.FromJSON(texNode));
                }
            }

            return appearance;
        }
    }

    [System.Serializable]
    public class MaterialInfo
    {
        public string Name { get; private set; }
        public float AmbientIntensity { get; private set; }
        public Color DiffuseColor { get; private set; }
        public Color SpecularColor { get; private set; }
        public float Shininess { get; private set; }
        public float Transparency { get; private set; }

        public static MaterialInfo FromJSON(JSONNode node)
        {
            if (node == null) return null;

            return new MaterialInfo
            {
                Name = node["name"],
                AmbientIntensity = node["ambientIntensity"].AsFloat,
                DiffuseColor = ParseColor(node["diffuseColor"]),
                SpecularColor = ParseColor(node["specularColor"]),
                Shininess = node["shininess"].AsFloat,
                Transparency = node["transparency"].AsFloat
            };
        }

        private static Color ParseColor(JSONNode arr)
        {
            if (arr == null || arr.Count < 3) return Color.white;
            return new Color(arr[0].AsFloat, arr[1].AsFloat, arr[2].AsFloat, 1f);
        }
    }

    [System.Serializable]
    public class TextureInfo
    {
        public string Type { get; private set; }
        public string Image { get; private set; }
        public string WrapMode { get; private set; }
        public string TextureType { get; private set; }

        public static TextureInfo FromJSON(JSONNode node)
        {
            if (node == null) return null;

            return new TextureInfo
            {
                Type = node["type"],
                Image = node["image"],
                WrapMode = node["wrapMode"],
                TextureType = node["textureType"]
            };
        }
    }
}