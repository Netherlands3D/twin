using Netherlands3D.Events;
using Netherlands3D.Twin.Tools;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    public class ToolbarTopBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument appDocument;

        [Header("Tools")] 
        [SerializeField] private Tool domeTool;
        [SerializeField] private TriggerEvent snapshotEvent;

        #region UI Elements

        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private ToolbarToolbox toolbarToolbox;
        private ToolbarToolbox ToolbarToolbox => toolbarToolbox ??= Root?.Q<ToolbarToolbox>();

        #endregion

        private void OnEnable()
        {
            ToolbarToolbox.OnDomeToggled += OnDomeToggled;
            ToolbarToolbox.OnScreenshotClicked += OnScreenshotClicked;

            domeTool.onOpen.AddListener(OnDomeToolOpen);
            domeTool.onClose.AddListener(OnDomeToolClose);
            ToolbarToolbox.SetDomeValueWithoutNotify(domeTool.Open);
        }

        private void OnDisable()
        {
            ToolbarToolbox.OnDomeToggled -= OnDomeToggled;
            ToolbarToolbox.OnScreenshotClicked -= OnScreenshotClicked;

            domeTool.onOpen.RemoveListener(OnDomeToolOpen);
            domeTool.onClose.RemoveListener(OnDomeToolClose);
        }

        private void OnDomeToolOpen() => ToolbarToolbox.SetDomeValueWithoutNotify(true);

        private void OnDomeToolClose() => ToolbarToolbox.SetDomeValueWithoutNotify(false);

        private void OnDomeToggled(bool isOn)
        {
            if (isOn) domeTool.OpenInspector();
            else domeTool.CloseInspector();
        }

        private void OnScreenshotClicked()
        {
            snapshotEvent.InvokeStarted();
        }
    }
}