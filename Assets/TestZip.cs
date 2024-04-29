using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SharpZipLib.Unity.Helpers;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class TestZip : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void DownloadFromIndexedDB(string filename, string callbackObjectName, string callbackMethodName);
        [DllImport("__Internal")]
        private static extern void SyncFilesToIndexedDB(string callbackObjectName, string callbackMethodName);

        ZipOutputStream zipOutputStream;


        private string randomTime = "";
        private string zipName = "";

        void Start()
        {
            randomTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            zipName = $"test_{randomTime}.zip";
            

            //Open our zip stream, and keep it open
            zipOutputStream = new ZipOutputStream(File.Create(Application.persistentDataPath + $"/{zipName}"));
            zipOutputStream.SetLevel(9); // 0 - store only to 9 - means best compression
        }

        public void DoneDownloadZip()
        {
            Debug.Log("Done downloading zip");
        }

        // Append a file to zip
        public void AddFileToZip(string fileNames)
        {
            var fileName = fileNames.Split(",")[0];
            var persistentDataPath = Application.persistentDataPath + "/" + fileName;
            Debug.Log("Adding file to zip: " + persistentDataPath);
            byte[] buffer = new byte[4096];
            var randomFileTag = DateTime.Now.ToString("yyyyMMddHHmmss");
            ZipEntry entry = new ZipEntry(randomFileTag + "_" + fileName);
            zipOutputStream.PutNextEntry(entry);

            using (FileStream fs = File.OpenRead(persistentDataPath)) {
                StreamUtils.Copy(fs, zipOutputStream, buffer);
            }         
        }

        public void CloseZip()
        {
            zipOutputStream.Finish();
            zipOutputStream.Close();

            //Make sure indexedDB is synced
            #if !UNITY_EDITOR && UNITY_WEBGL
            SyncFilesToIndexedDB(gameObject.name, "ZipReadyInIndexedDB");
            #endif
        }

        public void ZipReadyInIndexedDB()
        {
            Debug.Log("Zip is ready in indexedDB");
            DownloadFromIndexedDB($"/{zipName}", this.gameObject.name, "DoneDownloadZip");
        }
    }
}
