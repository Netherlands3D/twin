using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using GG.Extensions;
using Netherlands3D.GeoJSON;
using Netherlands3D.SelectionTools;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Cameras.Input;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Rendering;
using Netherlands3D.Twin.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using KeyValuePair = Netherlands3D.Twin.UI.KeyValuePair;

namespace Netherlands3D.Functionalities.ObjectInformation
{
	[RequireComponent(typeof(FeatureSelector))]
	[RequireComponent(typeof(SubObjectSelector))]
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
		private Vector3 lastWorldClickedPosition;

		[Header("Practical information fields")]
		[SerializeField] private TMP_Text badIdText;
		[SerializeField] private TMP_Text districtText;
		[SerializeField] private TMP_Text buildYearText;
		[SerializeField] private TMP_Text statusText;

		[SerializeField] private GameObject placeholderPanel;
		[SerializeField] private GameObject buildingContentPanel;
        [SerializeField] private GameObject featureContentPanel;

        private Camera mainCamera;
		private CameraInputSystemProvider cameraInputSystemProvider;
		private bool draggedBeforeRelease = false;
		private bool waitingForRelease = false;

		private FeatureSelector featureSelector;
		private SubObjectSelector subObjectSelector;

		private List<GameObject> orderedMappings = new List<GameObject>();
		private float minClickDistance = 10;
		private float minClickTime = 0.5f;
		private float lastTimeClicked = 0;
		private int currentSelectedMappingIndex = -1;
		private bool filterDuplicateFeatures = true;

        private void Awake()
		{
			mainCamera = Camera.main;
			cameraInputSystemProvider = mainCamera.GetComponent<CameraInputSystemProvider>();
			subObjectSelector = GetComponent<SubObjectSelector>();
			featureSelector = GetComponent<FeatureSelector>();

			keyValuePairTemplate.gameObject.SetActive(false);

			HideObjectInformation();
		}

		private void Update()
		{
			if (IsClicked())
			{
				FindObjectMapping();
			}
		}

		private bool IsClicked()
		{
			var click = Pointer.current.press.wasPressedThisFrame;

			if (click)
			{
				waitingForRelease = true;
				draggedBeforeRelease = false;
				return false;
			}

			if (waitingForRelease && !draggedBeforeRelease)
			{
				//Check if next release should be ignored ( if we dragged too much )
				draggedBeforeRelease = Pointer.current.delta.ReadValue().sqrMagnitude > 0.5f;
			}

			if (Pointer.current.press.wasReleasedThisFrame == false) return false;
			
			waitingForRelease = false;

			if (draggedBeforeRelease) return false;

			return cameraInputSystemProvider.OverLockingObject == false;
		}

        /// <summary>
        /// Find objectmapping by raycast and get the BAG ID
        /// </summary>
        private void FindObjectMapping()
		{
			Deselect();		

			// Raycast from pointer position using main camera
			var position = Pointer.current.position.ReadValue();
			var ray = mainCamera.ScreenPointToRay(position);

			//the following method calls need to run in order!
			string bagId = subObjectSelector.FindSubObject(ray, out var hit);			
			if (hit.collider == null) return;

			bool clickedSamePosition = Vector3.Distance(lastWorldClickedPosition, hit.point) < minClickDistance;
            lastWorldClickedPosition = hit.point; 
			
			bool refreshSelection = Time.time - lastTimeClicked > minClickTime;
			lastTimeClicked = Time.time;

			if (!clickedSamePosition || refreshSelection)
			{
                featureSelector.SetBlockingObjectMapping(subObjectSelector.Object, lastWorldClickedPosition);
                featureSelector.FindFeature(ray);

				orderedMappings.Clear();
                Dictionary<GameObject, int> mappings = new Dictionary<GameObject, int>();		
                //lets order all mappings by layerorder (rootindex) from layerdata
                if (featureSelector.HasFeatureMapping)
				{
					List<Feature> filterDuplicates = new List<Feature>();
					foreach (FeatureMapping feature in featureSelector.FeatureMappings)
					{
						if (feature.VisualisationParent.LayerData.ActiveInHierarchy)
						{
							if(filterDuplicateFeatures)
							{
								if (!filterDuplicates.Contains(feature.Feature))
									filterDuplicates.Add(feature.Feature);
								else
									continue;
							}

							mappings.TryAdd(feature.gameObject, feature.VisualisationParent.LayerData.RootIndex);
						}
                    }
                }
				if (subObjectSelector.HasObjectMapping)
				{
					LayerGameObject subObjectParent = subObjectSelector.Object.transform.GetComponentInParent<LayerGameObject>();
					if (subObjectParent != null)
					{
						if(subObjectParent.LayerData.ActiveInHierarchy)
							mappings.TryAdd(subObjectSelector.Object.gameObject, subObjectParent.LayerData.RootIndex);
					}
				}

                orderedMappings = mappings.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToList();
				
                currentSelectedMappingIndex = 0;
			}
			else
			{
				//clicking at same position so lets toggle through the list
				currentSelectedMappingIndex++;
				if (currentSelectedMappingIndex >= orderedMappings.Count)
					currentSelectedMappingIndex = 0;
			}

			if (orderedMappings.Count == 0) return;

			//Debug.Log(orderedMappings[currentSelectedMappingIndex]);

			GameObject selection = orderedMappings[currentSelectedMappingIndex];
			if (selection.GetComponent<ObjectMapping>())
			{				
				SelectBuildingOnHit(bagId);
			}
			else
			{
                SelectFeatureOnHit(selection.GetComponent<FeatureMapping>());				
			}
        }

