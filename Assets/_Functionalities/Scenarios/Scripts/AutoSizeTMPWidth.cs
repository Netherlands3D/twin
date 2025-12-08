using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class AutoSizeTMPWidth : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private float padding = 20f;

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        if (targetText == null)
            targetText = GetComponentInChildren<TMP_Text>();
    }

    /// <summary>
    /// Call this after the TMP text has been updated.
    /// </summary>
    public void ResizeNow()
    {
        if (!targetText || !rect) return;

        targetText.ForceMeshUpdate();
        float width = targetText.preferredWidth;

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + padding);
    }
}
