using System.Linq;
using Netherlands3D.Functionalities.ObjectLibrary;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
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
                foreach (var reference in group.prefabReferences)
                {
                    CreateButton(reference, groupPanel);
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
                foreach (var reference in group.prefabReferences)
                {
                    CreateButton(reference, groupPanel);
                }
            }
        }

        private GameObject GetGroupPanel(string groupName)
        {
            Transform[] panels = contentParent.transform.GetComponentsInChildren<Transform>(true);
            var panel = panels
                .Where(t => t.gameObject.name == groupName)
                .Select(t => t.gameObject)
                .FirstOrDefault();

            if (!panel)
            {
                panel = CreateGroupPanel(groupName);
            }
            
            return panel;
        }

        private GameObject CreateGroupPanel(string groupGroupName)
        {
            var title = Instantiate(titlePrefab, titleRow);
            title.GetComponentInChildren<TMP_Text>().text = groupGroupName;
            title.SetActive(false);
            
            var button = Instantiate(panelButtonPrefab, mainButtonPanel);
            button.GetComponentInChildren<TMP_Text>().text = groupGroupName;

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
            button.SetPrefab(prefab.gameObject);
            button.GetComponentInChildren<TMP_Text>().text = LayerGameObjectFactory.GetLabel(prefab.gameObject);

            return button;
        }

        private ObjectLibraryButton CreateButton(PrefabReference reference, GameObject groupPanel)
        {
            var button = Instantiate(buttonPrefab, groupPanel.transform);
            button.SetPrefab(reference);
            button.GetComponentInChildren<TMP_Text>().text = LayerGameObjectFactory.GetLabel(reference);

            return button;
        }
    }
}
