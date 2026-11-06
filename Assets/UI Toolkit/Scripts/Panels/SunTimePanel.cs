using Netherlands3D.Snapshots;
using Netherlands3D.ProjectTime;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using static Netherlands3D.Snapshots.PeriodicSnapshots;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class SunTimePanel : BaseInspectorContentPanel
    {
        public override string Title => "Zonnestand";
        
        private DateField dateField;
        public DateField DateField => dateField ??= this.Q<DateField>("DateField");

        private SunDial sunDial;
        public SunDial SunDial => sunDial ??= this.Q<SunDial>("SunDial");

        private NumberField timeField;
        public NumberField TimeField => timeField ??= this.Q<NumberField>("TimeField");

        private Button nowButton;
        public Button NowButton => nowButton ??= this.Q<Button>("NowButton");

        private SimulationSpeedControls simulationSpeedControls;
        public SimulationSpeedControls SimulationSpeedControls => simulationSpeedControls ??= this.Q<SimulationSpeedControls>("SimulationSpeedControls");

        private ScreenshotContainer images;
        private VisualElement imagesContainer;
        private VisualElement imagesRow1, imagesRow2, imagesRow3;
        private Label textRow1, textRow2, textRow3;
        private const int maxRowCount = 5;

        public Button DownloadButton => downloadButton;
        private Button downloadButton;
        private List<Moment> moments;
        
        public UnityEvent TimeChanged;

        public SunTimePanel()
        {
        }

        //todo it would be nicer to have the scriptableobject support moments and images combined so we dont have to get them from periodicsnapshots
        public SunTimePanel(ScriptableObject imageContainer) : this()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
           
            TimeField.InputField.RegisterCallback<BlurEvent>(_ => TimeChanged.Invoke());
            TimeField.InputField.RegisterCallback<NavigationSubmitEvent>(_ =>TimeChanged.Invoke());

            NowButton.RegisterCallback<ClickEvent>(OnNowButtonClicked);

            

            downloadButton = this.Q<Button>("DownloadButton");

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

        public void SetMoments(List<Moment> moments)
        {
            this.moments = moments;
        }

        public string GetMomentsText(int startIndex, int count)
        {
            StringBuilder builder = new StringBuilder();
            moments.Sort((a, b) => a.ToDateTime().CompareTo(b.ToDateTime()));
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
            SimulationSpeedControls.TogglePlay();
        }

        public void OnTimeOfDayChanged(DateTime dt)
        {
            SunDial.SetTimeWithoutNotify(dt.Hour, dt.Minute);
            DateField.SetValueWithoutNotify(dt.Day, dt.Month, dt.Year);
            TimeField.SetValueWithoutNotify(dt);
        }
    }
}