using Netherlands3D.Snapshots;
using Netherlands3D.Sun;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine;
using UnityEngine.UIElements;
using static Netherlands3D.Snapshots.PeriodicSnapshots;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class SunTimePanel : BaseInspectorContentPanel
    {
        public override string Title => "Zonnestand";

        // SunTime stores speed as seconds/second internally.
        // The UI shows speed in hours/second so we apply this factor.
        private const float SecondsPerHour = 3600f;

        private SunTime sunTime;

        private DateField dateField;
        private DateField DateField => dateField ??= this.Q<DateField>("DateField");

        private SunDial sunDial;
        private SunDial SunDial => sunDial ??= this.Q<SunDial>("SunDial");

        private NumberField timeField;
        private NumberField TimeField => timeField ??= this.Q<NumberField>("TimeField");

        private Button nowButton;
        private Button NowButton => nowButton ??= this.Q<Button>("NowButton");

        private SimulationSpeedControls simulationSpeedControls;
        private SimulationSpeedControls SimulationSpeedControls => simulationSpeedControls ??= this.Q<SimulationSpeedControls>("SimulationSpeedControls");

        private ScreenshotContainer images;
        private VisualElement imagesContainer;
        private VisualElement imagesRow1, imagesRow2, imagesRow3;
        private Label textRow1, textRow2, textRow3;
        private const int maxRowCount = 5;
        
        private Button downloadButton;
        private PeriodicSnapshots periodicSnapshotsService;

        public SunTimePanel()
        {
        }

        //todo it would be nicer to have the scriptableobject support moments and images combined so we dont have to get them from periodicsnapshots
        public SunTimePanel(ScriptableObject imageContainer) : this()
        {
            sunTime = Services.ServiceLocator.GetService<SunTime>();
            periodicSnapshotsService = Services.ServiceLocator.GetService<PeriodicSnapshots>();

            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            SunDial.TimeChanged += OnSunDialTimeChanged;
            DateField.SubmitEvent += OnDateChanged;
            TimeField.InputField.RegisterCallback<BlurEvent>(_ =>OnTimeChanged());
            TimeField.InputField.RegisterCallback<NavigationSubmitEvent>(_ =>OnTimeChanged());

            NowButton.RegisterCallback<ClickEvent>(OnNowButtonClicked);

            SimulationSpeedControls.SpeedChanged += OnSimulationSpeedChanged;
            SimulationSpeedControls.PlayToggled += OnPlayToggled;
            
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                sunTime.timeOfDayChanged.AddListener(OnTimeOfDayChanged);
                sunTime.timeSpeedChanged.AddListener(OnTimeSpeedChanged);
                sunTime.isAnimatingChanged.AddListener(OnIsAnimatingChanged);
                OnTimeOfDayChanged(sunTime.Time);
                OnIsAnimatingChanged(sunTime.IsAnimating);    
            });
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                sunTime.timeOfDayChanged.RemoveListener(OnTimeOfDayChanged);
                sunTime.timeSpeedChanged.RemoveListener(OnTimeSpeedChanged);
                sunTime.isAnimatingChanged.RemoveListener(OnIsAnimatingChanged);
            });


            downloadButton = this.Q<Button>("DownloadButton");
            downloadButton.clicked += periodicSnapshotsService.DownloadSnapshots;

            if (imageContainer is not ScreenshotContainer screenshots)
            {
                Debug.LogError("missing images for schaduwstudie, please provide a screenshotcontainer scriptableobject");
                return;
            }
            else
                images = screenshots;

            imagesContainer = this.Q<VisualElement>("ImagesGrid");
            //once because we want to unregister immediately after firing or evertying will be instantiated multple times
            imagesContainer.RegisterCallbackOnce<GeometryChangedEvent>(OnGridGeometryChanged); 
        }

        private void OnGridGeometryChanged(GeometryChangedEvent evt)
        {
            float containerWidth = imagesContainer.resolvedStyle.width;
            if (containerWidth == 0) return;

            
            imagesRow1 = imagesContainer.Q<VisualElement>("ImagesRow1");
            AddImagesToRow(0, 4, containerWidth, imagesRow1);
            imagesRow2 = imagesContainer.Q<VisualElement>("ImagesRow2");
            AddImagesToRow(4, 5, containerWidth, imagesRow2);
            imagesRow3 = imagesContainer.Q<VisualElement>("ImagesRow3");
            AddImagesToRow(9, 3, containerWidth, imagesRow3);

            textRow1 = this.Q<Label>("TextRow1");
            textRow1.text = GetMomentsText(0, 4);
            textRow2 = this.Q<Label>("TextRow2");
            textRow2.text = GetMomentsText(4, 5);
            textRow3 = this.Q<Label>("TextRow3");
            textRow3.text = GetMomentsText(9, 3);

        }

        private const string dayMonthSeperator = "-";
        private const string aboutString = " om ";
        private const string timeSuffix = ":00";

        public string GetMomentsText(int startIndex, int count)
        {
            StringBuilder builder = new StringBuilder();
            List<Moment> moments = periodicSnapshotsService.Moments;
           // moments.Sort((a, b) => a.ToDateTime().CompareTo(b.ToDateTime()));
           for(int i = startIndex; i < startIndex + count; i++)
           {
                Moment moment = moments[i];               
                //example     21-03 om 12:00
                builder.Append(moment.day.ToString("D2"));
                builder.Append(dayMonthSeperator);
                builder.Append(moment.month.ToString("D2"));
                builder.Append(aboutString);
                builder.Append(moment.hour.ToString());
                builder.AppendLine(timeSuffix);
            }
            return builder.ToString();
        }

        private void AddImagesToRow(int startIndex, int count, float containerWidth, VisualElement row)
        {
            row.Clear();
            const float margin = 3f; //todo solve this margin to be a constant from uss?
            float cellWidth = (containerWidth - margin * 2 * (maxRowCount + 1)) / maxRowCount;

            for(int i = startIndex; i < startIndex + count; i++)
            {
                var tex = images.screenshots[i];
                if (tex == null) continue;
               
                var cell = new VisualElement();
                cell.AddToClassList("sun-shadow-panel__image-cell");
                cell.style.backgroundImage = new StyleBackground(tex);
                cell.style.width = cellWidth;
                cell.style.height = cellWidth * ((float)tex.texture.height / tex.texture.width);
                row.Add(cell);
            }
        }

        void OnNowButtonClicked(ClickEvent _)
        {
            sunTime?.ResetToNow();
            SimulationSpeedControls.Pause();
        }

        private void OnTimeOfDayChanged(DateTime dt)
        {
            SunDial.SetTimeWithoutNotify(dt.Hour, dt.Minute);
            DateField.SetValueWithoutNotify(dt.Day, dt.Month, dt.Year);
            TimeField.SetValueWithoutNotify(dt);
        }

        private void OnSunDialTimeChanged(int hour, int minute)
        {
            sunTime?.SetTime(hour, minute, 0);
        }

        private void OnIsAnimatingChanged(bool animating)
        {
            if (animating) SimulationSpeedControls.Play();
            else SimulationSpeedControls.Pause();
        }

        private void OnTimeSpeedChanged(float speedSecondsPerSecond)
        {
            SimulationSpeedControls.SetSpeedWithoutNotify(speedSecondsPerSecond / SecondsPerHour);
        }

        private void OnSimulationSpeedChanged(float hoursPerSecond)
        {
            sunTime?.SetTimeSpeed(hoursPerSecond * SecondsPerHour);
        }

        private void OnPlayToggled(bool isPlaying)
        {
            sunTime?.ToggleAnimation(isPlaying);
        }

        private void OnTimeChanged()
        {
            var dt = timeField.GetValueAsTime(sunTime.Time);
            sunTime?.SetTime(dt.Hour, dt.Minute, 0);
        }

        private void OnDateChanged(int day, int month, int year)
        {
            sunTime?.SetDay(day);
            sunTime?.SetMonth(month);
            sunTime?.SetYear(year);
        }
    }
}