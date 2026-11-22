using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Structure;
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
    /// Maintains session-only CityJSON bindings while dashboard functionality is available.
    /// </summary>
    internal sealed class DashboardObjectBinding : IDisposable
    {
        private readonly LayerService layers;
        private readonly ProjectDataHandler projectLoader;
        private readonly Functionality functionality;
        private readonly HashSet<CityJSONLayerGameObject> visualizations = new();
        private readonly HashSet<LayerData> namedLayers = new();
        private readonly HashSet<LayerData> currentProjectLayers = new();
        private readonly Dictionary<CityObject, DashboardObjectData.BlockData> objects = new();
        private DashboardCsvPropertyData source;
        private DashboardData data;
        private DashboardObjectData index = new(null);
        private int reportedInvalidBlockCount;

        public IReadOnlyDictionary<CityObject, DashboardObjectData.BlockData> Objects => objects;
        public bool HasData => functionality.IsEnabled && data != null;
        public IReadOnlyList<string> HousingSegments => index.HousingSegments;
        public IEnumerable<CityJSONLayerGameObject> Visualizations => visualizations.Where(IsCurrent);
        public event Action Changed;

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
            layers.LayerAdded.RemoveListener(OnLayerAdded);
            layers.LayerRemoved.RemoveListener(OnLayerRemoved);
            layers.VisualizationCreated.RemoveListener(OnVisualizationCreated);
            if (projectLoader != null)
                projectLoader.OnLoadCompleted.RemoveListener(OnProjectLoaded);
            functionality.OnEnableFunctionality.RemoveListener(Refresh);
            functionality.OnDisableFunctionality.RemoveListener(Refresh);
            source?.DataChanged.RemoveListener(Refresh);
            foreach (var layer in namedLayers)
                layer.NameChanged.RemoveListener(OnLayerNameChanged);
            namedLayers.Clear();
            currentProjectLayers.Clear();
            foreach (var visualization in visualizations)
                Unsubscribe(visualization);
            visualizations.Clear();
            objects.Clear();
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

        private void OnVisualizationCreated(LayerGameObject visualization)
        {
            if (visualization is not CityJSONLayerGameObject cityJson)
                return;

            Subscribe(cityJson);
            RefreshObjects();
        }

        private bool IsCurrent(CityJSONLayerGameObject visualization) => visualization != null && visualization.HasLayerData &&
            !visualization.LayerData.IsDisposed && currentProjectLayers.Contains(visualization.LayerData);

        private void Subscribe(CityJSONLayerGameObject visualization)
        {
            if (visualizations.Add(visualization))
                // VisualizationCreated is raised before SetData initializes CityJson on the layer behaviour.
                visualization.GetComponent<CityJSON>().onAllCityObjectsProcessed.AddListener(RefreshObjects);
        }

        private void Unsubscribe(CityJSONLayerGameObject visualization)
        {
            if (visualization != null)
                visualization.GetComponent<CityJSON>().onAllCityObjectsProcessed.RemoveListener(RefreshObjects);
        }

        private void DiscoverVisualizations()
        {
            // Recover existing and hidden visualizations only at initialization and project load.
            foreach (var visualization in UnityEngine.Object.FindObjectsByType<CityJSONLayerGameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (IsCurrent(visualization))
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

            foreach (var visualization in visualizations.Where(visualization => !IsCurrent(visualization)).ToArray())
            {
                Unsubscribe(visualization);
                visualizations.Remove(visualization);
            }

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
            }
            RefreshObjectsCore();
        }

        private void RefreshObjects()
        {
            CaptureCurrentLayers();
            RefreshObjectsCore();
        }

        private void RefreshObjectsCore()
        {
            // Layer data is assigned after VisualizationCreated, so observe names once it is available.
            var currentLayers = Visualizations.Select(visualization => visualization.LayerData).ToHashSet();
            foreach (var layer in namedLayers.Except(currentLayers).ToArray())
            {
                layer.NameChanged.RemoveListener(OnLayerNameChanged);
                namedLayers.Remove(layer);
            }
            foreach (var layer in currentLayers)
            {
                if (namedLayers.Add(layer))
                    layer.NameChanged.AddListener(OnLayerNameChanged);
            }

            objects.Clear();
            if (!functionality.IsEnabled || data == null)
            {
                reportedInvalidBlockCount = 0;
                Changed?.Invoke();
                return;
            }

            var invalidBlockCount = 0;
            foreach (var visualization in visualizations)
            {
                if (!IsCurrent(visualization))
                    continue;

                foreach (var cityObject in visualization.CityJson.CityObjects)
                {
                    if (!IsDashboardObject(cityObject))
                        continue;

                    var blockId = ReadBlockIdentifier(cityObject);
                    if (blockId == null)
                        invalidBlockCount++;
                    var block = index.Find(blockId);
                    if (block != null)
                        objects[cityObject] = block;
                }
            }
            if (invalidBlockCount > 0 && invalidBlockCount != reportedInvalidBlockCount)
            {
                var message = $"Bij {invalidBlockCount} dashboardobject(en) ontbreekt een bruikbare blokcode. Vul het attribuut Blok in met tekst of een getal.";
                Debug.LogWarning(message);
                App.Snackbar?.DisplayError(message);
            }
            reportedInvalidBlockCount = invalidBlockCount;
            Changed?.Invoke();
        }
    }
}
