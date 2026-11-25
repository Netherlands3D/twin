using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.Functionalities.GLBImporter;
using Netherlands3D.Functionalities.OBJImporter;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using LayerService = Netherlands3D.Twin.Services.Layers;

namespace Netherlands3D.Twin.DataSets
{
    /// <summary>
    /// Maintains session-only model bindings while dashboard functionality is available.
    /// </summary>
    internal sealed class DashboardObjectBinding : IDisposable
    {
        private readonly LayerService layers;
        private readonly ProjectDataHandler projectLoader;
        private readonly Functionality functionality;
        private readonly HashSet<HierarchicalObjectLayerGameObject> visualizations = new();
        private readonly HashSet<LayerData> namedLayers = new();
        private readonly HashSet<LayerData> currentProjectLayers = new();
        private readonly Dictionary<CityObject, DashboardObjectData.BlockData> objects = new();
        private readonly Dictionary<Material, DashboardObjectData.BlockData> objObjects = new();
        private readonly Dictionary<MeshRenderer, DashboardObjectData.BlockData> glbObjects = new();
        private readonly Dictionary<MeshRenderer, MeshCollider> objPickingColliders = new();
        private readonly Dictionary<HierarchicalObjectLayerGameObject, GameObject> objRoots = new();
        private DashboardCsvPropertyData source;
        private DashboardData data;
        private DashboardObjectData index = new(null);
        private int reportedInvalidBlockCount;
#if UNITY_EDITOR
        private readonly Dictionary<HierarchicalObjectLayerGameObject, string> bindingDiagnostics = new();
        private readonly List<(string Name, string Identifier, bool Matched)> diagnosticItems = new();
#endif

        public IReadOnlyDictionary<CityObject, DashboardObjectData.BlockData> Objects => objects;
        public IReadOnlyDictionary<Material, DashboardObjectData.BlockData> ObjObjects => objObjects;
        public IReadOnlyDictionary<MeshRenderer, DashboardObjectData.BlockData> GlbObjects => glbObjects;
        public bool HasData => functionality.IsEnabled && data != null;
        internal DashboardData Data => HasData ? data : null;
        public IReadOnlyList<string> BlockIdentifiers => index.BlockIdentifiers;
        public IReadOnlyList<string> HousingSegments => index.HousingSegments;
        public IReadOnlyList<string> AvailablePhases => data?.AvailablePhases ?? Array.Empty<string>();
        public IEnumerable<CityJSONLayerGameObject> Visualizations => visualizations.OfType<CityJSONLayerGameObject>().Where(IsCurrent);
        public IEnumerable<HierarchicalObjectLayerGameObject> ObjVisualizations => visualizations.Where(visualization => IsCurrent(visualization) && visualization.GetComponent<OBJSpawner>() != null);
        public IEnumerable<HierarchicalObjectLayerGameObject> GlbVisualizations => visualizations.Where(visualization => IsCurrent(visualization) && visualization.GetComponent<GLBSpawner>() != null);
        public event Action Changed;

        internal DashboardObjectData.BlockData GetMeshBlock(MeshRenderer renderer, int materialIndex)
        {
            if (!HasData || renderer == null)
                return null;

            var visualization = renderer.GetComponentInParent<HierarchicalObjectLayerGameObject>();
            if (!IsCurrent(visualization) || !visualization.LayerData.ActiveInHierarchy || !IsDashboardLayerName(visualization.Name))
                return null;

            if (glbObjects.TryGetValue(renderer, out var block))
                return block;

            if (visualization.GetComponent<OBJSpawner>() == null)
                return null;

            var materials = renderer.sharedMaterials;
            return materialIndex >= 0 && materialIndex < materials.Length && materials[materialIndex] != null
                && objObjects.TryGetValue(materials[materialIndex], out block) ? block : null;
        }

        internal IEnumerable<(MeshRenderer Renderer, int MaterialIndex)> GetMeshSlots(string block)
        {
            foreach (var visualization in ObjVisualizations.Concat(GlbVisualizations).Where(visualization => visualization.LayerData.ActiveInHierarchy))
            {
                foreach (var renderer in visualization.GetComponentsInChildren<MeshRenderer>())
                {
                    if (renderer.GetComponentInParent<HierarchicalObjectLayerGameObject>() != visualization)
                        continue;

                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        if (GetMeshBlock(renderer, i) is { } data && string.Equals(data.Block, block, StringComparison.OrdinalIgnoreCase))
                            yield return (renderer, i);
                    }
                }
            }
        }

