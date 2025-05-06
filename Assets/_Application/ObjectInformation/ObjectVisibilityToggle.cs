using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Twin.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D
{
    public class ObjectVisibilityToggle : MonoBehaviour
    {
        private ObjectSelectorService selector;

        [SerializeField] private ToggleGroupItem visibilityToggle;
        [SerializeField] private Dialog visibilityDialog;

        private IMapping currentSelectedMapping;
        private string currentSelectedBagId;

        private void Awake()
        {
            visibilityToggle = GetComponent<ToggleGroupItem>();
           
        }

        private void Start()
        {
            UpdateButton();
        }

        private void OnToggle(bool toggle)
        {
            if (currentSelectedMapping == null) return;

            DialogService service = ServiceLocator.GetService<DialogService>();
            if (toggle)
            {
                service.ShowDialog(visibilityDialog, new Vector2(20, 0), visibilityToggle.GetComponent<RectTransform>());
                service.ActiveDialog.Close.AddListener(() => visibilityToggle.Toggle.isOn = false);
                
                HideObjectDialog dialog = service.ActiveDialog as HideObjectDialog;
                dialog.SetBagId(currentSelectedBagId);
            }
            else
            {
                service.CloseDialog();
            }
        }

        private void OnBagIdFound(IMapping mapping, string bagId)
        {
            currentSelectedMapping = mapping;
            currentSelectedBagId = bagId;

            //when selecting a new bag id we should close any dialog if active
            CloseDialog();
            UpdateButton();
        }

        private void OnFeatureFound(IMapping mapping)
        {
            currentSelectedMapping = mapping;

            //when selecting a new bag id we should close any dialog if active
            CloseDialog();
            UpdateButton();
        }

        private void ClearSelection()
        {
            currentSelectedMapping = null;
            currentSelectedBagId = null;

            //when there is no selection it makes no sense to have a dialog active
            CloseDialog();
            UpdateButton();
        }

        private void CloseDialog()
        {
            DialogService service = ServiceLocator.GetService<DialogService>();
            if (service.ActiveDialog != null)
                service.CloseDialog();
        }

        private void UpdateButton()
        {
            SetVisibile(currentSelectedMapping != null);
        }

        private void SetVisibile(bool visible)
        {
            foreach(Image image in transform.GetComponentsInChildren<Image>())
                image.enabled = visible;
        }

        private void OnEnable()
        {
            selector = ServiceLocator.GetService<ObjectSelectorService>();
            selector.SelectSubObjectWithBagId.AddListener(OnBagIdFound);
            selector.SelectFeature.AddListener(OnFeatureFound);
            selector.OnDeselect.AddListener(ClearSelection);

            visibilityToggle.Toggle.onValueChanged.AddListener(OnToggle);

            UpdateButton();
        }

        private void OnDisable()
        {           
            selector.SelectSubObjectWithBagId.RemoveListener(OnBagIdFound);
            selector.SelectFeature.RemoveListener(OnFeatureFound);
            selector.OnDeselect.RemoveListener(ClearSelection);

            visibilityToggle.Toggle.onValueChanged.RemoveListener(OnToggle);

            UpdateButton();
        }
    }
}
