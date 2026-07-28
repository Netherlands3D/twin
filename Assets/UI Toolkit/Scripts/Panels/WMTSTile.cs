using System;
using KindMen.Uxios;
using Netherlands3D.Minimap;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class WMTSTile : VisualElement
{
    private const float fadeSpeed = 3.0f; // opacity per second
    private readonly VisualElement visual; //inner element handles scale, outer element (this) handles positioning

    private int zoomLevel;
    private Vector2 tileKey;
    private MinimapConfig minimapConfig;

    private Texture2D texture;
    private float opacity;

    private const float tileOverlap = 1f; // to fix seams

    public WMTSTile()
    {
        this.CloneComponentTree("Components");
        this.AddComponentStylesheet("Components");
        
        pickingMode = PickingMode.Ignore;
        visual = this.Q<VisualElement>("Visual");
        visual.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0));
    }

    public void Initialize(VisualElement container, int zoom, float size, float xPosition, float yPosition, Vector2 key, MinimapConfig config)
    {
        zoomLevel = zoom;
        tileKey = key;
        minimapConfig = config;
        name = tileKey.x + "/" + tileKey.y + "/" + zoomLevel;

        var nativeSize = minimapConfig.TileMatrixSet.TileSize;

        // Outer node: fixed native size, handles position
        style.width = nativeSize;
        style.height = nativeSize;
        style.left = 0;
        style.top = 0;
        transform.position = new Vector3(xPosition, yPosition, 0f);

        // Inner node: handles scale (shrunk down)
        visual.style.width = nativeSize + tileOverlap;
        visual.style.height = nativeSize + tileOverlap;
        visual.transform.scale = new Vector3(size / nativeSize, size / nativeSize, 1f);

        container.Add(this);

        StartTextureDownload(zoomLevel, (int)tileKey.x, (int)tileKey.y);
    }

    private void StartTextureDownload(int zoom, int x, int y)
    {
        var tileImageUrl = minimapConfig.ServiceUrl.Replace("{zoom}", zoom.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());

        var config = new Config();
        var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(tileImageUrl), config);
        promise.Then(response =>
            {
                texture = response.Data as Texture2D;
                texture.wrapMode = TextureWrapMode.Clamp;
                visual.style.backgroundImage = new StyleBackground(texture);
                StartFadeIn();
            }
        );
        promise.Catch(response =>
            Debug.Log("Could not find minimap tile :" + tileImageUrl)
        );
    }

    private void StartFadeIn()
    {
        visual.AddToClassList("visible");
    }

    public void Dispose()
    {
        if (texture != null)
        {
            Object.Destroy(texture);
            texture = null;
        }

        visual.style.backgroundImage = StyleKeyword.None;

        RemoveFromHierarchy();
    }
}