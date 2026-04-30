using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using SFB;
using UnityEngine.Events;

#if !UNITY_EDITOR && UNITY_WEBGL
using Netherlands3D.JavascriptConnection;
#endif

public class FileOpen : MonoBehaviour //todo: the FileOpener prefab should no longer rely on the scriptable event after transition to UI Toolkit
{
    [DllImport("__Internal")]
    [UsedImplicitly]
    private static extern void BrowseForFile(string inputFieldName);

    // [Tooltip("Allowed file input selections")] [SerializeField]
    // private string fileExtentions = "csv"; //todo: when transition to UI toolkit is complete, the serialized extensions should be able to be deleted and passed from the UI component

    [Tooltip("Allowed selection multiple files")] [SerializeField]
    private bool multiSelect = false;

    public UnityEvent<string> onFilesSelected = new();

#if !UNITY_EDITOR && UNITY_WEBGL
    private string fileInputName = string.Empty;
    private FileInputIndexedDB javaScriptFileInputHandler;
    private DrawHTMLOverCanvas javaScriptInput;
#endif

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

        var existingHandler = FindObjectOfType<FileInputIndexedDB>(true);
        if (existingHandler != null)
        {
            javaScriptFileInputHandler = existingHandler;
        }
        else
        {
            GameObject go = new GameObject("UserFileUploads");
            javaScriptFileInputHandler = go.AddComponent<FileInputIndexedDB>();
        }

        // Each FileOpen gets its own DrawHTMLOverCanvas and HTML input element
        javaScriptInput = gameObject.AddComponent<DrawHTMLOverCanvas>();
        javaScriptInput.AlignObjectID(fileInputName, false);
    }

    private void SetJavaScriptFileExtensions(string fileExtentions)
    {
        javaScriptInput.SetupInput(fileInputName, fileExtentions, multiSelect);
    }
    
#endif

    public void ClickNativeButton() //called in the jslib
    {
    }

    /// <summary>
    /// Opens the File browser to pick a file to import
    /// </summary>
    public void OpenFile(string fileExtentions)
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        javaScriptFileInputHandler.SetCallbackAddress(SendResults);
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
}