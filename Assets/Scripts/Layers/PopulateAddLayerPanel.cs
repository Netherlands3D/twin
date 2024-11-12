using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Projects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(AddLayerPanel))]
    public class PopulateAddLayerPanel : MonoBehaviour
    {
        private AddLayerPanel addLayerPanel;

        [Header("Panel references")]
        [SerializeField] private Button addLayerMenuButton;
        [SerializeField] private Transform titleRow;
        [SerializeField] private Transform mainButtonPanel;
        [SerializeField] private Transform contentParent;
        
        [Header("Prefab references")]
        [SerializeField] private GameObject titlePrefab;
        [SerializeField] private Button panelButtonPrefab;
        [SerializeField] private GameObject groupPanelPrefab;
        [SerializeField] private ObjectLibraryButton buttonPrefab;
        
        private void Awake()
        {
            addLayerPanel = GetComponent<AddLayerPanel>();
        }

        private void Start()
        {
            Populate(ProjectData.Current.PrefabLibrary);
        }

        private void Populate(PrefabLibrary library)
        {
            foreach (var group in library.prefabGroups)
            {
                if (!group.autoPopulateUI) continue;
                
                var groupPanel = CreateGroupPanel(group.groupName);
                foreach (var prefab in group.prefabs)
                {
                    CreateButton(prefab, groupPanel);
                }
            }

            foreach (var group in library.PrefabRuntimeGroups)
            {
                if (!group.autoPopulateUI) continue;

                var groupPanel = GetGroupPanel(group.groupName);
                foreach (var prefab in group.prefabs)
                {
                    CreateButton(prefab, groupPanel);
                }
            }
        }

        private GameObject GetGroupPanel(string groupName)
        {
            Transform[] panels = contentParent.transform.GetComponentsInChildren<Transform>(true);
            return panels
                .Where(t => t.gameObject.name == groupName)
                .Select(t => t.gameObject)
                .FirstOrDefault();
        }

        private GameObject CreateGroupPanel(string groupGroupName)
        {
            var title = Instantiate(titlePrefab, titleRow);
            title.GetComponentInChildren<TMP_Text>().text = groupGroupName;
            title.SetActive(false);
            
            var button = Instantiate(panelButtonPrefab, mainButtonPanel);
            var groupPanel = Instantiate(groupPanelPrefab, contentParent);
            groupPanel.name = groupGroupName;
        
            mainButtonPanel.GetComponent<EqualSpacingCalculator>().AddLayoutGroup(groupPanel.GetComponent<LayoutGroup>());
            
            //todo: add functionality listener if needed
            
            button.onClick.AddListener(()=>mainButtonPanel.gameObject.SetActive(false));
            button.onClick.AddListener(()=>title.SetActive(true));
            button.onClick.AddListener(()=>groupPanel.SetActive(true));
            button.onClick.AddListener(addLayerPanel.GetComponent<AdvancedScrollView>().ResetScrollActive);
            
            addLayerMenuButton.onClick.AddListener(()=> groupPanel.SetActive(false));
            addLayerMenuButton.onClick.AddListener(()=> title.SetActive(false));
            
            return groupPanel;
        }

        private ObjectLibraryButton CreateButton(LayerGameObject prefab, GameObject groupPanel)
        {
            var button = Instantiate(buttonPrefab, groupPanel.transform);
            button.Initialize(prefab.gameObject);
            button.GetComponentInChildren<TMP_Text>().text = prefab.name;

            return button;
        }
    }
}
