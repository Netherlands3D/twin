using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers
{
    public class HierarchicalObjectLayer : ReferencedLayer, IPointerClickHandler, ILayerWithProperties
    {
        [SerializeField] private UnityEvent<GameObject> objectCreated = new();
        private List<IPropertySection> propertySections = new();

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
            propertySections = GetComponents<IPropertySection>().ToList();
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

        public override void OnProxyTransformParentChanged()
        {
            if (ReferencedProxy.ParentLayer is PolygonSelectionLayer)
                ConvertToScatterLayer();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ReferencedProxy.UI.Select(true);
        }

        public override void OnSelect()
        {
            var rth = FindAnyObjectByType<RuntimeTransformHandle>(FindObjectsInactive.Include); //todo remove FindObjectOfType
            rth.SetTarget(gameObject);
        }

        public override void OnDeselect()
        {
            var rth = FindAnyObjectByType<RuntimeTransformHandle>(FindObjectsInactive.Include);
            if (rth.target == transform)
                rth.SetTarget(rth.gameObject); //todo: update RuntimeTransformHandles Package to accept null 
        }

        public List<IPropertySection> GetPropertySections()
        {
            return propertySections;
        }

        public void ConvertToScatterLayer()
        {
            print("converting to scatter layer");
            var scatterLayer = new GameObject(name + "_Scatter");
            var layerComponent = scatterLayer.AddComponent<ObjectScatterLayer>();

            var mesh = GetComponent<MeshFilter>().mesh; //todo: make this work with hierarchical meshes?
            var material = GetComponent<MeshRenderer>().material; //todo: make this work with hierarchical meshes?
            layerComponent.Initialize(ReferencedProxy.ParentLayer as PolygonSelectionLayer, mesh, material);

            Destroy(gameObject);
        }
    }
}