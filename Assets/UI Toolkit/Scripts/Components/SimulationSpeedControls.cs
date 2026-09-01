using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class SimulationSpeedControls : VisualElement
    {
        [UxmlAttribute("speed-steps")]
        public string SpeedStepsAttribute
        {
            get => string.Join(",", speedSteps);
            set => ParseSpeedSteps(value ?? string.Empty);
        }

        private float[] speedSteps = { 0.5f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f, 16f, 17f, 18f, 19f, 20f, 21f, 22f, 23f, 24f, 25f, 26f, 27f, 28f, 29f, 30f };

        public event Action<float> SpeedChanged;
        public event Action<bool> PlayToggled;

        private NumberField speedField;
        private NumberField SpeedField => speedField ??= this.Q<NumberField>("SpeedField");

        private Button slowDownButton;
        private Button SlowDownButton => slowDownButton ??= this.Q<Button>("SlowDownButton");

        private Button pauseButton;
        private Button PauseButton => pauseButton ??= this.Q<Button>("PauseButton");

        private Button playButton;
        private Button PlayButton => playButton ??= this.Q<Button>("PlayButton");

        private Button speedUpButton;
        private Button SpeedUpButton => speedUpButton ??= this.Q<Button>("SpeedUpButton");

        public SimulationSpeedControls()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            SpeedField.InputField.RegisterValueChangedCallback(OnSpeedFieldValueChanged);
            SpeedField.RegisterCallback<NavigationSubmitEvent>(OnSpeedFieldSubmit);
            PlayButton.RegisterCallback<ClickEvent>(OnPlayButtonClicked);
            PauseButton.RegisterCallback<ClickEvent>(OnPauseButtonClicked);
            SpeedUpButton.RegisterCallback<ClickEvent>(OnSpeedUpButtonClicked);
            SlowDownButton.RegisterCallback<ClickEvent>(OnSlowDownButtonClicked);
            PlayToggled += OnPlayToggled;
        }

        private void OnPlayButtonClicked(ClickEvent _) => Play();
        private void OnPauseButtonClicked(ClickEvent _) => Pause();
        private void OnSpeedUpButtonClicked(ClickEvent _) => StepUp();
        private void OnSlowDownButtonClicked(ClickEvent _) => StepDown();
        private void OnSpeedFieldChanged() => SpeedChanged?.Invoke(GetCurrentSpeed());
        private void OnPlayToggled(bool obj) => SetIsPlayingButtonStates(obj);
        
        // Needs both because Scrolling of the value doesn't trigger a ValueChanged event
        private void OnSpeedFieldSubmit(NavigationSubmitEvent _) => OnSpeedFieldChanged();
        private void OnSpeedFieldValueChanged(ChangeEvent<string> _) => OnSpeedFieldChanged();

        public float GetCurrentSpeed() => (float)SpeedField.GetValueAsDouble();
        public void SetSpeedWithoutNotify(float speed) => SpeedField.SetValueWithoutNotify(speed);

        public void Play() => PlayToggled?.Invoke(true);
        public void Pause() => PlayToggled?.Invoke(false);

        public void StepUp()
        {
            var current = GetCurrentSpeed();
            var currentIndex = FindNearestStepIndex(current);
            var nextIndex = Math.Clamp(currentIndex + 1, 0, speedSteps.Length - 1);
            var nextSpeed = speedSteps[nextIndex];

            SetSpeedWithoutNotify(nextSpeed);
            SpeedChanged?.Invoke(nextSpeed);
        }

        public void StepDown()
        {
            var current = GetCurrentSpeed();
            var currentIndex = FindNearestStepIndex(current);
            var prevIndex = Math.Clamp(currentIndex - 1, 0, speedSteps.Length - 1);
            var prevSpeed = speedSteps[prevIndex];

            SetSpeedWithoutNotify(prevSpeed);
            SpeedChanged?.Invoke(prevSpeed);
        }

        private void SetIsPlayingButtonStates(bool isPlaying)
        {
            PauseButton.EnableInClassList("simulation-speed-controls__control-button--active", !isPlaying);
            PlayButton.EnableInClassList("simulation-speed-controls__control-button--active", isPlaying);
        }

        private int FindNearestStepIndex(float value)
        {
            var bestIndex = 0;
            var bestDistance = Math.Abs(speedSteps[0] - value);

            for (var i = 1; i < speedSteps.Length; i++)
            {
                var distance = Math.Abs(speedSteps[i] - value);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                bestIndex = i;
            }

            return bestIndex;
        }

        private void ParseSpeedSteps(string commaSeparatedString)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedString)) return;

            var parts = commaSeparatedString.Split(',');
            var steps = new float[parts.Length];

            for (var i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i].Trim(), out var step)) continue;

                steps[i] = step;
            }

            speedSteps = steps;
        }
    }
}

