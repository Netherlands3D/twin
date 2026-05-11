using Netherlands3D.Functionalities.AreaDownload.UI;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class InspectorPolygonGridPanel : BaseInspectorContentPanel
    {
        public override string Title => "Tekengebied grid selecteren";

        public UnityEvent OnConfirmSelection = new();
        
        private VisualElement thumbnailContainer;
        private DownloadInspectorService downloadInspectorService;
        
        private Button confirmButton;
        
        public InspectorPolygonGridPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            thumbnailContainer = this.Q<VisualElement>("ThumbnailContainer");
            confirmButton = this.Q<Button>("ConfirmButton");

            OnShow += () => Show(true);
            OnHide += () => Show(false);
            
            // copyNorthEastExtentButton.onClick.AddListener(CopyNorthEastToClipboard);
            // copySouthWestExtentButton.onClick.AddListener(CopySouthWestToClipboard);

            confirmButton.clicked += OnConfirmSelection.Invoke;

            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                downloadInspectorService = ServiceLocator.GetService<DownloadInspectorService>();
                downloadInspectorService.OnSelectionBoundsChanged.AddListener(GetFeatureThumbnail);    
            });
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                downloadInspectorService.OnSelectionBoundsChanged.RemoveListener(GetFeatureThumbnail);
            });
        }
        
        private void GetFeatureThumbnail(Bounds bounds)
        {
            //schedule for next frame so the style is resolved
            thumbnailContainer.schedule.Execute(_ => 
            { 
                ThumbnailService thumbnailService = ServiceLocator.GetService<ThumbnailService>();
                //TODO: Use bbox and geometry.coordinates from GeoJSON object to create bounds to render thumbnail
                Texture2D tex = thumbnailService.RenderThumbnail(bounds, true, 90, 3);
                thumbnailContainer.style.backgroundImage = new StyleBackground(tex);
                float aspect = (float)tex.height / tex.width;
                float newHeight = thumbnailContainer.resolvedStyle.width * aspect;
                thumbnailContainer.style.height = newHeight;
            });
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
        
        
//         private void CopySouthWestToClipboard()
//         {
//             var text = $"{westExtentTextField.text},{southExtentTextField.text}";
// #if UNITY_WEBGL && !UNITY_EDITOR
//             CopyToClipboard(text);
// #else
//             GUIUtility.systemCopyBuffer = text;
// #endif
//         }
//
//         private void CopyNorthEastToClipboard()
//         {
//             var text = $"{eastExtentTextField.text},{northExtentTextField.text}";
// #if UNITY_WEBGL && !UNITY_EDITOR
//             CopyToClipboard(text);
// #else
//             GUIUtility.systemCopyBuffer = text;
// #endif
//         }
    }
}