using UnityEngine;

namespace Netherlands3D.E2ETesting.PageObjectModel
{
    public interface IElement
    {
        /// <summary>
        /// Finds a GameObject with the given name inside the current element.
        /// </summary>
        /// <remarks>
        /// Not recommended to use directly inside tests, but rather to construct a Page Object Model with; it is left
        /// public because there may be cases where you want to deviate from this guideline.
        /// </remarks>
        public Element<GameObject> GameObject(string name);

        /// <summary>
        /// Finds a MonoBehaviour/Component with the given type inside the current element.
        /// </summary>
        /// <remarks>
        /// Not recommended to use directly inside tests, but rather to construct a Page Object Model with; it is left
        /// public because there may be cases where you want to deviate from this guideline.
        /// </remarks>
        public Element<TK> Component<TK>() where TK : MonoBehaviour;
    }
}