using System.Collections.Generic;
using Netherlands3D.UI.Components;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.UI.HierarchyInspector
{
    public class ScenarioManager : MonoBehaviour
    {
        private readonly Dictionary<FolderPropertyData, ScenarioSubscription> subscriptions = new();
        private readonly List<FolderPropertyData> orderedFolders = new();

        private ToolbarScenario toolbar;
        private LayerData selectedScenario;
        private bool isApplyingScenarioChange; // reentrancy guard while we drive ActiveSelf ourselves
        
        private class ScenarioSubscription
        {
            public readonly FolderPropertyData FolderProperty;
            public readonly LayerData Layer;
            private readonly ScenarioManager owner;

            public ScenarioSubscription(FolderPropertyData folderProperty, LayerData layer, ScenarioManager owner)
            {
                FolderProperty = folderProperty;
                Layer = layer;
                this.owner = owner;
            }

            public void OnIsScenarioChanged(bool isScenario) => owner.HandleIsScenarioChanged(FolderProperty, Layer, isScenario);
            public void OnActiveSelfChanged(bool active) => owner.HandleActiveSelfChanged(FolderProperty, Layer, active);
            public void OnHierarchyOrderChanged(int newIndex) => owner.toolbar.SetFolderIndex(FolderProperty, newIndex);
            public void OnNameChanged(string newName) => owner.toolbar.SetFolderName(FolderProperty, newName);
        }

        private void Awake()
        {
            toolbar = App.UIRoot.Root.Q<ToolbarScenario>();
        }

        private void OnEnable()
        {
            toolbar.SelectionChanged.AddListener(OnToolbarSelectionChanged);

            App.Layers.LayerAdded.AddListener(OnLayerAdded);
            App.Layers.LayerRemoved.AddListener(OnLayerRemoved);
            ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);

            RebuildScenarios();
        }

        private void OnDisable()
        {
            toolbar.SelectionChanged.RemoveListener(OnToolbarSelectionChanged);

            App.Layers.LayerAdded.RemoveListener(OnLayerAdded);
            App.Layers.LayerRemoved.RemoveListener(OnLayerRemoved);
            ProjectData.Current.OnDataChanged.RemoveListener(OnProjectDataChanged);

            UnsubscribeFromAllFolders();
        }
        
        private void OnLayerAdded(LayerData layer)
        {
            var folderProperty = layer.GetProperty<FolderPropertyData>();
            if (folderProperty == null)
                return;

            RegisterFolder(folderProperty, layer);
        }
        
        private void OnLayerRemoved(LayerData layer)
        {
            var folderProperty = layer.GetProperty<FolderPropertyData>();
            if (folderProperty == null)
                return;

            UnregisterFolder(folderProperty, layer);
        }

        private void OnProjectDataChanged(ProjectData _)
        {
            RebuildScenarios();
        }

        private void RegisterFolder(FolderPropertyData folderProperty, LayerData layer)
        {
            var subscription = new ScenarioSubscription(folderProperty, layer, this);
            subscriptions.Add(folderProperty, subscription);

            folderProperty.IsScenarioChanged.AddListener(subscription.OnIsScenarioChanged);
            layer.ActiveSelfChanged.AddListener(subscription.OnActiveSelfChanged);
            layer.NameChanged.AddListener(subscription.OnNameChanged);
            layer.ParentOrSiblingIndexChanged.AddListener(subscription.OnHierarchyOrderChanged);

            var insertIndex = GetInsertIndexForLayer(layer);
            orderedFolders.Insert(insertIndex, folderProperty);
            toolbar.InsertFolder(folderProperty, insertIndex, layer.Name, folderProperty.IsScenario);

            if (folderProperty.IsScenario && layer.ActiveSelf)
                HandleScenarioActivated(folderProperty, layer);
        }

        private void UnregisterFolder(FolderPropertyData folderProperty, LayerData layer)
        {
            var subscription = subscriptions[folderProperty];

            folderProperty.IsScenarioChanged.RemoveListener(subscription.OnIsScenarioChanged);
            layer.ActiveSelfChanged.RemoveListener(subscription.OnActiveSelfChanged);

            subscriptions.Remove(folderProperty);
            orderedFolders.Remove(folderProperty);

            if (selectedScenario == layer)
                selectedScenario = null;

            toolbar.RemoveFolder(folderProperty);
        }
        
        private int GetInsertIndexForLayer(LayerData layer) //todo simplify
        {
            var flatHierarchy = ProjectData.Current.RootLayer.GetFlatHierarchy().ToList();
            var layerIndex = flatHierarchy.IndexOf(layer);
            if (layerIndex < 0)
                return orderedFolders.Count; // not found (shouldn't happen); append as a fallback

            for (var i = 0; i < orderedFolders.Count; i++)
            {
                var otherLayer = subscriptions[orderedFolders[i]].Layer;
                var otherIndex = flatHierarchy.IndexOf(otherLayer);
                if (otherIndex > layerIndex)
                    return i;
            }

            return orderedFolders.Count;
        }
        
        private void RebuildScenarios()
        {
            UnsubscribeFromAllFolders();
            selectedScenario = null;

            var layers = ProjectData.Current.RootLayer.GetFlatHierarchy();

            isApplyingScenarioChange = true; //we want to be able to enable/disable the scenarios when building without this triggering changes in the other scenarios that we are currently building. 
            foreach (var layer in layers)
            {
                var folderProperty = layer.GetProperty<FolderPropertyData>();
                if (folderProperty == null)
                    continue;

                RegisterFolder(folderProperty, layer);
            }
            isApplyingScenarioChange = false;
        }

        private void HandleIsScenarioChanged(FolderPropertyData folderProperty, LayerData layer, bool isScenario)
        {
            if (isScenario)
            {
                toolbar.SetScenarioVisible(folderProperty, true);
                
                //only activate the scenario if the folder was already active, otherwise just show the button 
                if (layer.ActiveSelf)
                    HandleScenarioActivated(folderProperty, layer);
            }
            else
            {
                toolbar.SetScenarioVisible(folderProperty, false);

                if (selectedScenario == layer)
                    selectedScenario = null;
            }
        }
        
        private void HandleActiveSelfChanged(FolderPropertyData folderProperty, LayerData layer, bool active)
        {
            if (isApplyingScenarioChange) return; //avoid problems when rebuilding the entire scenario hierarchy
            if (!folderProperty.IsScenario) return;

            if (active)
            {
                HandleScenarioActivated(folderProperty, layer);
            }
            else if (selectedScenario == layer)
            {
                selectedScenario = null;
                toolbar.SetSelectedFolderWithoutNotify(null);
            }
        }

        private void HandleScenarioActivated(FolderPropertyData folderProperty, LayerData layer)
        {
            if (selectedScenario == layer)
                return;

            isApplyingScenarioChange = true;

            if (selectedScenario != null)
                SetScenarioActive(selectedScenario, false);

            selectedScenario = layer;
            SetScenarioActive(layer, true);

            isApplyingScenarioChange = false;

            toolbar.SetSelectedFolderWithoutNotify(folderProperty);
        }

        private void OnToolbarSelectionChanged(FolderPropertyData folderProperty)
        {
            LayerData newScenario = null;
            if(folderProperty != null)
                newScenario = subscriptions[folderProperty].Layer;

            if (selectedScenario == newScenario)
                return;

            isApplyingScenarioChange = true;

            if (selectedScenario != null)
                SetScenarioActive(selectedScenario, false);

            selectedScenario = newScenario;
            if (selectedScenario != null)
                SetScenarioActive(newScenario, true);

            isApplyingScenarioChange = false;
        }

        private void SetScenarioActive(LayerData scenario, bool active)
        {
            if (scenario.ActiveSelf == active)
                return;

            scenario.ActiveSelf = active;

            if (active && !scenario.IsSelected)
                scenario.SelectLayer();
            else if (!active && scenario.IsSelected)
                scenario.DeselectLayer();
        }

        private void UnsubscribeFromAllFolders()
        {
            foreach (var subscription in subscriptions.Values)
            {
                subscription.FolderProperty.IsScenarioChanged.RemoveListener(subscription.OnIsScenarioChanged);
                subscription.Layer.ActiveSelfChanged.RemoveListener(subscription.OnActiveSelfChanged);
                subscription.Layer.NameChanged.RemoveListener(subscription.OnNameChanged);
                subscription.Layer.ParentOrSiblingIndexChanged.RemoveListener(subscription.OnHierarchyOrderChanged);
            }

            subscriptions.Clear();
            orderedFolders.Clear();
        }
    }
}