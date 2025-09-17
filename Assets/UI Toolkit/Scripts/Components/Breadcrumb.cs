using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Breadcrumb : VisualElement
    {
        // ---- CSS / UXML constants ----
        private const string RootClass = "breadcrumb";
        private const string NoLeadingIconClass = "no-leading-icon";
        private const string NoBackButtonClass = "no-back-button";
        private const string LeadingIconClass = "leadingicon";
        private const string BackButtonName = "BackButton";
        private const string TrailName = "Trail";
        private const string SeparatorClass = "separator";
        private const string CrumbLinkClass = "crumb-link";
        private const string CrumbLabelClass = "crumb-label";
        private const string CurrentClass = "current";

        // ---- Cached references (set after CloneTree) ----
        private Icon _leadingIcon;
        private VisualElement _trail;

        // ---- Visibility toggles (default ON) ----
        private bool showLeadingIcon = true;
        private bool showBackButton = true;

        /// <summary>Show or hide the leading icon. Default: true.</summary>
        [UxmlAttribute("show-leading-icon")]
        public bool ShowLeadingIcon
        {
            get => showLeadingIcon;
            set { if (showLeadingIcon != value) { showLeadingIcon = value; ApplyVisibility(); } }
        }

        /// <summary>Show or hide the back button. Default: true.</summary>
        [UxmlAttribute("show-back-button")]
        public bool ShowBackButton
        {
            get => showBackButton;
            set { if (showBackButton != value) { showBackButton = value; ApplyVisibility(); } }
        }

        /// <summary>
        /// Leading icon override (unset = CSS variable; set = explicit enum image).
        /// </summary>
        [UxmlAttribute("leading-icon-image")]
        public Icon.IconImage LeadingIconImageExposed
        {
            get => _leadingIcon != null ? _leadingIcon.Image : default;
            set { if (_leadingIcon != null) _leadingIcon.Image = value; }
        }

        // ---- Data model ----
        public struct Crumb { public string Label; public string Route; }
        private readonly List<Crumb> crumbs = new();

        /// <summary>Raised when a non-last crumb is clicked: (index, crumb).</summary>
        public event Action<int, Crumb> CrumbClicked;

        public Breadcrumb()
        {
            // Find and load UXML template for this component
            var vta = Resources.Load<VisualTreeAsset>("UI/" + nameof(Breadcrumb));
            if (vta != null) vta.CloneTree(this);

            // Find and load USS stylesheet specific for this component
            var ss = Resources.Load<StyleSheet>("UI/" + nameof(Breadcrumb) + "-style");
            if (ss != null) styleSheets.Add(ss);

            // Cache references now that tree is cloned
            _leadingIcon = this.Q<Icon>(className: LeadingIconClass);
            _trail = this.Q(TrailName);

            // Ensure defaults are reflected in classes on attach
            RegisterCallback<AttachToPanelEvent>(_ =>
            { 
                ApplyVisibility(); 
            });
        }

        /// <summary>Toggle root classes for visibility based on current booleans.</summary>
        private void ApplyVisibility()
        {
            EnableInClassList(NoLeadingIconClass, !showLeadingIcon);
            EnableInClassList(NoBackButtonClass, !showBackButton);
        }

        /// <summary>Replace the entire crumb path and rebuild the UI.</summary>
        public void SetCrumbs(IList<Crumb> items)
        {
            crumbs.Clear();
            if (items != null && items.Count > 0) crumbs.AddRange(items);
            RebuildTrail();
        }

        /// <summary>Add a single crumb at the end and rebuild.</summary>
        public void AddCrumb(string label, string route)
        {
            crumbs.Add(new Crumb { Label = label, Route = route });
            RebuildTrail();
        }

        /// <summary>Remove all crumbs and rebuild.</summary>
        public void ClearCrumbs()
        {
            crumbs.Clear();
            RebuildTrail();
        }

        // ---- UI building ----
        private void RebuildTrail()
        {
            if (_trail == null) return;

            _trail.Clear();
            int count = crumbs.Count;

            for (int i = 0; i < count; i++)
            {
                if (i > 0) _trail.Add(CreateSeparator(i));

                bool isLast = (i == count - 1);
                if (isLast)
                    _trail.Add(CreateCrumbLabel(crumbs[i].Label, i));
                else
                    _trail.Add(CreateCrumbButton(crumbs[i].Label, i));
            }
        }

        private VisualElement CreateSeparator(int index)
        {
            var sep = new Icon { name = $"Sep{index}" };
            sep.AddToClassList(SeparatorClass);
            return sep;
        }

        private VisualElement CreateCrumbButton(string text, int index)
        {
            var btn = new Button { name = $"Crumb{index}" };
            btn.AddToClassList(CrumbLinkClass);
            btn.text = text ?? string.Empty;    // defensive
            btn.clicked += () => CrumbClicked?.Invoke(index, crumbs[index]);
            return btn;
        }

        private VisualElement CreateCrumbLabel(string text, int index)
        {
            var lab = new Label(text ?? string.Empty) { name = $"Crumb{index}" };
            lab.AddToClassList(CrumbLabelClass);
            lab.AddToClassList(CurrentClass);
            return lab;
        }
    }
}
