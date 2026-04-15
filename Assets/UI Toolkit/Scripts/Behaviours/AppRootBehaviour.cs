using Netherlands3D.Twin.Functionalities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    [RequireComponent(typeof(UIDocument))]
    public class AppRootBehaviour : MonoBehaviour
    {
        private UIDocument appDocument;
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private void Start()
        {
            appDocument = GetComponent<UIDocument>();
        }

        public void Show()
        {
            Root.RemoveFromClassList("app--hide");
        }

        public void Hide()
        {
            Root.AddToClassList("app--hide");
        }

        /// <summary>
        /// Some UI elements should behave differently (i.e. be shown) when a functionality is enabled. This code
        /// will add a class on the top-most level so that each component can decide how to respond when a functionality
        /// is enabled.
        /// </summary>
        public void EnableFunctionality(Functionality functionality)
        {
            Root.AddToClassList("app--functionality-" + functionality.Id);
        }

        /// <summary>
        /// Removes the global class that allows part of the application's UI to respond to the functionality being
        /// disabled.
        /// </summary>
        public void DisableFunctionality(Functionality functionality)
        {
            Root.RemoveFromClassList("app--functionality-" + functionality.Id);
        }
    }
}