        internal void SetObjPickingEnabled(bool enabled)
        {
            var renderers = enabled && HasData ? ObjVisualizations.Where(visualization => IsDashboardLayerName(visualization.Name))
                .SelectMany(visualization => visualization.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(renderer => renderer.GetComponentInParent<HierarchicalObjectLayerGameObject>(true) == visualization)).ToHashSet() : new HashSet<MeshRenderer>();
            foreach (var renderer in objPickingColliders.Keys.Except(renderers).ToArray())
            {
                var collider = objPickingColliders[renderer];
                if (collider != null)
                {
                    collider.enabled = false;
                    UnityEngine.Object.Destroy(collider);
                }
                objPickingColliders.Remove(renderer);
            }
            foreach (var renderer in renderers)
            {
                if (!renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null)
                    continue;
                if (objPickingColliders.TryGetValue(renderer, out var owned) && owned != null)
                {
                    owned.sharedMesh = filter.sharedMesh;
                    continue;
                }
                if (renderer.GetComponents<MeshCollider>().Any(collider => collider.enabled && !collider.convex && !collider.isTrigger && collider.sharedMesh == filter.sharedMesh))
                    continue;

                // The OBJ importer only adds a collider to the root; split meshes need their own while dashboarding.
                var picking = renderer.gameObject.AddComponent<MeshCollider>();
                picking.hideFlags = HideFlags.DontSave;
                picking.sharedMesh = filter.sharedMesh;
                objPickingColliders[renderer] = picking;
            }
        }

        internal IEnumerable<CityJSONMapping> GetCityJsonMappings(string block)
        {
            foreach (var visualization in Visualizations.Where(visualization => visualization.LayerData.ActiveInHierarchy))
            {
                var bounds = visualization.Bounds;
                if (bounds == null)
                    continue;

                foreach (var mapping in SelectionService.MappingTree.Query<CityJSONMapping>(bounds).OfType<CityJSONMapping>())
                {
                    if (mapping.VisualisationParent == visualization && mapping.Feature != null && objects.TryGetValue(mapping.Feature, out var data)
                        && string.Equals(data.Block, block, StringComparison.OrdinalIgnoreCase))
                        yield return mapping;
                }
            }
        }

        public static bool IsDashboardLayerName(string name) => name?.TrimStart().StartsWith("dashboard", StringComparison.OrdinalIgnoreCase) == true;

        public bool IsDashboardObject(CityObject cityObject)
        {
            if (!HasData || cityObject == null)
                return false;

            var visualization = cityObject.GetComponentInParent<CityJSONLayerGameObject>(true);
            return visualization != null && visualization.HasLayerData && IsDashboardLayerName(visualization.Name);
        }

        internal static string ReadBlockIdentifier(CityObject cityObject)
        {
            if (!cityObject.Attributes.TryGetValue("Blok", out var attribute) || attribute.Value == null)
                return null;

            var value = attribute.Value;
            if (!value.IsString && !value.IsNumber)
                return null;
            if (value.IsNumber && (double.IsNaN(value.AsDouble) || double.IsInfinity(value.AsDouble)))
                return null;

            return string.IsNullOrWhiteSpace(value.Value) ? null : value.Value.Trim();
        }

        internal static string ReadObjBlockIdentifier(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                return null;

            var identifier = materialName.Trim();
            const string instanceSuffix = " (Instance)";
            if (identifier.EndsWith(instanceSuffix, StringComparison.Ordinal))
                identifier = identifier.Substring(0, identifier.Length - instanceSuffix.Length).TrimEnd();

            return string.IsNullOrWhiteSpace(identifier) ? null : identifier;
        }

        internal static DashboardObjectData.BlockData FindGlbBlock(DashboardObjectData index, Transform geometry, Transform modelRoot)
        {
            if (geometry == null || modelRoot == null || !geometry.IsChildOf(modelRoot))
                return null;

            // Exporters may split one named group into several mesh children. Prefer the nearest matching node.
            for (var node = geometry; node != null && node != modelRoot; node = node.parent)
            {
                var block = index.Find(node.name);
                if (block != null)
                    return block;
            }
            return null;
        }

