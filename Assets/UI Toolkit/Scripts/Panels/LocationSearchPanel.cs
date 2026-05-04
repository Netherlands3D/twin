using System;
using System.Collections.Generic;
using Netherlands3D.AddressSearch;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Behaviours;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    /// <summary>
    /// Presentational inspector panel for location search.
    /// Owns a <see cref="SearchBar"/> and exposes C# events for user interactions.
    /// </summary>
    [UxmlElement]
    public partial class LocationSearchPanel : BaseInspectorContentPanel
    {
        private SearchBar searchBar;
        private SearchBar SearchBar => searchBar ??= this.Q<SearchBar>("AddressSearchBar");
        private NumberField coordinateXField;
        private NumberField CoordinateXField => coordinateXField ??= this.Q<NumberField>("CoordinateXField");
        private NumberField coordinateYField;
        private NumberField CoordinateYField => coordinateYField ??= this.Q<NumberField>("CoordinateYField");

        private List<SuggestionResult> currentSuggestions = new();
        private int navigatedIndex = -1;
        private bool callbacksRegistered;

        /// <summary>Fires when the query text changes.</summary>
        public event Action<string> QueryChanged;

        /// <summary>
        /// Fires when the user presses Enter.
        /// The second argument is the currently highlighted suggestion (null when list is empty).
        /// </summary>
        public event Action<string, SuggestionResult?> SubmitRequested;

        /// <summary>Fires when the user clicks a suggestion item.</summary>
        public event Action<SuggestionResult> SuggestionSelected;

        /// <summary>Fires when X is submitted from the coordinate input.</summary>
        public event Action<string> CoordinateXSubmitted;

        /// <summary>Fires when Y is submitted from the coordinate input.</summary>
        public event Action<string> CoordinateYSubmitted;

        public LocationSearchPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += () => EnableInClassList("active", true);
            OnHide += () =>
            {
                EnableInClassList("active", false);
                ClearSuggestions();
            };

            RegisterCallback<AttachToPanelEvent>(_ => SetupInputCallbacks());
        }

        public override string GetTitle() => "Zoeken";

        /// <summary>Populate the results list. Highlights the first item automatically.</summary>
        public void SetSuggestions(List<SuggestionResult> suggestions)
        {
            currentSuggestions = suggestions ?? new List<SuggestionResult>();
            navigatedIndex = currentSuggestions.Count > 0 ? 0 : -1;

            SearchBar.ResultsList.itemsSource = currentSuggestions;
            SearchBar.ResultsList.Rebuild();
            SearchBar.IsOpen = currentSuggestions.Count > 0;

            if (navigatedIndex >= 0)
            {
                SearchBar.ResultsList.SetSelectionWithoutNotify(new[] { navigatedIndex });
            }
        }

        /// <summary>Remove all result items and close the dropdown.</summary>
        public void ClearSuggestions()
        {
            currentSuggestions.Clear();
            navigatedIndex = -1;
            SearchBar.ResultsList.itemsSource = currentSuggestions;
            SearchBar.ResultsList.Rebuild();
            SearchBar.IsOpen = false;
        }

        /// <summary>Set the query field text without triggering <see cref="QueryChanged"/>.</summary>
        public void SetQueryText(string text)
        {
            SearchBar.QueryField.SetValueWithoutNotify(text);
        }

        /// <summary>Update both coordinate fields without triggering submit callbacks.</summary>
        public void SetCoordinates(int x, int y)
        {
            CoordinateXField.SetValueWithoutNotify(x);
            CoordinateYField.SetValueWithoutNotify(y);
        }

        /// <summary>Toggle invalid style for both coordinate fields.</summary>
        public void SetCoordinateValidity(bool valid)
        {
            CoordinateXField.EnableInClassList("invalid", !valid);
            CoordinateYField.EnableInClassList("invalid", !valid);
        }

        private void SetupInputCallbacks()
        {
            if (callbacksRegistered) return;
            callbacksRegistered = true;

            // Text value changed → notify behaviour
            SearchBar.QueryField.RegisterValueChangedCallback(evt =>
                QueryChanged?.Invoke(evt.newValue));

            // Enter key → submit with the currently navigated suggestion (if any)
            SearchBar.QueryField.RegisterCallback<NavigationSubmitEvent>(_ =>
            {
                SuggestionResult? active = navigatedIndex >= 0 && navigatedIndex < currentSuggestions.Count
                    ? currentSuggestions[navigatedIndex]
                    : null;
                SubmitRequested?.Invoke(SearchBar.QueryField.value, active);
            }, TrickleDown.TrickleDown);

            // Arrow keys → navigate suggestion list while keeping focus in the text field

            SearchBar.QueryField.RegisterCallback<KeyDownEvent>(OnQueryFieldKeyDown, TrickleDown.TrickleDown);

            // Configure the results ListView
            SearchBar.ResultsList.makeItem = MakeResultItem;
            SearchBar.ResultsList.bindItem = BindResultItem;
            SearchBar.ResultsList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            SearchBar.ResultsList.selectionType = SelectionType.Single;

            RegisterCoordinateCallbacks();
        }

        private void RegisterCoordinateCallbacks()
        {
            CoordinateXField.InputField.RegisterCallback<NavigationSubmitEvent>(_ =>
                CoordinateXSubmitted?.Invoke(CoordinateXField.InputField.value), TrickleDown.TrickleDown);
            CoordinateYField.InputField.RegisterCallback<NavigationSubmitEvent>(_ =>
                CoordinateYSubmitted?.Invoke(CoordinateYField.InputField.value), TrickleDown.TrickleDown);

            // Match UGUI onEndEdit behaviour when focus leaves the field.
            CoordinateXField.InputField.RegisterCallback<FocusOutEvent>(_ =>
                CoordinateXSubmitted?.Invoke(CoordinateXField.InputField.value));
            CoordinateYField.InputField.RegisterCallback<FocusOutEvent>(_ =>
                CoordinateYSubmitted?.Invoke(CoordinateYField.InputField.value));
        }

        private void OnQueryFieldKeyDown(KeyDownEvent evt)
        {
            if (currentSuggestions.Count == 0) return;

            if (evt.keyCode == KeyCode.DownArrow)
            {
                navigatedIndex = Mathf.Clamp(navigatedIndex + 1, 0, currentSuggestions.Count - 1);
                SearchBar.ResultsList.SetSelectionWithoutNotify(new[] { navigatedIndex });
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                navigatedIndex = Mathf.Clamp(navigatedIndex - 1, 0, currentSuggestions.Count - 1);
                SearchBar.ResultsList.SetSelectionWithoutNotify(new[] { navigatedIndex });
                evt.StopPropagation();
            }
        }

        private VisualElement MakeResultItem()
        {
            var item = new ListViewItem();
            var label = new Label();
            label.AddToClassList("label");
            item.Add(label);
            item.RegisterCallback<ClickEvent>(OnResultItemClicked);
            return item;
        }

        private void BindResultItem(VisualElement element, int index)
        {
            if (element.Q<Label>() is Label label && index < currentSuggestions.Count)
                label.text = currentSuggestions[index].Label;

            element.userData = index;
        }

        private void OnResultItemClicked(ClickEvent evt)
        {
            if (evt.currentTarget is VisualElement item &&
                item.userData is int index &&
                index >= 0 && index < currentSuggestions.Count)
            {
                navigatedIndex = index;
                SuggestionSelected?.Invoke(currentSuggestions[index]);
            }
        }
    }
}