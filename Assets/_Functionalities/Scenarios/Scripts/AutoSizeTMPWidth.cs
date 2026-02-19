using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class AutoSizeTMPWidth : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private float padding = 20f;

    [Header("Width limits")]
    [SerializeField] private float minWidth = 60f;
    [SerializeField] private float maxWidth = 280f;

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

        float width = targetText.preferredWidth + padding;

        width = Mathf.Clamp(width, minWidth, maxWidth);

        rect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width
        );
    }
}
