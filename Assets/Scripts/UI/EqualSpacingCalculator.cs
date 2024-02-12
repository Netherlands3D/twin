using System;
using System.Collections;
using System.Collections.Generic;
using SLIDDES.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class EqualSpacingCalculator : MonoBehaviour
    {
        private GridLayoutGroup gridLayoutGroup;
        private RectTransform rt;
        [SerializeField] private LayoutGroup[] additionalLayoutGroups;

        public float Spacing => gridLayoutGroup.spacing.x;

#if UNITY_EDITOR
        private void OnValidate()
        {
            Awake();
            Start();
        }
#endif

        private void Awake()
        {
            gridLayoutGroup = GetComponent<GridLayoutGroup>();
            rt = GetComponent<RectTransform>();
        }

        private void Start()
        {
            CalculateSpacing();
            ApplySpacingToOtherLayoutGroups();
        }

        void CalculateSpacing()
        {
            // rt.SetRect(0, 0, 16, 16);
            
            int columnCount = gridLayoutGroup.constraintCount;
            float parentWidth = rt.rect.width;

            float totalCellWidth = gridLayoutGroup.cellSize.x * transform.childCount;
            float spacingX = (parentWidth - totalCellWidth) / (columnCount + 1);

            // Adjust the padding to achieve equal spacing
            gridLayoutGroup.spacing = new Vector2(spacingX, gridLayoutGroup.spacing.y);
            gridLayoutGroup.padding.left = Mathf.RoundToInt(spacingX);
            gridLayoutGroup.padding.right = Mathf.RoundToInt(spacingX);
        }

        private void ApplySpacingToOtherLayoutGroups()
        {
            if(additionalLayoutGroups == null)
                return;
            
            foreach (var layoutGroup in additionalLayoutGroups)
            {
                layoutGroup.padding.left = Mathf.RoundToInt(Spacing);
                layoutGroup.padding.right = Mathf.RoundToInt(Spacing);

                if (layoutGroup is GridLayoutGroup)
                {
                    ((GridLayoutGroup)layoutGroup).spacing = gridLayoutGroup.spacing;
                }
            }
        }
    }
}