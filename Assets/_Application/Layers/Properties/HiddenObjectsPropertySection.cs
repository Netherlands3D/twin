using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    [PropertySection(typeof(HiddenObjectsPropertyData), Symbolizer.VisibilityProperty)]
    public class HiddenObjectsPropertySection : MonoBehaviour, IVisualizationWithPropertyData, IMultiSelectable
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject hiddenItemPrefab;
        [SerializeField] private RectTransform layerContent;
        [SerializeField] private float cameraDistance = 150f;
        [SerializeField] private Material selectionMaterial;

        private GameObject selectedGhostObject;
        private UnityAction<IMapping> waitForMappingLoaded;
        
        public int SelectedButtonIndex { get; set; } = -1;
        public List<ISelectable> SelectedItems { get; } = new();
        public List<ISelectable> Items { get; set; } = new();
        public ISelectable FirstSelectedItem { get; set; }

        private HiddenObjectsPropertyData stylingPropertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.GetDefaultStylingPropertyData<HiddenObjectsPropertyData>();
            if (stylingPropertyData == null) return;

            CreateItems();
            UpdateVisibility();
            stylingPropertyData.OnStylingChanged.AddListener(UpdateVisibility);

            ObjectSelectorService.MappingTree.OnMappingRemoved.AddListener(OnMappingRemoved);
            //deselect any selected feature in the world when opening the hidden feature panel
            ObjectSelectorService selector = ServiceLocator.GetService<ObjectSelectorService>();
            selector.Deselect();

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
            foreach(KeyValuePair<string, StylingRule> kv in stylingPropertyData.StylingRules)
            {
                if(kv.Key.Contains(HiddenObjectsPropertyData.VisibilityIdentifier))
                {
                    string objectId = stylingPropertyData.ObjectIdFromVisibilityStyleRuleName(kv.Key);                    
                    bool? visibility = stylingPropertyData.GetVisibilityForSubObjectByAttributeTag(objectId);
                    if (visibility == false)
                        CreateVisibilityItem(objectId);
                }
            }
        }

        private void CreateVisibilityItem(string objectID)
        {
            foreach (HiddenObjectsVisibilityItem obj in Items.OfType<HiddenObjectsVisibilityItem>())
                if (obj.ObjectId == objectID)
                    return;

            GameObject visibilityObject = Instantiate(hiddenItemPrefab, layerContent);            
            HiddenObjectsVisibilityItem item = visibilityObject.GetComponent<HiddenObjectsVisibilityItem>();
            item.SetObjectId(objectID);
            //because all ui elements will be destroyed on close an anonymous listener is fine here  
            item.ToggleVisibility.AddListener(isOn => OnClickToggle(objectID));
            item.ToggleVisibility.AddListener(visible => ToggleVisibilityForSelectedFeatures(objectID, visible));
            item.OnSelectItem.AddListener(OnClickItem);
            Items.Add(item);
        }

        private void UpdateVisibility()
        {
            //update the toggles based on visibility attributes in data
            foreach (HiddenObjectsVisibilityItem item in Items.OfType<HiddenObjectsVisibilityItem>())
            {
                bool? visibility = stylingPropertyData.GetVisibilityForSubObjectByAttributeTag(item.ObjectId);
                item.SetToggleState(visibility == true);
            }
        }        

        private void ToggleVisibilityForFeature(string objectId, bool visible)
        {
            //the feature being changed should always have its coordinate within the styling rule!
            Coordinate? coord;
            LayerFeature layerFeature = CartesianTileLayerGameObject.GetLayerFeatureFromBagId(objectId);
            if(layerFeature != null)
            {               
                coord = stylingPropertyData.GetVisibilityCoordinateForSubObject(layerFeature);
                if(coord == null)
                {
                    Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                    return;
                }
                stylingPropertyData.SetVisibilityForSubObject(layerFeature, visible, (Coordinate)coord);
                return;
            }
            coord = (Coordinate)stylingPropertyData.GetVisibilityCoordinateForSubObjectByTag(objectId);
            if (coord == null)
            {
                Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                return;
            }
            stylingPropertyData.SetVisibilityForSubObjectByAttributeTag(objectId, visible, (Coordinate)coord);            
        }

        private void ToggleVisibilityForSelectedFeatures(string objectId, bool visible)
        {
            foreach (HiddenObjectsVisibilityItem item in SelectedItems.OfType<HiddenObjectsVisibilityItem>())
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
            SelectedButtonIndex = -1;
            foreach (HiddenObjectsVisibilityItem item in Items.OfType<HiddenObjectsVisibilityItem>())
                if (item.ObjectId == objectId)
                {
                    SelectedButtonIndex = Items.IndexOf(item);
                    break;
                }
        }

        private void OnClickItem(string objectId)
        {        
            //select layer
            UpdateSelectedButtonIndex(objectId);
            MultiSelectionUtility.ProcessLayerSelection(this, anythingSelected => 
            { 
                if(anythingSelected)
                    HiddenFeatureSelected(objectId);
            });
        }

        private void OnClickToggle(string objectId)
        {
            //if there was already a selection of layers,
            //we should only toggle but not process a new selection of layers
            //but if the selected toggle was outside the selection of layers then process a new selection and select that layer
            UpdateSelectedButtonIndex(objectId);
            if (SelectedItems.Count > 1)
            {
                if (!SelectedItems.Contains(Items[SelectedButtonIndex]))
                {
                    OnClickItem(objectId);
                }
                return;
            }
            //same item selected do nothing
            if (SelectedItems.Count == 1 && SelectedItems[0] == Items[SelectedButtonIndex])
            {
                return;
            }
            //select the new layer
            OnClickItem(objectId);
        }

        private void HiddenFeatureSelected(string objectId)
        {
            Coordinate ? coord = stylingPropertyData.GetVisibilityCoordinateForSubObjectByTag(objectId);
            if (coord == null)
            {
                Debug.LogError("the styling rule does not contain a coordinate for this feature!");
                return;
            }

            LayerFeature layerFeature = CartesianTileLayerGameObject.GetLayerFeatureFromBagId(objectId);
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
            bool? visibility = stylingPropertyData.GetVisibilityForSubObjectByAttributeTag(objectId);
            if (visibility == true)
            {
                return;
            }

            Coordinate? coord = stylingPropertyData.GetVisibilityCoordinateForSubObjectByTag(objectId);
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
                Destroy(selectedGhostObject);
                selectedGhostObject = null;
            }
        }

        private void OnDestroy()
        {
            DestroyGhostMesh();
            stylingPropertyData.OnStylingChanged.RemoveListener(UpdateVisibility);
            ObjectSelectorService.MappingTree.OnMappingRemoved.RemoveListener(OnMappingRemoved);

            //remove all visibility data for features that became visible
            List<string> idsToRemove = new List<string>();
            foreach (KeyValuePair<string, StylingRule> kv in stylingPropertyData.StylingRules)
            {
                if (kv.Key.Contains(HiddenObjectsPropertyData.VisibilityIdentifier))
                {
                    string objectId = stylingPropertyData.ObjectIdFromVisibilityStyleRuleName(kv.Key);
                    bool? visibility = stylingPropertyData.GetVisibilityForSubObjectByAttributeTag(objectId);
                    if (visibility == true)
                        idsToRemove.Add(objectId);
                }
            }
            foreach (string id in idsToRemove)
                stylingPropertyData.RemoveVisibilityForSubObjectByAttributeTag(id);
        }
    }
}