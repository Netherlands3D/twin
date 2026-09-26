using UnityEditor;
using UnityEngine;

public class PngPreviewWindow : EditorWindow
{
    private Texture2D texture;

    public static void Open(byte[] pngBytes)
    {
        var newTexture = new Texture2D(2, 2);

        if (!ImageConversion.LoadImage(newTexture, pngBytes))
        {
            DestroyImmediate(newTexture);
            throw new System.ArgumentException("Invalid PNG data.", nameof(pngBytes));
        }

        bool alreadyOpen = HasOpenInstances<PngPreviewWindow>();
        var window = GetWindow<PngPreviewWindow>(
            utility: true,
            title: "PNG Preview",
            focus: true);

        if (!alreadyOpen)
            window.position = new Rect(200, 200, 600, 600);

        if (window.texture != null)
            DestroyImmediate(window.texture);

        window.texture = newTexture;
        window.Repaint();
    }

    public static void ClosePreview()
    {
        if (HasOpenInstances<PngPreviewWindow>())
            GetWindow<PngPreviewWindow>().Close();
    }

    private void OnGUI()
    {
        if (texture != null)
            GUI.DrawTexture(
                new Rect(0, 0, position.width, position.height),
                texture,
                ScaleMode.ScaleToFit);
    }

    private void OnDisable()
    {
        if (texture != null)
            DestroyImmediate(texture);

        texture = null;
    }
}