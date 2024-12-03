using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class ATMVlooienburgController : MonoBehaviour
    {
        [SerializeField] private TextAsset csv;        
        private Dictionary<string, string> addressAdamlinks = new();
        private AssetBundleLoader assetBundleLoader;
        private const string bundleName = "atmvlooienburg";

        private Dictionary<string, VlooienburgAsset> vlooienburgAssets = new();
        
        private struct VlooienburgAsset
        {
            public string adamLink;
            public int year;
            public Coordinate coord;
            public GameObject go;

            public VlooienburgAsset(string link, int year, Coordinate coord, GameObject go)
            {
                adamLink = link;
                this.year = year;
                this.coord = coord;
                this.go = go;
            }
        }


        private void Awake()
        {
            assetBundleLoader = GameObject.FindObjectsOfType<AssetBundleLoader>().FirstOrDefault(a => a.bundleName == bundleName);            
        }

        private void OnEnable()
        {
            //ProjectData.Current.OnCurrentDateTimeChanged.AddListener(UpdateBagIds);
        }

        private void OnDisable()
        {
            //ProjectData.Current.OnCurrentDateTimeChanged.RemoveListener(UpdateBagIds);
        }

        public bool HasAdamlink(string link)
        {
            return addressAdamlinks.Values.Contains(link);
        }

        public bool HasAddress(string address)
        {
            return addressAdamlinks.Keys.Contains(address);
        }

        public string GetAddressFromAdamlink(string link)
        {
            if(HasAdamlink(link))
                return addressAdamlinks.FirstOrDefault(pair => pair.Value == link).Key;
            return null;
        } 

        public void LoadAssetForAdamLink(string link)
        {
            if(vlooienburgAssets.ContainsKey(link))
            {
                if (vlooienburgAssets[link].go != null)
                    vlooienburgAssets[link].go.SetActive(true);
            }
            else
            {
                Coordinate targetPivotCoordinate = new Coordinate(CoordinateSystem.WGS84_LatLon, 52.3677697709198d, 4.90068151319564d);
                VlooienburgAsset entry = new VlooienburgAsset(link, 0, new Coordinate(), null); //need fake value because of async
                vlooienburgAssets.Add(link, entry);
                StartCoroutine(GetCoordinate(link, coord =>
                {
                    //we cannot use the coordinate because the coord is mapped to the adress position and not the center of the mesh
                    string adress = addressAdamlinks.FirstOrDefault(pair => pair.Value == link).Key;
                    assetBundleLoader.LoadAssetFromAssetBundle(bundleName, adress.ToLower() + ".prefab", go =>
                    {
                        //Vector3 unityPosition = coord.ToUnity();                        
                        //unityPosition.y = 0;                       

                        //Vector3 targetOffset = new Vector3(-86.7f, 0, -173.47f);

                        //Vector3 worldPositionMesh = go.GetComponent<Renderer>().bounds.center;
                        //Vector3 localToPivot = (worldPositionMesh - go.transform.position);
                        //go.transform.position -= localToPivot * 0.5f;
                        //worldPositionMesh = go.GetComponent<Renderer>().bounds.center;
                        //Vector3 offset = worldPositionMesh - unityPosition;
                        //go.transform.position += targetOffset;

                        //Coordinate testCoord = new Coordinate(CoordinateSystem.Unity, go.transform.position.x, go.transform.position.y, go.transform.position.z);
                        //Coordinate lonlat = testCoord.Convert(CoordinateSystem.WGS84);

                        //go.transform.position = lonlat.ToUnity();
                        //GameObject test = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //test.name = adress;
                        //test.transform.localScale = Vector3.one * 5;
                        //test.transform.position = go.transform.position - localToPivot + Vector3.up * 25;

                        //go.transform.position = unityPosition;
                        Vector3 pos = targetPivotCoordinate.ToUnity();
                        pos.y = 0;
                        go.transform.position = pos;
                        go.layer = LayerMask.NameToLayer("Projected2");
                        go.SetActive(true);

                        go.AddComponent<HierarchicalObjectLayerGameObject>();
                        WorldTransform wt = go.AddComponent<WorldTransform>();
                        GameObjectWorldTransformShifter shifter = go.GetComponent<GameObjectWorldTransformShifter>();
                        wt.SetShifter(shifter);
                        

                        //GameObject asset = GameObject.Instantiate(go, unityPosition, Quaternion.identity);
                        entry = new VlooienburgAsset(link, 0, coord, go);
                        vlooienburgAssets[link] = entry;
                    });
                }));
            }
        }

        private IEnumerator GetCoordinate(string url, Action<Coordinate> onGetCoordinate)
        {
            var geoJsonRequest = UnityWebRequest.Get(url);
            yield return geoJsonRequest.SendWebRequest();
            if (geoJsonRequest.result == UnityWebRequest.Result.Success)
            {
                string txt = geoJsonRequest.downloadHandler.text;
                //string pattern = @"""coordinates"":\s*\[\s*([\d.]+),\s*([\d.]+)\s*\]";
                //Match match = Regex.Match(txt, pattern);
                //if (match.Success)
                //{
                //    float longitude = float.Parse(match.Groups[1].Value);
                //    float latitude = float.Parse(match.Groups[2].Value);
                //    Coordinate coord = new Coordinate(CoordinateSystem.WGS84_LatLon, latitude, longitude);
                //    onGetCoordinate(coord);
                //}

                string pattern = @"Point\s*\(\s*([\d.]+)\s+([\d.]+)\s*\)";
                MatchCollection matches = Regex.Matches(txt, pattern);
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        if (match.Success) //TODO fix multiple matches 
                        {
                            float rdX = float.Parse(match.Groups[1].Value);
                            float rdY = float.Parse(match.Groups[2].Value);                            
                            Coordinate coord = new Coordinate(CoordinateSystem.RDNAP, rdX, rdY, 0);
                            onGetCoordinate(coord);
                        }
                    }
                }
            }
        }

        //private void UpdateBagIds(DateTime newTime)
        //{
        //    hiddenBagIds.bagIds.Clear();

        //    foreach (var building in availableAdamLinks)
        //    {
        //        if (newTime < building.Value)
        //        {
        //            hiddenBagIds.bagIds.Add(building.Key);
        //        }
        //    }

        //    bagIdHider.UpdateHiddenBuildings(true);
        //}

        private void Start()
        {
            var lines = CsvParser.ReadLines(csv.text, 1);

            foreach (var line in lines)
            {
                
                string[] data = line[0].Split(',');                
                addressAdamlinks.Add(Path.GetFileNameWithoutExtension(data[0]), data[1]);
            }

            //UpdateBagIds(ProjectData.Current.CurrentDateTime);
        }
    }
}
