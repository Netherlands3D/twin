using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DashboardHeader : VisualElement
    {
        private readonly ScrollView buildingBlocks;
        private readonly VisualElement blockNavigation;
        private readonly Button scrollLeft;
        private readonly Button scrollRight;
        private readonly Button totalButton;
        private readonly List<Button> blockButtons = new();
        private string[] blockIdentifiers = Array.Empty<string>();
        private Button selectedButton;

        internal string SelectedBlock { get; private set; }
        internal event Action BlockClicked;

        public DashboardHeader()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            buildingBlocks = this.Q<ScrollView>("BuildingBlocks");
            blockNavigation = this.Q("BlockNavigation");
            scrollLeft = this.Q<Button>("ScrollBlocksLeft");
            scrollRight = this.Q<Button>("ScrollBlocksRight");
            scrollLeft.clicked += () => ScrollBlocks(-buildingBlocks.contentViewport.layout.width * 0.75f);
            scrollRight.clicked += () => ScrollBlocks(buildingBlocks.contentViewport.layout.width * 0.75f);
            blockNavigation.RegisterCallback<GeometryChangedEvent>(_ => UpdateBlockNavigation());
            buildingBlocks.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => UpdateBlockNavigation());
            buildingBlocks.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => UpdateBlockNavigation());
            buildingBlocks.horizontalScroller.valueChanged += _ => UpdateBlockNavigation();
            blockNavigation.RegisterCallback<WheelEvent>(OnBlockWheel, TrickleDown.TrickleDown);
            buildingBlocks.RegisterCallback<FocusInEvent>(evt =>
            {
                if (evt.target is Button button)
                    buildingBlocks.ScrollTo(button);
            });
            totalButton = buildingBlocks.Q<Button>();
            totalButton.clicked += () => OnBlockClicked(totalButton, null);
            SelectBlock(totalButton, null);
        }

        internal void SetBlocks(IReadOnlyList<string> identifiers)
        {
            if (blockIdentifiers.SequenceEqual(identifiers))
                return;

            var selectedBlock = SelectedBlock;
            foreach (var button in blockButtons)
                button.RemoveFromHierarchy();
            blockButtons.Clear();

            blockIdentifiers = identifiers.ToArray();
            foreach (var identifier in blockIdentifiers)
            {
                var button = new Button { ShowIcon = Button.ButtonStyle.Normal, LabelText = identifier };
                button.clicked += () => OnBlockClicked(button, identifier);
                blockButtons.Add(button);
                buildingBlocks.Add(button);
            }

            var selectedIndex = Array.FindIndex(blockIdentifiers, identifier => string.Equals(identifier, selectedBlock, StringComparison.OrdinalIgnoreCase));
            SelectBlock(selectedIndex < 0 ? totalButton : blockButtons[selectedIndex], selectedIndex < 0 ? null : blockIdentifiers[selectedIndex]);
        }

        internal void SetLinkedBlocks(ISet<string> linkedBlocks)
        {
            for (var i = 0; i < blockIdentifiers.Length; i++)
            {
                var linked = linkedBlocks.Contains(blockIdentifiers[i]);
                blockButtons[i].LabelText = linked ? blockIdentifiers[i] : blockIdentifiers[i] + "**";
                blockButtons[i].tooltip = linked ? string.Empty : "Geen gekoppeld 3D-object";
            }
        }

        internal void SelectBlock(string identifier)
        {
            var index = Array.FindIndex(blockIdentifiers, block => string.Equals(block, identifier, StringComparison.OrdinalIgnoreCase));
            SelectBlock(index < 0 ? totalButton : blockButtons[index], index < 0 ? null : blockIdentifiers[index]);
            buildingBlocks.schedule.Execute(() =>
            {
                if (selectedButton.panel != null && buildingBlocks.contentViewport.layout.width > 0f)
                    buildingBlocks.ScrollTo(selectedButton);
            });
        }

        private float MaximumBlockScroll => Mathf.Max(0f, buildingBlocks.contentContainer.layout.width - buildingBlocks.contentViewport.layout.width);

        private void UpdateBlockNavigation()
        {
            var hasOverflow = buildingBlocks.contentContainer.layout.width > blockNavigation.layout.width + 0.5f;
            scrollLeft.EnableInClassList(UtilityClassConstants.HIDDEN, !hasOverflow);
            scrollRight.EnableInClassList(UtilityClassConstants.HIDDEN, !hasOverflow);
            scrollLeft.SetEnabled(buildingBlocks.scrollOffset.x > 0.5f);
            scrollRight.SetEnabled(buildingBlocks.scrollOffset.x < MaximumBlockScroll - 0.5f);
        }

        private void ScrollBlocks(float distance)
        {
            buildingBlocks.scrollOffset = new Vector2(Mathf.Clamp(buildingBlocks.scrollOffset.x + distance, 0f, MaximumBlockScroll), 0f);
            UpdateBlockNavigation();
        }

        private void OnBlockWheel(WheelEvent evt)
        {
            if (MaximumBlockScroll <= 0f)
                return;

            var delta = Mathf.Abs(evt.delta.x) > Mathf.Abs(evt.delta.y) ? evt.delta.x : evt.delta.y;
            ScrollBlocks(delta * 40f);
            evt.StopPropagation();
        }

        private void OnBlockClicked(Button button, string identifier)
        {
            SelectBlock(button, identifier);
            BlockClicked?.Invoke();
        }

        private void SelectBlock(Button button, string identifier)
        {
            selectedButton?.SetCheckedPseudoState(false);
            selectedButton = button;
            selectedButton.SetCheckedPseudoState(true);
            SelectedBlock = identifier;
        }
    }
}
