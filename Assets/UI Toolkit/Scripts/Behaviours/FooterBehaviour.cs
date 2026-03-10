using System.Collections;
using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.Twin;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument))]
    public class FooterBehaviour : MonoBehaviour
    {
        [SerializeField] private float UpdateInterval = 0.3f;
        [SerializeField] private StringEvent ShowAttributionEvent;
        
        private YieldInstruction intervalYieldInstruction;
        private UIDocument appDocument;

#region UI Elements
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private Footer footer;
        private Footer Footer => footer ??= Root?.Q<Footer>();
#endregion

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            
            // Ensure attribution is not visible upon start
            OnShowAttribution("");
            
            // Cannot disable because events do not support replaying, meaning you could lose attribution information
            // if attribution changed while object is disabled
            ShowAttributionEvent.AddListenerStarted(OnShowAttribution);
        }

        private void OnEnable()
        {
            // cache yield instruction to reduce allocations
            intervalYieldInstruction = new WaitForSeconds(UpdateInterval);
            StartCoroutine(ActiveCameraPositionChangeLoop());
        }

        private void OnDisable()
        {
            StopCoroutine(ActiveCameraPositionChangeLoop());
        }

        public void OnShowAttribution(string attribution)
        {
            Footer.Attribution = attribution;
        }

        public void OnActiveCameraPositionChanged(Camera activeCamera)
        {
            Coordinate coordinate = new Coordinate(activeCamera.transform.position);
            var rd = coordinate.Convert(CoordinateSystem.RDNAP);

            footer.X = (float)rd.easting;
            footer.Y = (float)rd.northing;
            footer.Z = (float)rd.height;
        }

        /// <summary>
        /// There is no event for when a coordinate changed, thus we introduce a UI loop that does not trigger every
        /// frame but at a slower interval. When an event becomes available, we can replace this timed interval with a
        /// listener.
        /// </summary>
        private IEnumerator ActiveCameraPositionChangeLoop()
        {
            while (true)
            {
                yield return intervalYieldInstruction;

                Camera activeCamera = App.Cameras.ActiveCamera;
                if (!activeCamera) continue;

                OnActiveCameraPositionChanged(activeCamera);
            }
        }
    }
}
