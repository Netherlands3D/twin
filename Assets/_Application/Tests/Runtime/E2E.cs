using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Tests
{
    public static class E2E
    {
        public static IEnumerator EnsureMainSceneIsLoaded()
        {
            var mainScene = "Main";
            if (SceneManager.GetActiveScene().name == mainScene) yield break;

            var asyncOperation = SceneManager.LoadSceneAsync(0);
            if (asyncOperation == null)
            {
                var sceneName = SceneManager.GetSceneAt(0).name;
                throw new Exception($"Failed to load scene '0': {sceneName}");
            }

            while (!asyncOperation.isDone) yield return null;
            while (SceneManager.GetActiveScene().name != mainScene) yield return null;
        }
        
        public static class Get
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
                
                public void Assert(Constraint constraint, string message = null)
                {
                    NUnit.Framework.Assert.That(Value, constraint, message);
                }
            }

            public static Element<GameObject> GameObject(string name)
            {
                return new Element<GameObject>(UnityEngine.GameObject.Find(name));
            }

            public static Element<T> Component<T>() where T : MonoBehaviour
            {
                 return new Element<T>(UnityEngine.Object.FindObjectOfType<T>());
            }
        }

        public class Subject<T>
        {
            public T Value { get; }

            public Subject(T value)
            {
                this.Value = value;
            }
        }

        public static class Sidebar
        {
            public static void OpenLayers()
            {
                var toolButton = GameObject.Find("ToolbarButton_Layers").GetComponent<Button>();
                toolButton.onClick.Invoke();
            }
        }
    }
}