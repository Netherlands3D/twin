using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
 
[RequireComponent(typeof(TMP_Text))]
public class LinkOpener : MonoBehaviour, IPointerClickHandler 
{
    /// <summary>
    /// TMP supports generic link tags like '<link="https://netherlands3d.eu">Link to website</link>'
    /// The link content is used to open the URL in the browser using default OpenURL application.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData) {
        var textMeshPro = GetComponent<TMP_Text>();
        var linkIndex = TMP_TextUtilities.FindIntersectingLink(textMeshPro, eventData.position, null);
        if (linkIndex != -1) {
            var linkInfo = textMeshPro.textInfo.linkInfo[linkIndex];
            Application.OpenURL(linkInfo.GetLinkID());
        }
    }
}