        public DashboardObjectBinding(LayerService layers, ProjectDataHandler projectLoader, Functionality functionality)
        {
            this.layers = layers;
            this.projectLoader = projectLoader;
            this.functionality = functionality;
            layers.LayerAdded.AddListener(OnLayerAdded);
            layers.LayerRemoved.AddListener(OnLayerRemoved);
            layers.VisualizationCreated.AddListener(OnVisualizationCreated);
            if (projectLoader != null)
                projectLoader.OnLoadCompleted.AddListener(OnProjectLoaded);
            functionality.OnEnableFunctionality.AddListener(Refresh);
            functionality.OnDisableFunctionality.AddListener(Refresh);
            Refresh(true);
        }

        public void Dispose()
        {
            SetObjPickingEnabled(false);
            layers.LayerAdded.RemoveListener(OnLayerAdded);
            layers.LayerRemoved.RemoveListener(OnLayerRemoved);
            layers.VisualizationCreated.RemoveListener(OnVisualizationCreated);
            if (projectLoader != null)
                projectLoader.OnLoadCompleted.RemoveListener(OnProjectLoaded);
            functionality.OnEnableFunctionality.RemoveListener(Refresh);
            functionality.OnDisableFunctionality.RemoveListener(Refresh);
            source?.DataChanged.RemoveListener(Refresh);
            foreach (var layer in namedLayers)
            {
                layer.NameChanged.RemoveListener(OnLayerNameChanged);
                layer.LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveChanged);
            }
            namedLayers.Clear();
            currentProjectLayers.Clear();
            foreach (var visualization in visualizations)
                Unsubscribe(visualization);
            visualizations.Clear();
            objects.Clear();
            objObjects.Clear();
            glbObjects.Clear();
            objRoots.Clear();
#if UNITY_EDITOR
            bindingDiagnostics.Clear();
            diagnosticItems.Clear();
#endif
        }

        private void OnLayerAdded(LayerData layer)
        {
            if (layer.HasProperty<DashboardCsvPropertyData>())
                Refresh();
        }

        private void OnLayerRemoved(LayerData layer, bool isRemovalRoot)
        {
            if (layer.HasProperty<DashboardCsvPropertyData>() || visualizations.Any(visualization => visualization != null && visualization.LayerData == layer))
                Refresh();
        }

        private void OnProjectLoaded(ProjectData project)
        {
            Refresh(true);
        }
        private void OnLayerNameChanged(string name) => RefreshObjects();

        private void OnLayerActiveChanged(bool active) => Changed?.Invoke();

        private void OnVisualizationCreated(LayerGameObject visualization)
        {
            if (visualization is not HierarchicalObjectLayerGameObject hierarchical || !IsSupported(hierarchical))
                return;

            Subscribe(hierarchical);
            RefreshObjects();
        }

        private static bool IsSupported(HierarchicalObjectLayerGameObject visualization) => visualization is CityJSONLayerGameObject || visualization.GetComponent<OBJSpawner>() != null || visualization.GetComponent<GLBSpawner>() != null;

        private bool IsCurrent(HierarchicalObjectLayerGameObject visualization) => visualization != null && visualization.HasLayerData &&
            !visualization.LayerData.IsDisposed && currentProjectLayers.Contains(visualization.LayerData);

        private void Subscribe(HierarchicalObjectLayerGameObject visualization)
        {
            if (!visualizations.Add(visualization))
                return;

            // VisualizationCreated is raised before SetData initializes the layer behaviour.
            if (visualization is CityJSONLayerGameObject)
                visualization.GetComponent<CityJSON>().onAllCityObjectsProcessed.AddListener(RefreshObjects);
            else
                visualization.GetComponent<IImportedObject>().ObjectVisualized.AddListener(OnObjVisualized);
        }

        private void Unsubscribe(HierarchicalObjectLayerGameObject visualization)
        {
            objRoots.Remove(visualization);
#if UNITY_EDITOR
            bindingDiagnostics.Remove(visualization);
#endif
            if (visualization == null)
                return;

            if (visualization is CityJSONLayerGameObject)
                visualization.GetComponent<CityJSON>().onAllCityObjectsProcessed.RemoveListener(RefreshObjects);
            else if (visualization.TryGetComponent<IImportedObject>(out var spawner))
                spawner.ObjectVisualized.RemoveListener(OnObjVisualized);
        }

        private void OnObjVisualized(GameObject importedObject)
        {
            if (importedObject == null)
                return;

            var visualization = importedObject.GetComponentInParent<HierarchicalObjectLayerGameObject>(true);
            if (visualization == null || !visualizations.Contains(visualization))
                return;

            objRoots[visualization] = importedObject;
            CaptureCurrentLayers();
            RefreshObjectsCore(visualization);
        }

        private void DiscoverVisualizations()
        {
            // Recover existing and hidden visualizations only at initialization and project load.
            foreach (var visualization in UnityEngine.Object.FindObjectsByType<HierarchicalObjectLayerGameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (IsCurrent(visualization) && IsSupported(visualization))
                    Subscribe(visualization);
            }
        }

        private LayerData[] CaptureCurrentLayers()
        {
            var hierarchy = ProjectData.Current?.RootLayer?.GetFlatHierarchy().Where(layer => !layer.IsDisposed).ToArray() ?? Array.Empty<LayerData>();
            currentProjectLayers.Clear();
            currentProjectLayers.UnionWith(hierarchy);
            return hierarchy;
        }

        private void Refresh() => Refresh(false);

        private void Refresh(bool discoverVisualizations)
        {
            var hierarchy = CaptureCurrentLayers();
            if (discoverVisualizations)
                DiscoverVisualizations();

            var nextSource = functionality.IsEnabled ? hierarchy
                .Select(layer => layer.GetProperty<DashboardCsvPropertyData>())
                .Where(candidate => candidate != null)
                // Prefer loaded data, but also observe a source whose project asset is still loading.
                .OrderByDescending(candidate => candidate.Data != null)
                .FirstOrDefault() : null;
            if (!ReferenceEquals(source, nextSource))
            {
                source?.DataChanged.RemoveListener(Refresh);
                source = nextSource;
                source?.DataChanged.AddListener(Refresh);
            }

            if (!ReferenceEquals(data, source?.Data))
            {
                data = source?.Data;
                index = new DashboardObjectData(data);
#if UNITY_EDITOR
                bindingDiagnostics.Clear();
#endif
            }
            RefreshObjectsCore();
        }

        private void RefreshObjects()
        {
            CaptureCurrentLayers();
            RefreshObjectsCore();
        }

        private void RefreshObjectsCore(HierarchicalObjectLayerGameObject importedVisualization = null)
        {
            // VisualizationCreated precedes SetData; keep pending visualizations until their data is assigned.
            foreach (var visualization in visualizations.Where(visualization => visualization == null || visualization.HasLayerData && !IsCurrent(visualization)).ToArray())
            {
                Unsubscribe(visualization);
                visualizations.Remove(visualization);
            }

            // Layer data is assigned after VisualizationCreated, so observe names once it is available.
            var currentLayers = visualizations.Where(IsCurrent).Select(visualization => visualization.LayerData).ToHashSet();
            foreach (var layer in namedLayers.Except(currentLayers).ToArray())
            {
                layer.NameChanged.RemoveListener(OnLayerNameChanged);
                layer.LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveChanged);
                namedLayers.Remove(layer);
            }
            foreach (var layer in currentLayers)
            {
                if (namedLayers.Add(layer))
                {
                    layer.NameChanged.AddListener(OnLayerNameChanged);
                    layer.LayerActiveInHierarchyChanged.AddListener(OnLayerActiveChanged);
                }
            }

            objects.Clear();
            objObjects.Clear();
            glbObjects.Clear();
            var invalidBlockCount = 0;
            foreach (var visualization in visualizations)
            {
                if (!IsCurrent(visualization))
                    continue;

#if UNITY_EDITOR
                diagnosticItems.Clear();
#endif
                if (HasData && IsDashboardLayerName(visualization.Name))
                {
                    if (visualization is CityJSONLayerGameObject cityJson)
                    {
                        foreach (var cityObject in cityJson.CityJson.CityObjects)
                        {
                            var blockId = ReadBlockIdentifier(cityObject);
                            if (blockId == null)
                                invalidBlockCount++;
                            var block = index.Find(blockId);
                            if (block != null)
                                objects[cityObject] = block;
                            RecordBindingDiagnostic(cityObject.name, blockId, block != null);
                        }
                    }
                    else if (visualization.GetComponent<GLBSpawner>() != null)
                        BindGlbMeshes(visualization);
                    else
                        BindObjMaterials(visualization, ref invalidBlockCount);
                }
                LogBinding(visualization, visualization == importedVisualization);
            }
