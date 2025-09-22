using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Structure;
using UnityEngine;

namespace Netherlands3D.CityJson.Visualisation
{
    [System.Serializable]
    public class CityMaterialConverter
    {
        [SerializeField] private SemanticMaterials[] semanticMaterials;
        private Material materialTemplate;
        public Material[] cachedMaterials;
        
        public void Initialize(CityAppearance appearance)
        {
            var nullMaterial = semanticMaterials.FirstOrDefault(sm => sm.Type == SurfaceSemanticType.Null);
            if (nullMaterial == null)
            {
                throw new ArgumentException("there is no material defined for the surface semantic \"null\". This is needed to use as a template material and as a fallback.");
            }

            materialTemplate = nullMaterial.Material;

            ConvertMaterialInfosToUnityMaterials(appearance.MaterialInfos);
        }

        private void ConvertMaterialInfosToUnityMaterials(List<CityMaterialInfo> cityMaterialInfos)
        {
            cachedMaterials = new Material[cityMaterialInfos.Count];
            for (var i = 0; i < cityMaterialInfos.Count; i++)
            {
                var cityMaterialInfo = cityMaterialInfos[i];
                var material = ToUnityMaterial(cityMaterialInfo, materialTemplate);
                cachedMaterials[i] = material;
            }
        }

        public Material[] GetMaterials(List<int> materialIndices, SurfaceSemanticType semanticTypeFallback)
        {
            var semanticMaterial = semanticMaterials.FirstOrDefault(sm => sm.Type == semanticTypeFallback);
            if (materialIndices == null || materialIndices.Count == 0) //no materials defined, return the default
            {
                Debug.Log("no material defined for object: returning semantic material: " + semanticMaterial.Material.name);
                return new[] { semanticMaterial.Material };
            }

            var materials = new Material[materialIndices.Count];
            for (var i = 0; i < materialIndices.Count; i++)
            {
                var materialIndex = materialIndices[i];
                if (materialIndex == -1) //no material defined
                {
                    Debug.Log("no material defined for surface: returning semantic material: " + semanticMaterial.Material.name);
                    materials[i] = semanticMaterial.Material;
                    continue;
                }

                materials[i] = cachedMaterials[materialIndex];
            }

            return materials;
        }

        public static Material ToUnityMaterial(CityMaterialInfo matInfo, Material materialTemplate)
        {
            var mat = new Material(materialTemplate);
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
    }
}