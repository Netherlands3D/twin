using Netherlands3D.Functionalities.Wms;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class WMSLegendPanel : VisualElement
    {
        private Label emptyTextLabel;
        private ScrollView imageContainer;

        public string activeUrl => activeLegendUrlContainer?.GetCapabilitiesUrl;
        public LegendUrlContainer activeLegendUrlContainer;
        public bool LegendVisible => !ClassListContains(UtilityClassConstants.HIDDEN);

        public WMSLegendPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            emptyTextLabel = this.Q<Label>("EmptyText");
            imageContainer = this.Q<ScrollView>("ImageListView");
        }

        public void SetVisible(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        public void SetContainer(LegendUrlContainer container)
        {
            ClearGraphics();
            
            foreach (var entry in container?.LayerNameLegendUrlDictionary.Values)
            {
                entry.LayerActiveChanged.RemoveListener(SetImageActive);
            }
            
            activeLegendUrlContainer = container;
            
            bool isEmpty = container.LayerNameLegendUrlDictionary.Count == 0;
            ShowEmptyText(isEmpty);
            
            foreach (var entry in container.LayerNameLegendUrlDictionary.Values)
            {
                AddImage(entry.LayerName, entry.Texture, entry.Active);
                entry.LayerActiveChanged.AddListener(SetImageActive);
            }
        }
        
        private void AddImage(string layerName, Texture2D texture, bool isActive)
        {
            var image = new Image();
            image.name = layerName;
            image.image = texture;
            image.AddToClassList("wms-legend-panel__image");
            imageContainer.Add(image);
            SetImageActive(layerName, isActive);
        }

        public void RefreshImage(string layerName, Texture2D texture)
        {
            var image = imageContainer.Q<Image>(layerName);
            if (image == null) return;
            image.image = texture;
        }

        private void SetImageActive(string layerName, bool isActive)
        {
            imageContainer.Q<Image>(layerName).EnableInClassList(UtilityClassConstants.HIDDEN, !isActive);
        }

        private void ShowEmptyText(bool show)
        {
            emptyTextLabel.EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        private void ClearGraphics()
        {
            imageContainer.Clear();
        }
    }
}