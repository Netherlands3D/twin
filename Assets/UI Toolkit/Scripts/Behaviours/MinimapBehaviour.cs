using Netherlands3D.Minimap;
using Netherlands3D.Twin;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    public class MinimapBehaviour : MonoBehaviour
    {
        [SerializeField] private MinimapConfig minimapConfig;
        void Start()
        {
            var minimap = App.UIRoot.Root.Q<MapViewport>();
            minimap.Initialize(minimapConfig);
        }
    }
}
