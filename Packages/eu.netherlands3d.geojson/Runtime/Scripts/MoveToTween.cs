using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands.GeoJSON
{
    public class MoveToTween
    {
        private readonly MonoBehaviour behaviour;
        private readonly GameObject subject;
        private readonly Vector3 targetPosition;
        private readonly Quaternion targetRotation;
        private readonly float duration;
        private readonly AnimationCurve curve;
        public readonly UnityEvent OnCompleted = new();

        public MoveToTween(
            MonoBehaviour callingBehaviour, 
            GameObject subject, 
            Vector3 targetPosition, 
            Quaternion targetRotation, 
            float duration, 
            AnimationCurve curve = null
        ) {
            this.behaviour = callingBehaviour;
            this.subject = subject.gameObject;
            this.targetPosition = targetPosition;
            this.targetRotation = targetRotation;
            this.duration = duration;
            this.curve = curve ?? AnimationCurve.EaseInOut(0, 0, duration, duration);
        }

        public void Play()
        {
            behaviour.StartCoroutine(MoveTo());
        }
        
        private IEnumerator MoveTo()
        {
            float t = 0;
            Vector3 startPos = subject.transform.position;
            Quaternion startRot = subject.transform.rotation;
            while (t < duration)
            {
                subject.transform.position = Vector3.Lerp(startPos, targetPosition, curve.Evaluate(t / duration));
                subject.transform.rotation = Quaternion.Lerp(startRot, targetRotation, curve.Evaluate(t / duration));
                t += Time.deltaTime;
                yield return null;
            }

            subject.transform.position = targetPosition;
            OnCompleted.Invoke();
        }
    }
}