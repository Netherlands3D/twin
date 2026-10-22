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
using System.IO;
using System.Runtime.InteropServices;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
#if UNITY_EDITOR
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

        [SerializeField] private bool useViewSize = true;
        [SerializeField] private int width = 1920;
        [SerializeField] private int height = 1080;
        [SerializeField] private string targetPath = "screenshots";
        [SerializeField] private string fileName = "Snapshot";
        [SerializeField] private SnapshotFileType fileType = SnapshotFileType.png;
        [SerializeField] private LayerMask snapshotLayers;
        
        public UnityEvent<string> DownloadSnapshotComplete = new();

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

        public void TakeSnapshot()
        {
            var snapshotWidth = useViewSize ? Screen.width : width;
            var snapshotHeight = useViewSize ? Screen.height : height;

            Camera sourceCamera = ServiceLocator.GetService<CameraService>().ActiveCamera;

            RenderTexture previousTarget = sourceCamera.targetTexture;
            int previousMask = sourceCamera.cullingMask;
            RenderTexture previousActive = RenderTexture.active;

            RenderTexture renderTexture = new RenderTexture(
                snapshotWidth,
                snapshotHeight,
                24,
                RenderTextureFormat.ARGB32);

            renderTexture.antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            renderTexture.Create();

            Texture2D texture = null;

            try
            {
                sourceCamera.cullingMask = snapshotLayers;
                sourceCamera.targetTexture = renderTexture;

                sourceCamera.Render();

                RenderTexture.active = renderTexture;

                texture = new Texture2D(
                    snapshotWidth,
                    snapshotHeight,
                    TextureFormat.RGB24,
                    false);

                texture.ReadPixels(
                    new Rect(0, 0, snapshotWidth, snapshotHeight),
                    0,
                    0);

                // Only needed if you use the Texture2D inside Unity afterwards.
                // texture.Apply();

                byte[] bytes = fileType switch
                {
                    SnapshotFileType.png => texture.EncodeToPNG(),
                    SnapshotFileType.jpg => texture.EncodeToJPG(),
                    SnapshotFileType.raw => texture.GetRawTextureData(),
                    _ => texture.EncodeToPNG()
                };

                var path = DetermineSaveLocation();

        #if UNITY_WEBGL && !UNITY_EDITOR
                DownloadFile(gameObject.name, "OnSnapshotDownloadComplete", Path.GetFileName(path), bytes, bytes.Length);
        #else
                File.WriteAllBytes(path, bytes);
        #endif
            }
            finally
            {
                sourceCamera.targetTexture = previousTarget;
                sourceCamera.cullingMask = previousMask;
                RenderTexture.active = previousActive;

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Destroy(renderTexture);
                }

                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }

        public void OnSnapshotDownloadComplete(string message)
        {
            DownloadSnapshotComplete.Invoke(message);
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
            StandaloneFileBrowser.SaveFilePanel("Save texture as file", "", outputFileName, FileType.ToString());
#endif
            return location;
        }
    }
}
