using System;
using System.Collections.Generic;
using Netherlands3D.AddressSearch;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    /// <summary>
    /// Presentational inspector panel for location search.
    /// Owns a <see cref="AutoComplete"/> and exposes C# events for user interactions.
    /// </summary>
    [UxmlElement]
    public partial class LocationSearchPanel : BaseInspectorContentPanel
    {
        private AutoComplete addressSearch;
        private AutoComplete AddressSearch => addressSearch ??= this.Q<AutoComplete>("AddressSearchBar");
        private NumberField coordinateXField;
        private NumberField CoordinateXField => coordinateXField ??= this.Q<NumberField>("CoordinateXField");
        private NumberField coordinateYField;
        private NumberField CoordinateYField => coordinateYField ??= this.Q<NumberField>("CoordinateYField");

        private List<SuggestionResult> currentSuggestions = new();

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
                SetQueryText(string.Empty);
                ClearSuggestions();
            };

            RegisterCallback<AttachToPanelEvent>(_ => OnAttachToPanel());
            RegisterCallback<DetachFromPanelEvent>(_ => OnDetachFromPanel());
        }

        public override string GetTitle() => "Zoeken";

        /// <summary>Populate the results list. Starts in an unselected state.</summary>
        public void SetSuggestions(List<SuggestionResult> suggestions)
        {
            currentSuggestions = suggestions ?? new List<SuggestionResult>();
            AddressSearch.SetResults(currentSuggestions, suggestion => suggestion.Label);
        }

        /// <summary>Remove all result items and close the dropdown.</summary>
        public void ClearSuggestions()
        {
            currentSuggestions.Clear();
            AddressSearch.ClearResults();
        }

        /// <summary>Set the query field text without triggering <see cref="QueryChanged"/>.</summary>
        public void SetQueryText(string text)
        {
            AddressSearch.SetQueryText(text);
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

        private void OnAttachToPanel()
        {
            AddressSearch.QueryChanged += OnAddressSearchQueryChanged;
            AddressSearch.SubmitRequested += OnAddressSearchSubmitRequested;
            AddressSearch.ResultActivated += OnAddressSearchResultActivated;

            RegisterCoordinateCallbacks();
        }

        private void OnDetachFromPanel()
        {
            AddressSearch.QueryChanged -= OnAddressSearchQueryChanged;
            AddressSearch.SubmitRequested -= OnAddressSearchSubmitRequested;
            AddressSearch.ResultActivated -= OnAddressSearchResultActivated;

            UnregisterCoordinateCallbacks();
        }

        private void OnAddressSearchQueryChanged(string query)
        {
            QueryChanged?.Invoke(query);
        }

        private void OnAddressSearchSubmitRequested(string query, int? activeIndex)
        {
            SuggestionResult? activeSuggestion = null;
            if (activeIndex.HasValue && activeIndex.Value >= 0 && activeIndex.Value < currentSuggestions.Count)
                activeSuggestion = currentSuggestions[activeIndex.Value];

            SubmitRequested?.Invoke(query, activeSuggestion);
        }

        private void OnAddressSearchResultActivated(int index)
        {
            if (index < 0 || index >= currentSuggestions.Count) return;
            SuggestionSelected?.Invoke(currentSuggestions[index]);
        }

        private void RegisterCoordinateCallbacks()
        {
            CoordinateXField.InputField.RegisterCallback<NavigationSubmitEvent>(OnCoordinateXSubmit, TrickleDown.TrickleDown);
            CoordinateYField.InputField.RegisterCallback<NavigationSubmitEvent>(OnCoordinateYSubmit, TrickleDown.TrickleDown);
            CoordinateXField.InputField.RegisterCallback<FocusOutEvent>(OnCoordinateXFocusOut);
            CoordinateYField.InputField.RegisterCallback<FocusOutEvent>(OnCoordinateYFocusOut);
        }

        private void UnregisterCoordinateCallbacks()
        {
            CoordinateXField.InputField.UnregisterCallback<NavigationSubmitEvent>(OnCoordinateXSubmit, TrickleDown.TrickleDown);
            CoordinateYField.InputField.UnregisterCallback<NavigationSubmitEvent>(OnCoordinateYSubmit, TrickleDown.TrickleDown);
            CoordinateXField.InputField.UnregisterCallback<FocusOutEvent>(OnCoordinateXFocusOut);
            CoordinateYField.InputField.UnregisterCallback<FocusOutEvent>(OnCoordinateYFocusOut);
        }

        private void OnCoordinateXSubmit(NavigationSubmitEvent _)
        {
            CoordinateXSubmitted?.Invoke(CoordinateXField.InputField.value);
        }

        private void OnCoordinateYSubmit(NavigationSubmitEvent _)
        {
            CoordinateYSubmitted?.Invoke(CoordinateYField.InputField.value);
        }

        private void OnCoordinateXFocusOut(FocusOutEvent _)
        {
            CoordinateXSubmitted?.Invoke(CoordinateXField.InputField.value);
        }

        private void OnCoordinateYFocusOut(FocusOutEvent _)
        {
            CoordinateYSubmitted?.Invoke(CoordinateYField.InputField.value);
        }
    }
}