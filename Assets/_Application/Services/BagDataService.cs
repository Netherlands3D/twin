using System.Collections;
using System.Collections.Generic;
using Netherlands3D.GeoJSON;
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
        
        private Dictionary<string, RequestState> downloadProcesses = new();

        public class BagRequestHandle
        {
            public System.Action<BagData> OnBagData;
            public System.Action<List<string>> OnAddresses;
            public System.Action OnFailed;
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

        public void LoadBagId(string bagId, BagRequestHandle handle)
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
            bool gotDistrict = false;
            var reader = new GeoJSONStreamReader(webRequest.downloadHandler.text);
            while (reader.GotoNextFeature())
            {
                var properties = reader.GetProperties();
                if (!gotDistrict)
                {
                    _ = properties["openbare_ruimte"];
                    gotDistrict = true;
                }
                string address = $"{properties["openbare_ruimte"]} {properties["huisnummer"]} " +
                                 $"{properties["huisletter"]}{properties["toevoeging"]}";

                addresses.Add(address);
            }

            GetHandle(bagID)?.OnAddresses?.Invoke(addresses);
        }

        private BagRequestHandle GetHandle(string bagID)
        {
            return downloadProcesses.TryGetValue(bagID, out var state)
                ? state.handle
                : null;
        }
    }
}