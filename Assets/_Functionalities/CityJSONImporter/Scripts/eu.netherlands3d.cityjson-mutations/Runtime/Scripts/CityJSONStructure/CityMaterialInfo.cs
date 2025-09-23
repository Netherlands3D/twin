using SimpleJSON;
using UnityEngine;

namespace Netherlands3D.CityJson.Structure
{
    [System.Serializable]
    public class CityMaterialInfo
    {
        public string Name { get; private set; }
        public float? AmbientIntensity { get; private set; }
        public Color? DiffuseColor { get; private set; }
        public Color? EmissiveColor { get; private set; }
        public Color? SpecularColor { get; private set; }
        public float? Shininess { get; private set; }
        public float? Transparency { get; private set; }
        public bool? IsSmooth { get; private set; }

        public static CityMaterialInfo FromJSON(JSONNode node)
        {
            if (node == null) return null;

            var ambientNode = node["ambientIntensity"];
            var shininessNode = node["shininess"];
            var transparencyNode = node["transparency"];
            var isSmoothNode = node["isSmooth"];

            return new CityMaterialInfo
            {
                Name = node["name"],
                AmbientIntensity = ambientNode != null ? ambientNode.AsFloat : null,
                DiffuseColor = ParseColor(node["diffuseColor"]),
                EmissiveColor = ParseColor(node["emissiveColor"]),
                SpecularColor = ParseColor(node["specularColor"]),
                Shininess = shininessNode != null ? shininessNode.AsFloat : null,
                Transparency = transparencyNode != null ? transparencyNode.AsFloat : null,
                IsSmooth = isSmoothNode != null ? isSmoothNode.AsBool : null
            };
        }

        private static Color? ParseColor(JSONNode arr)
        {
            if (arr == null || arr.Count < 3) return null;
            return new Color(arr[0].AsFloat, arr[1].AsFloat, arr[2].AsFloat, 1f);
        }
    }
}