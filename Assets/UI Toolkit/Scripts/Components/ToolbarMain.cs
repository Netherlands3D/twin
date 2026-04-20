using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarMain : VisualElement
    {
        public ToggleButtonGroup Group => this.Q<ToggleButtonGroup>("ButtonGroup");

        private Button layerButton;
        private Button LayerButton => layerButton ??= this.Q<Button>("Layer");

        private Button libraryButton;
        private Button LibraryButton => libraryButton ??= this.Q<Button>("Library");

        private Button addButton;
        private Button AddButton => addButton ??= this.Q<Button>("Add");

        private Button searchButton;
        private Button SearchButton => searchButton ??= this.Q<Button>("Search");

        private Button sunPositionButton;
        private Button SunPositionButton => sunPositionButton ??= this.Q<Button>("SunPosition");

        private Button downloadTileButton;
        private Button DownloadTileButton => downloadTileButton ??= this.Q<Button>("DownloadTile");

        public event Action OnLayerClicked;
        public event Action OnLibraryClicked;
        public event Action OnAddClicked;
        public event Action OnSearchClicked;
        public event Action OnSunPositionClicked;
        public event Action OnDownloadTileClicked;

        private VisualElement divider;
        public VisualElement Divider => divider ??= this.Q<VisualElement>("Divider");

        public ToolbarMain()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterButtonCallbacks();

            RegisterCallback<AttachToPanelEvent>(NotifyAttachedToPanel);
        }

        private void RegisterButtonCallbacks()
        {
            LayerButton.clicked += NotifyLayerClicked;
            LibraryButton.clicked += NotifyLibraryClicked;
            AddButton.clicked += NotifyAddClicked;
            SearchButton.clicked += NotifySearchClicked;
            SunPositionButton.clicked += NotifySunPositionClicked;
            DownloadTileButton.clicked += NotifyDownloadTileClicked;
        }

        private void NotifyAttachedToPanel(AttachToPanelEvent _)
        {
            // Defaults: single selection, empty selection allowed
            Group.allowEmptySelection = true;
            Group.isMultipleSelection = false;

            ClearWithoutNotify();
        }

        private void NotifyLayerClicked() => OnLayerClicked?.Invoke();
        private void NotifyLibraryClicked() => OnLibraryClicked?.Invoke();
        private void NotifyAddClicked() => OnAddClicked?.Invoke();
        private void NotifySearchClicked() => OnSearchClicked?.Invoke();
        private void NotifySunPositionClicked() => OnSunPositionClicked?.Invoke();
        private void NotifyDownloadTileClicked() => OnDownloadTileClicked?.Invoke();

        public void ClearWithoutNotify()
        {
            // Clear selection: bitmask 0, length = number of options
            Group.SetValueWithoutNotify(new ToggleButtonGroupState(0ul, Group.childCount));
        }
    }
}