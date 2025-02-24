using System;
using System.Collections;
using System.IO;
using KindMen.Uxios;
using KindMen.Uxios.Api;
using KindMen.Uxios.Errors;
using KindMen.Uxios.Errors.Http;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.DataTypeAdapters
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
        public UnityEvent<string> OnAuthenticationFailed = new();
        public UnityEvent<string> OnAuthenticationSucceeded = new();

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
            if (chain == null) return;

            StopCoroutine(chain);
        }

        private IEnumerator DownloadAndCheckSupport(string url)
        {
            // Start by download the file, so we can do a detailed check of the content to determine the type
            var urlAndData = new LocalFile { SourceUrl = url, LocalFilePath = "" };

            yield return DownloadDataToLocalCache(urlAndData);

            // No local cache? Download failed.
            if (string.IsNullOrEmpty(urlAndData.LocalFilePath))
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
            var futureFileInfo = Resource<FileInfo>.At(urlAndData.SourceUrl).Value;

            // We want to use and manipulate urlAndData, so we 'curry' it by wrapping a method call in a lambda 
            futureFileInfo.Then(info =>
            {
                
                DownloadSucceeded(urlAndData, info);
            });        
            futureFileInfo.Catch(error =>
            {
                if(error is AuthenticationError)
                {
                    OnAuthenticationFailed?.Invoke(urlAndData.SourceUrl);
                }
                DownloadFailed(urlAndData, error);
            });
            
            yield return Uxios.WaitForRequest(futureFileInfo);
        }

        private string DownloadSucceeded(LocalFile urlAndData, FileSystemInfo info)
        {
            // Ideally, we want to keep the fileInfo object because you can do cool stuff with it, but for now:
            // let's fit it in the existing LocalFile object.
            return urlAndData.LocalFilePath = info.FullName;
        }

        private void DownloadFailed(LocalFile urlAndData, Exception error)
        {
            urlAndData.LocalFilePath = "";
            if (debugLog)
            {
                Debug.LogError("Download failed: " + error.Message);
            }
        }

        private IEnumerator AdapterChain(LocalFile urlAndData)
        {
            // Get our interface references
            dataTypeAdapterInterfaces = new IDataTypeAdapter[dataTypeAdapters.Length];
            for (int i = 0; i < dataTypeAdapters.Length; i++)
            {
                if(dataTypeAdapters[i] == null)
                {
                    Debug.LogError("An adapter in chain is null. Please check your dataTypeAdapters list.",this.gameObject);
                    yield break;
                }

                dataTypeAdapterInterfaces[i] = dataTypeAdapters[i] as IDataTypeAdapter;
            }

            // Check data type per adapter using order set in inspector
            foreach (var adapter in dataTypeAdapterInterfaces)
            {
                if (!adapter.Supports(urlAndData)) continue;

                if(debugLog) Debug.Log("<color=green>Adapter found: " + adapter.GetType().Name + "</color>");
                adapter.Execute(urlAndData);
                OnAdapterFound.Invoke(adapter);
                yield break;
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
