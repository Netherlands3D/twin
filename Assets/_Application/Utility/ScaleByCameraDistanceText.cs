using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Utility
{
    public class ScaleByCameraDistanceText : MonoBehaviour
    {
        [SerializeField] private float distanceScale = 0.001f;

        private float maxScale = 0.5f;
        private Camera mainCamera;
        private TextMeshPro text;
        private TMP_TextInfo textInfo;
        private float characterWorldScale;
        private Vector3 previousPosition;
        private Vector3 previousRotation;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            mainCamera = Camera.main;
            text = GetComponent<TextMeshPro>();
            textInfo = text.textInfo;

            text.ForceMeshUpdate();
            int characterCount = textInfo.characterCount;
            Vector3 leftVertex = textInfo.characterInfo[0].bottomLeft;
            Vector3 rightVertex = textInfo.characterInfo[characterCount - 1].bottomRight;
            Vector3 worldLeft = text.transform.TransformPoint(leftVertex);
            Vector3 worldRight = text.transform.TransformPoint(rightVertex);
            characterWorldScale = Vector3.Distance(worldLeft, worldRight) / characterCount;

        }

        // Update is called once per frame
        void Update()
        {
            if (mainCamera.transform.position.y != previousPosition.y || mainCamera.transform.rotation.eulerAngles.y != previousRotation.y)
            {
                previousPosition = mainCamera.transform.position;
                previousRotation = mainCamera.transform.rotation.eulerAngles;           
                float cameraScale = Vector3.Distance(mainCamera.transform.position, transform.position) * distanceScale;   
                float distToGround = Mathf.Abs(transform.position.y - mainCamera.transform.position.y) / 5000;
                if (cameraScale > maxScale + distToGround)
                    cameraScale = maxScale + distToGround;

                transform.localScale = Vector3.one * cameraScale;
            }
        }
    }
}
