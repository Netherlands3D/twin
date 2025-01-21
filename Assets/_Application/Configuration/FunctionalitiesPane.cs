using System.Collections.Generic;
using Netherlands3D.Twin.Configuration.UI;
using Netherlands3D.Twin.Functionalities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Configuration
{
    public class FunctionalitiesPane : MonoBehaviour
    {
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject watermark;

        [Tooltip("The text that is shown when the selected functionality description is empty")]
        [SerializeField] private string placeholderHeaderText;

        [FormerlySerializedAs("placeHolderText")]
        [TextArea(3, 10)]
        [SerializeField] private string placeHolderDescriptionText;
        [SerializeField] private ScrollRect scrollRectText;
        [SerializeField] private GameObject functionalitiesListContent;

        [Header("Events")]
        public UnityEvent<Functionality> Selected = new UnityEvent<Functionality>();
        public UnityEvent<Functionality> Toggled = new UnityEvent<Functionality>();
        public UnityEvent Deselected = new UnityEvent();

        [Header("Prefab")][SerializeField] private FunctionalitySelection functionalitySelectionPrefab;

        void Start()
        {
            headerText.text = placeholderHeaderText;
            descriptionText.text = placeHolderDescriptionText;
        }

        public void Init(List<Functionality> functionalities)
        {
            foreach (var availableFunctionality in functionalities)
            {
                FunctionalitySelection functionalitySelection = Instantiate(functionalitySelectionPrefab, functionalitiesListContent.transform);
                functionalitySelection.Init(availableFunctionality);
                functionalitySelection.OnClick.AddListener(() => OnFunctionalitySelected(availableFunctionality));
                functionalitySelection.OnToggle.AddListener(value => OnFunctionalityChanged(availableFunctionality, value));
            }
        }

        private void LateUpdate()
        {
            if(Pointer.current.press.wasReleasedThisFrame)
                CheckDeselect();
        }

        private void CheckDeselect()
        {
            GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
            if (currentSelectedGameObject)
            {
                var parent = currentSelectedGameObject.transform.parent;
                var selectedFunctionality = parent && parent.TryGetComponent(out FunctionalitySelection functionalitySelection);
                if (selectedFunctionality)
                    return;
            }

            Deselect();
        }

        private void OnFunctionalitySelected(Functionality functionality)
        {
            ShowInformation(functionality);
            Selected.Invoke(functionality);
        }

        private void OnFunctionalityChanged(Functionality functionality, bool value)
        {
            functionality.IsEnabled = value;
            Toggled.Invoke(functionality);
        }

        private void Deselect()
        {
            ShowInformation(null);
            Deselected.Invoke();
        }

        private void ShowInformation(Functionality functionality)
        {
            //Fallback to placeholder text if there is no description
            var hasDescription = functionality && !string.IsNullOrEmpty(functionality.Description);
            headerText.text = (hasDescription) ? functionality.Header : placeholderHeaderText;
            descriptionText.text = (hasDescription) ? functionality.Description : placeHolderDescriptionText;

            watermark.SetActive(!hasDescription);

            //Scale text to wrap around its content
            descriptionText.rectTransform.sizeDelta = new Vector2(descriptionText.rectTransform.sizeDelta.x, descriptionText.preferredHeight);

            //Reset entire scrollview and momentum back to top
            scrollRectText.velocity = Vector2.zero;
            scrollRectText.normalizedPosition = new Vector2(0, 1);
        }
    }
}
