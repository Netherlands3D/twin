using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Twin.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D
{
    public class ObjectVisibilityToggle : MonoBehaviour
    {
        private ObjectSelectorService selector;
        private TransformHandleInterfaceToggle transformInterfaceToggle;

        [SerializeField] private ToggleGroupItem visibilityToggle;
        [SerializeField] private Dialog visibilityDialog;

        private object currentSelectedFeatureObject;
        private object currentSelectedTransformObject;
        private string currentSelectedBagId;        

        private void Awake()
        {
            visibilityToggle = GetComponent<ToggleGroupItem>();           
        }

        private void Start()
        {
            selector = ServiceLocator.GetService<ObjectSelectorService>();
            transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            transformInterfaceToggle.SetTarget.AddListener(OnTransformObjectFound);
            selector.OnSelectDifferentLayer.AddListener(ClearSelection);
            UpdateButton();
        }

        private void OnToggle(bool toggle)
        {
            if (currentSelectedFeatureObject == null) return;

            DialogService service = ServiceLocator.GetService<DialogService>();
            if (toggle)
            {
                service.ShowDialog(visibilityDialog, new Vector2(20, 0), visibilityToggle.GetComponent<RectTransform>());
                service.ActiveDialog.Close.AddListener(() => visibilityToggle.Toggle.isOn = false);

                if (currentSelectedBagId != null)
                {
                    HideObjectDialog dialog = service.ActiveDialog as HideObjectDialog;
                    dialog.SetBagId(currentSelectedBagId);
                }
            }
            else
            {
                service.CloseDialog();
            }
        }

        private void OnBagIdFound(IMapping mapping, string bagId)
        {
            currentSelectedFeatureObject = mapping.MappingObject;
            currentSelectedBagId = bagId;

            //when selecting a new bag id we should close any dialog if active
            CloseDialog();
            UpdateButton();
        }

        private void OnFeatureFound(IMapping mapping)
        {
            currentSelectedFeatureObject = mapping.MappingObject;

            //when selecting a new bag id we should close any dialog if active
            CloseDialog();
            UpdateButton();
        }

        private void OnTransformObjectFound(GameObject target)
        {
            currentSelectedTransformObject = target;

            //when selecting a new bag id we should close any dialog if active
            CloseDialog();
            UpdateButton();
        }

        private void ClearSelection()
        {
            currentSelectedFeatureObject = null;
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
            SetVisibile(currentSelectedFeatureObject != null || currentSelectedTransformObject != null);
        }

        private void SetVisibile(bool visible)
        {
            foreach(Image image in transform.GetComponentsInChildren<Image>())
                image.enabled = visible;
        }

        private void OnEnable()
        {          
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
