using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// NL3D ListView wrapper around UI Toolkit ListView.
    /// - Loads UXML/USS from Resources/UI.
    /// - Adds ScrollView-style.uss and Scroller-style.uss for consistent internals.
    /// - Stable root class "listview".
    /// - Default makeItem creates a ListViewItem wrapper with #Content and a Spacer element
    ///   to guarantee inter-item gap under virtualization.
    /// - Intercepts bindItem so we can apply inline icon gaps after user binding.
    /// </summary>
    [UxmlElement]
    public partial class ListView : UnityEngine.UIElements.ListView
    {
        private const string RootClass = "listview";
        private const string ItemClass = "listview-item";
        private const string ContentName = "Content";
        private const string SpacerName = "Spacer";

        // Inter-item gap (pixels) applied as Spacer height.
        private float itemGap = 8f;

        /// <summary>Vertical spacing between items, in pixels.</summary>
        [UxmlAttribute("item-gap")]
        public float ItemGap
        {
            get => itemGap;
            set { itemGap = value; ApplyItemSpacerHeight(); }
        }

        // Inline icon gaps for NL3D components inside items (configurable if desired).
        [UxmlAttribute("button-icon-gap")] private float buttonIconGap = 8f;
        [UxmlAttribute("toggle-icon-gap")] private float toggleIconGap = 8f;

        // Keep user bind so we can call it first.
        private Action<VisualElement, int> _userBind;

        /// <summary>
        /// Intercept bindItem so we can apply inline fixes after user binding.
        /// </summary>
        public new Action<VisualElement, int> bindItem
        {
            get => _userBind;
            set
            {
                _userBind = value;
                base.bindItem = (ve, i) =>
                {
                    _userBind?.Invoke(ve, i);
                };
            }
        }

        /// <summary>
        /// Unity internal ScrollView content container. Item roots are direct children.
        /// </summary>
        private VisualElement GetInternalContentContainer()
            => this.Q<VisualElement>(className: "unity-content-container");

        // Minimal pass-throughs (keep optional)
        [UxmlAttribute("fixed-item-height")]
        public float FixedItemHeight { get => fixedItemHeight; set => fixedItemHeight = value; }

        [UxmlAttribute("selection-type")]
        public SelectionType SelectionMode { get => selectionType; set => selectionType = value; }

        [UxmlAttribute("virtualization-method")]
        public CollectionVirtualizationMethod VirtualizationMethod { get => virtualizationMethod; set => virtualizationMethod = value; }

        [UxmlAttribute("show-alternating-row-backgrounds")]
        public AlternatingRowBackground ShowAlternatingRowBackgrounds { get => showAlternatingRowBackgrounds; set => showAlternatingRowBackgrounds = value; }

        public ListView()
        {
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/" + nameof(ListView));
            asset.CloneTree(this);

            // Find and load USS stylesheet specific for this component
            var styleSheet = Resources.Load<StyleSheet>("UI/" + nameof(ListView) + "-style");
            styleSheets.Add(styleSheet);

            // Load shared scroller/scrollview styles for internal parts
            var scrollerSheet = Resources.Load<StyleSheet>("UI/Scroller-style");
            styleSheets.Add(scrollerSheet);
            var scrollViewSheet = Resources.Load<StyleSheet>("UI/ScrollView-style");
            styleSheets.Add(scrollViewSheet);

            // Defaults only if user code did not set factories
            if (makeItem == null) makeItem = CreateDefaultItem;
            if (base.bindItem == null) this.bindItem = DefaultBind;

            // Keep spacer heights in sync with realized items
            this.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                HookSpacerHandlers();
                ApplyItemSpacerHeight();
            });
        }

        /// <summary>
        /// Default item: ListViewItem UXML with #Content and a Spacer (bottom).
        /// </summary>
        private VisualElement CreateDefaultItem()
        {
            var vta = Resources.Load<VisualTreeAsset>("UI/" + nameof(ListViewItem));
            if (vta != null)
            {
                var root = vta.Instantiate();
                var ss = Resources.Load<StyleSheet>("UI/" + nameof(ListViewItem) + "-style");
                if (ss != null) root.styleSheets.Add(ss);

                if (!root.ClassListContains(ItemClass)) root.AddToClassList(ItemClass);

                var content = root.Q(ContentName) ?? new VisualElement { name = ContentName };
                if (content.parent != root) root.Add(content);

                var spacer = root.Q<VisualElement>(SpacerName);
                if (spacer == null)
                {
                    spacer = new VisualElement { name = SpacerName };
                    spacer.style.flexShrink = 0;
                    root.Add(spacer);
                }
                spacer.style.height = itemGap;

                return root;
            }

            // Fallback (code-built)
            var fallback = new VisualElement();
            fallback.AddToClassList(ItemClass);

            var contentVe = new VisualElement { name = ContentName };
            fallback.Add(contentVe);

            var spacerVe = new VisualElement { name = SpacerName };
            spacerVe.style.flexShrink = 0;
            spacerVe.style.height = itemGap;
            fallback.Add(spacerVe);

            return fallback;
        }

        /// <summary>
        /// Default bind does nothing; controllers populate #Content in their bindItem.
        /// </summary>
        private void DefaultBind(VisualElement item, int index) { }

        /// <summary>
        /// Re-apply spacer heights whenever realized items change.
        /// </summary>
        private void HookSpacerHandlers()
        {
            var content = GetInternalContentContainer();
            if (content == null) return;

            content.RegisterCallback<GeometryChangedEvent>(_ => ApplyItemSpacerHeight());
        }

        /// <summary>
        /// Set Spacer height on all realized item roots.
        /// </summary>
        private void ApplyItemSpacerHeight()
        {
            var content = GetInternalContentContainer();
            if (content == null) return;

            int n = content.childCount;
            for (int i = 0; i < n; i++)
            {
                var itemRoot = content.ElementAt(i);
                var spacer = itemRoot?.Q<VisualElement>(SpacerName);
                if (spacer != null)
                {
                    spacer.style.height = itemGap;
                    spacer.style.display = DisplayStyle.Flex;
                }
            }
        }
    }
}
