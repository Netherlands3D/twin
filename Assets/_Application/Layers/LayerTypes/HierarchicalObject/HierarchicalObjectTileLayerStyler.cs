using System;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Rendering;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    /// <summary>
    /// Helper class that helps with styling HierarchicalObject layers.
    ///
    /// Hierarchical Object layers can be styled by changing the color for a material - or set of layer features. This
    /// class can provide helpers to ensure a consistent set of styling rules is made, and to manage them.  
    /// </summary>
    public static class HierarchicalObjectTileLayerStyler
    {
        /// <summary>
        /// Sets a custom color for all layer features matching the material index of the given layer feature.
        /// </summary>
        public static void SetColor(HierarchicalObjectLayerGameObject layer, Color? color)
        {
            var symbolizer = layer.LayerData.DefaultStyle.AnyFeature.Symbolizer;
            if (color.HasValue)
            {
                symbolizer.SetFillColor(color.Value);
            }
            else
            {
                symbolizer.ClearFillColor();
            }
        }

        /// <summary>
        /// Retrieves the color for the current object.
        /// </summary>
        public static Color? GetColor(HierarchicalObjectLayerGameObject layer)
        {
            return layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.GetFillColor();
        }

        public static void ResetColoring(HierarchicalObjectLayerGameObject layer)
        {
            SetColor(layer, null);
            layer.ApplyStyling();
        }

        /// <summary>
        /// The other methods deal with manipulating the styles for a layerfeature, this method takes the outcome of
        /// those actions and applies them to the materials for the binary mesh layer.
        /// </summary>
        public static void Apply(HierarchicalObjectLayerGameObject layer, Symbolizer styling, LayerFeature layerFeature)
        {
            switch (layerFeature.Geometry)
            {
                case MeshRenderer meshRenderer:
                    ApplyToMeshRenderer(layer, styling, meshRenderer);
                    break;
                case LineRenderer3D lineRenderer:
                    ApplyToLineRenderer(layer, styling, lineRenderer);
                    break;
                case PointRenderer3D pointRenderer:
                    ApplyToPointRenderer(layer, styling, pointRenderer);
                    break;
                case BatchedMeshInstanceRenderer batchedRenderer:
                    ApplyToBatchedRenderer(layer, styling, batchedRenderer);
                    break;
            }
        }

        private static void ApplyToMeshRenderer(HierarchicalObjectLayerGameObject layer, Symbolizer styling, MeshRenderer meshRenderer)
        {
            var fillColor = styling.GetFillColor();

            // Keep the original material color if fill color is not set (null)
            if (!fillColor.HasValue) return;

            layer.LayerData.Color = fillColor.Value;
            SetUrpLitColorOptimized(meshRenderer, fillColor.Value);
        }

        private static void ApplyToPointRenderer(HierarchicalObjectLayerGameObject layer, Symbolizer styling, PointRenderer3D renderer)
        {
            var fillColor = styling.GetFillColor();
            if (!fillColor.HasValue) return;

            layer.LayerData.Color = fillColor.Value;

            var material = EnsureMaterialInstance(renderer.PointMaterial, m => renderer.PointMaterial = m);
            if (!material) return;

            material.color = fillColor.Value;
            renderer.SetDefaultColors();
        }

        private static void ApplyToLineRenderer(HierarchicalObjectLayerGameObject layer, Symbolizer styling, LineRenderer3D renderer)
        {
            var strokeColor = styling.GetStrokeColor() ?? styling.GetFillColor();
            if (!strokeColor.HasValue) return;

            layer.LayerData.Color = strokeColor.Value;

            var lineMaterial = EnsureMaterialInstance(renderer.LineMaterial, m => renderer.LineMaterial = m);
            if (lineMaterial)
            {
                lineMaterial.color = strokeColor.Value;
            }

            var pointMaterial = EnsureMaterialInstance(renderer.PointMaterial, m => renderer.PointMaterial = m);
            if (pointMaterial)
            {
                pointMaterial.color = strokeColor.Value;
            }

            renderer.SetDefaultColors();
        }

        private static void ApplyToBatchedRenderer(HierarchicalObjectLayerGameObject layer, Symbolizer styling, BatchedMeshInstanceRenderer renderer)
        {
            var fillColor = styling.GetFillColor();
            if (!fillColor.HasValue) return;

            layer.LayerData.Color = fillColor.Value;

            var material = EnsureMaterialInstance(renderer.PointMaterial, m => renderer.PointMaterial = m);
            if (!material) return;

            material.color = fillColor.Value;
            renderer.SetDefaultColors();
        }

        private static Material EnsureMaterialInstance(Material material, Action<Material> setter)
        {
            if (!material) return null;

            if (material.name.EndsWith("(Instance)", StringComparison.Ordinal)) return material;

            var source = material;
            var instance = new Material(source)
            {
                name = $"{source.name} (Instance)"
            };
            setter(instance);

            return instance;
        }

        private static void SetUrpLitColorOptimized(MeshRenderer renderer, Color color, int? materialIndex = null)
        {
            int BaseColor = Shader.PropertyToID("_BaseColor");

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
