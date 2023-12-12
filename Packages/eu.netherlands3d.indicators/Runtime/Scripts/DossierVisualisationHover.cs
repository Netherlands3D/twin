using System.Collections;
using Netherlands3D.Indicators.Dossiers;
using Netherlands3D.SelectionTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class DossierVisualisationHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool isHovered;

    public UnityEvent<double> onDataSampled;

    private Coroutine hoveringCoroutine;

    private InputSystemUIInputModule inputModule;

    private void Awake()
    {
        //Cache the inputmodule so we can reuse the last raycast result
       inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        if(hoveringCoroutine != null)
            StopCoroutine(hoveringCoroutine);
        
        hoveringCoroutine = StartCoroutine(Hovering());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    private void SampleData()
    {
        var getLastRaycastResult = inputModule.GetLastRaycastResult(0);
        if(getLastRaycastResult.gameObject == this.gameObject)
        {
            var raycastWordPosition = getLastRaycastResult.worldPosition; 
            Debug.Log("Clicked dossier area visualisation at: " + raycastWordPosition);

            
        }
    }

    private IEnumerator Hovering()
    {
        while(isHovered)
        {
            SampleData();
            yield return null;
        }
    }
}
