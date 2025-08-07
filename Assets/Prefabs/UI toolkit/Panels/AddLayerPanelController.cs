using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class AddLayerPanelController : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset addLayerPanelUXML; // AddLayerPanel UXML
    [SerializeField] private VisualTreeAsset listViewItemUXML;  // ListViewItem UXML (wraps your button)

    // simple data model for each layer entry
    private readonly List<(string label, string iconName)> layerItems = new()
    {
        ("Layer A", "icon_layer_a"),
        ("Layer B", "icon_layer_b"),
        ("Layer C", "icon_layer_c"),
        ("Layer D", "icon_layer_d")
    };

    private UIDocument inspectorDocument;
    private VisualElement root;
    private Button libraryButton;
    private VisualElement contentPlaceholder;
    private ListView layerListView;

    private void Awake()
    {
        // cache references to Inspector elements
        inspectorDocument = GetComponent<UIDocument>();
        root = inspectorDocument.rootVisualElement;
        libraryButton = root.Q<Button>("LibraryButton");
        contentPlaceholder = root.Q<VisualElement>("Content");

        libraryButton.clicked += ShowAddLayerPanel;
    }

    private void ShowAddLayerPanel()
    {
        // instantiate the panel and make it visible
        var panel = addLayerPanelUXML.CloneTree();
        panel.style.display = DisplayStyle.Flex;

        // clear previous and add the panel
        contentPlaceholder.Clear();
        contentPlaceholder.Add(panel);

        // configure ListView to use your new ListViewItem template
        layerListView = panel.Q<ListView>();
        layerListView.itemsSource = layerItems;
        layerListView.makeItem = MakeListViewItem;
        layerListView.bindItem = BindListViewItem;
        layerListView.selectionType = SelectionType.None;
    }

    // instantiate one copy of the ListViewItem template
    private VisualElement MakeListViewItem()
    {
        return listViewItemUXML.CloneTree();
    }

    // bind the label and icon to each item instance
    private void BindListViewItem(VisualElement element, int index)
    {
        var data = layerItems[index];

        // assign text on the Button inside your ListViewItem
        var btn = element.Q<Button>();
        if (btn != null)
            btn.text = data.label;

        // assign background image on the 'Icon' element
        var iconElt = element.Q<VisualElement>("Icon");
        if (iconElt != null)
        {
            var tex = Resources.Load<Texture2D>(data.iconName);
            iconElt.style.backgroundImage = new StyleBackground(tex);
        }
    }
}