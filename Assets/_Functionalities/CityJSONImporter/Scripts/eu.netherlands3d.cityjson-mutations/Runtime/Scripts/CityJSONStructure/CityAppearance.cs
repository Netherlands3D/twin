using Netherlands3D.Tiles3D;
using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.CityJson.Structure
{
    /// <summary>
    /// CityJSON Appearance object according to v1.0.3 spec
    /// https://www.cityjson.org/specs/1.0.3/#appearance-object
    /// </summary>
    [System.Serializable]
    public class CityAppearance
    {
        static CityAppearance()
        {
            var materialGenerator = new NL3DMaterialGenerator();
            defaultMaterial = materialGenerator.GetDefaultMaterial();
        }

        private static Material defaultMaterial;

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

        public void GenerateMaterialsForGeometry(CityGeometry geometry)
        {
            for (int i = 0; i < geometry.MaterialIndices.Count; i++)
            {
                int matIndex = geometry.MaterialIndices[i];
                if (matIndex >= 0 && matIndex < Materials.Count)
                {
                    var matInfo = Materials[matIndex];
                    Material unityMat = MaterialInfo.ToUnityMaterial(matInfo, defaultMaterial);
                    // assign unityMat to the mesh face i
                }
            }
        }
    }
    

    [System.Serializable]
    public class MaterialInfo
    {
        public string Name { get; private set; }
        public float AmbientIntensity { get; private set; }
        public Color? DiffuseColor { get; private set; }
        public Color? SpecularColor { get; private set; }
        public float? Shininess { get; private set; }
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

        public static Material ToUnityMaterial(MaterialInfo matInfo, Material defaultMaterial)
        {
            // Use Unity's built-in Standard Shader or URP Lit, depending on your pipeline

            var mat = new Material(defaultMaterial);

            if (matInfo.DiffuseColor.HasValue)
            {
                mat.color = matInfo.DiffuseColor.Value;
            }

            if (matInfo.SpecularColor.HasValue)
            {
                mat.SetColor("_SpecColor", matInfo.SpecularColor.Value);
            }

            //if (matInfo.EmissiveColor.HasValue)
            //{
            //    mat.SetColor("_EmissionColor", matInfo.EmissiveColor.Value);
            //    mat.EnableKeyword("_EMISSION");
            //}

            if (matInfo.Shininess.HasValue)
            {
                mat.SetFloat("_Glossiness", matInfo.Shininess.Value);
            }

            return mat;
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