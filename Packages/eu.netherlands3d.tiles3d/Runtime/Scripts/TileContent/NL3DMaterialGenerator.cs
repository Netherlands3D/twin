using System;
using GLTFast;
using GLTFast.Materials;
using GLTFast.Schema;
using GLTFast.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

namespace Netherlands3D.Tiles3D
{
    public class NL3DMaterialGenerator : ShaderGraphMaterialGenerator
    {
        protected override Material GenerateDefaultMaterial(bool pointsSupport = false)
        {
            if (pointsSupport)
            {
                Logger?.Warning(LogCode.TopologyPointsMaterialUnsupported);
            }

            var defaultMaterial = GetMetallicMaterial(MetallicShaderFeatures.Default);
            if (defaultMaterial != null)
            {
                defaultMaterial.name = DefaultMaterialName;
            }

            return defaultMaterial;
        }

        public override Material GenerateMaterial(MaterialBase gltfMaterial, IGltfReadable gltf, bool pointsSupport = false)
        {
            if (pointsSupport)
            {
                Logger?.Warning(LogCode.TopologyPointsMaterialUnsupported);
            }

            Material material;

            MaterialType? materialType;
            ShaderMode shaderMode = ShaderMode.Opaque;

            bool isUnlit = gltfMaterial.Extensions?.KHR_materials_unlit != null;
            bool isSpecularGlossiness = gltfMaterial.Extensions?.KHR_materials_pbrSpecularGlossiness != null;

            if (isUnlit)
            {
                material = GetUnlitMaterial(gltfMaterial);
                materialType = MaterialType.Unlit;
                shaderMode = gltfMaterial.GetAlphaMode() == MaterialBase.AlphaMode.Blend ? ShaderMode.Blend : ShaderMode.Opaque;
            }
            else if (isSpecularGlossiness)
            {
                materialType = MaterialType.SpecularGlossiness;
                var specularShaderFeatures = GetSpecularShaderFeatures(gltfMaterial);
                material = GetSpecularMaterial(specularShaderFeatures);
                if ((specularShaderFeatures & SpecularShaderFeatures.AlphaBlend) != 0)
                {
                    shaderMode = ShaderMode.Blend;
                }
            }
            else
            {
                materialType = MaterialType.MetallicRoughness;
                var metallicShaderFeatures = GetMetallicShaderFeatures(gltfMaterial);
                material = GetMetallicMaterial(metallicShaderFeatures);
                shaderMode = (ShaderMode)(metallicShaderFeatures & MetallicShaderFeatures.ModeMask);
            }

            if (material == null) return null;

            material.name = gltfMaterial.name;

            Color baseColorLinear = Color.white;
            RenderQueue? renderQueue = null;

            //added support for KHR_materials_pbrSpecularGlossiness
            if (gltfMaterial.Extensions != null)
            {
                PbrSpecularGlossiness specGloss = gltfMaterial.Extensions.KHR_materials_pbrSpecularGlossiness;
                if (specGloss != null)
                {
                    baseColorLinear = specGloss.DiffuseColor;
                    TrySetTexture(
                        specGloss.diffuseTexture,
                        material,
                        gltf,
                        NL3DShaders.MainTextureShaderProperty
                    );
                }
            }

            if (gltfMaterial.PbrMetallicRoughness != null
                // If there's a specular-glossiness extension, ignore metallic-roughness
                // (according to extension specification)
                && gltfMaterial.Extensions?.KHR_materials_pbrSpecularGlossiness == null)
            {
                baseColorLinear = gltfMaterial.PbrMetallicRoughness.BaseColor;

                if (materialType != MaterialType.SpecularGlossiness)
                {
                    // baseColorTexture can be used by both MetallicRoughness AND Unlit materials
                    TrySetTexture(
                        gltfMaterial.PbrMetallicRoughness.BaseColorTexture,
                        material,
                        gltf,
                        NL3DShaders.MainTextureShaderProperty
                    );
                }
            }

            if (materialType == MaterialType.MetallicRoughness)
            {
                material.SetFloat(NL3DShaders.MetallicShaderProperty, gltfMaterial.PbrMetallicRoughness.metallicFactor);
                material.SetFloat(NL3DShaders.SmoothnessShaderProperty, 1 - gltfMaterial.PbrMetallicRoughness.roughnessFactor); //roughness is inverted smoothness
            }

            if (gltfMaterial.Extensions != null)
            {
                // Transmission - Approximation
                var transmission = gltfMaterial.Extensions.KHR_materials_transmission;
                if (transmission != null)
                {
                    renderQueue = ApplyTransmission(ref baseColorLinear, gltf, transmission, material, null);
                }
            }

            if (gltfMaterial.GetAlphaMode() == MaterialBase.AlphaMode.Mask)
            {
                SetAlphaModeMask(gltfMaterial, material);
#if USING_HDRP
                if (gltfMaterial.Extensions?.KHR_materials_unlit != null) {
                    renderQueue = RenderQueue.Transparent;
                } else
#endif
                renderQueue = RenderQueue.AlphaTest;
            }
            else
            {
                // double sided opaque would make errors in HDRP 7.3 otherwise
                material.SetOverrideTag(MotionVectorTag, MotionVectorUser);
                material.SetShaderPassEnabled(MotionVectorsPass, false);
            }

            if (!renderQueue.HasValue)
            {
                if (shaderMode == ShaderMode.Opaque)
                {
                    renderQueue = gltfMaterial.GetAlphaMode() == MaterialBase.AlphaMode.Mask
                        ? RenderQueue.AlphaTest
                        : RenderQueue.Geometry;
                }
                else
                {
                    renderQueue = RenderQueue.Transparent;
                }
            }

            material.renderQueue = (int)renderQueue.Value;

            if (gltfMaterial.doubleSided)
            {
                SetDoubleSided(gltfMaterial, material);
            }

            switch (shaderMode)
            {
                case ShaderMode.Opaque:
                    SetShaderModeOpaque(gltfMaterial, material);
                    break;
                case ShaderMode.Blend:
                    SetShaderModeBlend(gltfMaterial, material);
                    break;
                case ShaderMode.Premultiply:
                    SetShaderModePremultiply(gltfMaterial, material);
                    break;
            }

            material.SetVector(NL3DShaders.BaseColorShaderProperty, baseColorLinear.gamma);

            return material;
        }

