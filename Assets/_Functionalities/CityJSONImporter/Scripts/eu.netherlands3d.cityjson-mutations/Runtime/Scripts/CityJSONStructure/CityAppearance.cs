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
        //static CityAppearance()
        //{
        //    var materialGenerator = new NL3DMaterialGenerator();
        //    defaultMaterial = materialGenerator.GetDefaultMaterial();
        //}

        // private static Material defaultMaterial;
        // private static Material[] defaultMaterials;
        public List<MaterialInfo> MaterialInfos { get; private set; } = new List<MaterialInfo>();
        public List<TextureInfo> TextureInfos { get; private set; } = new List<TextureInfo>();
        // Todo: Add vertices-texture support
        // Todo: Add default-theme-texture support
        // Todo: Add default-theme-material support
        
        private Material materialTemplate;
        public Material[] cachedMaterials;

        // private static Dictionary<int, Material> cachedMaterials = new Dictionary<int, Material>();

        public CityAppearance(Material materialTemplate)
        {
            this.materialTemplate = materialTemplate;
        }

        public static CityAppearance FromJSON(JSONNode node, Material materialTemplate)
        {
            // if (node == null || node.Count == 0) return null;

            var appearance = new CityAppearance(materialTemplate);

            var materialsNode = node["materials"];

            appearance.cachedMaterials = new Material[materialsNode.Count];
            for (var i = 0; i < materialsNode.Count; i++)
            {
                var matNode = materialsNode[i];
                var materialInfo = MaterialInfo.FromJSON(matNode);
                var material = MaterialInfo.ToUnityMaterial(materialInfo, materialTemplate);
                appearance.cachedMaterials[i] = material;
            }

            var texturesNode = node["textures"];
            if (texturesNode != null && texturesNode.IsArray)
            {
                foreach (var texNode in texturesNode.AsArray)
                {
                    appearance.TextureInfos.Add(TextureInfo.FromJSON(texNode));
                }
            }

            return appearance;
        }

        public Material[] GetMaterials(List<int> materialIndices)
        {
            if (materialIndices == null || materialIndices.Count == 0) //no materials defined, return the default
                return new[] { materialTemplate };
            
            var materials = new Material[materialIndices.Count];
            for (var i = 0; i < materialIndices.Count; i++)
            {
                var materialIndex = materialIndices[i];
                if (materialIndex == -1) //no material defined
                {
                    materials[i] = materialTemplate;
                    continue;
                }

                materials[i] = cachedMaterials[materialIndex];
            }

            return materials;
        }

        //     public Material[] GenerateMaterialsForGeometry(CityGeometry geometry)
        //     {
        //         // Material[] materials = null;
        //         // if (geometry.MaterialCount == 0)
        //         // {
        //         //     return defaultMaterials;
        //         // }
        //
        //         var materials = new Material[geometry.MaterialCount];
        //         for (int i = 0; i < geometry.MaterialUniqueIndices.Count; i++)
        //         {
        //             int matIndex = geometry.MaterialUniqueIndices[i];
        //             if (matIndex >= 0 && matIndex < Materials.Count)
        //             {
        //                 var matInfo = Materials[matIndex];
        //                 if (cachedMaterials.ContainsKey(matIndex))
        //                 {
        //                     materials[i] = cachedMaterials[matIndex];
        //                     continue;
        //                 }
        //
        //                 Material unityMat = MaterialInfo.ToUnityMaterial(matInfo, defaultMaterial);
        //                 cachedMaterials.Add(matIndex, unityMat);
        //                 materials[i] = unityMat;
        //             }
        //         }
        //         return materials;
        //     }
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
            mat.name = matInfo.Name;

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