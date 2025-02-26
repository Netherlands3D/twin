using System;
using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine.SceneManagement;

namespace Netherlands3D
{
    public static partial class E2E
    {
        /// <summary>
        /// Helper method that will ensure the scene with the name "Main" is loaded before continuing; this
        /// can be used in a UnitySetup function that ensure the Main scene is loaded first -even with the
        /// ConfigLoader in front- so that End to End testing can be done directly on the main scene.
        ///
        /// See the E2ETestCase.cs for a base class that can be used for E2E testing
        /// </summary>
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
        
        public static void Then<T>(Element<T> testSubject, IResolveConstraint expression)
        {
            Assert.That(testSubject.Value, expression);
        }

        public static void Then(object testSubject, IResolveConstraint expression)
        {
            Assert.That(testSubject, expression);
        }

        public static void Then<T>(Element<T> testSubject, IResolveConstraint expression, string message)
        {
            Assert.That(testSubject.Value, expression, message);
        }

        public static void Then(object testSubject, IResolveConstraint expression, string message)
        {
            Assert.That(testSubject, expression, message);
        }

    }
}