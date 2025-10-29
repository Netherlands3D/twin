using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PresentationUIManager : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("Start with presentation mode active on play.")]
    public bool startActive = false;

    [Tooltip("Optional hotkey to toggle presentation mode.")]
    public KeyCode toggleKey = KeyCode.F10;

    [Header("Edge Hover Reveal")]
    [Tooltip("Thickness (pixels) of the screen edge that triggers reveal.")]
    public float edgeRevealThickness = 28f;

    [Tooltip("Time the mouse must linger on the edge before reveal (seconds).")]
    public float hoverDelay = 0.08f;

    [Header("Optional")]
    [Tooltip("If true, hover reveal only works when presentation mode is active.")]
    public bool revealOnlyInPresentation = true;

    Canvas rootCanvas;
    List<SideSlideUI> panels = new List<SideSlideUI>();
    bool presentationActive;

    float edgeHoverTimerLeft, edgeHoverTimerRight, edgeHoverTimerTop, edgeHoverTimerBottom;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        panels = GetComponentsInChildren<SideSlideUI>(includeInactive: true).ToList();

        SetPresentationMode(startActive, snap: true);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetPresentationMode(!presentationActive);

        HandleEdgeHover();
    }

    public void SetPresentationMode(bool active, bool snap = false)
    {
        presentationActive = active;

        foreach (var p in panels)
        {
            if (p == null) continue;

            // Pinned panels stay visible no matter what
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

    void HandleEdgeHover()
    {
        if (revealOnlyInPresentation && !presentationActive) return;

        var mp = Input.mousePosition;
        var screenW = Screen.width;
        var screenH = Screen.height;

        bool atLeft = mp.x <= edgeRevealThickness;
        bool atRight = mp.x >= screenW - edgeRevealThickness;
        bool atBottom = mp.y <= edgeRevealThickness;
        bool atTop = mp.y >= screenH - edgeRevealThickness;

        // timers
        edgeHoverTimerLeft = atLeft ? edgeHoverTimerLeft + Time.unscaledDeltaTime : 0f;
        edgeHoverTimerRight = atRight ? edgeHoverTimerRight + Time.unscaledDeltaTime : 0f;
        edgeHoverTimerTop = atTop ? edgeHoverTimerTop + Time.unscaledDeltaTime : 0f;
        edgeHoverTimerBottom = atBottom ? edgeHoverTimerBottom + Time.unscaledDeltaTime : 0f;

        // Reveal when lingered; keep visible while mouse is over their area.
        if (edgeHoverTimerLeft >= hoverDelay) RevealSide(SideSlideUI.Side.Left, keepIfMouseOver: true);
        else MaybeHideSide(SideSlideUI.Side.Left);

        if (edgeHoverTimerRight >= hoverDelay) RevealSide(SideSlideUI.Side.Right, keepIfMouseOver: true);
        else MaybeHideSide(SideSlideUI.Side.Right);

        if (edgeHoverTimerTop >= hoverDelay) RevealSide(SideSlideUI.Side.Top, keepIfMouseOver: true);
        else MaybeHideSide(SideSlideUI.Side.Top);

        if (edgeHoverTimerBottom >= hoverDelay) RevealSide(SideSlideUI.Side.Bottom, keepIfMouseOver: true);
        else MaybeHideSide(SideSlideUI.Side.Bottom);
    }

    void RevealSide(SideSlideUI.Side side, bool keepIfMouseOver)
    {
        foreach (var p in panels)
        {
            if (p == null || p.side != side) continue;
            p.Show();
        }
    }

    void MaybeHideSide(SideSlideUI.Side side)
    {
        // Hide those on this side if: presentation mode is active, panel is not pinned,
        // the mouse is not hovering over ANY panel on this side, and mouse is no longer on the edge.
        if (!presentationActive) return;

        bool mouseOverAny = false;
        foreach (var p in panels)
        {
            if (p == null || p.side != side) continue;
            if (p.IsMouseOver()) { mouseOverAny = true; break; }
        }
        if (mouseOverAny) return;

        // Also ensure the mouse is not at the edge anymore (so we don’t insta-hide when exiting panel).
        var mp = Input.mousePosition;
        var screenW = Screen.width;
        var screenH = Screen.height;
        bool atEdge = false;
        switch (side)
        {
            case SideSlideUI.Side.Left: atEdge = mp.x <= edgeRevealThickness; break;
            case SideSlideUI.Side.Right: atEdge = mp.x >= screenW - edgeRevealThickness; break;
            case SideSlideUI.Side.Top: atEdge = mp.y >= screenH - edgeRevealThickness; break;
            case SideSlideUI.Side.Bottom: atEdge = mp.y <= edgeRevealThickness; break;
        }
        if (atEdge) return;

        foreach (var p in panels)
        {
            if (p == null || p.side != side || p.pinned) continue;
            p.Hide();
        }
    }

    // Optional simple UI hook (e.g., wire a button)
    public void TogglePresentationMode()
    {
        SetPresentationMode(!presentationActive);
    }
}
