using System;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using LayerService = Netherlands3D.Twin.Services.Layers;

namespace Netherlands3D.Twin.DataSets
{
    /// <summary>
    /// Tracks the dashboard data source while dashboard functionality is available.
    /// </summary>
    internal sealed class DashboardDataBinding : IDisposable
    {
        private readonly LayerService layers;
        private readonly ProjectDataHandler projectLoader;
        private readonly Functionality functionality;

        public DashboardCsvPropertyData Source { get; private set; }
        public DashboardData Data => Source?.Data;
        public event Action Changed;

        public DashboardDataBinding(LayerService layers, ProjectDataHandler projectLoader, Functionality functionality)
        {
            this.layers = layers;
            this.projectLoader = projectLoader;
            this.functionality = functionality;
            layers.LayerAdded.AddListener(OnLayerAdded);
            layers.LayerRemoved.AddListener(OnLayerRemoved);
            projectLoader.OnLoadCompleted.AddListener(OnProjectLoaded);
            functionality.OnEnableFunctionality.AddListener(Refresh);
            functionality.OnDisableFunctionality.AddListener(Refresh);
            Refresh();
        }

        public void Dispose()
        {
            layers.LayerAdded.RemoveListener(OnLayerAdded);
            layers.LayerRemoved.RemoveListener(OnLayerRemoved);
            projectLoader.OnLoadCompleted.RemoveListener(OnProjectLoaded);
            functionality.OnEnableFunctionality.RemoveListener(Refresh);
            functionality.OnDisableFunctionality.RemoveListener(Refresh);
            Source?.DataChanged.RemoveListener(Refresh);
            Source = null;
        }

        private void OnLayerAdded(LayerData layer)
        {
            if (layer.HasProperty<DashboardCsvPropertyData>())
                Refresh();
        }

        private void OnLayerRemoved(LayerData layer, bool isRemovalRoot) => OnLayerAdded(layer);

        private void OnProjectLoaded(ProjectData project) => Refresh();

        private void Refresh()
        {
            DashboardCsvPropertyData nextSource = null;
            if (functionality.IsEnabled)
            {
                foreach (var layer in ProjectData.Current.RootLayer.GetFlatHierarchy())
                {
                    if (layer.IsDisposed || layer.GetProperty<DashboardCsvPropertyData>() is not { } candidate)
                        continue;

                    // Prefer loaded data, but also observe a source whose project asset is still loading.
                    nextSource ??= candidate;
                    if (candidate.Data == null)
                        continue;

                    nextSource = candidate;
                    break;
                }
            }

            if (!ReferenceEquals(Source, nextSource))
            {
                Source?.DataChanged.RemoveListener(Refresh);
                Source = nextSource;
                Source?.DataChanged.AddListener(Refresh);
            }

            Changed?.Invoke();
        }
    }
}
