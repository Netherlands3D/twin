using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.GeoJSON;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Rendering;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using KeyValuePair = Netherlands3D.Twin.UI.KeyValuePair;

namespace Netherlands3D.Functionalities.ObjectInformation
{	
	public class BagInspector : MonoBehaviour
	{
		[Header("GeoJSON Data Sources")]
		[Tooltip("Id replacement string will be replaced")]
		[SerializeField] private string idReplacementString = "{BagID}";
		[SerializeField] private string geoJsonBagRequestURL = "https://service.pdok.nl/lv/bag/wfs/v2_0?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:pand&count=100&outputFormat=xml&srsName=EPSG:28992&filter=%3cFilter%3e%3cPropertyIsEqualTo%3e%3cPropertyName%3eidentificatie%3c/PropertyName%3e%3cLiteral%3e{BagID}%3c/Literal%3e%3c/PropertyIsEqualTo%3e%3c/Filter%3e";
		[SerializeField] private string geoJsonAddressesRequestURL = "https://service.pdok.nl/lv/bag/wfs/v2_0?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:pand&count=100&outputFormat=xml&srsName=EPSG:28992&filter=%3cFilter%3e%3cPropertyIsEqualTo%3e%3cPropertyName%3eidentificatie%3c/PropertyName%3e%3cLiteral%3e{BagID}%3c/Literal%3e%3c/PropertyIsEqualTo%3e%3c/Filter%3e";
		[SerializeField] private string removeFromID = "NL.IMBAG.Pand.";

		[SerializeField] private GameObject addressTitle;
		[SerializeField] private Line addressTemplate;
		[SerializeField] private GameObject loadingIndicatorPrefab;		
		[SerializeField] private GameObject keyValuePairTemplate;

		private Coroutine downloadProcess;

		[SerializeField] private RenderedThumbnail buildingThumbnail;
        [SerializeField] private RenderedThumbnail featureThumbnail;

        [SerializeField] private RectTransform buildingContentRectTransform;
		[SerializeField] private RectTransform featureContentRectTransform;

		private List<GameObject> dynamicInterfaceItems = new List<GameObject>();
		private List<GameObject> keyValueItems = new List<GameObject>();
		

		[Header("Practical information fields")]
		[SerializeField] private TMP_Text badIdText;
		[SerializeField] private TMP_Text districtText;
		[SerializeField] private TMP_Text buildYearText;
		[SerializeField] private TMP_Text statusText;

		[SerializeField] private GameObject placeholderPanel;
		[SerializeField] private GameObject buildingContentPanel;
        [SerializeField] private GameObject featureContentPanel;

		private ObjectSelector objectSelector;      

        private void Awake()
		{
			keyValuePairTemplate.gameObject.SetActive(false);

			HideObjectInformation();

			objectSelector = FindAnyObjectByType<ObjectSelector>();
			objectSelector.SelectSubObjectWithBagId.AddListener(SelectBuildingOnHit);
			objectSelector.SelectFeature.AddListener(SelectFeatureOnHit);
            //todo, update when the object information mode is opened so this will update with the current selected object
        }

        private void SelectBuildingOnHit(MeshMapping mapping, string bagId)
		{
            ShowObjectInformation();
			LoadBuildingContent(bagId);
		}
		
		private void SelectFeatureOnHit(FeatureMapping mapping)
		{
		    ShowFeatureInformation();
			LoadFeatureContent(mapping);
			RenderThumbnailForFeature(mapping);            
        }

		private void RenderThumbnailForFeature(FeatureMapping mapping)
		{
            if (mapping.VisualisationLayer is GeoJSONPolygonLayer)
            {
                GeoJSONPolygonLayer polygonLayer = mapping.VisualisationLayer as GeoJSONPolygonLayer;
                List<Mesh> meshes = mapping.FeatureMeshes;
                
                PolygonVisualisation pv = polygonLayer.GetPolygonVisualisationByMesh(meshes);
                Bounds currentObjectBounds = new Bounds(pv.transform.position, meshes[0].bounds.size);
                featureThumbnail.RenderThumbnail(currentObjectBounds);
				return;
            }

			if (mapping.SelectedGameObject == null) return;

            if (mapping.VisualisationLayer is GeoJSONLineLayer)
            {
                Vector3 centroid = Vector3.zero;
                Vector3[] vertices = mapping.FeatureMeshes[0].vertices;
                foreach (Vector3 v in vertices)
                    centroid += v;
                centroid /= vertices.Length;
                Vector3 size = mapping.FeatureMeshes[0].bounds.size;
                size.y = Mathf.Min(50, size.y);
                size.x = Mathf.Clamp(size.x, 50, 100);
                size.z = Mathf.Clamp(size.z, 50, 100);
                Bounds currentObjectBounds = new Bounds(mapping.SelectedGameObject.transform.position + centroid, size);
                featureThumbnail.RenderThumbnail(currentObjectBounds);
            }
            else if (mapping.VisualisationLayer is GeoJSONPointLayer)
            {
                Bounds currentObjectBounds = new Bounds(mapping.SelectedGameObject.transform.position + mapping.FeatureMeshes[0].vertices[0] - mapping.FeatureMeshes[0].bounds.center, Vector3.one * 50);
                featureThumbnail.RenderThumbnail(currentObjectBounds);
            }
        }

		private void Deselect()
		{
            HideObjectInformation();
			HideFeatureInformation();

            objectSelector.Deselect();
		}

		private void OnDestroy()
		{
            objectSelector.SelectSubObjectWithBagId.RemoveListener(SelectBuildingOnHit);
            objectSelector.SelectFeature.RemoveListener(SelectFeatureOnHit);

            Deselect();
		}

