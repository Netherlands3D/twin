using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Netherlands3D
{
    public static partial class E2E
    {
        public class Element<T>
        {
            public T Value { get; }

            public Element(T value)
            {
                this.Value = value;
            }

            public Element<GameObject> GameObject(string name)
            {
                return Value switch
                {
                    GameObject gameObject => new Element<GameObject>(gameObject.transform.Find(name).gameObject),
                    MonoBehaviour component => new Element<GameObject>(component.transform.Find(name).gameObject),
                    _ => throw new Exception($"Failed to find '{name}'")
                };
            }

            public Element<TK> Component<TK>() where TK : MonoBehaviour
            {
                return Value switch
                {
                    GameObject gameObject => new Element<TK>(gameObject.transform.GetComponentInChildren<TK>()),
                    MonoBehaviour component => new Element<TK>(component.transform.GetComponentInChildren<TK>()),
                    _ => throw new Exception($"Failed to find '{typeof(TK).Name}'")
                };
            }
                
            public void Click()
            {
                var button = this.Value as Button;
                if (!button)
                {
                    Debug.LogError("Attempting to click element that is not a button");
                    return;
                }

                button.onClick.Invoke();
            }
        }

        public static Element<GameObject> Find(string name)
        {
            return new Element<GameObject>(UnityEngine.GameObject.Find(name));
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