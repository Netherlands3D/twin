using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KindMen.Uxios;
using Netherlands3D.Credentials.StoredAuthorization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.DataTypeAdapters
{
    public class DataTypeChain : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private bool debugLog = false;

        [Header("Data type adapters")] [Space(5)] [SerializeField]
        private ScriptableObject[] dataTypeAdapters;

        private IDataTypeAdapter<object>[] dataTypeAdapterInterfaces;
        public UnityEvent<IDataTypeAdapter<object>> OnAdapterFound = new();


        [Header("Events invoked on failures")] [Space(5)]
        public UnityEvent<string> CouldNotFindAdapter = new();

        public UnityEvent<string> OnDownloadFailed = new();
        public UnityEvent<string> OnLocalCacheFailed = new();

        private CancellationTokenSource cancellationTokenSource;

        private void Awake()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy)
                return;

            // Make sure all scriptable objects we plug in are of the correct type
            var list = new List<IDataTypeAdapter<object>>(dataTypeAdapters.Length);
            for (int i = 0; i < dataTypeAdapters.Length; i++)
            {
                if (dataTypeAdapters[i] is not IDataTypeAdapter<object>)
                {
                    if (debugLog) Debug.LogError("ScriptableObject does not have the IDataTypeAdapter interface implemented. Removing from chain.", dataTypeAdapters[i]);
                    continue;
                }
                list.Add(dataTypeAdapters[i] as IDataTypeAdapter<object>);
            }
            
            dataTypeAdapterInterfaces = list.ToArray();
        }

        private void OnDisable()
        {
            AbortChain();
        }

        //the void signature is needed for event listeners
        public void DetermineAdapter(Uri sourceUri, StoredAuthorization auth)
        {
            DetermineAdapterAndReturnResult(sourceUri, auth);
        }
        
        /// <summary>
        /// Determine the type of data using chain of responsibility
        /// </summary>
        /// <param name="url">Url to file or service</param>
        public Task<object> DetermineAdapterAndReturnResult(Uri sourceUri, StoredAuthorization auth)
        {
            AbortChain();
            cancellationTokenSource = new CancellationTokenSource();
            return DownloadAndCheckSupport(sourceUri, auth, cancellationTokenSource.Token);
        }

        private void AbortChain()
        {
            if (cancellationTokenSource == null)
                return;

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        private async Task<object> DownloadAndCheckSupport(Uri sourceUri, StoredAuthorization auth, CancellationToken token)
        {
            // Start by download the file, so we can do a detailed check of the content to determine the type
            var urlAndData = await DownloadDataToLocalCache(sourceUri, auth, token);
            token.ThrowIfCancellationRequested();

            // No local cache? Download failed.
            if (string.IsNullOrEmpty(urlAndData.LocalFilePath))
            {
                Debug.LogError("No local cache found, download failed");
                OnLocalCacheFailed?.Invoke(urlAndData.SourceUrl);
                return null;
            }

            token.ThrowIfCancellationRequested();
            
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            // Find the proper adapter in a chain of responsibility
            return AdapterChain(urlAndData);
        }

        /// <summary>
        /// We download the file to a local cache so we can check the content from the adapters using streamreading.
        /// This way we keep the heap memory usage low and can handle large files (like large obj's, or large WFS responses)
        /// </summary>
        /// <param name="urlAndData">The local file object where the path will set</param>
        /// <returns></returns>
        private Task<LocalFile> DownloadDataToLocalCache(Uri url, StoredAuthorization auth, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<LocalFile>();

            var localFile = new LocalFile
            {
                SourceUrl = url.ToString(),
                LocalFilePath = ""
            };

            var config = Config.Default();
            config = auth.AddToConfig(config);
            config.CancelToken = token;
            var promise = Uxios.DefaultInstance.Get<FileInfo>(url, config);

            // We want to use and manipulate urlAndData, so we 'curry' it by wrapping a method call in a lambda 
            promise.Then(response =>
            {
                if (token.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(token);
                    return;
                }
                
                var info = response.Data as FileInfo;
                localFile.LocalFilePath = info.FullName;
                tcs.TrySetResult(localFile);
            });
            promise.Catch(error =>
            {
                if (token.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(token);
                    return;
                }
                localFile.LocalFilePath = "";
                if (debugLog)
                {
                    Debug.LogError("Download failed: " + error.Message);
                }

                tcs.TrySetException(error);
                OnDownloadFailed.Invoke(localFile.SourceUrl);
            });

            return tcs.Task;
        }


        private object AdapterChain(LocalFile urlAndData)
        {
            // Check data type per adapter using order set in inspector
            foreach (var adapter in dataTypeAdapterInterfaces)
            {
                if (!adapter.Supports(urlAndData)) continue;

                if (debugLog) Debug.Log("<color=green>Adapter found: " + adapter.GetType().Name + "</color>");
                OnAdapterFound.Invoke(adapter);
                return adapter.Execute(urlAndData);
            }

            CouldNotFindAdapter.Invoke(urlAndData.SourceUrl);
            return null;
        }
    }
}