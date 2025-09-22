using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Breadcrumb : VisualElement
    {
        private int CurrentCrumbIndex => crumbs.Count - 1;
        private int PreviousCrumbIndex => CurrentCrumbIndex - 1;

        // ---- CSS / UXML constants ----
        private const string NoLeadingIconClass = "no-leading-icon";
        private const string NoBackButtonClass = "no-back-button";
        private const string LeadingIconClass = "leadingicon";
        private const string TrailName = "Trail";
        private const string SeparatorClass = "separator";
        private const string CrumbLinkClass = "crumb-link";
        private const string CrumbLabelClass = "crumb-label";
        private const string CurrentClass = "current";

        // ---- Cached references (set after CloneTree) ----
        private readonly Icon leadingIcon;
        private readonly VisualElement trail;
        private readonly BackButton backButton;

        // ---- Visibility toggles (default ON) ----
        private bool showLeadingIcon = true;
        private bool showBackButton = true;

        /// <summary>Show or hide the leading icon. Default: true.</summary>
        [UxmlAttribute("show-leading-icon")]
        public bool ShowLeadingIcon
        {
            get => showLeadingIcon;
            set
            {
                if (showLeadingIcon == value) return;
                
                showLeadingIcon = value;
                ApplyVisibility();
            }
        }

        /// <summary>Show or hide the back button. Default: true.</summary>
        [UxmlAttribute("show-back-button")]
        public bool ShowBackButton
        {
            get => showBackButton;
            set
            {
                if (showBackButton == value) return;
                
                showBackButton = value;
                ApplyVisibility();
            }
        }

        /// <summary>
        /// Leading icon override (unset = CSS variable; set = explicit enum image).
        /// </summary>
        [UxmlAttribute("leading-icon-image")]
        public Icon.IconImage LeadingIconImageExposed
        {
            get => leadingIcon != null ? leadingIcon.Image : default;
            set
            {
                if (leadingIcon == null) return;
                
                leadingIcon.Image = value;
            }
        }

        // ---- Data model ----
        public record Crumb
        {
            public string Label;
            public object Target;
        }

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
            leadingIcon = this.Q<Icon>(className: LeadingIconClass);
            backButton = this.Q<BackButton>();
            backButton.Clicked += GoBack;
            trail = this.Q(TrailName);

            // Ensure defaults are reflected in classes on attach
            RegisterCallback<AttachToPanelEvent>(_ => ApplyVisibility());
        }

        /// <summary>Toggle root classes for visibility based on current booleans.</summary>
        private void ApplyVisibility()
        {
            EnableInClassList(NoLeadingIconClass, !showLeadingIcon || CurrentCrumbIndex > 0);
            EnableInClassList(NoBackButtonClass, !showBackButton || CurrentCrumbIndex == 0);
        }

        /// <summary>Replace the entire crumb path and rebuild the UI.</summary>
        public void SetCrumbs(IList<Crumb> items)
        {
            crumbs.Clear();
            if (items != null && items.Count > 0) crumbs.AddRange(items);
            RebuildTrail();
        }

        /// <summary>Add a single crumb at the end and rebuild.</summary>
        public void AddCrumb(string label, object target)
        {
            crumbs.Add(new Crumb { Label = label, Target = target });
            RebuildTrail();
        }

        public void GoTo(int index)
        {
            if (index < 0 || index >= CurrentCrumbIndex) return;

            CrumbClicked?.Invoke(index, crumbs[index]);
            // Remove any crumb after this one as we have now this level selected
            RemoveAfterCrumb(crumbs[index]);
        }

        public void GoBack()
        {
            GoTo(PreviousCrumbIndex);
        }

        /// <summary>Remove all crumbs and rebuild.</summary>
        public void ClearCrumbs()
        {
            crumbs.Clear();
            RebuildTrail();
        }

        private void RemoveAfterCrumb(Crumb crumb)
        {
            var index = crumbs.IndexOf(crumb);
            if (index >= 0 && index < CurrentCrumbIndex)
            {
                crumbs.RemoveRange(index + 1, crumbs.Count - (index + 1));
            }

            RebuildTrail();
        }

        private void RebuildTrail()
        {
            if (trail == null) return;

            trail.Clear();
            int count = crumbs.Count;

            for (int i = 0; i < count; i++)
            {
                if (i > 0) trail.Add(CreateSeparator(i));

                bool isLast = i == count - 1;
                var crumb = !isLast
                    ? CreateCrumbButton(crumbs[i].Label, i)
                    : CreateCrumbLabel(crumbs[i].Label, i);
                trail.Add(crumb);
            }

            ApplyVisibility();
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
            btn.LabelText = text ?? string.Empty;
            btn.clicked += () => GoTo(index);
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