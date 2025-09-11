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
            item.OnClickHiddenItem.AddListener(feature => HiddenFeatureSelected(layerFeature));

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

        private void HiddenFeatureSelected(LayerFeature layerFeature)
        {
            if (layerFeature.Geometry is ObjectMappingItem mapping)
            {
                string coordString = layerFeature.GetAttribute(CartesianTileLayerStyler.VisibilityPositionIdentifier);
                if(coordString != null)
                {
                    Coordinate coord = CartesianTileLayerStyler.VisibilityPositionFromIdentifierValue(coordString);
                    Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget(coord, cameraDistance);
                }
            }
        }

        private void DebugVertices(Vector3[] vertices, int start, int length, Transform transform)
        {
            for (int i = start; i < start + length; i++)
            {
                Vector3 vertexWorld = transform.TransformPoint(vertices[i]);

                GameObject testPos = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vertexWorld.y = 50;
                testPos.transform.position = vertexWorld;
                testPos.GetComponent<MeshRenderer>().material.color = Color.green;
                testPos.transform.localScale = Vector3.one * 5;
            }
        }

        
    }
}