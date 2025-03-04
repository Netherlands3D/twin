using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Netherlands3D.Sun;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Snapshots
{
    public class PeriodicSnapshots : MonoBehaviour
    {
        //import this from filebrowser package which includes tghe download functions in its jslib        
        [DllImport("__Internal")]
        private static extern void DownloadFromIndexedDB(string filePath, string callbackObject, string callbackMethod);

        [DllImport("__Internal")]
        private static extern void SyncFilesToIndexedDB(string callbackObjectName, string callbackMethodName);

        public UnityEvent<string> DownloadSnapshotComplete = new();

        public Font font;

        [Serializable]
        public class Moment
        {
            [HideInInspector]
            public string name = "";

            [Range(1, 31)]
            public int day;
            [Range(1, 12)]
            public int month;
            [Range(0, 23)]
            public int hour;

            public DateTime ToDateTime()
            {
                return new DateTime(DateTime.Now.Year, month, day, hour, 0, 0);
            }
        }

        public List<Moment> Moments
        {
            get { return moments; }
            set
            {
                moments = value;
            }
        }

        [SerializeField] private Camera sourceCamera;
        [SerializeField] private SunTime sunTime;
        [SerializeField] private int snapshotWidth = 1024;
        [SerializeField] private int snapshotHeight = 768;
        [SerializeField] private LayerMask snapshotLayers;
        [SerializeField] private List<Moment> moments = new();

        [Tooltip("Generating can take a while, this event can be used to show a loader")]
        public UnityEvent onStartGenerating = new();

        [Tooltip("Generating can take a while, this event can be used to show the progress while generating")]
        public UnityEvent<float> onProgress = new();

        [Tooltip("Generating can take a while, this event can be used to hide a loader")]
        public UnityEvent onFinishedGenerating = new();

        private void Start()
        {
            if (!sourceCamera) sourceCamera = Camera.main;
        }

        private void OnValidate()
        {
            foreach (var moment in moments)
            {
                moment.name = $"{moment.day}-{moment.month} {moment.hour}:00";
            }
        }

        public void TakeSnapshots()
        {
            string timestamp = $"{DateTime.Now:yyyy-MM-ddTHH-mm}";
            var path = FetchPath(timestamp);

            StartCoroutine(TakeSnapshotsAcrossFrames(timestamp, path));
        }

        public void DownloadSnapshots()
        {
            string timestamp = $"{DateTime.Now:yyyy-MM-ddTHH-mm}";
            var path = FetchPath(timestamp);

            StartCoroutine(DownloadSnapshots(timestamp, path));
        }

        private IEnumerator DownloadSnapshots(string timestamp, string path)
        {
            yield return TakeSnapshotsAcrossFrames(timestamp, path);

#if UNITY_WEBGL && !UNITY_EDITOR
            var archivePath = FetchArchivePath(timestamp);
            SyncFilesToIndexedDB(gameObject.name, "SnapshotSavedToIndexedDB");
            lastSavePath = Path.GetFileName(archivePath);
#endif
        }

        private string lastSavePath;

        public void SnapshotSavedToIndexedDB()
        {
            var fileName = Path.GetFileName(lastSavePath);
            DownloadFromIndexedDB($"{fileName}", gameObject.name, "OnSnapshotDownloadComplete");
        }

        private IEnumerator TakeSnapshotsAcrossFrames(string timestamp, string path)
        {
            onStartGenerating.Invoke();

            var cachedTimeOfDay = sunTime.GetTime();
            for (var index = 0; index < moments.Count; index++)
            {
                onProgress.Invoke(1f / moments.Count * (index + 1));

                yield return TakeSnapshot(moments[index], path);
            }
            sunTime.SetTime(cachedTimeOfDay);

            var archiveFilePath = FetchArchivePath(timestamp);
            if (File.Exists(archiveFilePath)) File.Delete(archiveFilePath);

            ZipFile.CreateFromDirectory(path, archiveFilePath);
            Directory.Delete(path, true);

            // Make sure no rounding errors occur and set it to 1f
            onProgress.Invoke(1f);

            onFinishedGenerating.Invoke();
        }

        private static string FetchArchivePath(string timestamp)
        {
            return $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}{timestamp}-schaduwstudie.zip";
            //2025-03-04T15-51-schaduwstudie.zip
        }

        private static string FetchPath(string timestamp)
        {
            string path = $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}{timestamp}-schaduwstudie";
            if (Directory.Exists(path))
            {
                throw new Exception(
                    $"Path '{path}' should not exist, aborting taking a burst of snapshots to prevent inadvertent data loss"
                );
            }

            Directory.CreateDirectory(path);

            return path;
        }

        private IEnumerator TakeSnapshot(Moment moment, string path)
        {
            sunTime.SetDay(moment.day);
            sunTime.SetMonth(moment.month);
            sunTime.SetHour(moment.hour);
            sunTime.SetMinutes(0);
            sunTime.SetSeconds(0);

            // Skip frame to give rendering time to do its magic
            yield return null;

            byte[] bytes = Snapshot.ToImageBytes(
                snapshotWidth,
                snapshotHeight,
                sourceCamera,
                snapshotLayers,
                SnapshotFileType.png
            );

            DateTime dateTime = moment.ToDateTime();

            Texture2D texture = CreateTimestampTexture(bytes, dateTime, snapshotWidth, snapshotHeight);
            bytes = texture.EncodeToPNG();
            Destroy(texture );

            File.WriteAllBytes($"{path}{Path.DirectorySeparatorChar}{dateTime:yyyy-MM-ddTHH-mm}.png", bytes);
        }

        public Texture2D CreateTimestampTexture(byte[] bytes, DateTime time, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.LoadImage(bytes);

            Camera customCamera = new GameObject("TimestampCamera").AddComponent<Camera>(); //coudl be optimized
            customCamera.orthographic = true;
            customCamera.orthographicSize = 1;
            customCamera.clearFlags = CameraClearFlags.SolidColor;
            customCamera.backgroundColor = Color.clear;
            customCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");

            int textureWidth = 256;
            int textureHeight = 50;

            RenderTexture renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);  //clear buffer
            GameObject textObject = new GameObject("TimestampText");
            textObject.layer = LayerMask.NameToLayer("UI");
            textObject.transform.SetParent(transform);
            TextMesh textComponent = textObject.AddComponent<TextMesh>();
            textComponent.font = font;
            textComponent.text = time.ToString("yyyy-MM-dd HH:mm:ss");
            textComponent.fontSize = 200;
            textComponent.color = Color.white;
            textComponent.characterSize = 1;
            textComponent.alignment = TextAlignment.Center;
            textComponent.anchor = TextAnchor.MiddleCenter;

            customCamera.orthographic = true;
            customCamera.orthographicSize = textureHeight * 0.5f;
            customCamera.transform.position = new Vector3(0, 0, -1);
            customCamera.transform.rotation = Quaternion.identity;
            customCamera.targetTexture = renderTexture;
            customCamera.Render();

            Texture2D finalTexture = new Texture2D(textureWidth, textureHeight);
            finalTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
            finalTexture.Apply();

            //make see through
            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    Color col = finalTexture.GetPixel(x, y);
                    
                    if (col.a == 0) 
                    {
                        Color bgPixelColor = texture.GetPixel(width - textureWidth + x, height - textureHeight + y);
                        finalTexture.SetPixel(x, y, Color.Lerp(bgPixelColor, col, 0.5f));
                    }
                    else
                    {                       
                        finalTexture.SetPixel(x, y, col);
                    }
                }
            }

            finalTexture.Apply();

            Destroy(textObject);
            RenderTexture.active = null;
            customCamera.targetTexture = null;

            int posX = width - finalTexture.width;
            int posY = height - finalTexture.height;

            Color[] pixels = finalTexture.GetPixels();
            texture.SetPixels(posX, posY, finalTexture.width, finalTexture.height, pixels);
            texture.Apply();

            Destroy(customCamera.gameObject);
            return texture;
        }
    }
}
