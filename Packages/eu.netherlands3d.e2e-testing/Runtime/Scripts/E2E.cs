using System;
using System.Collections;
using System.Threading.Tasks;
using Netherlands3D.E2ETesting.PageObjectModel;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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

        public static void ThenNot(object testSubject, string message = null)
        {
            Then(testSubject, Is.False, message);
        }

        public static void Then(object testSubject, IResolveConstraint expression = null, string message = null)
        {
            // By default, we test on True, a lot of assertions are of this type and it makes the tests a bit more
            // readable
            expression ??= Is.True;

            Assert.That(testSubject, expression, message);
        }

        public static void Then<T>(Element<T> testSubject, IResolveConstraint expression = null, string message = null) where T : Object
        {
            Then(testSubject.Value, expression, message);
        }

        /// <summary>
        /// Expect is similar to Then, except that it will wait for a given timeout in seconds for the constraint to
        /// evaluate. If it doesn't, then the assertion exception is thrown.
        /// </summary>
        /// <param name="testSubject">
        /// A callable that will return the value to assert - we assume this value will not be constant, but might
        /// change over time, as such we use a Func. The changes are actually what we want to verify, as Expect is meant
        /// to track whether the subject will achieve the state as defined by the assertion
        /// </param>
        /// <param name="assertion">
        /// The assertion that we expect the subject to succeed on within the given timeout
        /// </param>
        /// <param name="timeoutInSeconds">How long to wait for the assertion to become true</param>
        /// <param name="message">
        /// A text to show when the assertion fails - it is recommended to use as it will test results clearer
        /// </param>
        public static IEnumerator Expect(
            Func<object> testSubject, 
            IResolveConstraint assertion = null, 
            float timeoutInSeconds = 1f, 
            string message = null
        ) {
            float start = Time.time;
            AssertionException lastAssertionException;
            do
            {
                // First: wait until the end of the frame so that all processing is complete
                yield return new WaitForEndOfFrame(); 

                try
                {
                    Then(testSubject(), assertion, message);

                    // No exception means it was successful
                    yield break;
                }
                catch (AssertionException e)
                {
                    // Remember the exception to re-throw it after the timeout has occurred
                    lastAssertionException = e;
                }
            } while (Time.time < start + timeoutInSeconds);

            // timeout reached - let's throw the last assertion timeout
            // This should always have a value because of the "do .. while" and a success should have returned before
            // this.
            throw lastAssertionException;
        }

        public static IEnumerator Assume(
            Func<object> testSubject, 
            IResolveConstraint assertion = null, 
            float timeoutInSeconds = 1f, 
            string message = null
        ) {
            assertion ??= Is.True; 
            float start = Time.time;
            InconclusiveException lastAssertionException;
            do
            {
                // First: wait until the end of the frame so that all processing is complete
                yield return new WaitForEndOfFrame(); 

                try
                {
                    NUnit.Framework.Assume.That(testSubject(), assertion, message);

                    // No exception means it was successful
                    yield break;
                }
                catch (InconclusiveException e)
                {
                    // Remember the exception to re-throw it after the timeout has occurred
                    lastAssertionException = e;
                }
            } while (Time.time < start + timeoutInSeconds);

            // timeout reached - let's throw the last assertion timeout
            // This should always have a value because of the "do .. while" and a success should have returned before
            // this.
            throw lastAssertionException;
        }
    }
}