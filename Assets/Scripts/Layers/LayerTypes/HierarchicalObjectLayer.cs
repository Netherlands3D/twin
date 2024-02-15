using System;
using System.Collections;
using System.Collections.Generic;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class HierarchicalObjectLayer : ReferencedLayer, IPointerClickHandler
    {
        [SerializeField] private UnityEvent<GameObject> objectCreated = new(); 
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
            var rth = FindAnyObjectByType<RuntimeTransformHandle>(FindObjectsInactive.Include); //todo remove FindObjectOfType
            rth.SetTarget(gameObject);
        }

        public override void OnDeselect()
        {
            var rth = FindAnyObjectByType<RuntimeTransformHandle>(FindObjectsInactive.Include);
            if (rth.target == transform)
                rth.SetTarget(rth.gameObject); //todo: update RuntimeTransformHandles Package to accept null 
        }

    }
}
