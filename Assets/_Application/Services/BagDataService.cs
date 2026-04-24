using System.Collections;
using System.Collections.Generic;
using Netherlands3D.GeoJSON;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D
{
    public class BagDataService : MonoBehaviour
    {
        private const string idReplacementString = "{BagID}";
        private const string bagRequestUrl = "https://service.pdok.nl/lv/bag/wfs/v2_0?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:pand&count=100&outputFormat=xml&srsName=EPSG:28992&filter=%3cFilter%3e%3cPropertyIsEqualTo%3e%3cPropertyName%3eidentificatie%3c/PropertyName%3e%3cLiteral%3e{BagID}%3c/Literal%3e%3c/PropertyIsEqualTo%3e%3c/Filter%3e";
        private const string addressRequestUrl = "https://service.pdok.nl/lv/bag/wfs/v2_0?service=wfs&request=getFeature&version=2.0.0&outputFormat=geojson&typeName=bag:verblijfsobject&filter=%3CFilter%3E%3CPropertyIsEqualTo%3E%3CPropertyName%3Epandidentificatie%3C/PropertyName%3E%3CLiteral%3E{BagID}%3C/Literal%3E%3C/PropertyIsEqualTo%3E%3C/Filter%3E";
        private const string removeFromID = "NL.IMBAG.Pand.";

        public UnityEvent OnBagRequestFailed = new();
        public UnityEvent<List<string>> OnBagAddressesRequestSucceeded = new();
        public UnityEvent<BagData> OnBagDataRequestSucceeded = new();
        
        
        private Coroutine downloadProcess;

        public struct BagData
        {
            public string id;
            public string url;
            public string status;
            public string year;
        }

        public void LoadBagId(string bagId)
        {
            if (removeFromID.Length > 0) bagId = bagId.Replace(removeFromID, "");

            if (downloadProcess != null)
            {
                StopCoroutine(downloadProcess);
            }
            downloadProcess = StartCoroutine( GetBagIDData(bagId));
        }

        private IEnumerator GetBagIDData(string bagID)
        {
            yield return GetBAGData(bagID);
            yield return GetAddresses(bagID);
        }

        private IEnumerator GetBAGData(string bagID)
        {
            var requestUrl = bagRequestUrl.Replace(idReplacementString, bagID);
            var webRequest = UnityWebRequest.Get(requestUrl);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                OnBagRequestFailed.Invoke();
                yield break;
            }
            
            string bagIdText;
            string yearText;
            string statusText;

            GeoJSONStreamReader customJsonHandler = new GeoJSONStreamReader(webRequest.downloadHandler.text);
            while (customJsonHandler.GotoNextFeature())
            {
                var properties = customJsonHandler.GetProperties();

                bagIdText = properties["identificatie"].ToString();
                yearText = properties["bouwjaar"].ToString();
                statusText = properties["status"].ToString();
                
                BagData bagData = new BagData();
                
                bagData.id = bagID;
                bagData.url = "https://bagviewer.kadaster.nl/lvbag/bag-viewer/?objectId=" + bagIdText;
                
                bagData.status = statusText;
                bagData.year = yearText;
                
                OnBagDataRequestSucceeded.Invoke(bagData);
            }
        }
        
        private IEnumerator GetAddresses(string bagID)
        {
            var requestUrl = addressRequestUrl.Replace(idReplacementString, bagID);
            var webRequest = UnityWebRequest.Get(requestUrl);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                OnBagRequestFailed.Invoke();
                yield break;
            }

            List<string> addresses = new List<string>();
            string districtText;
            
            GeoJSONStreamReader customJsonHandler = new GeoJSONStreamReader(webRequest.downloadHandler.text);
            bool gotDistrict = false;
            while (customJsonHandler.GotoNextFeature())
            {
                var properties = customJsonHandler.GetProperties();

                //Use first address result to determine district
                if (!gotDistrict)
                {
                    districtText = properties["openbare_ruimte"].ToString();
                    gotDistrict = true;
                }

                string address = $"{properties["openbare_ruimte"]} {properties["huisnummer"]} {properties["huisletter"]}{properties["toevoeging"]}";
                addresses.Add(address);
            }
            OnBagAddressesRequestSucceeded.Invoke(addresses);
        }
    }
}
