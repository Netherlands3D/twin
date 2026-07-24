using Netherlands3D.Minimap;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class WMTSTile : VisualElement
{
    private const float fadeSpeed = 3.0f; // opacity per second

    private int zoomLevel;
    private Vector2 tileKey;
    private MinimapConfig config;

    private UnityWebRequest uwr;
    private Texture2D texture;
    private float opacity;

    private IVisualElementScheduledItem downloadPollTask;
    private IVisualElementScheduledItem fadeTask;

    public WMTSTile()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.opacity = 0f;
        style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
    }

    public void Initialize(VisualElement container, int zoom, float size, float xPosition, float yPosition, Vector2 key, MinimapConfig config)
    {
        zoomLevel = zoom;
        tileKey = key;
        name = tileKey.x + "/" + tileKey.y;

        this.config = config;

        style.width = size;
        style.height = size;
        style.left = xPosition;
        style.top = yPosition;

        container.Add(this);

        StartTextureDownload(zoomLevel, (int)tileKey.x, (int)tileKey.y);
    }

    private void StartTextureDownload(int zoom, int x, int y)
    {
        var tileImageUrl = config.ServiceUrl.Replace("{zoom}", zoom.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());

        uwr = UnityWebRequestTexture.GetTexture(tileImageUrl, true);
        uwr.SendWebRequest();

        // Todo: temp code replace this with Uxios?
        downloadPollTask = schedule.Execute(() => CheckDownloadProgress(tileImageUrl)).Every(16);
    }

    private void CheckDownloadProgress(string tileImageUrl)
    {
        if (uwr == null || !uwr.isDone) return;

        downloadPollTask?.Pause();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Could not find minimap tile :" + tileImageUrl);
        }
        else
        {
            texture = DownloadHandlerTexture.GetContent(uwr);
            texture.wrapMode = TextureWrapMode.Clamp;
            style.backgroundImage = new StyleBackground(texture);
            StartFadeIn();
        }

        uwr.Dispose();
        uwr = null;
    }

    private void StartFadeIn()
    {
        opacity = 0f;
        style.opacity = opacity;

        fadeTask = schedule.Execute(() =>
        {
            opacity += fadeSpeed * (16f / 1000f);
            if (opacity >= 1.0f)
            {
                opacity = 1.0f;
                style.opacity = opacity;
                fadeTask?.Pause();
            }
            else
            {
                style.opacity = opacity;
            }
        }).Every(16);
    }

    public void Dispose()
    {
        downloadPollTask?.Pause();
        fadeTask?.Pause();

        uwr?.Dispose();
        uwr = null;

        if (texture != null)
        {
            Object.Destroy(texture);
            texture = null;
        }

        style.backgroundImage = StyleKeyword.None;

        RemoveFromHierarchy();
    }
}