using Netherlands3D.Events;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Tools;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    public class ToolbarTopBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument appDocument;

        private Tool domeTool;
        [SerializeField] private TriggerEvent snapshotEvent;

        private ToolbarToolbox toolbarToolbox;

        private void OnEnable()
        {
            toolbarToolbox = App.UIRoot.Root.Q<ToolbarToolbox>();
            toolbarToolbox.OnDomeToggled += OnDomeToggled;
            toolbarToolbox.OnScreenshotClicked += OnScreenshotClicked;

            domeTool = ServiceLocator.GetService<ToolService>().GetTool(ToolType.Dome);
            domeTool.onOpen.AddListener(OnDomeToolOpen);
            domeTool.onClose.AddListener(OnDomeToolClose);
            toolbarToolbox.SetDomeValueWithoutNotify(domeTool.Open);
        }

        private void OnDisable()
        {
            toolbarToolbox.OnDomeToggled -= OnDomeToggled;
            toolbarToolbox.OnScreenshotClicked -= OnScreenshotClicked;

            domeTool.onOpen.RemoveListener(OnDomeToolOpen);
            domeTool.onClose.RemoveListener(OnDomeToolClose);
        }

        private void OnDomeToolOpen() => toolbarToolbox.SetDomeValueWithoutNotify(true);

        private void OnDomeToolClose() => toolbarToolbox.SetDomeValueWithoutNotify(false);

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