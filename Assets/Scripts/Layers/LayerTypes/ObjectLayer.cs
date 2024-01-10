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
        private RuntimeTransformHandle transformHandle;
        [SerializeField] private UnityEvent<GameObject> objectCreated = new();

        public override bool IsActiveInScene
        {
            get { return gameObject.activeSelf; }
            set { gameObject.SetActive(value); }
        }

        protected override void Awake()
        {
            base.Awake();
            transformHandle = FindAnyObjectByType<RuntimeTransformHandle>(FindObjectsInactive.Include); //todo remove FindObjectOfType
        }

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

        public override void OnSelect()
        {
            transformHandle.SetTarget(gameObject);
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