#if UNITY_EDITOR
            diagnosticItems.Clear();
#endif
            if (invalidBlockCount > 0 && invalidBlockCount != reportedInvalidBlockCount)
            {
                var message = $"Bij {invalidBlockCount} dashboardobject(en) ontbreekt een bruikbare blokcode. Gebruik het attribuut Blok bij CityJSON of een materiaalnaam bij OBJ.";
                Debug.LogWarning(message);
                App.Snackbar?.DisplayError(message);
            }
            reportedInvalidBlockCount = invalidBlockCount;
            Changed?.Invoke();
        }

        private void BindObjMaterials(HierarchicalObjectLayerGameObject visualization, ref int invalidBlockCount)
        {
            foreach (var material in GetObjMaterials(visualization))
            {
                var identifier = ReadObjBlockIdentifier(material.name);
                if (identifier == null)
                    invalidBlockCount++;

                var block = index.Find(identifier);
                if (block != null)
                    objObjects[material] = block;
                RecordBindingDiagnostic(material.name, identifier, block != null);
            }
        }

        private IEnumerable<Material> GetObjMaterials(HierarchicalObjectLayerGameObject visualization)
        {
            var root = GetImportedRoot(visualization);
            return root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct();
        }

        private GameObject GetImportedRoot(HierarchicalObjectLayerGameObject visualization) =>
            objRoots.TryGetValue(visualization, out var importedObject) && importedObject != null ? importedObject : visualization.gameObject;

        private void BindGlbMeshes(HierarchicalObjectLayerGameObject visualization)
        {
            var root = GetImportedRoot(visualization);
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var block = FindGlbBlock(index, renderer.transform, root.transform);
                if (block != null)
                    glbObjects[renderer] = block;
                RecordBindingDiagnostic(renderer.name, block?.Block ?? renderer.name, block != null);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void RecordBindingDiagnostic(string name, string identifier, bool matched)
        {
#if UNITY_EDITOR
            diagnosticItems.Add((name, identifier ?? "ontbreekt", matched));
#endif
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogBinding(HierarchicalObjectLayerGameObject visualization, bool importCompleted)
        {
#if UNITY_EDITOR
            var format = visualization is CityJSONLayerGameObject ? "CityJSON" : visualization.GetComponent<GLBSpawner>() != null ? "GLB" : "OBJ";
            var message = $"Dashboardkoppeling {format} '{visualization.Name}': ";
            if (!functionality.IsEnabled)
                message += "niet actief (dashboarding uitgeschakeld).";
            else if (!IsDashboardLayerName(visualization.Name))
                message += "niet actief (laagnaam begint niet met dashboard).";
            else if (!HasData)
                message += "niet actief (geen geladen dashboard-CSV).";
            else
            {
                var total = diagnosticItems.Count;
                var matched = diagnosticItems.Count(item => item.Matched);
                var identifiers = diagnosticItems.Select(item => item.Identifier).Distinct().OrderBy(identifier => identifier).ToArray();
                var unit = format == "CityJSON" ? "CityObjects" : format == "GLB" ? "meshonderdelen" : "materialen";
                message += $"{total} ingelezen {unit}, {matched} gekoppeld, {total - matched} zonder overeenkomst.";
                message += " Aangetroffen blokcodes: " + (identifiers.Length == 0 ? "geen" : string.Join(", ", identifiers.Take(20)));
                if (identifiers.Length > 20)
                    message += $" (eerste 20 van {identifiers.Length})";
                message += ".";
                if (matched < total)
                {
                    var unmatched = diagnosticItems.Where(item => !item.Matched).Select(item => $"{item.Name} => '{item.Identifier}'");
                    message += " Geen CSV-match of ontbrekende blokcode: " + string.Join(", ", unmatched.OrderBy(item => item).Take(10));
                }
            }

            if (!importCompleted && bindingDiagnostics.TryGetValue(visualization, out var previous) && previous == message)
                return;

            bindingDiagnostics[visualization] = message;
            Debug.Log(message + (importCompleted ? $" Dashboardkoppeling opnieuw beoordeeld na {(format == "GLB" ? "GLB" : "OBJ/MTL")}-import." : ""), visualization);
#endif
        }
    }
}
