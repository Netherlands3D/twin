using Netherlands3D.Functionalities.AreaDownload.UI;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class InspectorPolygonGridPanel : BaseInspectorContentPanel
    {
        public override string Title => "Tekengebied grid selecteren";

        public UnityEvent OnConfirmSelection = new();
        
        private VisualElement thumbnailContainer;
        private DownloadInspectorService downloadInspectorService;
        private Button copyZW;
        private Button copyNO;
        private NumberField zw_x, zw_y;
        private NumberField no_x, no_y;
        
        private Button confirmButton;
        
        public InspectorPolygonGridPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            thumbnailContainer = this.Q<VisualElement>("ThumbnailContainer");
            confirmButton = this.Q<Button>("ConfirmButton");
            copyZW = this.Q<Button>("ButtonCopyZw");
            copyNO = this.Q<Button>("ButtonCopyNo");
            zw_x = this.Q<NumberField>("ZW_X");
            zw_y = this.Q<NumberField>("ZW_Y");
            no_x = this.Q<NumberField>("NO_X");
            no_y = this.Q<NumberField>("NO_Y");

            OnShow += () => Show(true);
            OnHide += () => Show(false);

            confirmButton.clicked += OnConfirmSelection.Invoke;
            copyZW.clicked += CopySouthWestToClipboard;
            copyNO.clicked += CopyNorthEastToClipboard;

            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                downloadInspectorService = ServiceLocator.GetService<DownloadInspectorService>();
                downloadInspectorService.OnSelectionBoundsChanged.AddListener(GetFeatureThumbnail); 
                downloadInspectorService.OnSelectionBoundsChanged.AddListener(UpdateFields);
            });
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                downloadInspectorService.OnSelectionBoundsChanged.RemoveListener(GetFeatureThumbnail);
                downloadInspectorService.OnSelectionBoundsChanged.RemoveListener(UpdateFields);
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

        private void UpdateFields(Bounds bounds)
        {
            zw_x.SetValueWithoutNotify(int.Parse(downloadInspectorService.WestExtent));
            zw_y.SetValueWithoutNotify(int.Parse(downloadInspectorService.SouthExtent));
            no_x.SetValueWithoutNotify(int.Parse(downloadInspectorService.EastExtent));
            no_y.SetValueWithoutNotify(int.Parse(downloadInspectorService.NorthExtent));
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
        
        
        private void CopySouthWestToClipboard()
        {
            DownloadInspectorService downloadService = ServiceLocator.GetService<DownloadInspectorService>();
            var text = $"{downloadService.WestExtent},{downloadService.SouthExtent}";
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboard(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        private void CopyNorthEastToClipboard()
        {
            DownloadInspectorService downloadService = ServiceLocator.GetService<DownloadInspectorService>();
            var text = $"{downloadService.EastExtent},{downloadService.NorthExtent}";
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboard(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }
    }
}