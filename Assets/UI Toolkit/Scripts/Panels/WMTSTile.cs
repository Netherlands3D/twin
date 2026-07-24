using System;
using KindMen.Uxios;
using Netherlands3D.Minimap;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class WMTSTile : VisualElement
{
    private const float fadeSpeed = 3.0f; // opacity per second

    private int zoomLevel;
    private Vector2 tileKey;
    private MinimapConfig minimapConfig;

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

        minimapConfig = config;

        style.width = size;
        style.height = size;
        style.left = xPosition;
        style.top = yPosition;

        container.Add(this);

        StartTextureDownload(zoomLevel, (int)tileKey.x, (int)tileKey.y);
    }

    private void StartTextureDownload(int zoom, int x, int y)
    {
        var tileImageUrl = this.minimapConfig.ServiceUrl.Replace("{zoom}", zoom.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());

        var config = new Config();
        var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(tileImageUrl), config);
        promise.Then(response =>
            {
                texture = response.Data as Texture2D;
                texture.wrapMode = TextureWrapMode.Clamp;
                style.backgroundImage = new StyleBackground(texture);
                StartFadeIn();
            }
        );
        promise.Catch(response =>
            Debug.Log("Could not find minimap tile :" + tileImageUrl)
        );
    }

    private void OnTextureDownloaded(Texture2D texture)
    {
        style.backgroundImage = new StyleBackground(texture);
        StartFadeIn();
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

        if (texture != null)
        {
            Object.Destroy(texture);
            texture = null;
        }

        style.backgroundImage = StyleKeyword.None;

        RemoveFromHierarchy();
    }
}