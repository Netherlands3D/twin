using System;
using GG.Extensions;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.UI;
using UnityEngine;

namespace Netherlands3D
{
    public class Annotation : MonoBehaviour
    {
        [SerializeField] private TextPopout popoutPrefab;
        private TextPopout annotation;
        private string annotationText = "Test";
        private WorldTransform worldTransform;

        private void Awake()
        {
            worldTransform = GetComponent<WorldTransform>();
        }

        private void Start()
        {
            var canvasTransform = FindAnyObjectByType<Canvas>();
            annotation = CreateTextPopout(canvasTransform.transform, PivotPresets.BottomCenter);
            annotation.Show(annotationText, worldTransform.Coordinate, true);
            annotation.ReadOnly = false;
        }

        private TextPopout CreateTextPopout(Transform canvasTransform, PivotPresets pivotPoint)
        {
            var popout = Instantiate(popoutPrefab, canvasTransform);
            popout.RectTransform().SetPivot(pivotPoint);
            popout.transform.SetSiblingIndex(0);

            return popout;
        }

        private void Update()
        {
            annotation.StickTo(worldTransform.Coordinate);
        }
    }
}
