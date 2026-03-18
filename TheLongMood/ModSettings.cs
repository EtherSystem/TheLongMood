namespace TheLongMood
{
    internal class ModSettings : JsonModSettings
    {
        [Section("General Settings")]

        [Name("Rate")]
        [Description("Default: 10 - Overall speed of Boredom & Depression changes.")]
        [Slider(5f, 15f, 3, NumberFormat = "{0} / hour")]
        public int DropRate = 10;

        [Name("Recovery")]
        [Description("Default: 10 - How fast Boredom decreases while you're active.")]
        [Slider(5f, 15f, 3, NumberFormat = "{0} / hour")]
        public int RegenRate = 10;

        [Name("Boredom Delay")]
        [Description("Default: 10 - Time before Boredom starts increasing while inactive.")]
        [Slider(1, 60, 60, NumberFormat = "{0}min")]
        public int TimeForBoredomIncrease = 10;

        [Section("Fishing Settings")]

        [Name("Breaking ice")]
        [Description("Do you want breaking ice generate boredom ?")]
        public bool IsBreakingIceGenBoredom = false;

        [Name("Fishing")]
        [Description("Do you want fishing generate boredom ?")]
        public bool IsFishingGenBoredom = false;

        [Section("Struggle Settings")]

        [Name("Attack aftershock")]
        [Description("If disabled, all struggle-related features are disabled - enable by default.")]
        public bool IsAttackAftershock = true;

        [Name("Wolves")]
        [Description("Depression penalty - default : 10")]
        [Slider(1, 100, 100)]
        public int WolfPenalty = 10;

        [Name("Moose")]
        [Description("Depression penalty - default : 30")]
        [Slider(1, 100, 100)]
        public int MoosePenalty = 30;

        [Name("Bears")]
        [Description("Depression penalty - default : 60")]
        [Slider(1, 100, 100)]
        public int BearPenalty = 60;

        [Name("Cougars")]
        [Description("Depression penalty - default : 40")]
        [Slider(1, 100, 100)]
        public int CougarPenalty = 40;

        [Name("Instant penalty")]
        [Description("If enabled, the depression penalty is applied instantly. If disabled, it is applied over time.")]
        public bool InstantStrugglePenalty = true;

        [Name("Time needed to apply the penalty.")]
        [Description("Default : 6")]
        [Slider(1, 12, 12, NumberFormat = "{0}h")]
        public int HoursToApply = 6;

        [Section("Misery afflictions")]

        [Name("Diminished Form")]
        [Description("Default: Nothing - Choose whether Diminished Form affects Boredom, Depression, Both, or neither.")]
        [Choice("Boredom", "Depression", "Both", "Nothing")]
        public int DiminishedFormMode = 3;

        [Name("Sour Stomach")]
        [Description("Default: Nothing - Choose whether Sour Stomach affects Boredom, Depression, Both, or neither.")]
        [Choice("Boredom", "Depression", "Both", "Nothing")]
        public int SourStomachMode = 3;

        [Name("Frigid Bones")]
        [Description("Default: Nothing - Choose whether Frigid Bones affects Boredom, Depression, Both, or neither.")]
        [Choice("Boredom", "Depression", "Both", "Nothing")]
        public int FrigidBonesMode = 3;

        [Name("Rheumatic Joints")]
        [Description("Default: Nothing - Choose whether Rheumatic Joints affects Boredom, Depression, Both, or neither.")]
        [Choice("Boredom", "Depression", "Both", "Nothing")]
        public int RheumaticJointsMode = 3;

        [Name("Haunted Mind")]
        [Description("Default: Nothing - Choose whether Haunted Mind affects Boredom, Depression, Both, or neither.")]
        [Choice("Boredom", "Depression", "Both", "Nothing")]
        public int HauntedMindMode = 3;

        [Name("Broken Body")]
        [Description("Default: Nothing - Choose whether Broken Body affects Boredom, Depression, Both, or neither.")]
        [Choice("Boredom", "Depression", "Both", "Nothing")]
        public int BrokenBodyMode = 3;

        [Section("Advanced")]

        [Name("Show advanced options")]
        [Description("Reveals developer / debug settings.")]
        public bool ShowAdvanced = false;

        [Name("Debug overlay")]
        [Description("Shows live Boredom/Depression values on the HUD.")]
        public bool Debug = false;

        [Name("ML Logging")]
        [Description("Add logs for ModData/Boredom/Depression behavior in the ML console.")]
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
                return;
            }

            if (field.Name == nameof(IsAttackAftershock))
            {
                bool show = IsAttackAftershock;

                SetFieldVisible(nameof(WolfPenalty), show);
                SetFieldVisible(nameof(MoosePenalty), show);
                SetFieldVisible(nameof(BearPenalty), show);
                SetFieldVisible(nameof(CougarPenalty), show);
                SetFieldVisible(nameof(InstantStrugglePenalty), show);
                SetFieldVisible(nameof(HoursToApply), show && !InstantStrugglePenalty);
                return;
            }

            if (field.Name == nameof(InstantStrugglePenalty))
            {
                SetFieldVisible(nameof(HoursToApply), IsAttackAftershock && !InstantStrugglePenalty);
                return;
            }
        }

        protected override void OnConfirm()
        {
            if (!ShowAdvanced)
            {
                Debug = false;
                IsLogging = false;
            }

            SetFieldVisible(nameof(Debug), ShowAdvanced);
            SetFieldVisible(nameof(IsLogging), ShowAdvanced);

            bool show = IsAttackAftershock;
            SetFieldVisible(nameof(WolfPenalty), show);
            SetFieldVisible(nameof(MoosePenalty), show);
            SetFieldVisible(nameof(BearPenalty), show);
            SetFieldVisible(nameof(CougarPenalty), show);
            SetFieldVisible(nameof(InstantStrugglePenalty), show);
            SetFieldVisible(nameof(HoursToApply), show && !InstantStrugglePenalty);

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

            bool show = options.IsAttackAftershock;
            options.SetFieldVisible(nameof(ModSettings.WolfPenalty), show);
            options.SetFieldVisible(nameof(ModSettings.MoosePenalty), show);
            options.SetFieldVisible(nameof(ModSettings.BearPenalty), show);
            options.SetFieldVisible(nameof(ModSettings.CougarPenalty), show);
            options.SetFieldVisible(nameof(ModSettings.InstantStrugglePenalty), show);
            options.SetFieldVisible(nameof(ModSettings.HoursToApply), show && !options.InstantStrugglePenalty);
        }
    }
}