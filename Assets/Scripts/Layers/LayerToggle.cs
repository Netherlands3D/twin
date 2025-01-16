using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    [RequireComponent(typeof(Toggle))]
    public class LayerToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [FormerlySerializedAs("layerManager")] [SerializeField] protected LayerUIManager layerUIManager;
        [SerializeField] protected Transform layerParent;
        protected Toggle toggle;
        [FormerlySerializedAs("layer")] [SerializeField] protected LayerGameObject layerGameObject;
        [SerializeField] protected GameObject prefab;
        [SerializeField] protected GameObject binImage;
        [SerializeField] protected Sprite hoverSprite;
        protected Sprite defaultSprite;

        protected virtual void Awake()
        {
            toggle = GetComponent<Toggle>();
            defaultSprite = GetComponent<Image>().sprite;
        }

        protected virtual void OnEnable()
        {
            toggle.isOn = layerGameObject != null;
            ShowBin(false);

            ProjectData.Current.LayerDeleted.AddListener(OnLayerDeleted);
            toggle.onValueChanged.AddListener(CreateOrDestroyObject);
        }


        protected virtual void OnDisable()
        {
            ProjectData.Current.LayerDeleted.RemoveListener(OnLayerDeleted);
            toggle.onValueChanged.RemoveListener(CreateOrDestroyObject);
        }

        private void OnLayerDeleted(LayerData deletedLayer)
        {
            if (!toggle.isOn)
                return;

            if (deletedLayer == layerGameObject.LayerData)
            {
                toggle.onValueChanged.RemoveListener(CreateOrDestroyObject); //the layer was already deleted, it should only update the toggle
                toggle.isOn = false; //use the regular way instead of SetIsOnWithoutNotify because the toggle graphics should update.
                toggle.onValueChanged.AddListener(CreateOrDestroyObject);
            }
        }

        private void CreateOrDestroyObject(bool isOn)
        {
            if (isOn)
                layerGameObject = CreateObject();
            else
                layerGameObject.DestroyLayer();
        }

        private LayerGameObject CreateObject()
        {
            var newObject = Instantiate(prefab, Vector3.zero, Quaternion.identity, layerParent);
            newObject.name = prefab.name;

            var layerComponent = newObject.GetComponent<LayerGameObject>();
            if (!layerComponent)
                layerComponent = newObject.AddComponent<LayerGameObject>();
            
            return layerComponent;
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            ShowBin(toggle.isOn);
            GetComponent<Image>().sprite = hoverSprite;
            if (layerGameObject)
                layerUIManager.HighlightLayerUI(layerGameObject.LayerData, true);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            ShowBin(false);
            GetComponent<Image>().sprite = defaultSprite;
            if (layerGameObject)
                layerUIManager.HighlightLayerUI(layerGameObject.LayerData, false);
        }

        //also called in the inspector to update after a press
        public void ShowBin(bool isOn)
        {
            binImage.SetActive(isOn);
        }
    }
}