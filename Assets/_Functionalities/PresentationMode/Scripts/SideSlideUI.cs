using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class SideSlideUI : MonoBehaviour
{
    public enum Side { Left, Right, Top, Bottom }

    [Header("Side & Behavior")]
    public Side side = Side.Left;

    [Tooltip("Keep this element visible regardless of presentation mode.")]
    public bool pinned = false;

    [Tooltip("Extra distance (pixels) to push off-screen when hidden.")]
    public float offscreenMargin = 24f;

    [Header("Animation")]
    [Tooltip("Time (seconds) to slide in/out.")]
    public float slideTime = 0.25f;

    [Tooltip("Easing curve for the slide animation.")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional UI Hook")]
    [Tooltip("Optional Toggle to reflect/control pin state.")]
    public Toggle pinToggle;

    RectTransform rt;
    Canvas rootCanvas;

    Vector2 shownPos;   // cached anchoredPosition when visible
    Vector2 hiddenPos;  // computed off-screen anchoredPosition
    bool isShownRequested;  // current target (ignores 'pinned' logic set by manager)
    float t; // animation time accumulator
    Vector2 animStart, animTarget;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        if (pinToggle != null)
        {
            pinToggle.isOn = pinned;
            pinToggle.onValueChanged.AddListener(SetPinned);
        }

        shownPos = rt.anchoredPosition;
        ComputeHiddenPosition();
        // start shown by default
        isShownRequested = true;
        t = slideTime;
        animStart = shownPos;
        animTarget = shownPos;
    }

    void OnRectTransformDimensionsChange()
    {
        // Recompute when layout/scaling changes
        ComputeHiddenPosition();
        // Keep current visual position proportional to animation
        var current = GetCurrentAnimatedPosition();
        rt.anchoredPosition = current;
    }

    void ComputeHiddenPosition()
    {
        // Move the element fully off-screen along its side direction by at least its own size + margin.
        var size = rt.rect.size;
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

        shownPos = rt.anchoredPosition;                   // current designed position in view
        hiddenPos = shownPos + dir * push;                // push outward along the side
    }

    Vector2 GetCurrentAnimatedPosition()
    {
        float a = Mathf.Clamp01(slideTime <= 0f ? 1f : t / slideTime);
        float k = ease.Evaluate(a);
        return Vector2.LerpUnclamped(animStart, animTarget, k);
    }

    void Update()
    {
        // Animate
        if (rt == null) return;

        if ((rt.anchoredPosition - animTarget).sqrMagnitude > 0.01f)
        {
            t += Time.unscaledDeltaTime; // unaffected by timescale
            rt.anchoredPosition = GetCurrentAnimatedPosition();
        }
        else
        {
            rt.anchoredPosition = animTarget;
        }
    }

    // --- Public control API ---

    /// <summary>Force this panel to show (slide in). Manager decides when to call this.</summary>
    public void Show()
    {
        isShownRequested = true;
        StartAnim(toShown: true);
    }

    /// <summary>Force this panel to hide (slide out). Ignored if pinned.</summary>
    public void Hide()
    {
        if (pinned) { Show(); return; }
        isShownRequested = false;
        StartAnim(toShown: false);
    }

    void StartAnim(bool toShown)
    {
        t = 0f;
        animStart = rt.anchoredPosition;
        animTarget = toShown ? shownPos : hiddenPos;
    }

    public void SetPinned(bool value)
    {
        pinned = value;
        if (pinToggle != null && pinToggle.isOn != pinned)
            pinToggle.isOn = pinned;

        // If pinned, ensure visible immediately
        if (pinned) Show();
    }

    /// <summary>Convenience for wiring to a Button/Toggle.</summary>
    public void TogglePin()
    {
        SetPinned(!pinned);
    }

    /// <summary>Returns true if the mouse is over this panel (screen space).</summary>
    public bool IsMouseOver()
    {
        if (rootCanvas == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, rootCanvas.worldCamera);
    }

    /// <summary>Instantly snap to shown/hidden without anim (e.g., on mode switches).</summary>
    public void Snap(bool toShown)
    {
        isShownRequested = toShown;
        animStart = animTarget = toShown ? shownPos : hiddenPos;
        t = slideTime;
        rt.anchoredPosition = animTarget;
    }
}
