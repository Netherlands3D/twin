using System;
using Netherlands3D.Twin.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.E2ETesting.PageObjectModel
{
    public class Element : IElement
    {
        public Element<GameObject> GameObject(string name)
        {
            return new Element<GameObject>(UnityEngine.GameObject.Find(name));
        }

        public Element<TK> Component<TK>() where TK : MonoBehaviour
        {
            return new Element<TK>(UnityEngine.Object.FindObjectOfType<TK>());
        }
    }

    public class Element<T> : IElement where T : UnityEngine.Object
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

        // Use !Value to make sure we use Unity's lifecycle checks, checking against null is not sufficient
        public bool Exists => !Value == false;

        public virtual bool IsActive
        {
            get {
                if (!Exists) return false;

                return Value switch
                {
                    GameObject gameObject => gameObject.activeInHierarchy,
                    MonoBehaviour component => component.isActiveAndEnabled,
                    _ => true
                };
            }
        }
    }
}