using UnityEngine;

namespace Netherlands3D.LayerStyles.ExtensionMethods
{
    public static class RendererExtensionMethods
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// Change the color in an optimized way for materials using the URP Lit shader.
        /// 
        /// If you change the material.color directly, Unity will create a copy of the material and thus increase
        /// the number of drawcalls. By changing the color using a MaterialPropertyBlock, the color will change without
        /// increasing the number of draw calls.
        /// </summary>
        /// <param name="materialIndex">
        /// Which material index to alter the color for. By default this is `null`, meaning all materials in this
        /// renderer.
        /// </param>
        public static void SetUrpLitColorOptimized(
            this Renderer renderer, 
            Color color,
            int? materialIndex = null
        ) {
            // If a material index was provided: manipulate the start and end to do a single iteration in the loop with
            // this material index, otherwise we manipulate all
            int startIndex = materialIndex.HasValue ? materialIndex.Value : 0;
            int endIndex = materialIndex.HasValue ? materialIndex.Value : renderer.materials.Length - 1;
            
            // Make sure to assign the color to each material in the meshrenderer
            for (var index = startIndex; index <= endIndex; index++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, index);
                block.SetColor(BaseColor, color);
                renderer.SetPropertyBlock(block, index);
            }
        }
    }
}