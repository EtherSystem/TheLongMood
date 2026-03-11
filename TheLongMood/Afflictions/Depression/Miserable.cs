using AfflictionComponent.Components;
using AfflictionComponent.Enums;
using AfflictionComponent.Interfaces;
using TheLongMood.Resources.Localization;

namespace TheLongMood.Afflictions.Depression
{
    internal class Miserable
    {
        public class MiserableAffliction : CustomAffliction, IDuration, IRemedies, IInstance, ILocalizableAffliction
        {
            private const string NAME_KEY = "GAMEPLAY_MiserableName";
            private const string CAUSE_KEY = "GAMEPLAY_DepressionCause";
            private const string DESC_KEY = "GAMEPLAY_MiserableDescription";

            public InstanceType Type { get; set; } = InstanceType.Single;
            public void OnFoundExistingInstance(CustomAffliction existingAffliction)
            {
                if (existingAffliction is MiserableAffliction miserable)
                {
                    miserable.ResetAffliction(resetRemedies: false);
                    var now = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                    miserable.EndTime = now + miserable.Duration;
                }
            }

            public static bool IsActive { get; private set; } = false;
            public float Duration { get; set; }
            public float EndTime { get; set; }

            public Tuple<string, int, int>[] RemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();
            public Tuple<string, int, int>[] AltRemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();

            public bool InstantHeal { get; set; } = true;

            public MiserableAffliction(AfflictionBodyArea bodyArea) : base(NAME_KEY, CAUSE_KEY, DESC_KEY, null, "TheLongMood.Resources.Icons.Miserable.png", bodyArea, true)
            {
            }

            public void CureSymptoms()
            {
                //cure symptoms but not the affliction
            }

            public void OnCure()
            {
                IsActive = false;
            }

            public override void OnUpdate()
            {
                IsActive = true;
            }

            public void RefreshLocalization()
            {
                string oldName = m_Name;

                m_Name = Localization.Get(NAME_KEY);
                m_CauseText = Localization.Get(CAUSE_KEY);
                m_Description = Localization.Get(DESC_KEY);
                m_DescriptionNoHeal = null;

                if (Settings.options.IsLogging && Core.Instance != null)
                {
                    Core.Instance.LoggerInstance.Msg($"Miserable refresh -> '{oldName}' => '{m_Name}'");
                }
            }
        }
    }
}