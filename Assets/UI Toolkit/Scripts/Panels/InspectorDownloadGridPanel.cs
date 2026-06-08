using Netherlands3D.Events;
using Netherlands3D.Functionalities.AreaDownload.UI;
using Netherlands3D.Services;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class InspectorDownloadGridPanel : BaseInspectorContentPanel
    {
        public override string Title => "Download 3D data";

        private List<string> options = new List<string>() { "Collada (.dae)", "DXF (.dxf)" };
        
        private VisualElement thumbnailContainer;
        private DownloadInspectorService downloadInspectorService;
        private Button copyZW;
        private Button copyNO;
        private NumberField zw_x, zw_y;
        private NumberField no_x, no_y;

        private DropDown dropDown => this.Q<DropDown>("DropDown");

        private Button confirmButton;

        public InspectorDownloadGridPanel()
        {
            
        }
        
        public InspectorDownloadGridPanel(TriggerEvent OnGridConfirmed) : this()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            thumbnailContainer = this.Q<VisualElement>("ThumbnailContainer");
            confirmButton = this.Q<Button>("DownloadButton");
            copyZW = this.Q<Button>("ButtonCopyZw");
            copyNO = this.Q<Button>("ButtonCopyNo");
            zw_x = this.Q<NumberField>("ZW_X");
            zw_y = this.Q<NumberField>("ZW_Y");
            no_x = this.Q<NumberField>("NO_X");
            no_y = this.Q<NumberField>("NO_Y");
            
            confirmButton.clicked += OnGridConfirmed.Invoke;
            confirmButton.clicked += ServiceLocator.GetService<ToolService>().GetTool(ToolType.DownloadTile).Close;

            SetDropdownValues(options);

            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                downloadInspectorService = ServiceLocator.GetService<DownloadInspectorService>();
                downloadInspectorService.OnSelectionBoundsChanged.AddListener(GetFeatureThumbnail); 
                downloadInspectorService.OnSelectionBoundsChanged.AddListener(UpdateFields);
                
                copyZW.RegisterCallback<ClickEvent>(CopySouthWest);
                copyNO.RegisterCallback<ClickEvent>(CopyNorthEast);
            });
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                downloadInspectorService.OnSelectionBoundsChanged.RemoveListener(GetFeatureThumbnail);
                downloadInspectorService.OnSelectionBoundsChanged.RemoveListener(UpdateFields);
            });
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

        public int DropDownValue => dropDown.choices.IndexOf(dropDown.value);

        public void SetDropdownValues(List<string> values)
        {
            if (values == null || values.Count == 0) return;
           
            dropDown.choices = values;
            SetDropdownValue(0);
        }

        public void SetDropdownValue(int value)
        {
            dropDown.SetValue(value);
        }

        public void AddDropDownListener(UnityAction<int> listener)
        {
            dropDown.DropDownValueChanged.AddListener(listener);
        }

        public void RemoveDropDownListener(UnityAction<int> listener)
        {
            dropDown.DropDownValueChanged.RemoveListener(listener);
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
    }
}