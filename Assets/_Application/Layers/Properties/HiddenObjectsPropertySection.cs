using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsPropertySection : PropertySectionWithLayerGameObject
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject hiddenItemPrefab;
        [SerializeField] private RectTransform layerContent;
        [SerializeField] private float cameraDistance = 150f;
        [SerializeField] private Material selectionMaterial;

        private LayerGameObject layer;
        private List<HiddenObjectsVisibilityItem> hiddenObjects = new();
        private List<HiddenObjectsVisibilityItem> selectedItems = new();
        private HiddenObjectsVisibilityItem firstSelectedItem;
        private GameObject selectedHiddenObject;
        private UnityAction<IMapping> waitForMappingLoaded;
        private int currentButtonIndex = -1; 

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set => Initialize(value);
        }

        private void Initialize(LayerGameObject layer)
        {
            this.layer = layer;
            CreateItems();
            UpdateVisibility();
            layer.OnStylingApplied.AddListener(UpdateVisibility);

            ObjectSelectorService.MappingTree.OnMappingRemoved.AddListener(OnMappingRemoved);

            StartCoroutine(OnPropertySectionsLoaded());
        }

        private IEnumerator OnPropertySectionsLoaded()
        {
            yield return new WaitForEndOfFrame();
                       
            // workaround to have a minimum height for the content loaded (because of scrollrects)
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = content.rect.height;
        }

        private void CreateItems()
        {
            layerContent.ClearAllChildren();
            //find attributes within the data, we cannot rely on layer.layerfeatures.values because tiles arent potentialy loaded
            foreach(KeyValuePair<string, StylingRule> kv in layer.LayerData.DefaultStyle.StylingRules)
            {
                if(kv.Key.Contains(CartesianTileLayerStyler.VisibilityIdentifier))
                {
                    string objectId = CartesianTileLayerStyler.ObjectIdFromVisibilityStyleRuleName(kv.Key);                    
                    bool? visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObjectByAttributeTag(objectId);
                    if (visibility == false)
                        CreateVisibilityItem(objectId);
                }
            }
        }

        private void OnDestroy()
        {
            DestroyGhostMesh();
            layer.OnStylingApplied.RemoveListener(UpdateVisibility);
            ObjectSelectorService.MappingTree.OnMappingRemoved.RemoveListener(OnMappingRemoved);

            //remove all visibility data for features that became visible
            List<string> idsToRemove = new List<string>();
            foreach (KeyValuePair<string, StylingRule> kv in layer.LayerData.DefaultStyle.StylingRules)
            {
                if (kv.Key.Contains(CartesianTileLayerStyler.VisibilityIdentifier))
                {
                    string objectId = CartesianTileLayerStyler.ObjectIdFromVisibilityStyleRuleName(kv.Key);
                    bool? visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObjectByAttributeTag(objectId);
                    if (visibility == true)
                        idsToRemove.Add(objectId);
                }
            }
            foreach (string id in idsToRemove)
                (layer.Styler as CartesianTileLayerStyler).RemoveVisibilityForSubObjectByAttributeTag(id);
        }

        private void CreateVisibilityItem(string objectID)
        {
            foreach (HiddenObjectsVisibilityItem obj in hiddenObjects)
                if (obj.ObjectId == objectID)
                    return;

            GameObject visibilityObject = Instantiate(hiddenItemPrefab, layerContent);            
            HiddenObjectsVisibilityItem item = visibilityObject.GetComponent<HiddenObjectsVisibilityItem>();
            item.SetObjectId(objectID);
            //because all ui elements will be destroyed on close an anonymous listener is fine here  
            item.ToggleVisibility.AddListener(isOn => OnClickToggle(objectID));
            item.ToggleVisibility.AddListener(visible => ToggleVisibilityForSelectedFeatures(objectID, visible));
            item.OnSelectItem.AddListener(OnClickItem);
            item.OnSelectItem.AddListener(HiddenFeatureSelected);
            item.OnDeselectItem.AddListener(HiddenFeatureDeselected);
            hiddenObjects.Add(item);
        }

        private void UpdateVisibility()
        {
            //update the toggles based on visibility attributes in data
            foreach (HiddenObjectsVisibilityItem item in hiddenObjects)
            {
                bool? visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObjectByAttributeTag(item.ObjectId);
                item.SetToggleState(visibility == true);
            }
        }        

        private void ToggleVisibilityForFeature(string objectId, bool visible)
        {
            //the feature being changed should always have its coordinate within the styling rule!
            Coordinate? coord;
            LayerFeature layerFeature = (layer as CartesianTileLayerGameObject).GetLayerFeatureFromBagId(objectId);
            if(layerFeature != null)
            {               
                coord = (layer.Styler as CartesianTileLayerStyler).GetVisibilityCoordinateForSubObject(layerFeature);
                if(coord == null)
                {
                    Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                    return;
                }
                (layer.Styler as CartesianTileLayerStyler).SetVisibilityForSubObject(layerFeature, visible, (Coordinate)coord);
                return;
            }
            coord = (Coordinate)(layer.Styler as CartesianTileLayerStyler).GetVisibilityCoordinateForSubObjectByTag(objectId);
            if (coord == null)
            {
                Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                return;
            }
            (layer.Styler as CartesianTileLayerStyler).SetVisibilityForSubObjectByAttributeTag(objectId, visible, (Coordinate)coord);            
        }

        private void ToggleVisibilityForSelectedFeatures(string objectId, bool visible)
        {
            foreach (HiddenObjectsVisibilityItem item in selectedItems)
            {
                ToggleVisibilityForFeature(item.ObjectId, visible);
            }

            if (!visible)
                ShowGhostMesh(objectId);
            else
                DestroyGhostMesh();
        }

        private void UpdateSelectedButtonIndex(string objectId)
        {
            currentButtonIndex = -1;
            foreach (HiddenObjectsVisibilityItem item in hiddenObjects)
                if (item.ObjectId == objectId)
                {
                    currentButtonIndex = hiddenObjects.IndexOf(item);
                    break;
                }
        }

        private void OnClickItem(string objectId)
        {        
            //select layer
            UpdateSelectedButtonIndex(objectId);
            ProcessLayerSelection();            
            UpdateSelection();
        }

        private void OnClickToggle(string objectId)
        {
            //if there was already a selection of layers,
            //we should only toggle but not process a new selection of layers
            //but if the selected toggle was outside the selection of layers then process a new selection and select that layer
            if (selectedItems.Count > 1)
            {
                UpdateSelectedButtonIndex(objectId);
                if (!selectedItems.Contains(hiddenObjects[currentButtonIndex]))
                {
                    OnClickItem(objectId);                    
                }
                return;
            }
            //select the new layer
            OnClickItem(objectId);
        }

        private void HiddenFeatureSelected(string objectId)
        {
            Coordinate ? coord = (layer.Styler as CartesianTileLayerStyler).GetVisibilityCoordinateForSubObjectByTag(objectId);
            if (coord == null)
            {
                Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                return;
            }

            LayerFeature layerFeature = (layer as CartesianTileLayerGameObject).GetLayerFeatureFromBagId(objectId);
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
            if(selectedHiddenObject == null) return;

            string objectId = selectedHiddenObject.name;
            if(meshMapping.HasItemWithId(objectId))
            {
                DestroyGhostMesh();
            }
        }

        public void ShowGhostMesh(string objectId)
        {
            DestroyGhostMesh();
            bool? visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObjectByAttributeTag(objectId);
            if (visibility == true)
            {
                return;
            }

            Coordinate? coord = (layer.Styler as CartesianTileLayerStyler).GetVisibilityCoordinateForSubObjectByTag(objectId);
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

                selectedHiddenObject = new GameObject(objectId);
                Mesh mesh = MeshMapping.CreateMeshFromMapping(meshMapping.ObjectMapping, item.ObjectMappingItem, out Vector3 localCentroid);
                MeshFilter mFilter = selectedHiddenObject.AddComponent<MeshFilter>();
                mFilter.mesh = mesh;
                MeshRenderer mRenderer = selectedHiddenObject.AddComponent<MeshRenderer>();
                mRenderer.material = selectionMaterial;
                selectedHiddenObject.transform.position = meshMapping.ObjectMapping.transform.TransformPoint(localCentroid);
                return;
            }
        }

        public void DestroyGhostMesh()
        {
            if (selectedHiddenObject != null)
            {
                Destroy(selectedHiddenObject);
                selectedHiddenObject = null;
            }
        }

        private void HiddenFeatureDeselected(string objectId)
        {
            //lets not destroy the ghost yet as you can move around while having it in view
            //DestroyGhostMesh();
        }

        private bool NoModifierKeyPressed()
        {
            return !LayerUI.AddToSelectionModifierKeyIsPressed() && !LayerUI.SequentialSelectionModifierKeyIsPressed();
        }

        private void ProcessLayerSelection()
        {
            if (LayerUI.SequentialSelectionModifierKeyIsPressed()) 
            {               
                if (selectedItems.Count > 0)
                {
                    int firstSelectedIndex = hiddenObjects.IndexOf(selectedItems[0]);
                    int lastSelectedIndex = hiddenObjects.IndexOf(selectedItems[selectedItems.Count - 1]);                           
                    int targetIndex = currentButtonIndex;
                    int firstIndex = hiddenObjects.IndexOf(firstSelectedItem);
                   
                    bool addSelection = !hiddenObjects[currentButtonIndex].IsSelected;
                    if(!addSelection)
                    {
                        if (firstIndex < targetIndex)
                            for (int i = targetIndex + 1; i <= lastSelectedIndex; i++)
                                hiddenObjects[i].SetSelected(addSelection);
                        else if(firstIndex > targetIndex)
                            for (int i = 0; i < targetIndex; i++)
                                hiddenObjects[i].SetSelected(addSelection);
                        else if(firstIndex == targetIndex)
                            for (int i = 0; i <= lastSelectedIndex; i++)
                                if(i != currentButtonIndex)
                                    hiddenObjects[i].SetSelected(addSelection);
                    }
                    else
                    {
                        if(firstSelectedIndex < targetIndex)
                            for (int i = firstSelectedIndex; i <= targetIndex; i++)
                                hiddenObjects[i].SetSelected(addSelection);
                        else if(lastSelectedIndex > targetIndex)
                            for (int i = targetIndex; i <= lastSelectedIndex; i++)
                                hiddenObjects[i].SetSelected(addSelection);
                    }                    
                }
            }
            if (NoModifierKeyPressed())
            {
                foreach (var item in hiddenObjects)
                    item.SetSelected(false);                
                hiddenObjects[currentButtonIndex].SetSelected(true);
                UpdateSelection();
                //cache the first selected item for sequential selection to always know where to start
                if (selectedItems.Count == 0 || (selectedItems.Count == 1 && firstSelectedItem != hiddenObjects[currentButtonIndex]))
                    firstSelectedItem = hiddenObjects[currentButtonIndex];
            }
        }
        
        private void UpdateSelection()
        {
            selectedItems.Clear();
            foreach (HiddenObjectsVisibilityItem item in hiddenObjects)
                if (item.IsSelected)
                    selectedItems.Add(item);
            if (selectedItems.Count == 0)
                firstSelectedItem = null;
        }
    }
}