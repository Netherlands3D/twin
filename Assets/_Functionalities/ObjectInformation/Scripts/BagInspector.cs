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
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.GeoJSON;
using Netherlands3D.SelectionTools;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.Interface.BAG
{
	public class BagInspector : MonoBehaviour
	{
		private const int COLORIZER_PRIORITY = 0;

		[Tooltip("Id replacement string will be replaced")]

		[Header("GeoJSON Data Sources")]
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

		public ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());

		[SerializeField] private GameObject placeholderPanel;
		[SerializeField] private GameObject contentPanel;
		[SerializeField] private GameObject extraContentPanel;

		private Camera mainCamera;
		private CameraInputSystemProvider cameraInputSystemProvider;
		private bool draggedBeforeRelease = false;
		private bool waitingForRelease = false;

		private void Awake()
		{
			mainCamera = Camera.main;
			cameraInputSystemProvider = mainCamera.GetComponent<CameraInputSystemProvider>();

			addressTitle.gameObject.SetActive(false);
			addressTemplate.gameObject.SetActive(false);
			contentPanel.SetActive(false);
			placeholderPanel.SetActive(true);
		}

		private void Update()
		{
			var click = Pointer.current.press.wasPressedThisFrame;

			if (click)
			{
				waitingForRelease = true;
				draggedBeforeRelease = false;
				return;
			}

			if (waitingForRelease && !draggedBeforeRelease)
			{
				//Check if next release should be ignored ( if we dragged too much )
				draggedBeforeRelease = Pointer.current.delta.ReadValue().sqrMagnitude > 0;
			}

			var released = Pointer.current.press.wasReleasedThisFrame;
			if (released)
			{
				waitingForRelease = false;

				if (draggedBeforeRelease || cameraInputSystemProvider.OverLockingObject) return;

				FindObjectMapping();
			}
		}

		private RaycastHit[] raycastHits = new RaycastHit[16];

		public static Vector3 NearestPointOnLine(Vector3 lineOrigin, Vector3 lineDir, Vector3 target)
		{
			lineDir.Normalize();//this needs to be a unit vector
			Vector3 v = target - lineOrigin;
			float d = Vector3.Dot(v, lineDir);
			return lineOrigin + lineDir * d;
		}

		public static Vector3 NearestPointOnFiniteLine(Vector3 start, Vector3 end, Vector3 pnt)
		{
			var line = (end - start);
			var len = line.magnitude;
			line.Normalize();

			var v = pnt - start;
			var d = Vector3.Dot(v, line);
			d = Mathf.Clamp(d, 0f, len);
			return start + line * d;
		}
		
		private float hitDistance = 100000f;
		private float tubeHitRadius = 5f;
		private GameObject testHitPosition;
		private GameObject testGroundPosition;
        private Dictionary<GeoJsonLayerGameObject, List<FeatureMapping>> featureMappings = new Dictionary<GeoJsonLayerGameObject, List<FeatureMapping>>();
        /// <summary>
        /// Find objectmapping by raycast and get the BAG ID
        /// </summary>
        private void FindObjectMapping()
		{
			DeselectBuilding();
			DeselectFeature();

			// Raycast from pointer position using main camera
			var position = Pointer.current.position.ReadValue();
			var ray = mainCamera.ScreenPointToRay(position);
			if (Physics.Raycast(ray, out RaycastHit hit, hitDistance)) 
			{
				//lets use a capsule cast here to ensure objects are hit (some objects for features are really small) and use a nonalloc to prevent memory allocations
				var objectMapping = hit.collider.gameObject.GetComponent<ObjectMapping>();
				if (objectMapping)
				{
					lastWorldClickedPosition = hit.point;
					SelectBuildingOnHit(objectMapping.getObjectID(hit.triangleIndex));
					return;
				}
			}

			if (hit.collider == null)
				return;

			if(testHitPosition == null)
			{
                testHitPosition = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				testHitPosition.transform.localScale = Vector3.one * 3;

				testGroundPosition = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				testGroundPosition.transform.localScale = Vector3.one * tubeHitRadius;
				testGroundPosition.GetComponent<MeshRenderer>().material.color = Color.red;
            }

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            groundPlane.Raycast(ray, out float distance);
			Vector3 groundPosition = ray.GetPoint(distance);
			testGroundPosition.transform.position = groundPosition + Vector3.up * 5f;

			//clear the hit list or else it will use previous collider values
			raycastHits = new RaycastHit[16];

            //if (Physics.CapsuleCastNonAlloc(ray.origin, ray.GetPoint(hitDistance), tubeHitRadius, ray.direction, raycastHits, hitDistance) > 0)
            if (Physics.SphereCastNonAlloc(groundPosition, tubeHitRadius, Vector3.up, raycastHits, hitDistance) > 0)
			//if(Physics.RaycastNonAlloc(ray, raycastHits, hitDistance) > 0)
            {
				
                float closest = float.MaxValue;
                for (int i = 0; i < raycastHits.Length; i++)
                {
                    if (raycastHits[i].collider != null)
                    {
                        FeatureMapping mapping = raycastHits[i].collider.gameObject.GetComponent<FeatureMapping>();
                        if (mapping != null)
                        {							
							if(!featureMappings.ContainsKey(mapping.VisualisationParent))
								featureMappings.Add(mapping.VisualisationParent, new List<FeatureMapping>());
							featureMappings[mapping.VisualisationParent].Add(mapping);

							//Debug.Log(mapping.FeatureID + " " + mapping.VisualisationLayer.ToString());							
							//float dist = Vector3.Distance(raycastHits[i].point, groundPosition);
       //                     if (dist < closest)
       //                     {
       //                         closest = dist;        
       //                         hit = raycastHits[i];
       //                     }
                        }
                    }
                }
            }
            if (featureMappings.Count > 0)
            {
                lastWorldClickedPosition = groundPosition;
                foreach (KeyValuePair<GeoJsonLayerGameObject, List<FeatureMapping>> pair in featureMappings) 
				{					
					foreach(FeatureMapping mapping in pair.Value) 
						SelectFeatureOnHit(mapping);

					////for now lets always select the first set of featuremappings for a visiualisationlayer 
					//break;
				}
            }
			else
			{
                var camera = Camera.main;
                Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
                //not ideal but better than caching, would be better to have an quadtree approach here
                FeatureMapping[] mappings = FindObjectsOfType<FeatureMapping>();
				for (int i = 0; i < mappings.Length; i++)
				{
					GeoJSONPolygonLayer polygonLayer = mappings[i].VisualisationLayer as GeoJSONPolygonLayer;
					if (polygonLayer != null)
					{
						List<Mesh> meshes = mappings[i].FeatureMeshes;
						for (int j = 0; j < meshes.Count; j++)
						{
							PolygonVisualisation pv = polygonLayer.GetPolygonVisualisationByMesh(meshes);
                            bool isSelected = ProcessPolygonSelection(meshes[j], pv.transform, camera, frustumPlanes, groundPosition);
							if(isSelected)
							{
                                if (!featureMappings.ContainsKey(mappings[i].VisualisationParent))
                                    featureMappings.Add(mappings[i].VisualisationParent, new List<FeatureMapping>());
                                featureMappings[mappings[i].VisualisationParent].Add(mappings[i]);
                                SelectFeatureOnHit(mappings[i]);
								return;
							}
						}
					}
				}
			}
        }

        public static bool ProcessPolygonSelection(Mesh polygon, Transform transform, Camera camera, Plane[] frustumPlanes, Vector3 worldPoint)
        {
		    Bounds localBounds = polygon.bounds;
			Matrix4x4 localToWorld = transform.localToWorldMatrix;
			Vector3 worldCenter = localToWorld.MultiplyPoint3x4(localBounds.center);
            Bounds worldBounds = new Bounds(worldCenter, polygon.bounds.size);

			if (!PolygonSelectionCalculator.IsBoundsInView(worldBounds, frustumPlanes))
				return false;

            var point2d = new Vector2(worldPoint.x, worldPoint.z);
            if (!PolygonSelectionCalculator.IsInBounds2D(worldBounds, point2d))
                return false;

            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Vector3 localPosition = worldToLocal.MultiplyPoint(worldPoint);

			return IsPointInMesh(polygon, localPosition);
        }		

        public static bool IsPointInMesh(Mesh mesh, Vector3 point)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3 projectedPoint = new Vector3(point.x, 0, point.z);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = new Vector3(vertices[triangles[i]].x, 0, vertices[triangles[i]].z);
                Vector3 v1 = new Vector3(vertices[triangles[i + 1]].x, 0, vertices[triangles[i + 1]].z);
                Vector3 v2 = new Vector3(vertices[triangles[i + 2]].x, 0, vertices[triangles[i + 2]].z);
                if (ContainsPointProjected2D(new List<Vector3> { v0, v1, v2 }, projectedPoint))
                {
                    return true; 
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a 2d polygon contains point p
        /// </summary>
        /// <param name="polygon">array of points that define the polygon</param>
        /// <param name="p">point to test</param>
        /// <returns>true if point p is inside the polygon, otherwise false</returns>
        public static bool ContainsPointProjected2D(IList<Vector3> polygon, Vector3 p)
        {
            var j = polygon.Count - 1;
            var inside = false;
            for (int i = 0; i < polygon.Count; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                if (((pi.z <= p.z && p.z < pj.z) || (pj.z <= p.z && p.z < pi.z)) &&
                    (p.x < (pj.x - pi.x) * (p.z - pi.z) / (pj.z - pi.z) + pi.x))
                    inside = !inside;
            }
            return inside;
        }

        private void SelectBuildingOnHit(string bagId)
		{
            addressTitle.gameObject.SetActive(true);
            contentPanel.SetActive(true);
			placeholderPanel.SetActive(false);

			var objectIdAndColor = new Dictionary<string, Color>
			{
				{ bagId, new Color(1, 0, 0, 0) }
			};
			ColorSetLayer = GeometryColorizer.InsertCustomColorSet(-1, objectIdAndColor);

			GetBAGID(bagId);
		}

		private void DeselectBuilding()
		{
            addressTitle.gameObject.SetActive(false);
            contentPanel.SetActive(false);
			placeholderPanel.SetActive(true);

			GeometryColorizer.RemoveCustomColorSet(ColorSetLayer);
			ColorSetLayer = null;
		}

		private void SelectFeatureOnHit(FeatureMapping mapping)
		{
            addressTitle.gameObject.SetActive(false);
            contentPanel.SetActive(true);
            placeholderPanel.SetActive(false);
			//TODO populate the baginspector ui		
			mapping.SelectFeature();
        }

		private void DeselectFeature()
		{
			if (featureMappings.Count > 0)
			{
                foreach (KeyValuePair<GeoJsonLayerGameObject, List<FeatureMapping>> pair in featureMappings)
                {
					foreach(FeatureMapping mapping in pair.Value)
						mapping.DeselectFeature();
					//break;
				}
			}
			featureMappings.Clear();
            addressTitle.gameObject.SetActive(false);
            contentPanel.SetActive(false);
            placeholderPanel.SetActive(true);
        }

		private void OnDestroy()
		{
			DeselectBuilding();
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
				if (removeFromID.Length > 0) ID = ID.Replace(removeFromID, "");

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
					statusText.text = properties["status"].ToString();

					//TODO: Use bbox and geometry.coordinates from GeoJSON object to create bounds to render thumbnail
					Bounds currentObjectBounds = new Bounds(lastWorldClickedPosition, Vector3.one * 50.0f);
					buildingThumbnail.RenderThumbnail(currentObjectBounds);
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