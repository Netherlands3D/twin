using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.UI;
using Netherlands3D.Services;
using System.Collections;
using UnityEngine;

namespace Netherlands3D.Special2025
{
    public class SpecialStart : MonoBehaviour
    {
        public void SetLocation(Coordinate coordinate)
        {
            StartCoroutine(StartViewer(coordinate));
        }

        private IEnumerator StartViewer(Coordinate coordinate)
        {
            yield return new WaitForSeconds(2);
            FirstPersonViewer.FirstPersonViewer firstPersonViewer = ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>();

            //if(Physics.Raycast())

            firstPersonViewer.transform.position = coordinate.ToUnity();
            firstPersonViewer.OnViewerEntered?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