        Material GetUnlitMaterial(MaterialBase gltfMaterial)
        {
            Shader shader = NL3DShaders.UnlitShader;
            if (shader == null)
            {
                Debug.LogError("Unlit shader is missing, register in the class NL3DShaders, or use RegisterShaders.cs to do this for you");
                return null;
            }

            var mat = new Material(shader);
#if UNITY_EDITOR
            mat.doubleSidedGI = gltfMaterial.doubleSided;
#endif
            return mat;
        }

        Material GetMetallicMaterial(MetallicShaderFeatures metallicShaderFeatures)
        {
            Shader shader = NL3DShaders.MetallicShader;
            if (shader == null)
            {
                Debug.LogError("Main layers shader is missing, register in the class NL3DShaders, or use RegisterShaders.cs to do this for you");
                return null;
            }

            var mat = new Material(shader);
#if UNITY_EDITOR
            mat.doubleSidedGI = (metallicShaderFeatures & MetallicShaderFeatures.DoubleSided) != 0;
#endif
            return mat;
        }

        Material GetSpecularMaterial(SpecularShaderFeatures features)
        {
            Shader shader = NL3DShaders.SpecularShader;
            if (shader == null)
            {
                Debug.LogError("Main layers shader is missing, register in the class NL3DShaders, or use RegisterShaders.cs to do this for you");
                return null;
            }

            var mat = new Material(shader);
#if UNITY_EDITOR
            mat.doubleSidedGI = (features & SpecularShaderFeatures.DoubleSided) != 0;
#endif
            return mat;
        }

        static SpecularShaderFeatures GetSpecularShaderFeatures(MaterialBase gltfMaterial)
        {
            var feature = SpecularShaderFeatures.Default;
            if (gltfMaterial.doubleSided) feature |= SpecularShaderFeatures.DoubleSided;

            if (gltfMaterial.GetAlphaMode() != MaterialBase.AlphaMode.Opaque)
            {
                feature |= SpecularShaderFeatures.AlphaBlend;
            }

            return feature;
        }
    }
}