        private void SelectBuildingOnHit(string bagId)
		{
            ShowObjectInformation();

            subObjectSelector.Select(bagId);
			LoadBuildingContent(bagId);
		}
		
		private void SelectFeatureOnHit(FeatureMapping mapping)
		{
		    ShowFeatureInformation();
			ExtrudePointsForSelection(mapping);
            featureSelector.Select(mapping);
			LoadFeatureContent(mapping);
			RenderThumbnailForFeature(mapping);            
        }

		//we need to make the points visible, so temporary extrude the feature point mesh to a disc 
		//TODO reduce the dics back to a vertex after deselecting
		private void ExtrudePointsForSelection(FeatureMapping mapping)
		{
            if (mapping.VisualisationLayer is GeoJSONPointLayer)
            {
                float radius = ((GeoJSONPointLayer)mapping.VisualisationLayer).PointRenderer3D.MeshScale;
                List<Mesh> meshes = mapping.FeatureMeshes;
                int segments = 12;
                for (int i = 0; i < meshes.Count; i++)
                {
                    Vector3 centerVertex = Vector3.zero;
                    Vector3[] vertices = new Vector3[segments + 1];
                    int[] triangles = new int[segments * 3];                    
                    float angleIncrement = 360.0f / segments;
                    for (int j = 0; j < segments; j++)
                    {
                        float angle = Mathf.Deg2Rad * (j * angleIncrement);
                        float x = Mathf.Cos(angle) * radius;
                        float z = Mathf.Sin(angle) * radius;
                        vertices[j + 1] = new Vector3(centerVertex.x + x, centerVertex.y, centerVertex.z + z);
                        triangles[j * 3] = 0;
                        triangles[j * 3 + 1] = (j + 2 > segments) ? 1 : j + 2;
                        triangles[j * 3 + 2] = j + 1;
                    }
                    meshes[i].vertices = vertices;
                    meshes[i].triangles = triangles;
                }
                mapping.SetMeshes(meshes);
				//todo make this more efficient through the featuremapper
                mapping.gameObject.GetComponent<MeshFilter>().mesh = meshes[0];
                mapping.gameObject.GetOrAddComponent<MeshRenderer>().material = ((GeoJSONPointLayer)mapping.VisualisationLayer).PointRenderer3D.Material;
            }
        }

		private void RenderThumbnailForFeature(FeatureMapping mapping)
		{
            if (mapping.VisualisationLayer is GeoJSONPolygonLayer)
            {
                GeoJSONPolygonLayer polygonLayer = mapping.VisualisationLayer as GeoJSONPolygonLayer;
                List<Mesh> meshes = mapping.FeatureMeshes;
                for (int j = 0; j < meshes.Count; j++)
                {
                    PolygonVisualisation pv = polygonLayer.GetPolygonVisualisationByMesh(meshes);
                    Bounds currentObjectBounds = new Bounds(pv.transform.position, meshes[j].bounds.size);
                    featureThumbnail.RenderThumbnail(currentObjectBounds);
                    break;
                }
            }
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
                Bounds currentObjectBounds = new Bounds(mapping.gameObject.transform.position + centroid, size);
                featureThumbnail.RenderThumbnail(currentObjectBounds);
            }
            else if (mapping.VisualisationLayer is GeoJSONPointLayer)
            {
                Bounds currentObjectBounds = new Bounds(mapping.gameObject.transform.position + mapping.FeatureMeshes[0].vertices[0] - mapping.FeatureMeshes[0].bounds.center, Vector3.one * 50);
                featureThumbnail.RenderThumbnail(currentObjectBounds);
            }
        }

		private void Deselect()
		{
            HideObjectInformation();
			HideFeatureInformation();

            subObjectSelector.Deselect();
			featureSelector.Deselect();
		}

		private void OnDestroy()
		{
			Deselect();
		}

		public void LoadBuildingContent(string bagID)
		{
			DownloadGeoJSONProperties(new List<string>() { bagID });
		}

		#region Supporting methods to load building content
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

				//TODO: Use bbox and geometry.coordinates from GeoJSON object to create bounds to render thumbnail
				Bounds currentObjectBounds = new Bounds(lastWorldClickedPosition, Vector3.one * 50.0f);
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
		#endregion

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

		#region Supporting methods to load feature content
		// TODO Add this
		#endregion

		#region UGUI methods
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
		#endregion
	}
}