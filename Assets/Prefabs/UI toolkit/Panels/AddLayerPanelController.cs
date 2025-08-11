using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class AddLayerPanelController : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset addLayerPanelUXML;  // AddLayerPanel UXML
    [SerializeField] private VisualTreeAsset listViewItemUXML;   // ListViewItem UXML (wraps your button)

    [Header("Inspector (optional)")]
    [SerializeField] private UIDocument inspectorDocument;       // If not assigned, will use GetComponent<UIDocument>()

    // Simple data model for each layer entry (label + icon key)
    private readonly List<(string label, string iconKey)> layerItems = new()
    {
        ("Layer A", "icon_layer_a"),
        ("Layer B", "icon_layer_b"),
        ("Layer C", "icon_layer_c"),
        ("Layer D", "icon_layer_d")
    };

    // Icon mapping filled via Inspector so we don't rely on Resources/ paths
    [System.Serializable]
    public class IconEntry
    {
        public string key;        // e.g. "icon_layer_a"
        public Texture2D texture; // drag from Assets/Sprites/UI/Icons/...
    }

    [SerializeField] private List<IconEntry> iconEntries = new();
    private Dictionary<string, Texture2D> iconMap;

    private VisualElement root;
    private Button libraryButton;
    private VisualElement contentPlaceholder;
    private ListView layerListView;

    private void OnEnable()
    {
        // Build a fast lookup dictionary from Inspector-provided entries
        iconMap = new Dictionary<string, Texture2D>();
        foreach (var e in iconEntries)
        {
            if (!string.IsNullOrEmpty(e.key) && e.texture != null)
                iconMap[e.key] = e.texture;
        }
    }

    private void Awake()
    {
        // Get the UIDocument from Inspector or from the same GameObject
        if (inspectorDocument == null)
            inspectorDocument = GetComponent<UIDocument>();

        root = inspectorDocument.rootVisualElement;
        libraryButton = root.Q<Button>("LibraryButton");
        contentPlaceholder = root.Q<VisualElement>("Content");

        // Show the AddLayerPanel on click
        libraryButton.clicked += ShowAddLayerPanel;
    }

    private void OnDisable()
    {
        if (libraryButton != null)
            libraryButton.clicked -= ShowAddLayerPanel;
    }

    private void ShowAddLayerPanel()
    {
        // Instantiate the AddLayerPanel and make it visible
        var panel = addLayerPanelUXML.CloneTree();
        panel.style.display = DisplayStyle.Flex;

        // Replace previous content and add the panel to the placeholder
        contentPlaceholder.Clear();
        contentPlaceholder.Add(panel);

        // Find the first ListView in the panel (no hardcoded name required)
        layerListView = panel.Q<ListView>();
        layerListView.itemsSource = layerItems;
        layerListView.makeItem = MakeListViewItem;
        layerListView.bindItem = BindListViewItem;
        layerListView.selectionType = SelectionType.None;

        // Item height is defined by the item template (your button),
        // so we don't set fixedItemHeight here.
    }

    // Instantiate one copy of the ListViewItem template
    private VisualElement MakeListViewItem()
    {
        return listViewItemUXML.CloneTree();
    }

    // Bind the label and icon to each item instance
    private void BindListViewItem(VisualElement element, int index)
    {
        var data = layerItems[index];

        // Keep the built-in Button label empty so only your custom label is visible
        var btn = element.Q<Button>();
        if (btn != null) btn.text = string.Empty;

        // Update your custom label (Label with name="ItemLabel")
        var itemLabel = element.Q<Label>("ItemLabel");
        if (itemLabel != null) itemLabel.text = data.label;

        // Update icon only if we can resolve a texture (otherwise keep USS placeholder)
        var iconElt = element.Q<VisualElement>("Icon");
        if (iconElt != null)
        {
            var tex = ResolveIconTexture(data.iconKey);
            if (tex != null)
            {
                iconElt.style.backgroundImage = new StyleBackground(tex);
            }
        }
    }

    // Resolve a Texture2D from the Inspector-provided mapping
    private Texture2D ResolveIconTexture(string key)
    {
        if (iconMap != null && iconMap.TryGetValue(key, out var tex))
            return tex;

        return null; // keep the USS/UXML default if not found
    }
}
