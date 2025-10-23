using System.Collections;
using UnityEngine;

namespace Netherlands3D.Masking
{
    public class DisappearDome : MonoBehaviour
    {
        [SerializeField] private Color colorStart;
        [SerializeField] private Color colorEnd;
        [SerializeField] private AnimationCurve animationCurve;
        private Material materialInstance;

        [SerializeField] private float disappearTime = 1.0f;

        public void DisappearFrom(Vector3 position, Vector3 localScale)
        {
            this.transform.position = position;
            this.transform.localScale = localScale;
            materialInstance = GetComponent<MeshRenderer>().material;
            StartCoroutine(Animate(localScale));
        }

        private IEnumerator Animate(Vector3 startScale)
        {
            var targetScale = Vector3.zero;
            var animationTime = 0.0f;

            materialInstance.color = colorStart;

            while (animationTime < disappearTime)
            {
                animationTime += Time.deltaTime;
                var curveTime = animationTime / disappearTime;
                var curveValue = animationCurve.Evaluate(curveTime);

                this.transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
                materialInstance.color = Color.Lerp(colorStart, colorEnd, curveValue);

                yield return null;
            }
            Destroy(this.gameObject);
        }
    }
}