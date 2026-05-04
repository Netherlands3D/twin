using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    public class ToolbarMainBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument appDocument;

        #region UI Elements
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private HamburgerMenu hamburgerMenu;
        private HamburgerMenu HamburgerMenu => hamburgerMenu ??= Root?.Q<HamburgerMenu>();
        #endregion

        public UnityEvent OnOpenProject = new();
        public UnityEvent OnSaveProject = new();
        public UnityEvent OnOpenSettings = new();
        public UnityEvent OnHelp = new();

        private void Start()
        {
            HamburgerMenu.OpenProjectButton.RegisterCallback<ClickEvent>(OnOpenProjectAction);
            HamburgerMenu.SaveProjectButton.RegisterCallback<ClickEvent>(OnSaveProjectAction);
            HamburgerMenu.SettingsButton.RegisterCallback<ClickEvent>(OnOpenSettingsAction);
            HamburgerMenu.HelpButton.RegisterCallback<ClickEvent>(OnHelpAction);
        }

        private void OnOpenProjectAction(ClickEvent _) => OnOpenProject?.Invoke();
        private void OnSaveProjectAction(ClickEvent _) => OnSaveProject?.Invoke();
        private void OnOpenSettingsAction(ClickEvent _) => OnOpenSettings?.Invoke();
        private void OnHelpAction(ClickEvent _) => OnHelp?.Invoke();
        
        public void OpenHamburgerMenu() => HamburgerMenu.value = true;
        public void CloseHamburgerMenu() => HamburgerMenu.value = false;
    }
}