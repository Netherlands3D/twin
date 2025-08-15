using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Netherlands3D.UI.Components;
using Button = UnityEngine.UIElements.Button;
using ListView = UnityEngine.UIElements.ListView;

public class AddLayerPanelController : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset addLayerPanelUXML; // AddLayerPanel UXML

    [Header("Inspector (optional)")]
    [SerializeField] private UIDocument inspectorDocument; // If not assigned, will use GetComponent<UIDocument>()

    // Simple data model for each layer entry (label + icon key)
    private readonly List<(string label, Icon.IconImage iconKey)> layerItems = new()
    {
        ("Layer A", Icon.IconImage.Folder),
        ("Layer B", Icon.IconImage.Folder),
        ("Layer C", Icon.IconImage.Folder),
        ("Layer D", Icon.IconImage.Folder)
    };

    private VisualElement Root => inspectorDocument.rootVisualElement;
    private Button LibraryButton => Root.Q<Button>("LibraryButton");
    private VisualElement ContentPlaceholder => Root.Q("Content");

    private void Awake()
    {
        // Get the UIDocument from Inspector or from the same GameObject
        if (inspectorDocument == null)
        {
            inspectorDocument = GetComponent<UIDocument>();
        }
    }

    private void OnEnable()
    {
        // Show the AddLayerPanel on click
        if (LibraryButton != null)
        {
            LibraryButton.clicked += ShowAddLayerPanel;
        }
    }

    private void OnDisable()
    {
        if (LibraryButton != null)
        {
            LibraryButton.clicked -= ShowAddLayerPanel;
        }
    }

    private void ShowAddLayerPanel()
    {
        // Instantiate the AddLayerPanel and make it visible
        var panel = addLayerPanelUXML.CloneTree();
        panel.style.display = DisplayStyle.Flex;

        // Replace previous content and add the panel to the placeholder
        ContentPlaceholder.Clear();
        ContentPlaceholder.Add(panel);

        // Find the first ListView in the panel (no hardcoded name required)
        var layerListView = panel.Q<ListView>();
        layerListView.itemsSource = layerItems;
        layerListView.makeItem = MakeListViewItem;
        layerListView.bindItem = BindListViewItem;
        layerListView.selectionType = SelectionType.None;
    }

    private VisualElement MakeListViewItem()
    {
        return new ListViewItem();
    }

    // Bind the label and icon to each list view item as a child button
    private void BindListViewItem(VisualElement element, int index)
    {
        if (element is not ListViewItem item) return;

        var button = new Netherlands3D.UI.Components.Button();
        button.style.alignSelf = Align.Stretch;
        button.LabelText = layerItems[index].label;
        button.Image = layerItems[index].iconKey;

        item.Add(button);
    }
}
