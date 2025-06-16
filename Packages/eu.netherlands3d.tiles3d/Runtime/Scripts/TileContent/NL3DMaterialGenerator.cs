using System;
using GLTFast;
using GLTFast.Materials;
using GLTFast.Schema;
using GLTFast.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

public class NL3DMaterialGenerator : ShaderGraphMaterialGenerator
{
    static readonly int k_BaseMapPropId = Shader.PropertyToID("_MainTexture");
    static readonly int k_BaseMapScaleTransformPropId = Shader.PropertyToID("baseColorTexture_ST"); //TODO: support in shader!
    static readonly int k_BaseMapRotationPropId = Shader.PropertyToID("baseColorTexture_Rotation"); //TODO; support in shader!
    static readonly int k_BaseMapUVChannelPropId = Shader.PropertyToID("baseColorTexture_texCoord"); //TODO; support in shader!

    // const string k_OcclusionKeyword = "_OCCLUSION";
    // const string k_EmissiveKeyword = "_EMISSIVE";
    // const string k_ClearcoatKeyword = "_CLEARCOAT";

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
                material.SetVector(MaterialProperty.DiffuseFactor, specGloss.DiffuseColor.gamma);
#if UNITY_SHADER_GRAPH_12_OR_NEWER
                    material.SetVector(MaterialProperty.SpecularFactor, specGloss.SpecularColor);
#else
                material.SetVector(MaterialProperty.SpecularFactor, specGloss.SpecularColor);
#endif
                material.SetFloat(MaterialProperty.GlossinessFactor, specGloss.glossinessFactor);

                TrySetTexture(
                    specGloss.diffuseTexture,
                    material,
                    gltf,
                    MaterialProperty.DiffuseTexture,
                    MaterialProperty.DiffuseTextureScaleTransform,
                    MaterialProperty.DiffuseTextureRotation,
                    MaterialProperty.DiffuseTextureTexCoord
                );

