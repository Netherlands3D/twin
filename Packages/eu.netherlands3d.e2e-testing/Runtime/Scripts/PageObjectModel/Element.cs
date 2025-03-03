using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.E2ETesting.PageObjectModel
{
    public class Element : IElement
    {
        public Element<GameObject> GameObject(string name)
        {
            return Element<GameObject>.For(UnityEngine.GameObject.Find(name));
        }

        public Element<TK> Component<TK>() where TK : MonoBehaviour
        {
            return Element<TK>.For(Object.FindObjectOfType<TK>());
        }
    }

    public class Element<T> : Element<T, Element<T>> where T : Object
    {
    }

    /// <summary>
    /// Abstract base class for all Page Object Model elements.
    /// </summary>
    /// <typeparam name="T">
    /// The supported/contained type, such as GameObject, MonoBehaviour or a more specific Monobehaviour type.
    /// </typeparam>
    /// <typeparam name="TDerived">
    /// When using the `For` static factory method, this is the type it will instantiate and return; in concrete classes
    /// this will be the same as the Element class, i.e.: ButtonElement has Button as T, and ButtonElement as TDerived.
    /// This trick helps support polymorphic behaviour so that you can build a true model consisting of multiple types
    /// of elements with their own Setup and behaviour
    /// </typeparam>
    public abstract class Element<T, TDerived> : IElement 
        where T : Object 
        where TDerived : Element<T, TDerived>, new()
    {
        public T Value { get; internal set; }

        public static TDerived For(T value)
        {
            var element = new TDerived
            {
                Value = value
            };
            element.Setup();

            return element;
        }
        
        public static TDerived For(Element<T> value)
        {
            return For(value?.Value);
        }

        protected virtual void Setup()
        {
        }

        [CanBeNull]
        public Element<GameObject> GameObject(string name)
        {
            return Value switch
            {
                GameObject gameObject => Element<GameObject>.For(gameObject.transform.Find(name).gameObject),
                MonoBehaviour component => Element<GameObject>.For(component.transform.Find(name).gameObject),
                _ => null
            };
        }

        [CanBeNull]
        public Element<TK> Component<TK>() where TK : MonoBehaviour
        {
            return Value switch
            {
                GameObject gameObject => Element<TK>.For(gameObject.transform.GetComponentInChildren<TK>()),
                MonoBehaviour component => Element<TK>.For(component.transform.GetComponentInChildren<TK>()),
                _ => null
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