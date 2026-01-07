using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class SideSlideUI : MonoBehaviour
{
    public enum Side { Left, Right, Top, Bottom }

    [Header("Side & Behavior")]
    public Side side = Side.Left;
    public bool pinned = false;
    public float offscreenMargin = 24f;

    [Header("Animation")]
    public float slideTime = 0.25f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional UI Hook")]
    public Toggle pinToggle;

    RectTransform rt;
    Canvas rootCanvas;

    Vector2 shownPos;
    Vector2 hiddenPos;

    bool isShownRequested = true;
    float t;
    Vector2 animStart, animTarget;

    bool initialized;          // we’ve computed positions at least once
    bool pinHooked;            // avoid double listeners

    void OnEnable()
    {
        TryCache();
        // Defer initial layout-dependent work so rect sizes are valid.
        StartCoroutine(DeferredInit());
    }

    void Awake()
    {
        TryCache();
    }

    IEnumerator DeferredInit()
    {
        // Wait for layout to settle (end of frame + one extra frame).
        yield return new WaitForEndOfFrame();
        yield return null;

        // Try compute; if still zero-sized, try forcing a layout tick once.
        if (!TryComputeHiddenPosition())
        {
            Canvas.ForceUpdateCanvases();
            TryComputeHiddenPosition();
        }

        // Start visible by default (preserves authored position)
        isShownRequested = true;
        t = slideTime;
        animStart = shownPos;
        animTarget = shownPos;
        rt.anchoredPosition = animTarget;

        initialized = true;
    }

    void TryCache()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();

        if (pinToggle != null && !pinHooked)
        {
            pinHooked = true;
            pinToggle.isOn = pinned;
            pinToggle.onValueChanged.AddListener(SetPinned);
        }
    }

    void OnDisable()
    {
        // no-op; keep state
    }

    void OnRectTransformDimensionsChange()
    {
        // This can fire before Awake; be defensive.
        TryCache();
        if (!isActiveAndEnabled || rt == null) return;

        // Recompute hidden position when size/anchors change.
        if (TryComputeHiddenPosition())
        {
            // Keep current visual pose during resize by re-evaluating the animated position.
            rt.anchoredPosition = GetCurrentAnimatedPosition();
        }
    }

    bool TryComputeHiddenPosition()
    {
        if (rt == null) return false;

        // If rect is not yet valid, bail safely.
        var size = rt.rect.size;
        if (Mathf.Approximately(size.x, 0f) && Mathf.Approximately(size.y, 0f))
            return false;

        // Cache current authored in-view position as the "shown" pose
        shownPos = rt.anchoredPosition;

        Vector2 dir = Vector2.zero;
        switch (side)
        {
            case Side.Left: dir = Vector2.left; break;
            case Side.Right: dir = Vector2.right; break;
            case Side.Top: dir = Vector2.up; break;
            case Side.Bottom: dir = Vector2.down; break;
        }

        float distance = (side == Side.Left || side == Side.Right) ? size.x : size.y;
        float push = distance + offscreenMargin;

        hiddenPos = shownPos + dir * push;
        return true;
    }

    Vector2 GetCurrentAnimatedPosition()
    {
        if (slideTime <= 0f) return animTarget;
        float a = Mathf.Clamp01(t / slideTime);
        float k = ease.Evaluate(a);
        return Vector2.LerpUnclamped(animStart, animTarget, k);
    }

    void Update()
    {
        if (rt == null) return;

        // Animate
        if ((rt.anchoredPosition - animTarget).sqrMagnitude > 0.01f)
        {
            t += Time.unscaledDeltaTime;
            rt.anchoredPosition = GetCurrentAnimatedPosition();
        }
        else
        {
            rt.anchoredPosition = animTarget;
        }
    }

    // --- Public API ---

    public void Show()
    {
        isShownRequested = true;
        StartAnim(true);
    }

    public void Hide()
    {
        if (pinned) { Show(); return; }
        isShownRequested = false;
        StartAnim(false);
    }

    void StartAnim(bool toShown)
    {
        TryCache();
        // If positions aren’t ready yet, compute lazily to avoid NRE.
        if (!initialized)
        {
            if (!TryComputeHiddenPosition()) return; // wait until layout is valid
            initialized = true;
        }

        t = 0f;
        animStart = rt.anchoredPosition;
        animTarget = toShown ? shownPos : hiddenPos;
    }

    public void SetPinned(bool value)
    {
        pinned = value;
        if (pinToggle != null && pinToggle.isOn != pinned)
            pinToggle.isOn = pinned;

        if (pinned) Show();
    }

    public void TogglePin() => SetPinned(!pinned);

    public bool IsMouseOver()
    {
        TryCache();
        // For Screen Space Overlay, worldCamera can be null — API handles it.
        return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, rootCanvas ? rootCanvas.worldCamera : null);
    }

    public void Snap(bool toShown)
    {
        TryCache();
        if (!initialized) TryComputeHiddenPosition();
        isShownRequested = toShown;
        animStart = animTarget = toShown ? shownPos : hiddenPos;
        t = slideTime;
        if (rt != null) rt.anchoredPosition = animTarget;
    }
}
