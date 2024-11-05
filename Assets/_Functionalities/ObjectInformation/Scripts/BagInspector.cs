using System.Collections;
using System.Collections.Generic;
using Netherlands3D.GeoJSON;
using Netherlands3D.Twin.ObjectInformation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.Interface.BAG
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

		private Coroutine downloadProcess;

		[SerializeField] private RenderedThumbnail buildingThumbnail;

		[SerializeField] private RectTransform contentRectTransform;

		private List<GameObject> dynamicInterfaceItems = new List<GameObject>();
		private Vector3 lastWorldClickedPosition;

		[Header("Practical information fields")]
		[SerializeField] private TMP_Text badIdText;
		[SerializeField] private TMP_Text districtText;
		[SerializeField] private TMP_Text buildYearText;
		[SerializeField] private TMP_Text statusText;

		[SerializeField] private GameObject placeholderPanel;
		[SerializeField] private GameObject contentPanel;

		private Camera mainCamera;
		private CameraInputSystemProvider cameraInputSystemProvider;
		private bool draggedBeforeRelease = false;
		private bool waitingForRelease = false;

		private FeatureSelector featureSelector;
		private SubObjectSelector subObjectSelector;

		private void Awake()
		{
			mainCamera = Camera.main;
			cameraInputSystemProvider = mainCamera.GetComponent<CameraInputSystemProvider>();
			subObjectSelector = GetComponent<SubObjectSelector>();
			featureSelector = GetComponent<FeatureSelector>();

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
				draggedBeforeRelease = Pointer.current.delta.ReadValue().sqrMagnitude > 0;
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
			
			if (subObjectSelector.FindSubObject(ray, out var hit, SelectBuildingOnHit)) return;

			lastWorldClickedPosition = hit.point;

			if (hit.collider == null) return;

			featureSelector.FindFeature(ray, SelectFeatureOnHit);
		}

        private void SelectBuildingOnHit(string bagId)
		{
            ShowObjectInformation();

            subObjectSelector.Select(bagId);
			LoadBuildingContent(bagId);
		}

		private void SelectFeatureOnHit(FeatureMapping mapping)
		{
			ShowObjectInformation();

			featureSelector.Select(mapping);
			LoadFeatureContent(mapping);
		}

		private void Deselect()
		{
            HideObjectInformation();

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
			var loadingIndicator = Instantiate(loadingIndicatorPrefab, contentRectTransform);
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
			// TODO populate the baginspector ui
		}

		#region Supporting methods to load feature content
		// TODO Add this
		#endregion

		#region UGUI methods
		private void ShowObjectInformation()
		{
			addressTitle.gameObject.SetActive(true);
			contentPanel.SetActive(true);
			placeholderPanel.SetActive(false);
		}

		private void HideObjectInformation()
		{
			addressTitle.gameObject.SetActive(false);
			contentPanel.SetActive(false);
			placeholderPanel.SetActive(true);
		}

		private void SpawnLine(string text)
		{
			var spawnedField = Instantiate(addressTemplate, contentRectTransform);
			spawnedField.Set(text);
			spawnedField.gameObject.SetActive(true);
			dynamicInterfaceItems.Add(spawnedField.gameObject);
		}

		private void ClearLines()
		{
			foreach (var item in dynamicInterfaceItems)
			{
				Destroy(item);
			}
			dynamicInterfaceItems.Clear();
		}
		#endregion
	}
}