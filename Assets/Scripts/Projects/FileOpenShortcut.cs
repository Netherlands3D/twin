using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Netherlands3D.JavascriptConnection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
#if !UNITY_EDITOR && UNITY_WEBGL
using Netherlands3D.JavascriptConnection;
#endif
namespace Netherlands3D.Twin
{
    public class FileOpenShortcut : MonoBehaviour
    {
        [Tooltip("Allowed file input selections")]
        [SerializeField]
        private string fileExtentions = "csv";

        [Tooltip("Allowed selection multiple files")]
        [SerializeField]
        private bool multiSelect = false;

        [DllImport("__Internal")]
        private static extern void AddFileInput(string inputName, string fileExtentions, bool multiSelect);
        
        public UnityEvent<string> onFilesSelected = new();

#if !UNITY_EDITOR && UNITY_WEBGL
        private string fileInputName;
        private FileInputIndexedDB javaScriptFileInputHandler;

        void Start()
        {
            javaScriptFileInputHandler = FindObjectOfType<FileInputIndexedDB>(true);
            if (javaScriptFileInputHandler == null)
            {
                GameObject go = new GameObject("UserFileUploads");
                javaScriptFileInputHandler = go.AddComponent<FileInputIndexedDB>();
            }

            // Set file input name with generated id to avoid html conflicts
            fileInputName += "_" + gameObject.GetInstanceID();
            name = fileInputName;

            // DrawHTMLOverCanvas javascriptInput = gameObject.AddComponent<DrawHTMLOverCanvas>();
            AddFileInput(fileInputName, fileExtentions, multiSelect);
        }
        
        public void ClickNativeButton()
        {
            javaScriptFileInputHandler.SetCallbackAddress(SendResults);
        }
        #endif
        
        public void SendResults(string filePaths)
        {
            Debug.Log("o: " + Keyboard.current.oKey.isPressed);
            Debug.Log("cmd: " + Keyboard.current.leftCommandKey.isPressed);
            Debug.Log("shortcut object received: " + filePaths);
            onFilesSelected.Invoke(filePaths);
        }
    }
}