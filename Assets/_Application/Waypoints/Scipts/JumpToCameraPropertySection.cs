using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class JumpToCameraPropertySection : MonoBehaviour
    {
        [SerializeField] private Button button;

        public HierarchicalObjectLayerGameObject LayerGameObject { get; set; }

        private void OnEnable()
        {
            button.onClick.AddListener(JumpToCamera);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(JumpToCamera);
        }

        private void JumpToCamera()
        {
            LayerGameObject.transform.position = Camera.main.transform.position;
            LayerGameObject.transform.rotation = Camera.main.transform.rotation;
        }
    }
}