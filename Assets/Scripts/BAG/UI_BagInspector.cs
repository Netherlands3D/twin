/*
*  Copyright (C) X Gemeente
*                X Amsterdam
*                X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://github.com/Amsterdam/Netherlands3D/blob/main/LICENSE.txt
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.GeoJSON;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;


namespace Netherlands3D.Interface.BAG
{
	/// <summary>
	/// Loads GeoJSON from an URL using unique ID's, and invoke events for
	/// returned key/value pairs for all properties.
	/// </summary>
	public class UI_BagInspector : MonoBehaviour
	{
		[SerializeField]
		private string idReplacementString = "{BagID}";

		[Tooltip("Id replacement string will be replaced")]

		[Header("GeoJSON Data Sources")]
		[SerializeField] private string geoJsonBagRequestURL = "https://service.pdok.nl/lv/bag/wfs/v2_0?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:pand&count=100&outputFormat=xml&srsName=EPSG:28992&filter=%3cFilter%3e%3cPropertyIsEqualTo%3e%3cPropertyName%3eidentificatie%3c/PropertyName%3e%3cLiteral%3e{BagID}%3c/Literal%3e%3c/PropertyIsEqualTo%3e%3c/Filter%3e";
		[SerializeField] private string geoJsonAddressesRequestURL = "https://service.pdok.nl/lv/bag/wfs/v2_0?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:pand&count=100&outputFormat=xml&srsName=EPSG:28992&filter=%3cFilter%3e%3cPropertyIsEqualTo%3e%3cPropertyName%3eidentificatie%3c/PropertyName%3e%3cLiteral%3e{BagID}%3c/Literal%3e%3c/PropertyIsEqualTo%3e%3c/Filter%3e";
		
		
		[SerializeField] private string removeFromID = "NL.IMBAG.Pand.";
		[SerializeField] private UI_Line addressTemplate;
		[SerializeField] private GameObject loadingIndicatorPrefab;

		private Coroutine downloadProcess;

		[SerializeField] private RawImage thumbnail;
		[SerializeField] private RectTransform contentRectTransform;

		private List<GameObject> dynamicInterfaceItems = new List<GameObject>();

		private bool selectionlayerExists = false;

		[Header("Practical information fields")]
		[SerializeField] private TMP_Text badIdText;
		[SerializeField] private TMP_Text districtText;
		[SerializeField] private TMP_Text buildYearText;
		[SerializeField] private TMP_Text statusText;

		private void Awake() {
			addressTemplate.gameObject.SetActive(false);
		}
		private void Update()
        {
            //Listen to Pointer.current click and check what object raycast has clicked
            var click = Pointer.current.press.wasReleasedThisFrame;

            if (!click) return;

            FindObjectMapping();
        }

		/// <summary>
		/// Find objectmapping by raycast and get the BAG ID
		/// </summary>
		private void FindObjectMapping()
		{
			//Raycast from pointer position using main camera
			var position = Pointer.current.position.ReadValue();
			var ray = Camera.main.ScreenPointToRay(position);
			if (Physics.Raycast(ray, out RaycastHit hit, 100000f))
			{
				var objectMapping = hit.collider.gameObject.GetComponent<ObjectMapping>();
				if (objectMapping != null)
				{
					var hitIndex = hit.triangleIndex;
					var id = objectMapping.getObjectID(hitIndex);
					var objectIdAndColor = new Dictionary<string, Color>
					{
						{ id, new Color(1, 0, 0, 0) }
					};

					if (selectionlayerExists)
						GeometryColorizer.RemoveCustomColorSet(0);

					selectionlayerExists = true;
					GeometryColorizer.InsertCustomColorSet(0, objectIdAndColor);

					GetBAGID(id);
				}
			}
		}

        public void GetBAGID(string bagID)
		{
			DownloadGeoJSONProperties(new List<string>() { bagID });
		}
		
		private void DownloadGeoJSONProperties(List<string> bagIDs)
		{
			if (bagIDs.Count > 0)
			{
				var ID = bagIDs[0];
				if(removeFromID.Length > 0) ID = ID.Replace(removeFromID, "");

				if (downloadProcess != null)			
					StopCoroutine(downloadProcess);


				downloadProcess = StartCoroutine(GetBagIDData(ID));
			}
		}

		private void ClearOldItems()
		{
			foreach (var item in dynamicInterfaceItems)
			{
				Destroy(item);
			}
			dynamicInterfaceItems.Clear();
		}

		private IEnumerator GetBagIDData(string bagID)
		{
			//Get fast bag data
			var loadingIndicator = Instantiate(loadingIndicatorPrefab, contentRectTransform);
			yield return GetBAGData(bagID);
			loadingIndicator.transform.SetAsLastSibling();
			
			//Adressess (slower request next)
			yield return GetAdresses(bagID);
			Destroy(loadingIndicator);
		}

		private IEnumerator GetBAGData(string bagID)
		{
			var requestUrl = geoJsonBagRequestURL.Replace(idReplacementString, bagID);
			var webRequest = UnityWebRequest.Get(requestUrl);
			Debug.Log(requestUrl);
			yield return webRequest.SendWebRequest();

			if (webRequest.result == UnityWebRequest.Result.Success)
			{
				ClearOldItems();

				GeoJSONStreamReader customJsonHandler = new GeoJSONStreamReader(webRequest.downloadHandler.text);
				while (customJsonHandler.GotoNextFeature())
				{
					var properties = customJsonHandler.GetProperties();	
					badIdText.text = properties["identificatie"].ToString();	
					buildYearText.text = properties["bouwjaar"].ToString();	
					statusText.text = properties["status"].ToString();	//CHECK
				}
			}
			else
			{
				SpawnNewLine("Geen BAG data gevonden");
			}
		}

		private IEnumerator GetAdresses(string bagID)
		{
			var requestUrl = geoJsonAddressesRequestURL.Replace(idReplacementString, bagID);
			var webRequest = UnityWebRequest.Get(requestUrl);
			Debug.Log(requestUrl);
			yield return webRequest.SendWebRequest();

			if (webRequest.result == UnityWebRequest.Result.Success)
			{
				ClearOldItems();
				
				GeoJSONStreamReader customJsonHandler = new GeoJSONStreamReader(webRequest.downloadHandler.text);
				while (customJsonHandler.GotoNextFeature())
                {
                    var properties = customJsonHandler.GetProperties();
                    bool gotDistrict = false;

					
                    //Spawn address
                    var addressText = $"{properties["openbare_ruimte"]} {properties["huisnummer"]} {properties["huisletter"]}{properties["toevoeging"]}";
                    SpawnNewLine(addressText);
                }
            }
			else
			{
				SpawnNewLine("Geen adressen gevonden");
			}
		}

        private void SpawnNewLine(string addressText)
        {
            var spawnedField = Instantiate(addressTemplate, contentRectTransform);
            spawnedField.Set(addressText);
            spawnedField.gameObject.SetActive(true);
            dynamicInterfaceItems.Add(spawnedField.gameObject);
        }
    }
}