using System.Collections;
using Netherlands3D.Indicators;
using Netherlands3D.Indicators.Dossiers;
using Netherlands3D.SelectionTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class DossierVisualisationClickHandler : MonoBehaviour, IPointerClickHandler
{
    private InputSystemUIInputModule inputModule;

    private DossierVisualiser visualiser = null;

    private void Awake()
    {
        //Cache the inputmodule so we can reuse the last raycast result
       inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
    }

    public void SetVisualiser(DossierVisualiser visualiser)
    {
        this.visualiser = visualiser;
    }
 
    public void OnPointerClick(PointerEventData eventData)
    {
        if(!visualiser) 
        {
            Debug.LogWarning("No visualiser set for this dossier visualisation hover");
            return;
        }
        MovePointer();
    }

    private void MovePointer()
    {
        var getLastRaycastResult = inputModule.GetLastRaycastResult(0);
        if(getLastRaycastResult.gameObject == this.gameObject)
        {
            var raycastWordPosition = getLastRaycastResult.worldPosition; 
            Debug.Log("Clicked dossier area visualisation at: " + raycastWordPosition);
            
            this.visualiser.MoveSamplePointer(raycastWordPosition);
        }
    }
}
