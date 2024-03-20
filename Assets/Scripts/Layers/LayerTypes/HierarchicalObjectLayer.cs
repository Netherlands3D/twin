using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using RuntimeHandle;
using Unity.Mathematics;
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

            var mesh = CombineHierarchicalMeshes();
            var material = GetComponentInChildren<MeshRenderer>().material; //todo: make this work with multiple materials for hierarchical meshes?
            layerComponent.Initialize(ReferencedProxy.ParentLayer as PolygonSelectionLayer, mesh, material);

            Destroy(gameObject);
        }

        private Mesh CombineHierarchicalMeshes()
        {
            var originalPosition = transform.position;
            var originalRotation = transform.rotation;
            var originalScale = transform.localScale;

            transform.position = Vector3.zero; //set position to 0 to get the correct worldToLocalMatrix
            transform.rotation = quaternion.identity;
            transform.localScale = Vector3.one;

            var meshFilters = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            for (int i = 0; i < meshFilters.Length; i++)
            {
                print(meshFilters[i].mesh.vertices.Length);

                combine[i].mesh = meshFilters[i].mesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combine);
            mesh.RecalculateBounds();
            
            print(mesh.vertices.Length);
            print(mesh.bounds.center);
            print(mesh.bounds.extents);
            
            transform.position = originalPosition; //reset position
            transform.rotation = originalRotation; //reset rotation
            transform.localScale = originalScale; //reset scale
            return mesh;
        }
    }
}