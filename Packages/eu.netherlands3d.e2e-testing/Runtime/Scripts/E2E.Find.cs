using Netherlands3D.E2ETesting.PageObjectModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D
{
    public static partial class E2E
    {
        public static Element<GameObject> Find(string name)
        {
            return new Element<GameObject>(GameObject.Find(name));
        }

        public static Element<T> FindComponentOfType<T>() where T : MonoBehaviour
        {
            return new Element<T>(Object.FindObjectOfType<T>());
        }

        public static Element<T> FindComponentOnGameObject<T>(string onGameObject) where T : MonoBehaviour
        {
            return Find(onGameObject).Component<T>();
        }
    }
}