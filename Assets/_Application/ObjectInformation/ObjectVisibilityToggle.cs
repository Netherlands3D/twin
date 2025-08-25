using Netherlands3D.Functionalities.ObjectInformation;
using UnityEngine;
using UnityEngine.UI;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.SubObjects;

namespace Netherlands3D.Twin.UI
{
    public class ObjectVisibilityToggle : MonoBehaviour
    {
        private ObjectSelectorService selector;
        private TransformHandleInterfaceToggle transformInterfaceToggle;

        [SerializeField] private ToggleGroupItem visibilityToggle;
        [SerializeField] private Dialog visibilityDialog;

        private IMapping currentSelectedFeatureObject;
        private object currentSelectedTransformObject;
        private string currentSelectedBagId;        

        private void Awake()
        {
            visibilityToggle = GetComponent<ToggleGroupItem>();           
        }

        private void Start()
        {  
            UpdateButton();
        }

        private void OnEnable()
        {            
            transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle == null) return;
            selector = ServiceLocator.GetService<ObjectSelectorService>();
            if (selector == null) return;

            transformInterfaceToggle.SetTarget.AddListener(OnTransformObjectFound);          

            selector.SelectSubObjectWithBagId.AddListener(OnBagIdFound);
            selector.SelectFeature.AddListener(OnFeatureFound);
            selector.OnSelectDifferentLayer.AddListener(ClearSelection);
            selector.OnDeselect.AddListener(ClearSelection);

            visibilityToggle.Toggle.onValueChanged.AddListener(OnToggle);
        }

        private void OnDisable()
        {
          
            transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            transformInterfaceToggle.SetTarget.RemoveListener(OnTransformObjectFound);
            selector = ServiceLocator.GetService<ObjectSelectorService>();
            selector.SelectSubObjectWithBagId.RemoveListener(OnBagIdFound);
            selector.SelectFeature.RemoveListener(OnFeatureFound);
            selector.OnSelectDifferentLayer.RemoveListener(ClearSelection);
            selector.OnDeselect.RemoveListener(ClearSelection);
            
            visibilityToggle.Toggle.onValueChanged.RemoveListener(OnToggle);
        }


        private void OnToggle(bool toggle)
        {
            DialogService service = ServiceLocator.GetService<DialogService>();
            service.CloseDialog();

            if (currentSelectedFeatureObject == null) return;
            
            if (toggle)
            {
                service.ShowDialog(visibilityDialog, new Vector2(20, 0), visibilityToggle.GetComponent<RectTransform>());
                service.ActiveDialog.Close.AddListener(() => visibilityToggle.Toggle.isOn = false);
                service.ActiveDialog.Confirm.AddListener(() =>
                {
                    //TODO this is work in progress and the logic should be moved to its own service?
                    LayerGameObject layer = selector.GetLayerGameObjectFromMapping(currentSelectedFeatureObject);
                    if (currentSelectedFeatureObject is MeshMapping mapping)
                    {
                        foreach (ObjectMappingItem item in mapping.ObjectMapping.items)
                        {
                            if (currentSelectedBagId == item.objectID)
                            {
                                LayerFeature layerFeature = layer.LayerFeatures[item];
                                (layer.Styler as CartesianTileLayerStyler).SetVisibilityForSubObject(layerFeature, false);
                                break;
                            }
                        }
                    }                    
                });

                if (currentSelectedBagId != null)
                {
                    HideObjectDialog dialog = service.ActiveDialog as HideObjectDialog;
                    dialog.SetBagId(currentSelectedBagId);
                }
            }
        }

        private void OnBagIdFound(IMapping mapping, string bagId)
        {
            currentSelectedFeatureObject = mapping;
            currentSelectedBagId = bagId;

            //when selecting a new bag id we should close any dialog if active
            CloseDialog();
            UpdateButton();
        }

        private void OnFeatureFound(IMapping mapping)
        {
            currentSelectedFeatureObject = mapping;

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
            foreach (Transform child in transform)
            {
                Image image = child.GetComponent<Image>();
                if (image != null)
                    image.enabled = visible;
            }

            if (!visible)
                visibilityToggle.Toggle.isOn = false;
        }
    }
}
