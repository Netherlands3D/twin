using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class Timeline_Regulator : MonoBehaviour
    {
        public GameObject YearVisualPrefab; // Prefab for the year instance visual in the timeline  
        public RectTransform content; // Content RectTransform of the ScrollView  
        public TMP_InputField SelectedYearInputField; // Input field for the selected year  
        public TMP_InputField SelectedYearText; // Text object to display the selected year  
        public Slider speedSlider; // Slider to control year modification  
        private int minYear = 1625;
        private int maxYear = 2024;
        public TMP_InputField startYearInput; // Reference to the start year input field  
        public TMP_InputField endYearInput; // Reference to the end year input field  

        private List<RectTransform> yearInstances = new List<RectTransform>();
        public int currentYear;
        public UnityEvent<int> YearChanged;

        private float lerpSpeed = 10f; // Speed of the lerp transition  
        private Vector2 targetPosition; // Target position for the content  
        private Coroutine yearUpdateCoroutine; // Reference to the coroutine

        // Define original, highlighted and neighbor height values  
        private float defaultHeight = 3; // Original height of YearVisualPrefab  
        private float highlightedHeight = 12f; // Height for the current year  
        private float neighborHeight = 8; // Height for adjacent years

        private Color highlightedColor = new Color(0f, 0.274f, 0.6f); // HEX #004699  
        private Color defaultColor = new Color(0.8f, 0.847f, 0.894f); // HEX #CCD7E4

        // List of years with corresponding events  
        public List<int> eventYears = new List<int>() { 1625, 1700, 1800, 1878, 1900, 1930, 1949, 1999, 1989, 1949, 2000, 2024 }; // Example years

        void Start()
        {
            // Set the initial years to the default values  
            minYear = int.Parse(startYearInput.text);
            maxYear = int.Parse(endYearInput.text);

            InitializeTimeline();

            // Call ScrollToYear after a brief delay to ensure UI is fully initialized  
            StartCoroutine(ScrollToMaxYearAfterDelay());

            // Add listeners to the input fields to detect changes  
            startYearInput.onEndEdit.AddListener(delegate { UpdateTimeline(); });
            endYearInput.onEndEdit.AddListener(delegate { UpdateTimeline(); });

            // Set initial values in input fields  
            startYearInput.text = minYear.ToString();
            endYearInput.text = maxYear.ToString();
        }

        // Coroutine to wait before scrolling to max year  
        private IEnumerator ScrollToMaxYearAfterDelay()
        {
            yield return new WaitForEndOfFrame(); // Wait until the end of the frame. Delay is needed for the timeline to be fully rebuilt.  
            ScrollToYear(maxYear); // Now scroll to the maximum year  
        }

        private IEnumerator RebuildToCurrentAfterDelay()
        {
            yield return new WaitForEndOfFrame(); // Wait until the end of the frame. Delay is needed for the timeline to be fully rebuilt.
            ScrollToYear(currentYear); // Now scroll to the maximum year  
        }

        void InitializeTimeline()
        {
            // Clear previous year instances  
            foreach (var instance in yearInstances)
            {
                Destroy(instance.gameObject);
            }
            yearInstances.Clear();

            // Read the years from the input fields  
            minYear = int.Parse(startYearInput.text);
            maxYear = int.Parse(endYearInput.text);

            for (int year = minYear; year <= maxYear; year++)
            {
                GameObject yearInstance = Instantiate(YearVisualPrefab, content);
                yearInstance.GetComponentInChildren<TMP_Text>().text = year.ToString();
                yearInstances.Add(yearInstance.GetComponent<RectTransform>());

                // Handle event years as before  
                GameObject eventObject = yearInstance.transform.GetChild(1).gameObject; // Assuming event is the second child  
                if (eventYears.Contains(year))
                {
                    eventObject.SetActive(true);
                    Button eventButton = eventObject.GetComponent<Button>();
                    if (eventButton != null)
                    {
                        int yearToScroll = year; // Store the year in a local variable  
                        eventButton.onClick.AddListener(() =>
                        {
                            ScrollToYear(yearToScroll);
                        });
                    }
                }
                else
                {
                    eventObject.SetActive(false);
                }
            }

            ScrollToYear(currentYear);
        }

        void UpdateTimeline()
        {
            // Validate input and initialize timeline with new years  
            if (int.TryParse(startYearInput.text, out int newMinYear) && int.TryParse(endYearInput.text, out int newMaxYear))
            {
                minYear = newMinYear;
                maxYear = newMaxYear;
                InitializeTimeline();
                StartCoroutine(RebuildToCurrentAfterDelay());
            }
        }

        void Update()
        {
            // Smoothly move to the target position  
            content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, targetPosition, Time.deltaTime * lerpSpeed);

            // Handle year input submission  
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                int inputYear;
                if (int.TryParse(SelectedYearInputField.text, out inputYear))
                {
                    ScrollToYear(inputYear);
                }
            }

            // Start or stop coroutine based on slider value  
            if (speedSlider.value != 0f)
            {
                if (yearUpdateCoroutine == null) // If not already running  
                {
                    yearUpdateCoroutine = StartCoroutine(UpdateYearBasedOnSlider());
                }
            }
            else
            {
                if (yearUpdateCoroutine != null) // Stop the coroutine if the slider is at 0  
                {
                    StopCoroutine(yearUpdateCoroutine);
                    yearUpdateCoroutine = null; // Reset the coroutine reference  
                }
            }
        }

        private IEnumerator UpdateYearBasedOnSlider()
        {
            while (true) // Loop indefinitely 
            {
                yield return new WaitForSeconds(0.1f); // Wait for 0.1 second < this is a speed interval (a brake) to make the timeline slide slower or faster. he higher the number, the slower the timeline scrolls.

                float sliderValue = speedSlider.value;
                if (sliderValue != 0f)
                {
                    int yearChange = Mathf.RoundToInt(sliderValue);
                    int newYear = currentYear + yearChange;

                    ScrollToYear(newYear);
                }
            }
        }

        void ScrollToYear(int year)
        {
            year = Mathf.Clamp(year, minYear, maxYear);

            int index = year - minYear;
            if (index >= 0 && index < yearInstances.Count)
            {
                // Set the target position to smoothly lerp to  
                targetPosition = new Vector2((-yearInstances[index].anchoredPosition.x + 362), content.anchoredPosition.y);
                currentYear = year; // Update the current year  
                SelectedYearText.text = currentYear.ToString(); // Update the UI text  
                YearChanged.Invoke(year); //Fires the UnityEvent that changes the year

                // Adjust height, color, and text visibility  
                for (int i = 0; i < yearInstances.Count; i++)
                {
                    RectTransform yearRectTransform = yearInstances[i];
                    Transform yearText = yearRectTransform.GetChild(0);
                    Image yearImage = yearRectTransform.GetComponent<Image>(); // Access the Image component  
                    GameObject eventObject = yearRectTransform.GetChild(1).gameObject; // Access the event GameObject

                    if (i == index) // Current year  
                    {
                        yearRectTransform.sizeDelta = new Vector2(yearRectTransform.sizeDelta.x, highlightedHeight);
                        yearText.gameObject.SetActive(false); // Deactivate text  
                        yearImage.color = highlightedColor; // Change color to #004699  
                    }
                    else if (i == index - 1 || i == index + 1) // Previous or next year  
                    {
                        yearRectTransform.sizeDelta = new Vector2(yearRectTransform.sizeDelta.x, neighborHeight);
                        yearText.gameObject.SetActive(true); // Ensure text is active  
                        yearImage.color = defaultColor; // Set to default color (#CCD7E4)
                    }
                    else // Other years  
                    {
                        yearRectTransform.sizeDelta = new Vector2(yearRectTransform.sizeDelta.x, defaultHeight);
                        yearText.gameObject.SetActive(true); // Ensure text is active  
                        yearImage.color = defaultColor; // Set to default color (#CCD7E4)
                    }

                    // Reactivate the event GameObject if the year is in the list  
                    if (eventYears.Contains(i + minYear))
                    {
                        eventObject.SetActive(true);
                    }
                    else
                    {
                        eventObject.SetActive(false);
                    }
                }
            }
        }
    }
}