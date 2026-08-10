using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.layers.properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(HiddenObjectsPropertyData), PropertySectionCategory.Styling)]
    public partial class HiddenObjectsPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private float cameraDistance = 150f;
        private Material selectionMaterial;
        private GameObject selectedGhostObject;
        private UnityAction<IMapping> waitForMappingLoaded;
        private HiddenObjectsPropertyData stylingPropertyData;
        private ListView listView;
        private List<string> objectIds = new();
        private List<string> toggledObjectIds = new();
        private bool showSelection = true;
        private ObjectSelectorService selector;
        
        
        public HiddenObjectsPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            listView = this.Q<ListView>();


            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.selectionType = SelectionType.Multiple;

            listView.makeItem = MakeListViewItem;
            listView.bindItem = BindListViewItem;


            listView.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                //when clicked outside the listview, deselect the current selection
                listView.RegisterCallback<BlurEvent>(evt =>
                {
                    var pos = Pointer.current.position.ReadValue();
                    var panelPos = RuntimePanelUtils.ScreenToPanel(
                        listView.panel,
                        new Vector2(pos.x, Screen.height - pos.y)
                    );
                    if (!listView.worldBound.Contains(panelPos))
                    {
                        ClearSelection();
                    }
                });
            });

            listView.selectedIndicesChanged += indices =>
            {
                //show selection in world when items in panel are selected
                UpdateSelectionForIndices(indices);
            };
            
            RegisterCallback<DetachFromPanelEvent>(_ => 
            {
                OnDestroy();  
            });
        }
        
        private void UpdateSelectionForIndices(IEnumerable<int> indices)
        {
            selector.Deselect();
            
            if(!showSelection) return;
            
            foreach (int i in indices)
            {
                var id = listView.itemsSource[i] as string;
                bool? visibility = stylingPropertyData.GetVisibilityForSubObjectById(id);
                if (visibility == true)
                {
                    Coordinate coord = (Coordinate)stylingPropertyData.GetVisibilityCoordinateForSubObjectById(id);
                    selector.SelectBagId(id, coord);
                }
            }
        }
        
        private VisualElement MakeListViewItem()
        {
            HideObjectListViewItem item = new HideObjectListViewItem();
            item.ShowToggle(true);
            item.OnToggleVisibility.AddListener(ToggleVisibilityForSelectedFeatures);
            item.RegisterCallback<PointerUpEvent>(HiddenFeatureSelected);

            var listViewItem = new ListViewItem(item);
            return listViewItem;
        }
        
        private void HiddenFeatureSelected(PointerUpEvent evt)
        {
            var element = (HideObjectListViewItem)evt.currentTarget;
            HiddenFeatureSelected(element.ID);
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<HideObjectListViewItem>() is not HideObjectListViewItem element) return;           
           
            string mapping = listView.itemsSource[index] as string;
            bool? visibility = stylingPropertyData.GetVisibilityForSubObjectById(mapping);
            element.SetToggleValue(visibility == true);
            element.ID = mapping;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            selector = ServiceLocator.GetService<ObjectSelectorService>();
            stylingPropertyData = properties.GetDefaultStylingPropertyData<HiddenObjectsPropertyData>();
            if (stylingPropertyData == null) return;

            selectionMaterial = stylingPropertyData.SelectionMaterial;
            
            objectIds.Clear();
            UpdateVisibility();
            stylingPropertyData.OnStylingChanged.AddListener(UpdateVisibility);
            stylingPropertyData.OnStylingChanged.AddListener(UpdateSelection);
            ObjectSelectorService.MappingTree.OnMappingRemoved.AddListener(OnMappingRemoved);
        }

        private void ClearSelection()
        {
            listView.ClearSelection();
            DestroyGhostMesh();
        }

        private void UpdateVisibility()
        {
            //dont clear the list of id's because we want to keep them during the panels life
            //find attributes within the data, we cannot rely on layer.layerfeatures.values because tiles arent potentialy loaded
            foreach(KeyValuePair<string, StylingRule> kv in stylingPropertyData.StylingRules)
            {
                if(kv.Key.Contains(HiddenObjectsPropertyData.VisibilityIdentifier))
                {
                    string objectId = stylingPropertyData.GetStylingRuleName(kv.Key);                    
                    bool? visibility = stylingPropertyData.GetVisibilityForSubObjectById(objectId);
                    if (visibility == false && !objectIds.Contains(objectId))
                        objectIds.Add(objectId);
                }
            }
            listView.itemsSource = objectIds;
            listView.RefreshItems();
        }

        private void UpdateSelection()
        {
            //deselect any selected feature in the world when opening the hidden feature panel
            selector.Deselect();
            foreach(KeyValuePair<string, StylingRule> kv in stylingPropertyData.StylingRules)
            {
                if(kv.Key.Contains(HiddenObjectsPropertyData.VisibilityIdentifier))
                {
                    string objectId = stylingPropertyData.GetStylingRuleName(kv.Key);                    
                    bool? visibility = stylingPropertyData.GetVisibilityForSubObjectById(objectId);
                    //select the recently toggled on building so we can actually see what was toggled on
                    if (showSelection && visibility == true && toggledObjectIds.Contains(objectId))
                    {
                        Coordinate coord = (Coordinate)stylingPropertyData.GetVisibilityCoordinateForSubObjectById(objectId);
                        selector.SelectBagId(objectId, coord);
                    }
                }
            }
        }

        private HiddenObjectsPropertyData.SubObjectData? ToggleVisibilityForFeature(string objectId, bool visible)
        {
            //the feature being changed should always have its coordinate within the styling rule!
            Coordinate? coord;
            LayerFeature layerFeature = HiddenObject.GetLayerFeatureFromBagId(objectId);
            if(layerFeature != null)
            {               
                coord = stylingPropertyData.GetVisibilityCoordinateForSubObject(layerFeature);
                if(coord == null)
                {
                    Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                    return null;
                }
                HiddenObjectsPropertyData.SubObjectData layerFeatureData = new HiddenObjectsPropertyData.SubObjectData();
                layerFeatureData.layerFeature = layerFeature;
                layerFeatureData.coord = (Coordinate)coord;
                layerFeatureData.visible = visible;
                return layerFeatureData;
            }
            coord = (Coordinate)stylingPropertyData.GetVisibilityCoordinateForSubObjectById(objectId);
            if (coord == null)
            {
                Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                return null;
            }
            HiddenObjectsPropertyData.SubObjectData subObjectData = new HiddenObjectsPropertyData.SubObjectData();
            subObjectData.id = objectId;
            subObjectData.coord = (Coordinate)coord;
            subObjectData.visible = visible;
            return subObjectData;
        }

        private void ToggleVisibilityForSelectedFeatures(string objectId, bool visible)
        {
            //is the new layer not selected yet and is no modifier pressed, then clear selection and select the new layer
            int index = listView.itemsSource.IndexOf(objectId);
            if (index >= 0 && !listView.selectedIndices.Contains(index))
            {
                if(MultiSelectionUtility.NoModifierKeyPressed())
                    ClearSelection();

                listView.AddToSelection(index);
            }
            
            toggledObjectIds.Clear();
            List<HiddenObjectsPropertyData.SubObjectData> stylingData = new();
            //toggle the selection of items
            foreach (int i in listView.selectedIndices.ToList())
            {
                var id = listView.itemsSource[i] as string;
                if(visible)
                    toggledObjectIds.Add(id);
                var data = ToggleVisibilityForFeature(id, visible);
                if(data.HasValue)
                    stylingData.Add((HiddenObjectsPropertyData.SubObjectData)data);
            }
            stylingPropertyData.SetVisibilityForSubObjects(stylingData);
           
            if (!visible)
                ShowGhostMesh(objectId);
            else
                DestroyGhostMesh();
        }

        private void HiddenFeatureSelected(string objectId)
        {
            Coordinate ? coord = stylingPropertyData.GetVisibilityCoordinateForSubObjectById(objectId);
            if (coord == null)
            {
                Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                return;
            }

            LayerFeature layerFeature = HiddenObject.GetLayerFeatureFromBagId(objectId);
            if(layerFeature == null)
            {
                //there is no layerfeature present, lets attach a listener to wait for the mapping to be loaded
                DestroyGhostMesh();
                AddListenerForLoadingMapping(objectId);
                Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget((Coordinate)coord, cameraDistance);
                return;
            }
            Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget((Coordinate)coord, cameraDistance);
            ShowGhostMesh(objectId);
        }

        private void AddListenerForLoadingMapping(string objectId)
        {
            //remove previous listener if present
            if(waitForMappingLoaded != null)
                ObjectSelectorService.MappingTree.OnMappingAdded.RemoveListener(waitForMappingLoaded);
                
            waitForMappingLoaded = mapping => OnMappingLoaded(mapping, objectId);
            ObjectSelectorService.MappingTree.OnMappingAdded.AddListener(waitForMappingLoaded);
        }

        private void OnMappingLoaded(IMapping mapping, string objectId)
        {
            if (this == null)
            {
                DestroyGhostMesh();
                return; //object got destroyed in the meantime
            }

            if (mapping is not MeshMapping meshMapping) return;
            MeshMappingItem item = meshMapping.FindItemById(objectId);
            if (item == null) return;

            //dont remove the listener yet, we want to be able to refresh the ghost mesh when a new lod is loaded
            //ObjectSelectorService.MappingTree.OnMappingAdded.RemoveListener(waitForMappingLoaded);
            HiddenFeatureSelected(objectId);
        }

        private void OnMappingRemoved(IMapping mapping)
        {
            if (mapping is not MeshMapping meshMapping) return;
            if(selectedGhostObject == null) return;

            string objectId = selectedGhostObject.name;
            if(meshMapping.HasItemWithId(objectId))
            {
                DestroyGhostMesh();
            }
        }

        public void ShowGhostMesh(string objectId)
        {
            DestroyGhostMesh();
            bool? visibility = stylingPropertyData.GetVisibilityForSubObjectById(objectId);
            if (visibility == true)
            {
                return;
            }

            Coordinate? coord = stylingPropertyData.GetVisibilityCoordinateForSubObjectById(objectId);
            if (coord == null)
            {
                Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                return;
            }

            List<IMapping> mappings = ObjectSelectorService.MappingTree.Query<MeshMapping>((Coordinate)coord);
            foreach (IMapping m in mappings)
            {
                if (m is not MeshMapping meshMapping) continue;

                MeshMappingItem item = meshMapping.FindItemById(objectId);
                if (item == null) continue;

                DestroyGhostMesh();
                selectedGhostObject = new GameObject(objectId);
                Mesh mesh = MeshMapping.CreateMeshFromMapping(meshMapping.ObjectMapping, item.ObjectMappingItem, out Vector3 localCentroid);
                MeshFilter mFilter = selectedGhostObject.AddComponent<MeshFilter>();
                mFilter.mesh = mesh;
                MeshRenderer mRenderer = selectedGhostObject.AddComponent<MeshRenderer>();
                mRenderer.material = selectionMaterial;
                selectedGhostObject.transform.position = meshMapping.ObjectMapping.transform.TransformPoint(localCentroid);
                return;
            }
        }

        public void DestroyGhostMesh()
        {
            if (selectedGhostObject != null)
            {
                MonoBehaviour.Destroy(selectedGhostObject);
                selectedGhostObject = null;
            }
        }

        private void OnDestroy()
        {
            DestroyGhostMesh();
            stylingPropertyData.OnStylingChanged.RemoveListener(UpdateVisibility);
            stylingPropertyData.OnStylingChanged.RemoveListener(UpdateSelection);
            ObjectSelectorService.MappingTree.OnMappingRemoved.RemoveListener(OnMappingRemoved);

            //remove all visibility data for features that became visible
            List<string> idsToRemove = new List<string>();
            foreach (KeyValuePair<string, StylingRule> kv in stylingPropertyData.StylingRules)
            {
                if (kv.Key.Contains(HiddenObjectsPropertyData.VisibilityIdentifier))
                {
                    string objectId = stylingPropertyData.GetStylingRuleName(kv.Key);
                    bool? visibility = stylingPropertyData.GetVisibilityForSubObjectById(objectId);
                    if (visibility == true)
                        idsToRemove.Add(objectId);
                }
            }

            //all objects that were made visible while this panel was alive, should be removed from data
            foreach (string id in idsToRemove)
            {
                stylingPropertyData.RemoveVisibilityForSubObjectById(id);
            }
        }
    }
}