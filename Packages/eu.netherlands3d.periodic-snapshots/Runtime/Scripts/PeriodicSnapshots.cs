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
        [SerializeField] private Texture2D timeStampLabelBackground;
        [SerializeField] private int labelPaddingWidth = 10;
        [SerializeField] private int labelPaddingHeight = 10;
        [SerializeField] private Color timeStampTextColor = Color.white;
        [SerializeField] private string archiveName = "snapshot-series-";
        [SerializeField] private string timeStampDateFormat = "yyyy-MM-dd HH:mm";

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
            if (timeStampLabelBackground == null)
            {
                Debug.LogError("You are missing a label texture for the timestamp of the periodic snapshots");
                yield break;
            }

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

        private string FetchArchivePath(string timestamp)
        {
            return $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}{timestamp}{archiveName}.zip";
        }

        private string FetchPath(string timestamp)
        {
            string path = $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}{timestamp}{archiveName}";
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
            Destroy(texture);

            File.WriteAllBytes($"{path}{Path.DirectorySeparatorChar}{dateTime:yyyy-MM-ddTHH-mm}.png", bytes);
        }

        public Texture2D CreateTimestampTexture(byte[] bytes, DateTime time, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.LoadImage(bytes);          

            int textureWidth = timeStampLabelBackground.width;
            int textureHeight = timeStampLabelBackground.height;

            RenderTexture renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);  //clear buffer

            TextMesh textComponent = GetTimeStampTextComponent(time);
            Camera customCamera = GetTextRenderCamera();
            customCamera.targetTexture = renderTexture;
            customCamera.Render();

            Texture2D textTexture = new Texture2D(textureWidth, textureHeight);
            textTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
            textTexture.Apply();

            Texture2D timeStampTexture = GetBlendedTimestampTexture(width, height, textureWidth, textureHeight, textTexture, texture);

            Destroy(textComponent.gameObject);
            renderTexture.Release();
            RenderTexture.active = null;
            customCamera.targetTexture = null;

            int posX = width - timeStampTexture.width - labelPaddingWidth;
            int posY = height - timeStampTexture.height - labelPaddingHeight;

            Color[] pixels = timeStampTexture.GetPixels();
            texture.SetPixels(posX, posY, timeStampTexture.width, timeStampTexture.height, pixels);
            texture.Apply();

            Destroy(customCamera.gameObject);
            return texture;
        }

        private Camera GetTextRenderCamera()
        {
            Camera customCamera = new GameObject("TimestampCamera").AddComponent<Camera>(); //coudl be optimized          
            customCamera.clearFlags = CameraClearFlags.SolidColor;
            customCamera.backgroundColor = Color.clear;
            customCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            customCamera.orthographic = true;
            customCamera.orthographicSize = timeStampLabelBackground.height * 0.5f;
            customCamera.transform.position = new Vector3(0, 0, -1);
            customCamera.transform.rotation = Quaternion.identity;
            return customCamera;
        }

        private TextMesh GetTimeStampTextComponent(DateTime time)
        {
            GameObject textObject = new GameObject("TimestampText");
            textObject.layer = LayerMask.NameToLayer("UI");
            textObject.transform.SetParent(transform);
            TextMesh textComponent = textObject.AddComponent<TextMesh>();
            textComponent.font = font;
            textComponent.text = time.ToString(timeStampDateFormat);
            textComponent.fontSize = 150;
            textComponent.color = timeStampTextColor;
            textComponent.characterSize = 1;
            textComponent.alignment = TextAlignment.Center;
            textComponent.anchor = TextAnchor.MiddleCenter;
            return textComponent;
        }

        private Texture2D GetBlendedTimestampTexture(int baseTextureWidth, int baseTextureHeight, int width, int height, Texture2D textTexture, Texture2D baseTexture)
        {
            Texture2D timeStampTexture = new Texture2D(width, height);
            //make see through
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color textCol = textTexture.GetPixel(x, y);
                    Color col = timeStampLabelBackground.GetPixel(x, y);
                    //if the alpha is 0 it means there are no text texture pixels present, so lets take the label pixels here and blend it into the screenshot background
                    if (textCol.a == 0)
                    {
                        //get the pixel of the actual screenshot
                        Color baseCol = baseTexture.GetPixel(baseTextureWidth - width - labelPaddingWidth + x, baseTextureHeight - height - labelPaddingHeight + y);
                        float alpha = col.a;
                        //blend the pixel of label into the base pixel by its alpha (basic blending)
                        Color blendedColor = Color.Lerp(baseCol, col, alpha);

                        timeStampTexture.SetPixel(x, y, blendedColor);
                    }
                    else
                    {
                        timeStampTexture.SetPixel(x, y, textCol);
                    }
                }
            }

            timeStampTexture.Apply();
            return timeStampTexture;
        }
    }
}
