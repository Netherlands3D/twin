using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PresentationUIManager : MonoBehaviour
{
    [Header("Mode")]
    public bool startActive = false;
    public KeyCode toggleKey = KeyCode.F10;

    [Header("Edge Hover Reveal")]
    public float edgeRevealThickness = 28f;
    public float hoverDelay = 0.08f;

    [Header("Optional")]
    public bool revealOnlyInPresentation = true;

    Canvas rootCanvas;
    List<SideSlideUI> panels = new List<SideSlideUI>();
    bool presentationActive;

    float edgeHoverTimerLeft, edgeHoverTimerRight, edgeHoverTimerTop, edgeHoverTimerBottom;
    bool initialized = false;

    void Awake()
    {
        // We no longer initialize immediately.
        // We'll do it after a short delay to ensure the scene & UI are ready.
        StartCoroutine(DelayedInitialize());
    }

    IEnumerator DelayedInitialize()
    {
        // Wait for end of frame + extra frames to ensure layout has finished.
        yield return new WaitForEndOfFrame();
        yield return null; // one more frame

        rootCanvas = GetComponentInParent<Canvas>();
        panels = GetComponentsInChildren<SideSlideUI>(includeInactive: true).ToList();

        SetPresentationMode(startActive, snap: true);
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        if (Input.GetKeyDown(toggleKey))
            SetPresentationMode(!presentationActive);

        HandleEdgeHover();
    }

    public void SetPresentationMode(bool active, bool snap = false)
    {
        if (!initialized)
        {
            // Optionally queue or skip calls until ready
            Debug.LogWarning("PresentationUIManager not initialized yet. Delaying mode change.");
            StartCoroutine(WaitAndSetMode(active, snap));
            return;
        }

        presentationActive = active;

        foreach (var p in panels)
        {
            if (p == null) continue;

            if (p.pinned)
            {
                if (snap) p.Snap(true); else p.Show();
                continue;
            }

            if (presentationActive)
            {
                if (snap) p.Snap(false); else p.Hide();
            }
            else
            {
                if (snap) p.Snap(true); else p.Show();
            }
        }
    }

    IEnumerator WaitAndSetMode(bool active, bool snap)
    {
        yield return new WaitUntil(() => initialized);
        SetPresentationMode(active, snap);
    }

    void HandleEdgeHover()
    {
        if (!initialized) return;
        if (revealOnlyInPresentation && !presentationActive) return;

        var mp = Input.mousePosition;
        var screenW = Screen.width;
        var screenH = Screen.height;

        bool atLeft = mp.x <= edgeRevealThickness;
        bool atRight = mp.x >= screenW - edgeRevealThickness;
        bool atBottom = mp.y <= edgeRevealThickness;
        bool atTop = mp.y >= screenH - edgeRevealThickness;

        edgeHoverTimerLeft = atLeft ? edgeHoverTimerLeft + Time.unscaledDeltaTime : 0f;
        edgeHoverTimerRight = atRight ? edgeHoverTimerRight + Time.unscaledDeltaTime : 0f;
        edgeHoverTimerTop = atTop ? edgeHoverTimerTop + Time.unscaledDeltaTime : 0f;
        edgeHoverTimerBottom = atBottom ? edgeHoverTimerBottom + Time.unscaledDeltaTime : 0f;

        if (edgeHoverTimerLeft >= hoverDelay) RevealSide(SideSlideUI.Side.Left);
        else MaybeHideSide(SideSlideUI.Side.Left);

        if (edgeHoverTimerRight >= hoverDelay) RevealSide(SideSlideUI.Side.Right);
        else MaybeHideSide(SideSlideUI.Side.Right);

        if (edgeHoverTimerTop >= hoverDelay) RevealSide(SideSlideUI.Side.Top);
        else MaybeHideSide(SideSlideUI.Side.Top);

        if (edgeHoverTimerBottom >= hoverDelay) RevealSide(SideSlideUI.Side.Bottom);
        else MaybeHideSide(SideSlideUI.Side.Bottom);
    }

    void RevealSide(SideSlideUI.Side side)
    {
        foreach (var p in panels)
            if (p && p.side == side) p.Show();
    }

    void MaybeHideSide(SideSlideUI.Side side)
    {
        if (!presentationActive) return;
        bool mouseOverAny = panels.Any(p => p && p.side == side && p.IsMouseOver());
        if (mouseOverAny) return;

        var mp = Input.mousePosition;
        var screenW = Screen.width;
        var screenH = Screen.height;
        bool atEdge = side switch
        {
            SideSlideUI.Side.Left => mp.x <= edgeRevealThickness,
            SideSlideUI.Side.Right => mp.x >= screenW - edgeRevealThickness,
            SideSlideUI.Side.Top => mp.y >= screenH - edgeRevealThickness,
            SideSlideUI.Side.Bottom => mp.y <= edgeRevealThickness,
            _ => false
        };
        if (atEdge) return;

        foreach (var p in panels)
            if (p && p.side == side && !p.pinned) p.Hide();
    }

    public void TogglePresentationMode() => SetPresentationMode(!presentationActive);
}
