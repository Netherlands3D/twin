using Netherlands3D._Application._Twin;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument))]
    public class InspectorPanelBehaviour : MonoBehaviour
    {
        private UIDocument appDocument;
        [SerializeField] private AssetLibrary assetLibrary;
    
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private InspectorPanel inspectorPanel;
        private InspectorPanel InspectorPanel => inspectorPanel ??= Root?.Q<InspectorPanel>();
    
        private AssetLibraryPanel assetLibraryPanel;

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            InspectorPanel.Toolbar.OnOpenLibraryToggled += OnOpenLibraryToggled;
        }

        private void OnDisable()
        {
            InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
        }

        
        private void OnOpenLibraryToggled(ChangeEvent<bool> evt)
        {
            if (!evt.newValue)
            {
                CloseAssetLibrary();
                return;
            }

            OpenAssetLibrary();
        }

        public void Open()
        {
            InspectorPanel.Open();
        }

        public void Close()
        {
            InspectorPanel.Close();
        }

        public void OpenAssetLibrary()
        {
            EnsureInspectorIsOpen();
            EnsureLibraryPanelExists();

            InspectorPanel.HeaderText = "Toevoegen";
            assetLibraryPanel.SetAssetLibrary(assetLibrary);
            assetLibraryPanel.Show();

            InspectorPanel.Toolbar.OpenLibrary.SetValueWithoutNotify(true);
        }

        public void CloseAssetLibrary()
        {
            EnsureLibraryPanelExists();

            InspectorPanel.HeaderText = "Lagen";
            assetLibraryPanel.Hide();

            InspectorPanel.Toolbar.OpenLibrary.SetValueWithoutNotify(false);
            
            // TODO: At the moment - the InspectorPanel is only available for the Asset Library; once we add more
            // onto this panel, remove this line as it shouldn't auto-close yet
            InspectorPanel.Close();
        }

        private void EnsureInspectorIsOpen()
        {
            InspectorPanel.Open();
        }

        private void EnsureLibraryPanelExists()
        {
            assetLibraryPanel ??= new AssetLibraryPanel();
            if (!InspectorPanel.Content.Contains(assetLibraryPanel))
            {
                InspectorPanel.Content.Add(assetLibraryPanel);
            }
        }
    }
}
