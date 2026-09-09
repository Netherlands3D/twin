using Netherlands3D.Events;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Netherlands3D.Functionalities;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class InspectorPolygonGridPanel : BaseInspectorContentPanel
    {
        public override string Title => "Tekengebied grid selecteren";
        
        private VisualElement thumbnailContainer;
        private DownloadInspectorService downloadInspectorService;
        private Button copyZW;
        private Button copyNO;
        private NumberField zw_x, zw_y;
        private NumberField no_x, no_y;
        
        private Button confirmButton;

        private bool newPolygonSaved = false;
        

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
            
            newPolygonSaved = false;
            confirmButton.clicked += OnConfirm; 

            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                downloadInspectorService = ServiceLocator.GetService<DownloadInspectorService>();
                downloadInspectorService.OnSelectionBoundsChanged.AddListener(GetFeatureThumbnail); 
                downloadInspectorService.OnSelectionBoundsChanged.AddListener(UpdateFields);
                
                copyZW.RegisterCallback<ClickEvent>(CopySouthWest);
                copyNO.RegisterCallback<ClickEvent>(CopyNorthEast);
                zw_x.InputField.RegisterCallback<NavigationSubmitEvent>(evt => downloadInspectorService.SetWestValue(zw_x.GetValueAsInt()), TrickleDown.TrickleDown);
                zw_y.InputField.RegisterCallback<NavigationSubmitEvent>(evt => downloadInspectorService.SetSouthValue(zw_y.GetValueAsInt()), TrickleDown.TrickleDown);
                no_x.InputField.RegisterCallback<NavigationSubmitEvent>(evt => downloadInspectorService.SetEastValue(no_x.GetValueAsInt()), TrickleDown.TrickleDown);
                no_y.InputField.RegisterCallback<NavigationSubmitEvent>(evt => downloadInspectorService.SetNorthValue(no_y.GetValueAsInt()), TrickleDown.TrickleDown); 
            });
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                downloadInspectorService.OnSelectionBoundsChanged.RemoveListener(GetFeatureThumbnail);
                downloadInspectorService.OnSelectionBoundsChanged.RemoveListener(UpdateFields);
                
                if(newPolygonSaved) return;

                ServiceLocator.GetService<PolygonCreationService>().CancelLastCreatedGridLayer();
            });
        }

        private void OnConfirm()
        {
            newPolygonSaved = true;
            ServiceLocator.GetService<ToolService>().GetTool(ToolType.Layer).Open();
        }

        private void CopySouthWest(ClickEvent evt)
        {
            downloadInspectorService.CopySouthWestToClipboard();
        }

        private void CopyNorthEast(ClickEvent evt)
        {
            downloadInspectorService.CopyNorthEastToClipboard();
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
    }
}