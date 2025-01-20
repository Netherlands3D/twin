using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Netherlands3D.Functionalities.Indicators.Dossiers
{
    public class DossierVisualisationClickHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler
    {
        private InputSystemUIInputModule inputModule;

        private DossierVisualiser visualiser = null;
        private bool dragged = false;

        private void Awake()
        {
            //Cache the inputmodule so we can reuse the last raycast result
            inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
        }

        public void SetVisualiser(DossierVisualiser visualiser)
        {
            this.visualiser = visualiser;
        }

        //OnDrag required to use OnBeginDrag
        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragged = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (dragged)
            {
                dragged = false;
                return;
            }

            if (!visualiser)
            {
                Debug.LogWarning("No visualiser set for this dossier visualisation hover");
                return;
            }

            MovePointer();
        }

        private void MovePointer()
        {
            var getLastRaycastResult = inputModule.GetLastRaycastResult(0);
            if (getLastRaycastResult.gameObject != this.gameObject) return;

            var worldPosition = getLastRaycastResult.worldPosition;
            this.visualiser.MoveSamplePointer(worldPosition);
        }
    }
}
