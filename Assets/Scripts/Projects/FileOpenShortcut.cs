using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class FileOpenShortcut : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiselect);
        [Tooltip("Allowed file input selections")]
        [SerializeField] private string fileExtentions = "nl3d";

        [Tooltip("Allowed selection multiple files")]
        [SerializeField] private bool multiSelect = false;        
        
        public UnityEvent<string> onFilesSelected = new();

        public void OpenFileDialog()
        {
            // Call the JavaScript function to open the file dialog
            UploadFile(gameObject.name, "OnFileSelected", fileExtentions, multiSelect);
        }
        
        // This is the callback method that gets called when the file is selected
        public void OnFileSelected(string filePaths)
        {
            Debug.Log("shortcut received: " + filePaths);
            onFilesSelected.Invoke(filePaths);        
        }
    }
}
