using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class InspectorPolygonGridPanel : BaseInspectorContentPanel
    {
        public override string Title => "Tekengebied grid selecteren";
        
        private VisualElement thumbnailContainer;
        
        public InspectorPolygonGridPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            thumbnailContainer = this.Q<VisualElement>("ThumbnailContainer");

            OnShow += () => Show(true);
            OnHide += () => Show(false);
         
        }
        
        // private void GetFeatureThumbnail(BoundingBox bbox)
        // {
        //     //schedule for next frame so the style is resolved
        //     thumbnailContainer.schedule.Execute(_ => 
        //     { 
        //         ThumbnailService thumbnailService = ServiceLocator.GetService<ThumbnailService>();
        //         //TODO: Use bbox and geometry.coordinates from GeoJSON object to create bounds to render thumbnail
        //         Bounds currentObjectBounds = bbox.ToUnityBounds();
        //         Texture2D tex = thumbnailService.RenderThumbnail(currentObjectBounds);
        //         thumbnailContainer.style.backgroundImage = new StyleBackground(tex);
        //         float aspect = (float)tex.height / tex.width;
        //         float newHeight = thumbnailContainer.resolvedStyle.width * aspect;
        //         thumbnailContainer.style.height = newHeight;
        //     });
        // }
        //
        // private void Clear()
        // {
        //     thumbnailContainer.style.height = 0;
        //     PopulateAddresses(empty);
        // }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
    }
}