using Netherlands3D.Twin.Projects;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ATMVlooienburgController : MonoBehaviour
    {
        [SerializeField] private TextAsset csv;        
        private Dictionary<string, string> addressAdamlinks = new();
        private AssetBundleLoader assetBundleLoader;
        private const string bundleName = "atmvlooienburg";

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

        public void LoadAssetForAdamLink(string link)
        {
            string adress = addressAdamlinks.FirstOrDefault(pair => pair.Value == link).Key;
            assetBundleLoader.LoadAssetFromAssetBundle(bundleName, adress, go => 
            { 
            
            });
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
