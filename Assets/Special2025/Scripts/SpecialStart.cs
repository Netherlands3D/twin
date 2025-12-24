using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.UI;
using Netherlands3D.Services;
using System.Collections;
using UnityEngine;

namespace Netherlands3D.Special2025
{
    public class SpecialStart : MonoBehaviour
    {
        private Coordinate setCoordinate;

        public void SetLocation(Coordinate coordinate)
        {
            setCoordinate = coordinate;
        }

        public void PressStart()
        {
            StartCoroutine(StartViewer(setCoordinate));
        }

        private IEnumerator StartViewer(Coordinate coordinate)
        {
            yield return new WaitForSeconds(1);
            FirstPersonViewer.FirstPersonViewer firstPersonViewer = ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>();

            Vector3 rayOrigin = coordinate.ToUnity();
            rayOrigin.y = 300;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 400))
            {
                if (hit.collider != null)
                {
                    firstPersonViewer.transform.position = hit.point;
                } else firstPersonViewer.transform.position = coordinate.ToUnity();
            }
            
            firstPersonViewer.OnViewerEntered?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
