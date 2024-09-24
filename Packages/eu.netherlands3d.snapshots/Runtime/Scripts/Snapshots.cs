/*
*  Copyright (C) X Gemeente
*                X Amsterdam
*                X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://github.com/Amsterdam/3DAmsterdam/blob/master/LICENSE.txt
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/

using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Snapshots
{
    public class Snapshots : MonoBehaviour
    {
        //import this from filebrowser package which includes tghe download functions in its jslib
        [DllImport("__Internal")]
        private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

        [Tooltip("Optional source camera (Defaults to Camera.main)")]
        [SerializeField] private Camera sourceCamera;

        [SerializeField] private bool useViewSize = true;
        [SerializeField] private int width = 1920;
        [SerializeField] private int height = 1080;
        [SerializeField] private string targetPath = "screenshots";
        [SerializeField] private string fileName = "Snapshot";
        [SerializeField] private SnapshotFileType fileType = SnapshotFileType.png;
        [SerializeField] private LayerMask snapshotLayers;

        public int Width
        {
            get => width;
            set
            {
                useViewSize = false;
                width = value;
            }
        }

        public int Height
        {
            get => height;
            set
            {
                useViewSize = false;
                height = value;
            }
        }

        public string FileName { get => fileName; set => fileName = value; }
        public string TargetPath { get => targetPath; set => targetPath = value; }

        public string FileType
        {
            get => fileType.ToString();
            set
            {
                if (Enum.TryParse(value, out fileType) == false)
                {
                    fileType = SnapshotFileType.png;
                }
            }
        }

        public LayerMask SnapshotLayers { get => snapshotLayers; set => snapshotLayers = value;}

        private void Start()
        {
            if (!sourceCamera) sourceCamera = Camera.main;
        }
        
        public void UseViewSize(bool useViewSize) => this.useViewSize = useViewSize;

        public void TakeSnapshot()
        {
            var snapshotWidth = (useViewSize) ? Screen.width : width;
            var snapshotHeight = (useViewSize) ? Screen.height : height;

            byte[] bytes = Snapshot.ToImageBytes(snapshotWidth, snapshotHeight, sourceCamera, snapshotLayers, fileType);

            var path = DetermineSaveLocation();

#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadFile(gameObject.name, "OnSnapshotDownloadComplete", Path.GetFileName(path), bytes, bytes.Length);
#else
            File.WriteAllBytes(path, bytes);
#endif
        }

        public void OnSnapshotDownloadComplete(string message)
        {
            //Debug.Log("File download complete: " + message);
        }

        private string DetermineSaveLocation()
        {
            var outputFileName = fileName;
            if (string.IsNullOrEmpty(outputFileName))
            {
                outputFileName = $"Snapshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            }
            string location = Application.persistentDataPath;

#if UNITY_WEBGL && !UNITY_EDITOR
            outputFileName = $"{outputFileName}.{FileType}";
            location += Path.DirectorySeparatorChar
                + targetPath
                + Path.DirectorySeparatorChar
                + outputFileName;
#else       
            StandaloneFileBrowser.SaveFilePanel("Save texture as file", location, outputFileName, FileType.ToString());
#endif
            return location;
        }
    }
}
