using System.Collections.Generic;
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
        private VisualElement imageContainer;

        public string activeUrl => activeLegendUrlContainer?.GetCapabilitiesUrl;
        public LegendUrlContainer activeLegendUrlContainer;
        public bool LegendVisible => !ClassListContains(UtilityClassConstants.HIDDEN);

        public WMSLegendPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            emptyTextLabel = this.Q<Label>("EmptyText");
            imageContainer = this.Q<VisualElement>("ImageListView");
        }

        public void SetVisible(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        public void SetContainer(LegendUrlContainer container)
        {
            ClearGraphics();
            activeLegendUrlContainer = container;
            
            bool isEmpty = container.LayerNameLegendUrlDictionary.Count == 0;
            ShowEmptyText(isEmpty);
            
            foreach (var entry in container.LayerNameLegendUrlDictionary.Values)
            {
                AddImage(entry.LayerName, entry.Texture);
            }
        }

        public void RefreshImage(string layerName, Texture2D texture)
        {
            var image = imageContainer.Q<Image>(layerName);
            if (image == null) return;
    
            Debug.Log("refresh image " + layerName + "\t " + texture);
            
            image.image = texture;
            image.style.height = texture.height;
        }
        public void AddImage(string layerName, Texture2D texture)
        {
            var image = new Image();
            image.name = layerName;
            image.image = texture;
            image.scaleMode = ScaleMode.ScaleToFit;
            imageContainer.Add(image);
        }

        public void SetImageActive(string layerName, bool isActive)
        {
            imageContainer.Q<Image>(layerName).EnableInClassList(UtilityClassConstants.HIDDEN, !isActive);
        }

        private void ShowEmptyText(bool show)
        {
            emptyTextLabel.EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        public void ClearGraphics()
        {
            imageContainer.Clear();
        }
    }
}