		public void LoadBuildingContent(string bagID)
		{
			DownloadGeoJSONProperties(new List<string>() { bagID });
		}
		
		private void DownloadGeoJSONProperties(List<string> bagIDs)
		{
			if (bagIDs.Count <= 0) return;
			
			var ID = bagIDs[0];
			if (removeFromID.Length > 0) ID = ID.Replace(removeFromID, "");

			if (downloadProcess != null)
			{
				StopCoroutine(downloadProcess);
			}

			downloadProcess = StartCoroutine(GetBagIDData(ID));
		}

		private IEnumerator GetBagIDData(string bagID)
		{
			//Get fast bag data
			var loadingIndicator = Instantiate(loadingIndicatorPrefab, buildingContentRectTransform);
			yield return GetBAGData(bagID);
			loadingIndicator.transform.SetAsLastSibling();

			//Adressess (slower request next)
			yield return GetAddresses(bagID);
			Destroy(loadingIndicator);
		}

		private IEnumerator GetBAGData(string bagID)
		{
			var requestUrl = geoJsonBagRequestURL.Replace(idReplacementString, bagID);
			var webRequest = UnityWebRequest.Get(requestUrl);

			yield return webRequest.SendWebRequest();

			if (webRequest.result != UnityWebRequest.Result.Success)
			{
				SpawnLine("Geen BAG data gevonden");
				yield break;
			}

			ClearLines();

			GeoJSONStreamReader customJsonHandler = new GeoJSONStreamReader(webRequest.downloadHandler.text);
			while (customJsonHandler.GotoNextFeature())
			{
				var properties = customJsonHandler.GetProperties();

				badIdText.text = properties["identificatie"].ToString();
				buildYearText.text = properties["bouwjaar"].ToString();
				statusText.text = properties["status"].ToString();

				PointerToWorldPosition pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();

                //TODO: Use bbox and geometry.coordinates from GeoJSON object to create bounds to render thumbnail
                Bounds currentObjectBounds = new Bounds(pointerToWorldPosition.WorldPoint, Vector3.one * 50.0f);
				buildingThumbnail.RenderThumbnail(currentObjectBounds);
			}
		}

		private IEnumerator GetAddresses(string bagID)
		{
			var requestUrl = geoJsonAddressesRequestURL.Replace(idReplacementString, bagID);
			var webRequest = UnityWebRequest.Get(requestUrl);
			yield return webRequest.SendWebRequest();

			if (webRequest.result != UnityWebRequest.Result.Success)
			{
				SpawnLine("Geen adressen gevonden");
				yield break;
			}

			ClearLines();

			GeoJSONStreamReader customJsonHandler = new GeoJSONStreamReader(webRequest.downloadHandler.text);
			bool gotDistrict = false;
			while (customJsonHandler.GotoNextFeature())
			{
				var properties = customJsonHandler.GetProperties();

				//Use first address result to determine district
				if (!gotDistrict)
				{
					districtText.text = properties["openbare_ruimte"].ToString();
					gotDistrict = true;
				}
	
				SpawnLine(
					$"{properties["openbare_ruimte"]} {properties["huisnummer"]} {properties["huisletter"]}{properties["toevoeging"]}"
				);
			}
		}

		private void LoadFeatureContent(FeatureMapping mapping)
		{
			ClearKeyVaueItems();
			Dictionary<string, object> properties = mapping.Feature.Properties as Dictionary<string, object>;
			foreach (KeyValuePair<string, object> property in properties)
			{
				if(property.Value == null) continue;

				SpawnKeyValue(property.Key, property.Value.ToString());
			}
		}

		private void ShowObjectInformation()
		{
			buildingContentPanel.SetActive(true);
			placeholderPanel.SetActive(false);
			featureContentPanel.SetActive(false);		
		}

		private void HideObjectInformation()
		{
			buildingContentPanel.SetActive(false);
			placeholderPanel.SetActive(true);
			featureContentPanel.SetActive(false);
			ClearLines();
		}

		private void ShowFeatureInformation()
		{
            buildingContentPanel.SetActive(false);
            placeholderPanel.SetActive(false);
            featureContentPanel.SetActive(true);			
        }

		private void HideFeatureInformation()
		{
            buildingContentPanel.SetActive(false);
            placeholderPanel.SetActive(true);
            featureContentPanel.SetActive(false);
			ClearKeyVaueItems();
        }

		private void SpawnLine(string text)
		{
			var spawnedField = Instantiate(addressTemplate, buildingContentRectTransform);
			spawnedField.Set(text);
			spawnedField.gameObject.SetActive(true);
			dynamicInterfaceItems.Add(spawnedField.gameObject);
		}

		private void SpawnKeyValue(string key, string value)
		{
			GameObject keyValue = Instantiate(keyValuePairTemplate, featureContentRectTransform);
			KeyValuePair keyValuePair = keyValue.GetComponent<KeyValuePair>();
			AdjustHeightOnTextChange adjustHeightOnTextChange = keyValue.GetComponent<AdjustHeightOnTextChange>();
			keyValuePair.Set(key, value);
			keyValue.gameObject.SetActive(true);
			StartCoroutine(WaitForNextFrame(() => { adjustHeightOnTextChange.UpdateHeight(); }));			
			keyValueItems.Add(keyValue.gameObject);
		}

        private IEnumerator WaitForNextFrame(Action onNextFrame)
        {
            yield return new WaitForEndOfFrame();
            onNextFrame.Invoke();
        }

        private void ClearLines()
		{
			foreach (var item in dynamicInterfaceItems)
			{
				Destroy(item);
			}
			dynamicInterfaceItems.Clear();
		}

		private void ClearKeyVaueItems()
		{
			foreach (var item in keyValueItems)
			{
				Destroy(item);
			}
			keyValueItems.Clear();
		}
    }
}