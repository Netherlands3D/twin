using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.DataSets
{
    internal enum DashboardMapLayer
    {
        Base,
        Segment,
        Dwellings,
        Phase
    }

    /// <summary>
    /// Applies dashboard colors through temporary NL3D styling rules without changing saved layer styles.
    /// </summary>
    internal sealed class DashboardMapStyling : IDisposable
    {
        private const string ColorAttribute = "data-dashboard-color";
        private static readonly int HighlightColorProperty = Shader.PropertyToID("_HighlightColor");
        private readonly DashboardObjectBinding binding;
        private IReadOnlyDictionary<string, Color> colors;
        private DashboardMapLayer selectedLayer;
        private string selectedBlock;
        private readonly Dictionary<CityJSONLayerGameObject, StylingPropertyData> styles = new();
        private readonly Dictionary<HierarchicalObjectLayerGameObject, (StylingPropertyData[] Properties, UnityAction Listener)> meshLayers = new();
        private readonly Dictionary<(MeshRenderer Renderer, int MaterialIndex), (HierarchicalObjectLayerGameObject Layer, MeshColorOverride Override)> meshOverrides = new();
        private bool meshColorsPending;

        public DashboardMapStyling(DashboardObjectBinding binding)
        {
            this.binding = binding;
        }

        public void Update(DashboardMapLayer layer, IReadOnlyDictionary<string, Color> palette)
        {
            selectedLayer = layer;
            colors = palette;
            Refresh();
            meshColorsPending = true;
        }

        internal void SelectBlock(string block)
        {
            selectedBlock = block;
            meshColorsPending = true;
        }

        public void ApplyPendingMeshColors()
        {
            if (!meshColorsPending)
                return;

            meshColorsPending = false;
            RefreshMeshColors();
        }

        public void Dispose()
        {
            foreach (var layer in meshLayers.Keys.ToArray())
                UnsubscribeMeshLayer(layer);
            foreach (var entry in meshOverrides.Values)
                entry.Override.Restore();
            meshOverrides.Clear();
            meshColorsPending = false;
            foreach (var visualization in styles.Keys.ToArray())
                Restore(visualization);
        }

        private void RefreshMeshColors()
        {
            var targets = colors != null && binding.HasData
                ? binding.ObjVisualizations.Concat(binding.GlbVisualizations).Where(layer => DashboardObjectBinding.IsDashboardLayerName(layer.Name)).ToArray()
                : Array.Empty<HierarchicalObjectLayerGameObject>();
            foreach (var layer in meshLayers.Keys.Except(targets).ToArray())
                UnsubscribeMeshLayer(layer);

            var activeSlots = new HashSet<(MeshRenderer Renderer, int MaterialIndex)>();
            var glbLayers = binding.GlbVisualizations.ToHashSet();
            foreach (var layer in targets)
            {
                if (!meshLayers.ContainsKey(layer))
                {
                    var properties = layer.LayerData.GetProperties<StylingPropertyData>().ToArray();
                    UnityAction listener = () => OnMeshStylingChanged(layer);
                    meshLayers.Add(layer, (properties, listener));
                    foreach (var property in properties)
                        property.OnStylingChanged.AddListener(listener);
                }

                var isGlb = glbLayers.Contains(layer);
                foreach (var renderer in layer.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer == null || renderer.GetComponentInParent<HierarchicalObjectLayerGameObject>(true) != layer)
                        continue;

                    var materials = renderer.sharedMaterials;
                    for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        var material = materials[materialIndex];
                        if (material == null || !material.HasProperty(NL3DShaders.BaseColorShaderProperty))
                            continue;

                        DashboardObjectData.BlockData block;
                        if (isGlb)
                            binding.GlbObjects.TryGetValue(renderer, out block);
                        else
                            binding.ObjObjects.TryGetValue(material, out block);

                        var slot = (renderer, materialIndex);
                        if (!meshOverrides.TryGetValue(slot, out var entry))
                        {
                            entry = (layer, new MeshColorOverride(renderer, materialIndex));
                            meshOverrides.Add(slot, entry);
                        }
                        activeSlots.Add(slot);
                        var color = colors[GetColorKey(block, selectedLayer)];
                        if (block != null && string.Equals(block.Block, selectedBlock, StringComparison.OrdinalIgnoreCase) && material.HasProperty(HighlightColorProperty))
                            color = material.GetColor(HighlightColorProperty);
                        entry.Override.Apply(color);
                    }
                }
            }

            foreach (var slot in meshOverrides.Keys.Except(activeSlots).ToArray())
            {
                meshOverrides[slot].Override.Restore();
                meshOverrides.Remove(slot);
            }
        }

        private void OnMeshStylingChanged(HierarchicalObjectLayerGameObject layer)
        {
            if (layer == null || !layer.HasLayerData || layer.LayerData.IsDisposed)
                return;

            // The layer's normal styling listener runs first. Retain its current user color for restoration.
            foreach (var (slot, entry) in meshOverrides)
            {
                if (entry.Layer != layer || slot.Renderer == null)
                    continue;

                var feature = layer.GetLayerFeatureByGeometry(slot.Renderer) ?? LayerFeature.Create(layer, slot.Renderer);
                entry.Override.SetOriginalColor(layer.GetStyling(feature).GetFillColor());
            }
            meshColorsPending = true;
        }

        private void UnsubscribeMeshLayer(HierarchicalObjectLayerGameObject layer)
        {
            var subscription = meshLayers[layer];
            foreach (var property in subscription.Properties)
                property.OnStylingChanged.RemoveListener(subscription.Listener);
            meshLayers.Remove(layer);
        }

        internal sealed class MeshColorOverride
        {
            private readonly MeshRenderer renderer;
            private readonly int materialIndex;
            private readonly MaterialPropertyBlock original = new();
            private readonly MaterialPropertyBlock working = new();

            public MeshColorOverride(MeshRenderer renderer, int materialIndex)
            {
                this.renderer = renderer;
                this.materialIndex = materialIndex;
                renderer.GetPropertyBlock(original, materialIndex);
            }

            public void Apply(Color color)
            {
                renderer.GetPropertyBlock(working, materialIndex);
                if (working.isEmpty)
                    renderer.GetPropertyBlock(working);
                working.SetColor(NL3DShaders.BaseColorShaderProperty, color);
                renderer.SetPropertyBlock(working, materialIndex);
            }

            public void SetOriginalColor(Color? color)
            {
                if (!color.HasValue || renderer == null)
                    return;

                if (original.isEmpty)
                    renderer.GetPropertyBlock(original);
                original.SetColor(NL3DShaders.BaseColorShaderProperty, color.Value);
            }

            public void Restore()
            {
                if (renderer == null || materialIndex >= renderer.sharedMaterials.Length)
                    return;

                renderer.SetPropertyBlock(original.isEmpty ? null : original, materialIndex);
            }
        }

        private void Refresh()
        {
            var targets = colors != null ? binding.Visualizations
                .Where(visualization => visualization.CityJson.CityObjects.Any(binding.IsDashboardObject)).ToArray()
                : Array.Empty<CityJSONLayerGameObject>();
            foreach (var visualization in styles.Keys.Except(targets).ToArray())
                Restore(visualization);

            foreach (var visualization in targets)
            {
                if (!styles.TryGetValue(visualization, out var style))
                {
                    style = new StylingPropertyData();
                    style.StylingRules.Clear();
                    styles.Add(visualization, style);
                    visualization.RuntimeStyles.Add(style);
                    visualization.OnFeatureAdded.AddListener(OnFeatureAdded);
                }

                style.StylingRules.Clear();
                foreach (var (key, color) in colors)
                {
                    var rule = new StylingRule(key, Expression.EqualTo(Expression.Get(ColorAttribute), key));
                    rule.Symbolizer.SetFillColor(color);
                    rule.Symbolizer.SetStrokeColor(color);
                    style.StylingRules.Add(key, rule);
                }
                foreach (var feature in visualization.LayerFeatures.Values)
                {
                    if (feature.Geometry is CityObjectVisualizer visualizer)
                        SetColorAttribute(visualizer, feature.Attributes);
                }
                visualization.ApplyStyling();
            }
        }

        private void OnFeatureAdded(CityObjectVisualizer visualizer)
        {
            foreach (var visualization in styles.Keys)
            {
                var feature = visualization.GetLayerFeatureByGeometry(visualizer);
                if (feature == null)
                    continue;

                SetColorAttribute(visualizer, feature.Attributes);
                visualization.ApplyStylingToFeature(feature);
                return;
            }
        }

        private void SetColorAttribute(CityObjectVisualizer visualizer, Dictionary<string, string> attributes)
        {
            if (!binding.IsDashboardObject(visualizer.CityObject))
            {
                attributes.Remove(ColorAttribute);
                return;
            }

            binding.Objects.TryGetValue(visualizer.CityObject, out var block);
            attributes[ColorAttribute] = GetColorKey(block, selectedLayer);
        }

        private void Restore(CityJSONLayerGameObject visualization)
        {
            var style = styles[visualization];
            styles.Remove(visualization);
            if (visualization == null)
                return;

            visualization.OnFeatureAdded.RemoveListener(OnFeatureAdded);
            visualization.RuntimeStyles.Remove(style);
            foreach (var feature in visualization.LayerFeatures.Values)
                feature.Attributes.Remove(ColorAttribute);
            visualization.ApplyStyling();
        }

        internal static string GetColorKey(DashboardObjectData.BlockData block, DashboardMapLayer layer)
        {
            if (block == null || layer == DashboardMapLayer.Base)
                return "base";
            if (layer == DashboardMapLayer.Phase)
                return block.LatestPhase != null ? DashboardColors.PhaseKey(block.LatestPhase) : "base";

            var housing = block.HousingSummary;
            if (!housing.Total.CompleteValue.HasValue)
                return "base";
            if (layer == DashboardMapLayer.Dwellings)
                return housing.Total.Value < 300 ? "low" : housing.Total.Value < 600 ? "medium" : "high";
            if (housing.Total.Value == 0)
                return "base";

            var dominant = Enumerable.Range(0, housing.Segments.Length).OrderByDescending(index => housing.Values[index].Value).First();
            return DashboardColors.SegmentKey(housing.Segments[dominant]);
        }
    }
}
