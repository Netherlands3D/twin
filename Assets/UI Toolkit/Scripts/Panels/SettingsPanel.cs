using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Quality;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using QualitySettings = UnityEngine.QualitySettings;
using RadioButtonGroup = UnityEngine.UIElements.RadioButtonGroup;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [InspectorPanel]
    public partial class SettingsPanel : BaseInspectorContentPanel
    {
        public override string Title => "Instellingen";
        private RadioButtonGroup radioButtonGroup;

        public SettingsPanel()
        {
        }

        public SettingsPanel(Configuration configuration) : this()
        {
            this.CloneComponentTree("Panels"); 
            this.AddComponentStylesheet("Panels");
            
            radioButtonGroup = this.Q<RadioButtonGroup>("QualitySettings");
            radioButtonGroup.value = QualitySettings.GetQualityLevel();
            radioButtonGroup.RegisterValueChangedCallback(OnQualitySettingsChanged);
        }

        private void OnQualitySettingsChanged(ChangeEvent<int> evt)
        {
            var level = (GraphicsQualityLevel)evt.newValue;
            Twin.Quality.QualitySettings.SetGraphicsQuality(level, true);
        }
    }
}