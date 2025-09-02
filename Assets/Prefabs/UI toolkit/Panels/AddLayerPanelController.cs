using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Netherlands3D.UI.Components;

public class AddLayerPanelController : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset addLayerPanelUXML;

    [Header("Inspector (optional)")]
    [SerializeField] private UIDocument inspectorDocument;

    // Simple data model for each layer entry (label + icon key)
    private readonly List<(string label, Icon.IconImage iconKey)> layerItems = new()
    {
        ("Layer A", Icon.IconImage.Folder),
        ("Layer B", Icon.IconImage.Folder),
        ("Layer C", Icon.IconImage.Folder),
        ("Layer D", Icon.IconImage.Folder)
    };

    // Cached UI references
    private VisualElement Root => inspectorDocument != null ? inspectorDocument.rootVisualElement : null;
    private Netherlands3D.UI.Components.Toggle OpenLibraryToggle;
    private VisualElement ContentPlaceholder;  // Placeholder where the panel is inserted
    private TemplateContainer currentPanel;    // The instantiated AddLayerPanel

    // Keep callback reference to unsubscribe cleanly
    private EventCallback<ChangeEvent<bool>> _onOpenLibraryChanged;

    private void Awake()
    {
        // Get the UIDocument from Inspector or from the same GameObject
        if (inspectorDocument == null)
            inspectorDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (Root == null) return;

        // Query toolbar toggle and content area
        OpenLibraryToggle = Root.Q<Netherlands3D.UI.Components.Toggle>("OpenLibrary");
        ContentPlaceholder = Root.Q("Content");

        if (OpenLibraryToggle != null)
        {
            _onOpenLibraryChanged = OnOpenLibraryChanged;
            OpenLibraryToggle.RegisterValueChangedCallback(_onOpenLibraryChanged);
        }
    }

    private void OnDisable()
    {
        if (OpenLibraryToggle != null && _onOpenLibraryChanged != null)
            OpenLibraryToggle.UnregisterValueChangedCallback(_onOpenLibraryChanged);
    }

    /// <summary>
    /// Toggle changed: ON opens the panel, OFF closes it.
    /// </summary>
    private void OnOpenLibraryChanged(ChangeEvent<bool> evt)
    {
        if (evt.newValue) ShowAddLayerPanel();
        else CloseAddLayerPanel();
    }

    /// <summary>
    /// Instantiate and display the AddLayerPanel. Rebuilds every time it is opened.
    /// </summary>
    private void ShowAddLayerPanel()
    {
        if (addLayerPanelUXML == null || ContentPlaceholder == null) return;

        // Instantiate the panel
        currentPanel = addLayerPanelUXML.CloneTree();
        currentPanel.style.display = DisplayStyle.Flex;

        // Replace previous content
        ContentPlaceholder.Clear();
        ContentPlaceholder.Add(currentPanel);

        // Find the NL3D ListView inside the panel
        var listView = currentPanel.Q<Netherlands3D.UI.Components.ListView>();
        if (listView == null) return;

        // Virtualization and selection
        listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        listView.selectionType = SelectionType.None;

        // Provide data and binding; NL3D ListView supplies a wrapper with #Content
        listView.itemsSource = layerItems;
        listView.bindItem = BindListViewItem;

        // Keep toggle visually on (avoid loops)
        if (OpenLibraryToggle != null)
            OpenLibraryToggle.SetValueWithoutNotify(true);
    }

    /// <summary>
    /// Remove the panel and keep the toggle state consistent.
    /// </summary>
    private void CloseAddLayerPanel()
    {
        if (ContentPlaceholder == null) return;
        ContentPlaceholder.Clear();
        currentPanel = null;

        // Keep toggle visually off (avoid loops)
        if (OpenLibraryToggle != null)
            OpenLibraryToggle.SetValueWithoutNotify(false);
    }

    /// <summary>
    /// Bind data to a recycled item: ensure a single NL3D Button under #Content and set its data.
    /// </summary>
    private void BindListViewItem(VisualElement item, int index)
    {
        var content = item.Q("Content");
        if (content == null) return;

        // Create once per recycled item
        Netherlands3D.UI.Components.Button btn = content.Q<Netherlands3D.UI.Components.Button>("LayerButton");
        if (btn == null)
        {
            btn = new Netherlands3D.UI.Components.Button { name = "LayerButton" };
            btn.style.alignSelf = Align.Stretch;
            btn.style.flexGrow = 1;
            content.Add(btn);
        }

        // Bind data
        var (label, iconKey) = layerItems[index];
        btn.LabelText = label;
        btn.Image = iconKey;
    }
}
