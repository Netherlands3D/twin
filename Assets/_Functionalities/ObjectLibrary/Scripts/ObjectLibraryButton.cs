using System.Threading.Tasks;
using Netherlands3D._Application._Twin.SDK;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    [RequireComponent(typeof(Button))]
    public class ObjectLibraryButton : MonoBehaviour
    {
        private Button button;
        [SerializeField] protected LayerGameObject prefab;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(CreateObject);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(CreateObject);
        }

        // For when this component is created at runtime
        public void Initialize(LayerGameObject layerGameObject)
        {
            this.prefab = layerGameObject;
            
            var image = GetComponentInChildren<MatchImageToSelectionState>();
            if(layerGameObject.Thumbnail != null)
                image.SpriteState = layerGameObject.Thumbnail;
        }

        private void CreateObject()
        {
            // Discard task and fire and forget async task because the event listener cannot cleanly handle it
            _ = CreateLayer();
        }
        
        /// <summary>
        /// Provide an extension point where we can capture the LayerData, and thus the LayerGameObject, so that
        /// subclasses can manipulate the instantiated LayerGameObject.
        /// </summary>
        protected virtual async Task<LayerData> CreateLayer()
        {
            // TODO: Replace PrefabIdentifier with type - but this requires a change in all buttons
            var layer = Layer.OfType(prefab.PrefabIdentifier).NamedAs(prefab.name);
            
            return await Netherlands3D.Layers.Add(layer);
        }
    }
}