using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.GeoJSON;
using Netherlands3D.Twin.Utility;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D
{
    public class BagDataService : MonoBehaviour
    {
        private const string idReplacementString = "{BagID}";
        private const string bagRequestUrl = "https://service.pdok.nl/lv/bag/wfs/v2_0?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:pand&count=100&outputFormat=xml&srsName=EPSG:28992&filter=%3cFilter%3e%3cPropertyIsEqualTo%3e%3cPropertyName%3eidentificatie%3c/PropertyName%3e%3cLiteral%3e{BagID}%3c/Literal%3e%3c/PropertyIsEqualTo%3e%3c/Filter%3e";
        private const string addressRequestUrl = "https://service.pdok.nl/lv/bag/wfs/v2_0?service=wfs&request=getFeature&version=2.0.0&outputFormat=geojson&typeName=bag:verblijfsobject&filter=%3CFilter%3E%3CPropertyIsEqualTo%3E%3CPropertyName%3Epandidentificatie%3C/PropertyName%3E%3CLiteral%3E{BagID}%3C/Literal%3E%3C/PropertyIsEqualTo%3E%3C/Filter%3E";
        private const string removeFromID = "NL.IMBAG.Pand.";
        private const string bagWfsEndpoint = "https://service.pdok.nl/lv/bag/wfs/v2_0";
        
        private Dictionary<string, RequestState> downloadProcesses = new();

        public class BagRequestHandle
        {
            public Action<BagData> OnBagData;
            public Action<List<string>> OnAddresses;
            public Action OnFailed;
        }

        private class RequestState
        {
            public Coroutine coroutine;
            public BagRequestHandle handle;
        }

        public struct BagData
        {
            public string id;
            public string url;
            public string status;
            public string year;
        }

        public void LoadBagIdData(string bagId, BagRequestHandle handle)
        {
            if (removeFromID.Length > 0)
                bagId = bagId.Replace(removeFromID, "");

            if (downloadProcesses.TryGetValue(bagId, out var existing))
            {
                StopCoroutine(existing.coroutine);
                downloadProcesses.Remove(bagId);
            }
            var state = new RequestState
            {
                handle = handle,
                coroutine = StartCoroutine(GetBagIDData(bagId))
            };
            downloadProcesses[bagId] = state;
        }

        private IEnumerator GetBagIDData(string bagID)
        {
            yield return GetBAGData(bagID);
            yield return GetAddresses(bagID);
            downloadProcesses.Remove(bagID);
        }
        
        private IEnumerator GetBAGData(string bagID)
        {
            var requestUrl = bagRequestUrl.Replace(idReplacementString, bagID);
            var webRequest = UnityWebRequest.Get(requestUrl);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                GetHandle(bagID)?.OnFailed?.Invoke();
                yield break;
            }

            var reader = new GeoJSONStreamReader(webRequest.downloadHandler.text);

            while (reader.GotoNextFeature())
            {
                var properties = reader.GetProperties();

                var bagData = new BagData
                {
                    id = bagID,
                    url = "https://bagviewer.kadaster.nl/lvbag/bag-viewer/?objectId="
                          + properties["identificatie"],
                    status = properties["status"].ToString(),
                    year = properties["bouwjaar"].ToString()
                };

                GetHandle(bagID)?.OnBagData?.Invoke(bagData);
            }
        }

        private IEnumerator GetAddresses(string bagID)
        {
            var requestUrl = addressRequestUrl.Replace(idReplacementString, bagID);
            var webRequest = UnityWebRequest.Get(requestUrl);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                GetHandle(bagID)?.OnFailed?.Invoke();
                yield break;
            }
            List<string> addresses = new();
            var reader = new GeoJSONStreamReader(webRequest.downloadHandler.text);
            while (reader.GotoNextFeature())
            {
                var properties = reader.GetProperties();
                string address = $"{properties["openbare_ruimte"]} {properties["huisnummer"]} " +
                                 $"{properties["huisletter"]}{properties["toevoeging"]}";

                addresses.Add(address);
            }

            GetHandle(bagID)?.OnAddresses?.Invoke(addresses);
        }

        private BagRequestHandle GetHandle(string bagID)
        {
            return downloadProcesses.TryGetValue(bagID, out var state) ? state.handle : null;
        }
        
        public void LoadBuildingBounds(string bagId, Action<BoundingBox> onBoundsLoaded)
        {
            bagId = bagId.Replace("NL.IMBAG.Pand.", "");
            StartCoroutine(GetBuildingBounds(bagId, onBoundsLoaded));
        }

        private IEnumerator GetBuildingBounds(
            string bagId,
            Action<BoundingBox> onBoundsLoaded)
        {
            string filter =
                "<Filter><PropertyIsEqualTo>" +
                "<PropertyName>identificatie</PropertyName>" +
                "<Literal>" + bagId + "</Literal>" +
                "</PropertyIsEqualTo></Filter>";

            string url =
                "https://service.pdok.nl/lv/bag/wfs/v2_0" +
                "?SERVICE=WFS" +
                "&VERSION=2.0.0" +
                "&REQUEST=GetFeature" +
                "&typeNames=bag:pand" +
                "&count=1" +
                "&outputFormat=application/json" +
                "&srsName=EPSG:4326" +
                "&filter=" + UnityWebRequest.EscapeURL(filter);

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "[BagDataService] Pandgeometrie ophalen mislukt: "
                    + request.error);

                onBoundsLoaded?.Invoke(null);
                yield break;
            }

            JSONNode root;

            try
            {
                root = JSON.Parse(request.downloadHandler.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[BagDataService] Ongeldige GeoJSON-response: "
                    + exception.Message);

                onBoundsLoaded?.Invoke(null);
                yield break;
            }

            JSONNode features = root["features"];

            if (features == null || !features.IsArray || features.Count == 0)
            {
                Debug.LogWarning(
                    "[BagDataService] Geen pand gevonden voor BAG-ID: " + bagId);

                onBoundsLoaded?.Invoke(null);
                yield break;
            }

            JSONNode geometry = features[0]["geometry"];
            JSONNode coordinates = geometry?["coordinates"];

            double minLon = double.PositiveInfinity;
            double minLat = double.PositiveInfinity;
            double maxLon = double.NegativeInfinity;
            double maxLat = double.NegativeInfinity;

            if (coordinates == null ||
                !AccumulateGeoJsonBounds(
                    coordinates,
                    ref minLon,
                    ref minLat,
                    ref maxLon,
                    ref maxLat))
            {
                Debug.LogWarning(
                    "[BagDataService] Geen geldige pandcoördinaten gevonden.");

                onBoundsLoaded?.Invoke(null);
                yield break;
            }

            var bounds = new BoundingBox(
                new Coordinate(
                    CoordinateSystem.WGS84_LatLon,
                    minLat,
                    minLon),
                new Coordinate(
                    CoordinateSystem.WGS84_LatLon,
                    maxLat,
                    maxLon));

            onBoundsLoaded?.Invoke(bounds);
        }

        private static bool AccumulateGeoJsonBounds(
            JSONNode node,
            ref double minLon,
            ref double minLat,
            ref double maxLon,
            ref double maxLat)
        {
            if (node == null || node.IsNull)
                return false;

            // GeoJSON-posities zijn [longitude, latitude].
            if (node.IsArray &&
                node.Count >= 2 &&
                node[0].IsNumber &&
                node[1].IsNumber)
            {
                double lon = node[0].AsDouble;
                double lat = node[1].AsDouble;

                minLon = Math.Min(minLon, lon);
                minLat = Math.Min(minLat, lat);
                maxLon = Math.Max(maxLon, lon);
                maxLat = Math.Max(maxLat, lat);

                return true;
            }

            bool found = false;

            if (node.IsArray)
            {
                for (int i = 0; i < node.Count; i++)
                {
                    if (AccumulateGeoJsonBounds(
                            node[i],
                            ref minLon,
                            ref minLat,
                            ref maxLon,
                            ref maxLat))
                    {
                        found = true;
                    }
                }
            }

            return found;
        }

        public void GetBAGIDFromResidentialID(string residentialObjectId, Action<List<string>> onBagIdsLoaded)
        {
            StartCoroutine(GetBAGID(residentialObjectId, onBagIdsLoaded));
        }
        
        public IEnumerator GetBAGID(string residentialObjectID, Action<List<string>> onBagIdsLoaded)
        {
            string filter = "<Filter><PropertyIsEqualTo>" + "<PropertyName>identificatie</PropertyName>" + "<Literal>" + residentialObjectID + "</Literal>" + "</PropertyIsEqualTo></Filter>";
            string url = bagWfsEndpoint.TrimEnd('/') + "?SERVICE=WFS" + "&VERSION=2.0.0" + "&REQUEST=GetFeature" + "&typeNames=bag:verblijfsobject" + "&count=100" + "&outputFormat=application/json" + "&srsName=EPSG:28992" + "&filter=" + UnityWebRequest.EscapeURL(filter);
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[AddressSearchService] BAG WFS-request mislukt: " + request.error);
                yield break;
            }

            JSONNode root;
            try
            {
                root = JSON.Parse(request.downloadHandler.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AddressSearchService] Ongeldige BAG WFS-response: " + exception.Message);
                yield break;
            }

            JSONNode features = root["features"];
            if (features == null || !features.IsArray || features.Count == 0)
            {
                Debug.LogWarning("[AddressSearchService] Geen BAG-pand gevonden.");
                yield break;
            }

            JSONNode properties = features[0]["properties"];
            string bagId = ReadString(properties, "pandidentificatie");
            if (string.IsNullOrEmpty(bagId))
            {
                Debug.LogWarning("[AddressSearchService] De BAG-response bevat " + "geen pandidentificatie.");
                yield break;
            }
            Debug.Log("[AddressSearchService] BAG pand-ID: " + bagId);
            onBagIdsLoaded?.Invoke(new List<string> { bagId });
        }
        
        private static string ReadString(JSONNode node, string key)
        {
            if (node == null)
                return string.Empty;

            JSONNode value = node[key];
            if (value == null || value.IsNull)
                return string.Empty;

            return value.Value;
        }
    }
}