using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DefaultHUD : VisualElement
    {
        private const string HiddenClass = "presentation-section--hidden";
        
        public DefaultHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            SetupAutoHideSection("LeftSection", "LeftRevealZone");
            SetupAutoHideSection("TopSection", "TopRevealZone");
            SetupAutoHideSection("BottomSection", "BottomRevealZone");
        }

        private void SetupAutoHideSection(
            string sectionName,
            string revealZoneName)
        {
            var section = this.Q<VisualElement>(sectionName);
            var revealZone = this.Q<VisualElement>(revealZoneName);

            void Show() => section.RemoveFromClassList(HiddenClass);
            void Hide() => section.AddToClassList(HiddenClass);

            revealZone.RegisterCallback<PointerEnterEvent>(_ => Show());
            revealZone.RegisterCallback<PointerLeaveEvent>(_ => Hide());

            section.RegisterCallback<PointerEnterEvent>(_ => Show());
            section.RegisterCallback<PointerLeaveEvent>(_ => Hide());
        }
        
        
    }
}