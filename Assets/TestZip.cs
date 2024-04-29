using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        private Stopwatch stopwatch = new Stopwatch();

        void OnEnable()
        {
            randomTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            zipName = $"test_{randomTime}.zip";
            

            //Open our zip stream, and keep it open
            zipOutputStream = new ZipOutputStream(File.Create(Application.persistentDataPath + $"/{zipName}"));
            zipOutputStream.SetLevel(9); // 0 - store only to 9 - means best compression
        }

        private void OnDisable() {
            stopwatch.Stop();
            zipOutputStream.Finish();
            zipOutputStream.Close();
            zipOutputStream.Dispose();
        }

        public void DoneDownloadZip()
        {
            UnityEngine.Debug.Log("Done downloading zip");
        }

        // Append a file to zip
        public void AddFileToZip(string fileNames)
        {
            var fileName = fileNames.Split(",")[0];
            var persistentDataPath = Application.persistentDataPath + "/" + fileName;
            UnityEngine.Debug.Log("Adding file to zip: " + persistentDataPath);
            stopwatch.Reset();
            stopwatch.Start();


            byte[] buffer = new byte[4096];
            var randomFileTag = DateTime.Now.ToString("yyyyMMddHHmmss");
            ZipEntry entry = new ZipEntry(randomFileTag + "_" + fileName);
            zipOutputStream.PutNextEntry(entry);

            using (FileStream fs = File.OpenRead(persistentDataPath)) {
                StreamUtils.Copy(fs, zipOutputStream, buffer);
            }     

            stopwatch.Stop();
            UnityEngine.Debug.Log($"Added file to zip in {stopwatch.ElapsedMilliseconds}ms");    
        }

        public void CloseZip()
        {
            stopwatch.Reset();
            stopwatch.Start();

            zipOutputStream.Finish();
            zipOutputStream.Close();

            //Make sure indexedDB is synced
            #if !UNITY_EDITOR && UNITY_WEBGL
            SyncFilesToIndexedDB(gameObject.name, "ZipReadyInIndexedDB");
            #endif
        }

        public void ZipReadyInIndexedDB()
        {
            stopwatch.Stop();
            UnityEngine.Debug.Log($"Finished zip in IndexedDB in {stopwatch.ElapsedMilliseconds}ms");
            DownloadFromIndexedDB($"{zipName}", this.gameObject.name, "DoneDownloadZip");
        }
    }
}
