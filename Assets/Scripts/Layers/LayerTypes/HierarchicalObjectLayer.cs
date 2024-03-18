using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class HierarchicalObjectLayer : ReferencedLayer, IPointerClickHandler, ILayerWithProperties
    {
        [SerializeField] private UnityEvent<GameObject> objectCreated = new();
        private List<IPropertySectionInstantiator> propertySections = new();

        private TransformHandleInterfaceToggle transformHandleInterfaceToggle;

        public override bool IsActiveInScene
        {
            get => gameObject.activeSelf;
            set
            {
                gameObject.SetActive(value);
                ReferencedProxy.UI.MarkLayerUIAsDirty();
            }
        }
        
        private void OnEnable()
        {
            ClickNothingPlane.ClickedOnNothing.AddListener(OnMouseClickNothing);
        }

        protected override void Awake()
        {
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            base.Awake();
        }

        private void Start()
        {
            objectCreated.Invoke(gameObject);
        }

        private void OnMouseClickNothing()
        {
            if (ReferencedProxy.UI.IsSelected)
            {
                ReferencedProxy.UI.Deselect();
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            ReferencedProxy.UI.Select(true);
        }
        
        public override void OnSelect()
        {
           transformHandleInterfaceToggle.SetTransformTarget(gameObject);
        }

        public override void OnDeselect()
        {
            transformHandleInterfaceToggle.ClearTransformTarget();
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }
    }
}
