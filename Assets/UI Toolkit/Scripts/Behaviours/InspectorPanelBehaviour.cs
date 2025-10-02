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
        private ImportAssetPanel importAssetPanel;

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            assetLibraryPanel ??= new AssetLibraryPanel();
            InspectorPanel.Content.Add(assetLibraryPanel);
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

            InspectorPanel.HeaderText = assetLibraryPanel.GetTitle();
            assetLibraryPanel.SetAssetLibrary(assetLibrary);
            assetLibraryPanel.Open();

            InspectorPanel.Toolbar.OpenLibrary.SetValueWithoutNotify(true);
        }

        public void CloseAssetLibrary()
        {
            InspectorPanel.HeaderText = "Lagen";
            assetLibraryPanel.Close();

            InspectorPanel.Toolbar.OpenLibrary.SetValueWithoutNotify(false);
            
            // TODO: At the moment - the InspectorPanel is only available for the Asset Library; once we add more
            // onto this panel, remove this line as it shouldn't auto-close yet
            InspectorPanel.Close();
        }

        private void EnsureInspectorIsOpen()
        {
            InspectorPanel.Open();
        }
    }
}
