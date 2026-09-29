using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;
using SFB;
using UnityEngine.Events;

#if !UNITY_EDITOR && UNITY_WEBGL
using Netherlands3D.JavascriptConnection;
#endif

//todo shouldnt this be like a importerservice from now on?
public class FileImportService : MonoBehaviour //todo: the FileOpener prefab should no longer rely on the scriptable event after transition to UI Toolkit
{
    [DllImport("__Internal")]
    [UsedImplicitly]
    private static extern void BrowseForFile(string inputFieldName);
    
    [DllImport("__Internal")]
    private static extern void SetAllowedDropExtensions(string extensions);
    
    [DllImport("__Internal")]
    private static extern void SetFileImporterCallbackObject(string objectName);
    
    public UnityEvent<string> filesImportedEvent;

    [DllImport("__Internal")]
    private static extern void InitializeIndexedDB(string dataPath);
    
    [DllImport("__Internal")]
    private static extern void SyncFilesFromIndexedDB(string callbackObject,string callbackMethod);

    [DllImport("__Internal")]
    private static extern void SyncFilesToIndexedDB(string callbackObject, string callbackMethod);
    

    private Action<string> callbackAddress;
    private List<string> filenames = new List<string>();
    private int numberOfFilesToLoad = 0;
    private int fileCount = 0;
    

    [Tooltip("Allowed selection multiple files")] [SerializeField]
    private bool multiSelect = false;

    public UnityEvent<string> onFilesSelected = new();
    public UnityEvent<string> onFilesNotSupported = new();
    
    public List<string> SupportedFileTypes => supportedFileTypes;
    
    private readonly List<string> baseSupportedFileTypes = new(){ "obj", "csv", "json", "geojson", "glb" };
    private List<string> supportedFileTypes = new List<string>();

#if !UNITY_EDITOR && UNITY_WEBGL
    private string fileInputName = string.Empty;
    private DrawHTMLOverCanvas javaScriptInput;
#endif

    private void Awake()
    {
        supportedFileTypes.AddRange(baseSupportedFileTypes);
#if !UNITY_EDITOR && UNITY_WEBGL
        InitializeIndexedDB(Application.persistentDataPath);
        SetFileImporterCallbackObject(this.gameObject.name);
#endif
    }
    
    private void Start()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        CreateJavaScriptImporter();
#endif
    }

#if !UNITY_EDITOR && UNITY_WEBGL
    private void CreateJavaScriptImporter()
    {
        fileInputName = "_" + gameObject.GetInstanceID();

        // Each FileOpen gets its own DrawHTMLOverCanvas and HTML input element
        javaScriptInput = gameObject.AddComponent<DrawHTMLOverCanvas>();
        javaScriptInput.AlignObjectID(fileInputName, false);

        SetAllowedDropExtensions(string.Join(",", supportedFileTypes));
    }

    private void SetJavaScriptFileExtensions(string fileExtentions)
    {
        javaScriptInput.SetupInput(fileInputName, fileExtentions, multiSelect);
    }
    
#endif

    public void ClickNativeButton() //called in the jslib
    {
    }
    
    public void UnsupportedFileDropped(string fileNames)
    {
        onFilesNotSupported.Invoke(fileNames);
    }

    /// <summary>
    /// Opens the File browser to pick a file to import
    /// </summary>
    public void OpenFile(string fileExtentions)
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        SetCallbackAddress(SendResults);
        SetJavaScriptFileExtensions(fileExtentions);
        BrowseForFile(fileInputName);
