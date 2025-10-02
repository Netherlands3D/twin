using System;
using Netherlands3D.UI.ExtensionMethods;
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
    public partial class ListView : UnityEngine.UIElements.ListView, IComponent
    {
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
                base.bindItem = (ve, i) => _userBind?.Invoke(ve, i);
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
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            // Defaults only if user code did not set factories
            if (makeItem == null) makeItem = CreateDefaultItem;
            if (base.bindItem == null) this.bindItem = DefaultBind;
        }

        /// <summary>
        /// Default item: ListViewItem UXML with #Content and a Spacer (bottom).
        /// </summary>
        private VisualElement CreateDefaultItem()
        {
            return new ListViewItem();
        }

        /// <summary>
        /// Default bind does nothing; controllers populate #Content in their bindItem.
        /// </summary>
        private void DefaultBind(VisualElement item, int index) { }
    }
}
