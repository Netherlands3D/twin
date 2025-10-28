using System;
using System.Collections.Generic;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Breadcrumb : VisualElement
    {
        private int CurrentCrumbIndex => crumbs.Count - 1;
        private int PreviousCrumbIndex => CurrentCrumbIndex - 1;

        private Icon leadingIcon;
        private Icon LeadingIcon => leadingIcon ??= this.Q<Icon>(className: "leadingicon");
        
        private VisualElement trail;
        private VisualElement Trail => trail ??= this.Q("Trail");
        
        private BackButton backButton;
        private BackButton BackButton => backButton ??= this.Q<BackButton>();

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
                UpdateClassList();
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
                UpdateClassList();
            }
        }

        /// <summary>
        /// Leading icon override (unset = CSS variable; set = explicit enum image).
        /// </summary>
        [UxmlAttribute("leading-icon-image")]
        public IconImage LeadingIconImageExposed
        {
            get => LeadingIcon.Image;
            set => LeadingIcon.Image = value;
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
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            BackButton.Clicked += GoBack;

            // Ensure defaults are reflected in classes on attach
            RegisterCallback<AttachToPanelEvent>(_ => UpdateClassList());
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
            Trail.Clear();
            int count = crumbs.Count;

            for (int i = 0; i < count; i++)
            {
                if (i > 0) Trail.Add(CreateSeparator(i));

                bool isLast = i == count - 1;
                var crumb = !isLast
                    ? CreateCrumbButton(crumbs[i].Label, i)
                    : CreateCrumbLabel(crumbs[i].Label, i);
                Trail.Add(crumb);
            }

            UpdateClassList();
        }

        private VisualElement CreateSeparator(int index)
        {
            var sep = new Icon { name = $"Sep{index}" };
            sep.AddToClassList("separator");
            return sep;
        }

        private VisualElement CreateCrumbButton(string text, int index)
        {
            var btn = new Button { name = $"Crumb{index}" };
            btn.AddToClassList("crumb-link");
            btn.LabelText = text ?? string.Empty;
            btn.clicked += () => GoTo(index);
            
            return btn;
        }

        private VisualElement CreateCrumbLabel(string text, int index)
        {
            var lab = new Label(text ?? string.Empty) { name = $"Crumb{index}" };
            lab.AddToClassList("crumb-label");
            lab.AddToClassList("current");
            
            return lab;
        }

        private void UpdateClassList()
        {
            EnableInClassList("no-leading-icon", !showLeadingIcon || CurrentCrumbIndex > 0);
            EnableInClassList("no-back-button", !showBackButton || CurrentCrumbIndex == 0);
        }
    }
}