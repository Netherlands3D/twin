using UnityEngine;

namespace Netherlands3D.Twin
{
    public class TransformHandleUI : MonoBehaviour
    {
        [SerializeField] private GameObject ui;

        private void OnEnable()
        {
            ui.SetActive(true);
        }

        private void OnDisable()
        {
            ui.SetActive(false);
        }
    }
}
