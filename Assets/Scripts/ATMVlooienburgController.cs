using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Projects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private int currentYear;
        private Dictionary<string, ATMAsset> vlooienburgAssets = new();
        private Coordinate targetPivotCoordinate = new Coordinate(CoordinateSystem.WGS84_LatLon, 52.3677697709198d, 4.90068151319564d);

        private void OnEnable()
        {
            ProjectData.Current.OnDataChanged.AddListener(Initialize);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnDataChanged.RemoveListener(Initialize);
            ProjectData.Current.OnCurrentDateTimeChanged.RemoveListener(OnTimeChanged);
        }

        private void Initialize(ProjectData newProject)
        {
            newProject.OnCurrentDateTimeChanged.AddListener(OnTimeChanged);
        }

        private void OnTimeChanged(DateTime newTime)
        {
            var yearToLoad = newTime.Year;
            if (yearToLoad != currentYear)
            {

                currentYear = yearToLoad;
            }
        }

        private void Awake()
        {
            assetBundleLoader = GameObject.FindObjectsOfType<AssetBundleLoader>().FirstOrDefault(a => a.bundleName == bundleName);            
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

        public void LoadAssetForAdamLink(string link, Feature feature)
        {
            if(vlooienburgAssets.ContainsKey(link))
            {
                ATMAsset asset = vlooienburgAssets[link];
                if (asset.go != null)
                    asset.go.SetActive(true);
                asset.isActive = true;
            }
            else
            {
                //we cannot use the coordinate because the coord is mapped to the adress position and not the center of the mesh
                string address = addressAdamlinks.FirstOrDefault(pair => pair.Value == link).Key;
                string tempLink = link;
                //this may be a bit inefficient but works for now
                GameObject loadingObject = new GameObject("loadingasset");
                ATMAsset asset = loadingObject.AddComponent<ATMAsset>();

                vlooienburgAssets.Add(link, asset);               
                assetBundleLoader.LoadAssetFromAssetBundle(bundleName, address.ToLower() + ".prefab", go =>
                {                    
                    Vector3 pos = targetPivotCoordinate.ToUnity();
                    pos.y = 0;
                    go.transform.position = pos;
                    go.layer = LayerMask.NameToLayer("Projected2");
                    go.SetActive(asset.isActive);

                    go.AddComponent<HierarchicalObjectLayerGameObject>();
                    WorldTransform wt = go.AddComponent<WorldTransform>();
                    GameObjectWorldTransformShifter shifter = go.GetComponent<GameObjectWorldTransformShifter>();
                    wt.SetShifter(shifter);

                    ATMAsset previous = vlooienburgAssets[tempLink];
                    Destroy(previous.gameObject);
                    vlooienburgAssets[tempLink] = null;

                    ATMAsset entry = go.AddComponent<ATMAsset>();

                    entry.adamLink = tempLink;
                    entry.address = address;
                    entry.coord = targetPivotCoordinate;
                    entry.go = go;
                    entry.feature = feature;
                    entry.Initialize();

                    vlooienburgAssets[link] = entry;
                });
            }
        }

        //public void LoadAssetForAddress(string address)
        //{
        //    foreach (VlooienburgAsset va in vlooienburgAssets.Values)
        //    {
        //        if (va.go != null && va.address == address)
        //        {
        //            va.go.SetActive(true);
        //            return;
        //        }
        //    }
        //}

        public void DisableAllAssets()
        {
            foreach(KeyValuePair<string, ATMAsset> kvp in vlooienburgAssets)
            {
                ATMAsset va = kvp.Value; 
                //is null when still loading
                if (va.go != null)
                    va.go.SetActive(false);
                va.isActive = false;
            }
        }

        private void Start()
        {
            var lines = CsvParser.ReadLines(csv.text, 1);

            foreach (var line in lines)
            {                
                string[] data = line[0].Split(',');                
                addressAdamlinks.Add(Path.GetFileNameWithoutExtension(data[0]), data[1]);
            }
        }
    }

    public class ATMAsset : MonoBehaviour
    {
        public string adamLink;
        public Coordinate coord;
        public GameObject go;
        public bool isActive; //if the asset was still loading we can use this to sync the active state of the gameobject
        public string address;
        public Feature feature;

        public void Initialize()
        {
            MeshCollider box = go.AddComponent<MeshCollider>();
            //this because sometimes you have a building with a chimney but the boxcollider will stick out alot
            //box.center = new Vector3(box.center.x, box.center.y, 8.5f);
            //box.size = new Vector3(box.size.x, box.size.y, 15f);
        }
    }
}
