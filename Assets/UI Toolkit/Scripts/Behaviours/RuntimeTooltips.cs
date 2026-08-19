using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    [RequireComponent(typeof(UIDocument))]
    public class RuntimeTooltips : MonoBehaviour
    {
        [SerializeField] private StyleSheet tooltipStyleSheet;

        private UIDocument uiDocument;
        private VisualElement root;
        private Label tooltipLabel;

        private const float TooltipOffset = 6f;

        private VisualElement hoveredTooltipElement;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            root = uiDocument != null ? uiDocument.rootVisualElement : null;
            if (root == null)
                return;

            if (tooltipStyleSheet != null && !root.styleSheets.Contains(tooltipStyleSheet))
                root.styleSheets.Add(tooltipStyleSheet);

            CreateTooltip();
            RegisterTooltips();
        }

        private void CreateTooltip()
        {
            tooltipLabel?.RemoveFromHierarchy();

            tooltipLabel = new Label
            {
                name = "runtime-tooltip",
                pickingMode = PickingMode.Ignore
            };

            tooltipLabel.AddToClassList("runtime-tooltip");
            tooltipLabel.style.position = Position.Absolute;
            tooltipLabel.style.display = DisplayStyle.None;

            root.Add(tooltipLabel);
        }

        private void RegisterTooltips()
        {
            UnregisterTooltips();

            root.RegisterCallback<PointerEnterEvent>(ShowTooltip, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(HideTooltip, TrickleDown.TrickleDown);
        }

        private void ShowTooltip(PointerEnterEvent evt)
        {
            if (root == null || tooltipLabel == null)
                return;

            VisualElement element = FindTooltipElement(evt.target as VisualElement);
            if (element == null)
                return;

            hoveredTooltipElement = element;
            tooltipLabel.text = element.tooltip;

            tooltipLabel.style.display = DisplayStyle.Flex;
            tooltipLabel.BringToFront();
            PositionTooltip(element);
            tooltipLabel.schedule.Execute(() => PositionTooltip(element));
        }

        private void HideTooltip(PointerLeaveEvent evt)
        {
            if (tooltipLabel == null || hoveredTooltipElement == null)
                return;

            if (evt.target != hoveredTooltipElement)
                return;

            tooltipLabel.style.display = DisplayStyle.None;
            hoveredTooltipElement = null;
        }

        private void OnDisable()
        {
            UnregisterTooltips();

            tooltipLabel?.RemoveFromHierarchy();
            tooltipLabel = null;
            root = null;
        }

        private void UnregisterTooltips()
        {
            if (root == null)
                return;

            root.UnregisterCallback<PointerEnterEvent>(ShowTooltip, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerLeaveEvent>(HideTooltip, TrickleDown.TrickleDown);
            hoveredTooltipElement = null;
        }

        private VisualElement FindTooltipElement(VisualElement element)
        {
            while (element != null && element != root && element != tooltipLabel)
            {
                if (!string.IsNullOrWhiteSpace(element.tooltip))
                    return element;

                element = element.parent;
            }

            return null;
        }

        private void PositionTooltip(VisualElement element)
        {
            if (root == null || tooltipLabel == null || element == null)
                return;

            Vector2 elementMin = root.WorldToLocal(new Vector2(element.worldBound.xMin, element.worldBound.yMin));
            Vector2 elementMax = root.WorldToLocal(new Vector2(element.worldBound.xMax, element.worldBound.yMax));
            float tooltipWidth = tooltipLabel.resolvedStyle.width;
            float tooltipHeight = tooltipLabel.resolvedStyle.height;
            float rootWidth = root.resolvedStyle.width;
            float rootHeight = root.resolvedStyle.height;

            if (float.IsNaN(tooltipWidth) || tooltipWidth <= 0f)
                tooltipWidth = 260f;

            if (float.IsNaN(tooltipHeight) || tooltipHeight <= 0f)
                tooltipHeight = 32f;

            float left = Mathf.Clamp(elementMin.x, 0f, Mathf.Max(0f, rootWidth - tooltipWidth));
            float top = elementMax.y + TooltipOffset;

            if (top + tooltipHeight > rootHeight)
                top = elementMin.y - tooltipHeight - TooltipOffset;

            top = Mathf.Clamp(top, 0f, Mathf.Max(0f, rootHeight - tooltipHeight));

            tooltipLabel.style.left = left;
            tooltipLabel.style.top = top;
        }
    }
}
