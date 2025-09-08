using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    [RequireComponent(typeof(Toggle))]
    public class LayerToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [FormerlySerializedAs("layerManager")] [SerializeField] protected LayerUIManager layerUIManager;
        [SerializeField] protected Transform layerParent;
        protected Toggle toggle;
        [FormerlySerializedAs("layer")] [SerializeField] protected LayerGameObject layerGameObject;
        [SerializeField] protected LayerGameObject prefab;
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

        private async void CreateOrDestroyObject(bool isOn)
        {
            if (isOn)
            {
                CreateObject();
                return;
            }

            await App.Layers.Remove(layerGameObject.LayerData);
            layerGameObject = null;
        }

        private async void CreateObject()
        {
            var layerData = await App.Layers.Add(
                LayerBuilder.Create()
                    .OfType(prefab.PrefabIdentifier)
                    .NamedAs(prefab.name)
            );
            
            layerGameObject = layerData.Reference;
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