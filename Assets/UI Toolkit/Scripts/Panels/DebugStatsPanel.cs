using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Services;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [InspectorPanel]
    public partial class DebugStatsPanel : BaseInspectorContentPanel
    {
        public override string Title => "Debug stats";
        
        private DebugStatsService debugStatsService;

        private StatsGraph frameDurationGraph;
        private StatsGraph randomNumberGraph;
        
        private StatsGraph totalUsedMemoryGraph;
        private StatsGraph totalReservedMemoryGraph;
        private StatsGraph gcUsedMemoryGraph;
        private StatsGraph gcReservedMemoryGraph;

        private StatsGraph loadedCartesianTileCount;
        private StatsGraph pendingCartesianTileChangeCount;
        private StatsGraph activeCartesianTileChangeCount;
        
        private VisualElement statsContainer;

        public DebugStatsPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            statsContainer = this.Q<VisualElement>("StatsContainer");
            
            CreateStatsGraphs();
            
            /*
            frameDurationGraph = this.Q<StatsGraph>("FrameDuration");
            randomNumberGraph = this.Q<StatsGraph>("RandomNumber");
            
            totalUsedMemoryGraph = this.Q<StatsGraph>("TotalUsedMemory");
            totalReservedMemoryGraph = this.Q<StatsGraph>("TotalReservedMemory");
            gcUsedMemoryGraph = this.Q<StatsGraph>("GCUsedMemory");
            gcReservedMemoryGraph = this.Q<StatsGraph>("GCReservedMemory");
            
            loadedCartesianTileCount = this.Q<StatsGraph>("LoadedCartesianTileCount");
            pendingCartesianTileChangeCount = this.Q<StatsGraph>("PendingCartesianTileChangeCount");
            activeCartesianTileChangeCount = this.Q<StatsGraph>("ActiveCartesianTileChangeCount");           
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);*/
        }
        
        /*
        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            debugStatsService = ServiceLocator.GetService<DebugStatsService>();
            
            frameDurationGraph.Bind(debugStatsService.FrameDuration);
            randomNumberGraph.Bind(debugStatsService.RandomNumber);
            
            totalUsedMemoryGraph.Bind(debugStatsService.TotalUsedMemory);
            totalReservedMemoryGraph.Bind(debugStatsService.TotalReservedMemory);
            gcUsedMemoryGraph.Bind(debugStatsService.GCUsedMemory);
            gcReservedMemoryGraph.Bind(debugStatsService.GCReservedMemory);
            
            loadedCartesianTileCount.Bind(debugStatsService.LoadedCartesianTileCount);
            pendingCartesianTileChangeCount.Bind(debugStatsService.PendingCartesianTileChangeCount);
            activeCartesianTileChangeCount.Bind(debugStatsService.ActiveCartesianTileChangeCount);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            debugStatsService = null;
        }*/
        
        private void CreateStatsGraphs()
        {
            var service =
                ServiceLocator.GetService<DebugStatsService>();

            foreach (var stat in service.Stats)
            {
                var graph = new StatsGraph();
                graph.Title = stat.DisplayName;
                graph.Bind(stat);
                statsContainer.Add(graph);
            }
        }

    }
    
    

}