#else
        string[] fileExtentionNames = fileExtentions.Split(',');
        ExtensionFilter[] extentionfilters = new ExtensionFilter[1];

        extentionfilters[0] = new ExtensionFilter(fileExtentionNames[0], fileExtentionNames);

        string[] filenames = SFB.StandaloneFileBrowser.OpenFilePanel("select file(s)", "", extentionfilters, multiSelect);
        string resultingFiles = "";
        for (int i = 0; i < filenames.Length; i++)
        {
            string destinationFolder = Application.persistentDataPath;
            string originalFileName = System.IO.Path.GetFileName(filenames[i]);
            string destinationPath = System.IO.Path.Combine(destinationFolder, originalFileName);

            int counter = 1;
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(originalFileName);
            string fileExtension = System.IO.Path.GetExtension(originalFileName);

            while (System.IO.File.Exists(destinationPath))
            {
                // Create a new filename with a counter appended
                string newFileName = $"{fileNameWithoutExtension}({counter}){fileExtension}";
                destinationPath = System.IO.Path.Combine(destinationFolder, newFileName);
                counter++;
            }

            System.IO.File.Copy(filenames[i], destinationPath, true);
            resultingFiles += System.IO.Path.GetFileName(destinationPath) + ",";
        }

        SendResults(resultingFiles);
#endif
    }

    public void SendResults(string filePaths)
    {
        Debug.Log("button received: " + filePaths);
        onFilesSelected.Invoke(filePaths);
    }
    
    public void AddSupportedFileType(string extention)
    {
        if(supportedFileTypes.Contains(extention)) return;
        
        supportedFileTypes.Add(extention);
        
        SetAllowedDropExtensions(string.Join(",", supportedFileTypes));
    }
        
    public void RemoveSupportedFileType(string extention)
    {
        supportedFileTypes.Remove(extention);
        
        SetAllowedDropExtensions(string.Join(",", supportedFileTypes));
    }

    public bool IsExtentionExperimental(string extention) //todo this generic enough?
    {
        return !baseSupportedFileTypes.Contains(extention) && supportedFileTypes.Contains(extention);
    }

    public void SetCallbackAddress(Action<string> callback)
    {
        Debug.Log("Callback set for FileInputIndexedDB");
        callbackAddress = callback;
    }

    // Called from javascript, the total number of files that are being loaded.
    public void FileCount(int count)
    {
        numberOfFilesToLoad = count;
        fileCount = 0;
        filenames = new List<string>();
        Debug.Log("expecting " + count + " files");

        StartCoroutine(WaitForFilesToBeLoaded());
    }

    //called from javascript
    public void LoadFile(string filename)
    {
        filenames.Add(filename);
        fileCount++;
        Debug.Log("received: " + filename);
    }

    // called from javascript
    public void LoadFileError(string name)
    {
        fileCount++;
        //LoadingScreen.Instance.Hide();
        Debug.Log("unable to load " + name);
    }

    // runs while javascript is busy saving files to indexedDB.
    IEnumerator WaitForFilesToBeLoaded()
    {
        while (fileCount < numberOfFilesToLoad)
        {
            yield return null;
        }
        numberOfFilesToLoad = 0;
        fileCount = 0;
        ProcessFiles();
    }

    public void ProcessFiles()
    {
        // start js-function to update the contents of application.persistentdatapath to match the contents of indexedDB.
        SyncFilesFromIndexedDB(this.gameObject.name, "IndexedDBUpdated");
    }

    public void IndexedDBUpdated() // called from SyncFilesFromIndexedDB
    {
        ProcessAllFiles();
    }

    void ProcessAllFiles()
    {
        var files = string.Join(",", filenames);
        if (callbackAddress == null)
        {
            Debug.Log("FileInputIndexedDB: No callback set. Using default file import event.");
            filesImportedEvent.Invoke(files);
        }
        else
        {
            callbackAddress(files);
            callbackAddress = null;
        }
    }

    public void IndexedDBSyncCompleted()
    {
        Debug.Log("Synced Unity file changes back to IndexedDB");
    }

    public void ClearDatabase(bool succes)
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        filenames.Clear();
        if (succes)
        {
            SyncFilesToIndexedDB(this.gameObject.name,"IndexedDBSyncCompleted");
        }
#endif
    }
}