                if (TrySetTexture(
                        specGloss.specularGlossinessTexture,
                        material,
                        gltf,
                        MaterialProperty.SpecularGlossinessTexture,
                        MaterialProperty.SpecularGlossinessTextureScaleTransform,
                        MaterialProperty.SpecularGlossinessTextureRotation,
                        MaterialProperty.SpecularGlossinessTextureTexCoord
                    ))
                {
                    // material.EnableKeyword();
                }
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
                    k_BaseMapPropId,
                    k_BaseMapScaleTransformPropId,
                    k_BaseMapRotationPropId,
                    k_BaseMapUVChannelPropId
                );
            }

            if (materialType == MaterialType.MetallicRoughness)
            {
                material.SetFloat(MaterialProperty.Metallic, gltfMaterial.PbrMetallicRoughness.metallicFactor);
                material.SetFloat(MaterialProperty.RoughnessFactor, gltfMaterial.PbrMetallicRoughness.roughnessFactor);

                if (TrySetTexture(
                        gltfMaterial.PbrMetallicRoughness.MetallicRoughnessTexture,
                        material,
                        gltf,
                        MaterialProperty.MetallicRoughnessMap,
                        MaterialProperty.MetallicRoughnessMapScaleTransform,
                        MaterialProperty.MetallicRoughnessMapRotation,
                        MaterialProperty.MetallicRoughnessMapTexCoord
                    ))
                {
                    // material.EnableKeyword(KW_METALLIC_ROUGHNESS_MAP);
                }

                // TODO: When the occlusionTexture equals the metallicRoughnessTexture, we could sample just once instead of twice.
                // if (!DifferentIndex(gltfMaterial.occlusionTexture,gltfMaterial.pbrMetallicRoughness.metallicRoughnessTexture)) {
                //    ...
                // }
            }
        }

        if (TrySetTexture(
                gltfMaterial.NormalTexture,
                material,
                gltf,
                MaterialProperty.NormalTexture,
                MaterialProperty.NormalTextureScaleTransform,
                MaterialProperty.NormalTextureRotation,
                MaterialProperty.NormalTextureTexCoord
            ))
        {
            // material.EnableKeyword(ShaderKeyword.normalMap);
            material.SetFloat(MaterialProperty.NormalTextureScale, gltfMaterial.NormalTexture.scale);
        }

        // if (TrySetTexture(
        //         gltfMaterial.OcclusionTexture,
        //         material,
        //         gltf,
        //         MaterialProperty.OcclusionTexture,
        //         MaterialProperty.OcclusionTextureScaleTransform,
        //         MaterialProperty.OcclusionTextureRotation,
        //         MaterialProperty.OcclusionTextureTexCoord
        //     ))
        // {
        //     material.EnableKeyword(k_OcclusionKeyword);
        //     material.SetFloat(MaterialProperty.OcclusionTextureStrength, gltfMaterial.OcclusionTexture.strength);
        // }

        // if (TrySetTexture(
        //         gltfMaterial.EmissiveTexture,
        //         material,
        //         gltf,
        //         MaterialProperty.EmissiveTexture,
        //         MaterialProperty.EmissiveTextureScaleTransform,
        //         MaterialProperty.EmissiveTextureRotation,
        //         MaterialProperty.EmissiveTextureTexCoord
        //     ))
        // {
        //     material.EnableKeyword(k_EmissiveKeyword);
        // }

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
            material.SetFloat(MaterialProperty.AlphaCutoff, 0);
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

        material.SetVector(MaterialProperty.BaseColor, baseColorLinear.gamma);

        // if (gltfMaterial.Emissive != Color.black)
        // {
        //     material.SetColor(MaterialProperty.EmissiveFactor, gltfMaterial.Emissive);
        //     material.EnableKeyword(k_EmissiveKeyword);
        // }

        // if (gltfMaterial.Extensions?.KHR_materials_clearcoat?.clearcoatFactor > 0)
        // {
        //     var clearcoat = gltfMaterial.Extensions.KHR_materials_clearcoat;
        //     material.SetFloat(ClearcoatProperty, clearcoat.clearcoatFactor);
        //     TrySetTexture(clearcoat.clearcoatTexture,
        //         material,
        //         gltf,
        //         ClearcoatTextureProperty,
        //         ClearcoatTextureScaleTransformProperty,
        //         ClearcoatTextureRotationProperty,
        //         ClearcoatTextureTexCoordProperty);
        //     material.SetFloat(ClearcoatRoughnessProperty, clearcoat.clearcoatRoughnessFactor);
        //     material.EnableKeyword(k_ClearcoatKeyword);
        //     TrySetTexture(clearcoat.clearcoatRoughnessTexture,
        //         material,
        //         gltf,
        //         ClearcoatRoughnessTextureProperty,
        //         ClearcoatRoughnessTextureScaleTransformProperty,
        //         ClearcoatRoughnessTextureRotationProperty,
        //         ClearcoatRoughnessTextureTexCoordProperty);
        //     TrySetTexture(clearcoat.clearcoatNormalTexture,
        //         material,
        //         gltf,
        //         ClearcoatNormalTextureProperty,
        //         ClearcoatNormalTextureScaleTransformProperty,
        //         ClearcoatNormalTextureRotationProperty,
        //         ClearcoatNormalTextureTexCoordProperty);
        //     material.SetFloat(ClearcoatNormalTextureScaleProperty, clearcoat.clearcoatNormalTexture.scale);
        // }

        return material;
    }

    Material GetUnlitMaterial(MaterialBase gltfMaterial)
    {
        Shader shader = NL3DShaders.UnlitShader;
        if(shader==null) {
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
        Shader shader = NL3DShaders.MainLayersShader;
        if (shader == null)
        {
            return null;
        }

        var mat = new Material(shader);
#if UNITY_EDITOR
        mat.doubleSidedGI = (metallicShaderFeatures & MetallicShaderFeatures.DoubleSided) != 0;
#endif
        return mat;
    }
    
    Material GetSpecularMaterial(SpecularShaderFeatures features) {
        var shader = NL3DShaders.MainLayersShader;
        if(shader==null) {
            return null;
        }
        var mat = new Material(shader);
#if UNITY_EDITOR
        mat.doubleSidedGI = (features & SpecularShaderFeatures.DoubleSided) != 0;
#endif
        return mat;
    }

    static SpecularShaderFeatures GetSpecularShaderFeatures(MaterialBase gltfMaterial) {

        var feature = SpecularShaderFeatures.Default;
        if (gltfMaterial.doubleSided) feature |= SpecularShaderFeatures.DoubleSided;

        if (gltfMaterial.GetAlphaMode() != MaterialBase.AlphaMode.Opaque) {
            feature |= SpecularShaderFeatures.AlphaBlend;
        }
        return feature;
    }
}