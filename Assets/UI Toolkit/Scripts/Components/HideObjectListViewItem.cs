using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class HideObjectListViewItem : VisualElement
    {
        public UnityEvent<string, bool> OnToggleVisibility = new();
        
        
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>("bagicon");
        
        private VisibilityToggle visibilityToggle;
        private VisibilityToggle VisibilityToggle => visibilityToggle ??= this.Q<VisibilityToggle>("VisibilityToggle");
       
        public IconImage Image
        {
            get => Icon.Image;
            set => Icon.Image = value;
        }
        
        private Label labelField;
        private Label Label => labelField ??= this.Q<Label>("bagidtext");
      
        private string id;
        public string ID
        {
            get => id;
            set
            {
                id = value;
                Label.text = id;
            }
        }
        
        public HideObjectListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            Image = IconImage.Object;
            
            VisibilityToggle.RegisterValueChangedCallback(OnToggleValueChanged);
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                visibilityToggle.UnregisterValueChangedCallback(OnToggleValueChanged);
            });
            
        }
        
        private void OnToggleValueChanged(ChangeEvent<bool> evt)
        {
            OnToggleVisibility.Invoke(id, evt.newValue);
        }

        public void ShowToggle(bool show)
        {
            VisibilityToggle.Show(show);
        }
        
        public void SetToggleValue(bool value)
        {
            VisibilityToggle.value = value;
        }
    }
}