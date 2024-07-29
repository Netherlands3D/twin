using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class DataTypeChain : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool debugLog = false;

        [Header("Data type adapters")] [Space(5)]
        [SerializeField] private ScriptableObject[] dataTypeAdapters;
        private IDataTypeAdapter[] dataTypeAdapterInterfaces;
        public UnityEvent<IDataTypeAdapter> OnAdapterFound = new();

        [Header("Events invoked on failures")] [Space(5)]
        public UnityEvent<string> CouldNotFindAdapter = new();
        public UnityEvent<string> OnDownloadFailed = new();

        private string targetUrl = "";

        /// <summary>
        /// The target url can be set directly from an input field.
        /// Using <ref> DetermineAdapter </ref> without an url will start the chain of responsibility using the set url.
        /// </summary>
        public string TargetUrl 
        { 
            get => targetUrl; 
            set
            {
                AbortChain();
                targetUrl = value;   
            }
        }

        private Coroutine chain;

        private void OnDisable() {
            AbortChain();
        }

        /// <summary>
        /// Determine the type of data using chain of responsibility
        /// </summary>
        /// <param name="url">Url to file or service</param>
        public void DetermineAdapter(string url)
        {
            TargetUrl = url;
            DetermineAdapter();
        }
        public void DetermineAdapter()
        {
            AbortChain();
            chain = StartCoroutine(DownloadAndCheckSupport(TargetUrl));
        }

        private void AbortChain()
        {
            if(chain != null)
                StopCoroutine(chain);
        }

        private IEnumerator DownloadAndCheckSupport(string url)
        {
            // Start by download the file, so we can do a detailed check of the content to determine the type
            var urlAndData = new LocalFile()
            {
                SourceUrl = url,
                LocalFilePath = ""
            };
            yield return DownloadDataToLocalCache(urlAndData);

            // No local cache? Download failed.
            if(string.IsNullOrEmpty(urlAndData.LocalFilePath))
            {
                OnDownloadFailed.Invoke(url);
                yield break;
            }

            // Find the proper adapter in a chain of responsibility
            yield return AdapterChain(urlAndData);
        }

        /// <summary>
        /// We download the file to a local cache so we can check the content from the adapters using streamreading.
        /// This way we keep the heap memory usage low and can handle large files (like large obj's, or large WFS responses)
        /// </summary>
        /// <param name="urlAndData">The local file object where the path will set</param>
        /// <returns></returns>
        private IEnumerator DownloadDataToLocalCache(LocalFile urlAndData)
        {
            var url = urlAndData.SourceUrl;
            var optionalExtention = Path.GetExtension(url);
            var uwr = UnityWebRequest.Get(url);
            var guidFilename = Guid.NewGuid().ToString() + optionalExtention;
            string path = Path.Combine(Application.persistentDataPath, guidFilename);

            uwr.downloadHandler = new DownloadHandlerFile(path);
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                urlAndData.LocalFilePath = path;
            }
            else
            {
                urlAndData.LocalFilePath = "";
                if(debugLog) Debug.LogError("Download failed: " + uwr.error);
            }
        }

        private IEnumerator AdapterChain(LocalFile urlAndData)
        {
            // Get our interface references
            dataTypeAdapterInterfaces = new IDataTypeAdapter[dataTypeAdapters.Length];
            for (int i = 0; i < dataTypeAdapters.Length; i++)
                dataTypeAdapterInterfaces[i] = dataTypeAdapters[i] as IDataTypeAdapter;

            // Check data type per adapter using order set in inspector
            foreach (var adapter in dataTypeAdapterInterfaces)
            {
                if (adapter.Supports(urlAndData))
                {
                    if(debugLog) Debug.Log("<color=green>Adapter found: " + adapter.GetType().Name + "</color>");
                    adapter.Execute(urlAndData);
                    OnAdapterFound.Invoke(adapter);
                    yield break;
                }
            }

            CouldNotFindAdapter.Invoke(urlAndData.SourceUrl);
        }

        private void OnValidate() {
            if(!Application.isPlaying || !gameObject.activeInHierarchy)
                return;

            // Make sure all scriptable objects we plug in are of the correct type
            for (int i = 0; i < dataTypeAdapters.Length; i++)
            {
                if (dataTypeAdapters[i] is not IDataTypeAdapter)
                {
                    if(debugLog) Debug.LogError("ScriptableObject does not have the IDataTypeAdapter interface implemented. Removing from chain.", dataTypeAdapters[i]);
                    dataTypeAdapters[i] = null;
                }
            }
        }
    }
}
