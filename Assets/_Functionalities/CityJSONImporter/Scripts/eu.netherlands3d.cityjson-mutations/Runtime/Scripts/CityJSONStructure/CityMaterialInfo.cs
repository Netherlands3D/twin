using SimpleJSON;
using UnityEngine;

namespace Netherlands3D.CityJson.Structure
{
    [System.Serializable]
    public class CityMaterialInfo
    {
        public string Name { get; private set; }
        public float AmbientIntensity { get; private set; }
        public Color? DiffuseColor { get; private set; }
        public Color? SpecularColor { get; private set; }
        public float? Shininess { get; private set; }
        public float Transparency { get; private set; }

        public static CityMaterialInfo FromJSON(JSONNode node)
        {
            if (node == null) return null;

            return new CityMaterialInfo
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
}
