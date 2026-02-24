using System;
using GLTFast;
using GLTFast.Materials;
using GLTFast.Schema;
using GLTFast.Logging;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
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
                    var success = TrySetTexture(
                        specGloss.diffuseTexture,
                        material,
                        gltf,
                        NL3DShaders.MainTextureShaderProperty,
                        NL3DShaders.MainTextureScaleTransformPropertyId,
                        NL3DShaders.MainTextureRotationPropertyId,
                        NL3DShaders.MainTextureUVChannelPropertyId
                        
                    );
                    if(!success)
                        Debug.LogError("Could not set texture");
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
                    var success = TrySetTexture(
                        gltfMaterial.PbrMetallicRoughness.BaseColorTexture,
                        material,
                        gltf,
                        NL3DShaders.MainTextureShaderProperty,
                        NL3DShaders.MainTextureScaleTransformPropertyId,
                        NL3DShaders.MainTextureRotationPropertyId,
                        NL3DShaders.MainTextureUVChannelPropertyId
                    );
                    if(!success)
                        Debug.LogError("Could not set texture");
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
        
        private bool TrySetTexture2(
            TextureInfoBase textureInfo,
            UnityEngine.Material material,
            IGltfReadable gltf,
            int texturePropertyId,
            int scaleTransformPropertyId = -1,
            int rotationPropertyId = -1,
            int uvChannelPropertyId = -1
            )
        {
            Debug.Log("starting trySetTexture");
            Debug.Log("textureInfo: " + textureInfo);
            Debug.Log("textureInfo.Index: " + textureInfo?.index);

            if (textureInfo != null && textureInfo.index >= 0)
            {
                int textureIndex = textureInfo.index;
                var srcTexture = gltf.GetSourceTexture(textureIndex);
                Debug.Log("srcTexture: " + srcTexture);
                
                if (srcTexture != null)
                {
                    var texture = gltf.GetTexture(textureIndex);
                    Debug.Log("texture: " + texture);

                    if (texture != null)
                    {
                        Debug.Log("setting texture at  id: " + texturePropertyId);
                        material.SetTexture(texturePropertyId, texture);
                        // TODO: Implement texture transform and UV channel selection for all texture types and remove
                        // this condition
                        if (scaleTransformPropertyId >= 0 && rotationPropertyId >= 0 && uvChannelPropertyId >= 0)
                        {
                            var flipY = gltf.IsTextureYFlipped(textureIndex);
                            Debug.Log("setting texture transform. FlipY: "+ flipY);
                            TrySetTextureTransform(
                                textureInfo,
                                material,
                                texturePropertyId,
                                scaleTransformPropertyId,
                                rotationPropertyId,
                                uvChannelPropertyId,
                                flipY
                                );
                        }
                        Debug.Log("succesfully set texture");
                        return true;
                    }
#if UNITY_IMAGECONVERSION
                    Logger?.Error(LogCode.TextureLoadFailed,textureIndex.ToString());
#endif
                    Debug.Log("Texture is null");
                }
                else
                {
                    Debug.Log("srcTexture is null");
                    Logger?.Error(LogCode.TextureNotFound, textureIndex.ToString());
                }
            }
            Debug.LogError("could not set texture");
            return false;
        }
        
        
        void TrySetTextureTransform(
            TextureInfoBase textureInfo,
            UnityEngine.Material material,
            int texturePropertyId,
            int scaleTransformPropertyId = -1,
            int rotationPropertyId = -1,
            int uvChannelPropertyId = -1,
            bool flipY = false
            )
        {
            var hasTransform = false;
            // Scale (x,y) and Transform (z,w)
            var textureScaleTranslation = new float4(
                1, 1,// scale
                0, 0 // translation
                );

            var texCoord = textureInfo.texCoord;

            if (textureInfo.Extensions?.KHR_texture_transform != null)
            {
                hasTransform = true;
                var tt = textureInfo.Extensions.KHR_texture_transform;
                if (tt.texCoord >= 0)
                {
                    texCoord = tt.texCoord;
                }

                if (tt.offset != null)
                {
                    textureScaleTranslation.z = tt.offset[0];
                    textureScaleTranslation.w = 1 - tt.offset[1];
                }
                if (tt.scale != null)
                {
                    textureScaleTranslation.x = tt.scale[0];
                    textureScaleTranslation.y = tt.scale[1];
                }
                if (math.abs(tt.rotation) >= float.Epsilon)
                {
                    var cos = math.cos(tt.rotation);
                    var sin = math.sin(tt.rotation);

                    var newRot = new Vector2(textureScaleTranslation.x * sin, textureScaleTranslation.y * -sin);

                    Assert.IsTrue(rotationPropertyId >= 0, "Texture rotation property invalid!");
                    material.SetVector(rotationPropertyId, newRot);

                    textureScaleTranslation.x *= cos;
                    textureScaleTranslation.y *= cos;

                    textureScaleTranslation.z -= newRot.y; // move offset to move rotation point (horizontally)
                }
                else
                {
                    // Make sure the rotation is properly zeroed
                    material.SetVector(rotationPropertyId, Vector4.zero);
                }

                textureScaleTranslation.w -= textureScaleTranslation.y; // move offset to move flip axis point (vertically)
            }

            if (texCoord != 0)
            {
                if (uvChannelPropertyId >= 0 && texCoord < 2f)
                {
                    material.EnableKeyword(UVChannelSelectKeyword);
                    material.SetFloat(uvChannelPropertyId, texCoord);
                }
                else
                {
                    Logger?.Error(LogCode.UVMulti, texCoord.ToString());
                }
            }

            if (flipY)
            {
                hasTransform = true;
                textureScaleTranslation.w = 1 - textureScaleTranslation.w; // flip offset in Y
                textureScaleTranslation.y = -textureScaleTranslation.y; // flip scale in Y
            }

            if (hasTransform)
            {
                material.EnableKeyword(TextureTransformKeyword);
            }

            material.SetTextureOffset(texturePropertyId, textureScaleTranslation.zw);
            material.SetTextureScale(texturePropertyId, textureScaleTranslation.xy);
            Assert.IsTrue(scaleTransformPropertyId >= 0, "Texture scale/transform property invalid!");
            material.SetVector(scaleTransformPropertyId, textureScaleTranslation);
        }
    }
}