using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Utility;
using UnityEngine;
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

        private Dictionary<string, HiddenObjectsVisibilityItem> hiddenObjects = new();

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

        private void OnDestroy()
        {
            layer.OnStylingApplied.RemoveListener(UpdateVisibility);

            //TODO we need to actively clear any layerfeatures visibility position data on destroy
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
            foreach(var layerFeature in layer.LayerFeatures.Values)
            {
                bool? visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObject(layerFeature);
                if(visibility == false)
                    CreateVisibilityItem(layerFeature);
            }
        }

        private void CreateVisibilityItem(LayerFeature layerFeature)
        {
            string objectID = layerFeature.GetAttribute(CartesianTileLayerStyler.VisibilityIdentifier);
            if (hiddenObjects.ContainsKey(objectID)) return;

            GameObject visibilityObject = Instantiate(hiddenItemPrefab, layerContent);
            
            HiddenObjectsVisibilityItem item = visibilityObject.GetComponent<HiddenObjectsVisibilityItem>();
            item.SetObjectId(objectID);
            item.SetLayerFeature(layerFeature);
            //because all ui elements will be destroyed on close an anonymous listener is fine here              
            item.ToggleVisibility.AddListener(visible => SetVisibilityForFeature(layerFeature, visible));
            item.OnSelectItem.AddListener(feature => HiddenFeatureSelected(layerFeature));
            item.OnDeselectItem.AddListener(feature => HiddenFeatureDeselected(layerFeature));

            hiddenObjects.Add(objectID, item);
        }

        private void UpdateVisibility()
        {
            foreach (KeyValuePair<string, HiddenObjectsVisibilityItem> kv in hiddenObjects)
            {
                bool? visibility = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObject(kv.Value.LayerFeature);
                kv.Value.SetToggleState(visibility == true);
            }
        }        

        private void SetVisibilityForFeature(LayerFeature layerFeature, bool visible)
        {
            (layer.Styler as CartesianTileLayerStyler).SetVisibilityForSubObject(layerFeature, visible);           
        }

        private GameObject selectedHiddenObject;

        private void HiddenFeatureSelected(LayerFeature layerFeature)
        {
            if (layerFeature.Geometry is ObjectMappingItem mapping)
            {
                string coordString = layerFeature.GetAttribute(CartesianTileLayerStyler.VisibilityPositionIdentifier);
                if(coordString != null)
                {
                    Coordinate coord = CartesianTileLayerStyler.VisibilityPositionFromIdentifierValue(coordString);
                    Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget(coord, cameraDistance);

                    CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;

                    selectedHiddenObject = new GameObject(mapping.objectID);
                    MeshFilter mFilter = selectedHiddenObject.AddComponent<MeshFilter>();
                    ObjectMapping objectMapping = cartesianTileLayerGameObject.FindObjectMapping(mapping);
                    Mesh mesh = CartesianTileLayerGameObject.CreateMeshFromMapping(objectMapping, mapping, out Vector3 localCentroid);
                    mFilter.mesh = mesh;
                    MeshRenderer mRenderer = selectedHiddenObject.AddComponent<MeshRenderer>();
                    mRenderer.material = selectionMaterial;
                    selectedHiddenObject.transform.position = objectMapping.transform.TransformPoint(localCentroid);
                }                
            }
        }

        private void HiddenFeatureDeselected(LayerFeature layerFeature)
        {
            if(selectedHiddenObject != null)
            {
                Destroy(selectedHiddenObject);
                selectedHiddenObject = null;
            }
            if (layerFeature.Geometry is ObjectMappingItem mapping)
            {
               
            }
        }

        
        
    }
}