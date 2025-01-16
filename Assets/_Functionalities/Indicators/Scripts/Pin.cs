using TMPro;
using UnityEngine;

namespace Netherlands3D.Functionalities.Indicators
{
    public class Pin : MonoBehaviour
    {
        [SerializeField] private Transform textFacingCamera;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float scaleMultiplier = 1.0f;
        [SerializeField] private GameObject pinModel;
        
        public void SetLabel(string value)
        {
            label.text = value;

            //Restart animation
            pinModel.SetActive(false);
            pinModel.SetActive(true);
        }

        void Update()
        {
            //Scale pin based on camera distance
            var distance = Vector3.Distance(Camera.main.transform.position, transform.position);
            var scale = distance*scaleMultiplier;
            transform.localScale = new Vector3(scale, scale, scale);

            textFacingCamera.LookAt(Camera.main.transform.position, Vector3.up);
            textFacingCamera.Rotate(0, 180, 0);

            label.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
