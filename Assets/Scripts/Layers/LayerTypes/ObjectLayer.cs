using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Events;
using Netherlands3D.Twin.UI.LayerInspector;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class ObjectLayer : LayerNL3DBase, IPointerClickHandler
    {
        public override bool IsActiveInScene
        {
            get { return gameObject.activeSelf; }
            set { gameObject.SetActive(value); }
        }
        
        public override void OnSelect()
        {
            FindObjectOfType<RuntimeTransformHandle>(true).SetTarget(gameObject); //todo remove FindObjectOfType
        }

        public override void OnDeselect()
        {
            var rth = FindObjectOfType<RuntimeTransformHandle>(true);
            if (rth.target == transform)
                rth.SetTarget(rth.gameObject); //todo: update RuntimeTransformHandles Package to accept null 
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            UI.Select(true);
        }
    }
}
