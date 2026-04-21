using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class ErrorPanel : VisualElement
    {
        private ErrorPanelContent Content;
        private ContentContainer contentContainer;
        
        [UxmlAttribute("header-text")]
        public string HeaderText
        {
            get => contentContainer.HeaderText;
            set => contentContainer.HeaderText = value;
        }
        
        [UxmlAttribute("message")]
        public string Message
        {
            get => Content.Message;
            set => Content.Message = value;
        }

        public ErrorPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            Content = this.Q<ErrorPanelContent>();
            contentContainer = this.Q<ContentContainer>();
            
            Content.OnHide.AddListener(Hide);
            Content.OnShow.AddListener(Show);
        }


        public void Show()
        {
            RemoveFromClassList(UtilityClassConstants.HIDDEN);
            Content.RemoveFromClassList(UtilityClassConstants.HIDDEN); //do not call Content.Show() as this would give an infinite loop
        }

        public void Hide()
        {
            AddToClassList(UtilityClassConstants.HIDDEN);
            Content.AddToClassList(UtilityClassConstants.HIDDEN); //do not call Content.Show() as this would give an infinite loop
        }
    }
}
