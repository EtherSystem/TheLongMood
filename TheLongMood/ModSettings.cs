namespace TheLongMood
{
    internal class ModSettings : JsonModSettings
    {
        [Section("General Settings")]

        [Name("Rate")]
        [Description("Default: 10 - Overall speed of Boredom & Depression changes (per in-game hour)")]
        [Slider(5f, 15f, 3)]
        public float DropRate = 10f;

        [Name("Recovery")]
        [Description("Default: 10 - How fast Boredom decreases while you're active (per in-game hour)")]
        [Slider(5f, 15f, 3)]
        public float RegenRate = 10f;

        [Name("Boredom Delay")]
        [Description("Default: 10 - Time in minutes before Boredom starts increasing while inactive.")]
        [Slider(1f, 60f, 60, NumberFormat = "{0} MIN")]
        public int TimeForBoredomIncrease = 10;

        [Section("Advanced")]

        [Name("Show advanced options")]
        [Description("Reveals developer / debug settings.")]
        public bool ShowAdvanced = false;

        [Name("Debug overlay")]
        [Description("Shows live Boredom/Depression values on the HUD.")]
        public bool Debug = false;

        [Name("ML Logging")]
        [Description("Add logs for ModData/inactivity in the ML console.")]
        public bool IsLogging = false;

        protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
        {
            base.OnChange(field, oldValue, newValue);

            if (field.Name == nameof(ShowAdvanced))
            {
                SetFieldVisible(nameof(Debug), ShowAdvanced);
                SetFieldVisible(nameof(IsLogging), ShowAdvanced);

                if (!ShowAdvanced)
                {
                    Debug = false;
                    IsLogging = false;
                }
            }
        }

        protected override void OnConfirm()
        {
            if (!ShowAdvanced)
            {
                Debug = false;
                IsLogging = false;
            }

            base.OnConfirm();
        }
    }

    internal static class Settings
    {
        public static ModSettings options;

        public static void OnLoad()
        {
            options = new ModSettings();
            options.AddToModSettings("TheLongMood");

            options.SetFieldVisible(nameof(ModSettings.Debug), options.ShowAdvanced);
            options.SetFieldVisible(nameof(ModSettings.IsLogging), options.ShowAdvanced);

            if (!options.ShowAdvanced)
            {
                options.Debug = false;
                options.IsLogging = false;
            }
        }
    }
}