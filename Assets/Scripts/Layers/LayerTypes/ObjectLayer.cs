using System;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class ObjectLayer : LayerNL3DBase, IPointerClickHandler
    {
        [SerializeField] private UnityEvent<GameObject> objectCreated = new(); 
        
        protected void Start()
        {
            objectCreated.Invoke(gameObject);
        }

        private void OnEnable()
        {
            ClickNothingPlane.ClickedOnNothing.AddListener(OnMouseClick);
        }

        private void OnMouseClick()
        {
            if (UI.IsSelected)
            {
                UI.Deselect();
            }
        }

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
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

        public void OnPointerClick(PointerEventData eventData)
        {
            UI.Select(true);
        }
    }
}