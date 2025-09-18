using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.SubObjects;
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
        private GameObject selectedHiddenObject;
        private UnityAction<IMapping> waitForMappingLoaded;

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
            layer.OnStylingApplied.RemoveListener(UpdateVisibility);
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
            item.ToggleVisibility.AddListener(visible => ToggleVisibilityForFeature(objectID, visible));
            item.OnSelectItem.AddListener(OnClickItem);
            item.OnSelectItem.AddListener(HiddenFeatureSelected);
            item.OnDeselectItem.AddListener(HiddenFeatureDeselected);
            hiddenObjects.Add(item);
        }

        private void UpdateVisibility()
        {
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

        private void OnClickItem(string objectId)
        {        
            currentButtonIndex = -1;
            foreach (HiddenObjectsVisibilityItem item in hiddenObjects)
                if (item.ObjectId == objectId)
                {
                    currentButtonIndex = hiddenObjects.IndexOf(item);
                    break;
                }
            if (currentButtonIndex < 0) return;

            ProcessLayerSelection();
            UpdateSelection();
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
                if(waitForMappingLoaded == null)
                {
                    waitForMappingLoaded = (mapping) =>
                    {                        
                        if (mapping is not MeshMapping meshMapping) return;
                        MeshMappingItem item = meshMapping.FindItemById(objectId);
                        if (item == null) return;

                        ObjectSelectorService.MappingTree.OnMappingAdded.RemoveListener(waitForMappingLoaded);
                        HiddenFeatureSelected(objectId);
                    };
                }
                ObjectSelectorService.MappingTree.OnMappingAdded.AddListener(waitForMappingLoaded);
                Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget((Coordinate)coord, cameraDistance);
                return;
            }

            if (layerFeature.Geometry is ObjectMappingItem mapping)
            {
                Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget((Coordinate)coord, cameraDistance);
                bool? visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObjectByAttributeTag(objectId);
                if (visibility == true)
                    return;
               
                List<IMapping> mappings = ObjectSelectorService.MappingTree.Query<MeshMapping>((Coordinate)coord);
                foreach(IMapping m in mappings)
                {
                    if (m is not MeshMapping meshMapping) continue;

                    MeshMappingItem item = meshMapping.FindItemById(objectId);
                    if (item == null) continue;

                    selectedHiddenObject = new GameObject(mapping.objectID);
                    Mesh mesh = MeshMapping.CreateMeshFromMapping(meshMapping.ObjectMapping, mapping, out Vector3 localCentroid);
                    MeshFilter mFilter = selectedHiddenObject.AddComponent<MeshFilter>();
                    mFilter.mesh = mesh;
                    MeshRenderer mRenderer = selectedHiddenObject.AddComponent<MeshRenderer>();
                    mRenderer.material = selectionMaterial;
                    selectedHiddenObject.transform.position = meshMapping.ObjectMapping.transform.TransformPoint(localCentroid);
                    return;
                }                
            }
        }

        private void HiddenFeatureDeselected(string objectId)
        {
            if(selectedHiddenObject != null)
            {
                Destroy(selectedHiddenObject);
                selectedHiddenObject = null;
            }
        }

        private int currentButtonIndex = -1;


        private bool NoModifierKeyPressed()
        {
            return !LayerUI.AddToSelectionModifierKeyIsPressed() && !LayerUI.SequentialSelectionModifierKeyIsPressed();
        }

        private void ProcessLayerSelection()
        {
            if (LayerUI.SequentialSelectionModifierKeyIsPressed() && selectedItems.Count > 0) //if no layers are selected, there will be no reference layer to add to
            {
                int lastIndex = hiddenObjects.IndexOf(selectedItems[selectedItems.Count - 1]); //last element is always the last selected layer                               
                int targetIndex = currentButtonIndex;
                if (lastIndex > targetIndex)
                {
                    int temp = lastIndex;
                    lastIndex = targetIndex;
                    targetIndex = temp;
                }
                bool addSelection = !hiddenObjects[currentButtonIndex].IsSelected;
                for (int i = lastIndex; i <= targetIndex; i++)
                    hiddenObjects[i].SetSelected(addSelection);
                hiddenObjects[currentButtonIndex].SetSelected(!addSelection);
            }
            if (NoModifierKeyPressed())
            {
                foreach (var item in selectedItems)
                    item.SetSelected(false);

            }
            UpdateSelection();
        }

        private List<HiddenObjectsVisibilityItem> selectedItems = new();
        private List<int> selectedIndices = new();
        private void UpdateSelection()
        {
            selectedIndices.Clear();
            selectedItems.Clear();
            foreach (HiddenObjectsVisibilityItem item in hiddenObjects)
                if (item.IsSelected)
                {
                    selectedItems.Add(item);
                    selectedIndices.Add(hiddenObjects.IndexOf(item));
                }
        }
    }
}