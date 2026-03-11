using AfflictionComponent.Components;
using AfflictionComponent.Interfaces;
using AfflictionComponent.Enums;
using TheLongMood.Resources.Localization;

namespace TheLongMood.Afflictions.Boredom
{
    internal class Bored
    {
        public class BoredAffliction : CustomAffliction, IDuration, IRemedies, IInstance, ILocalizableAffliction
        {
            private const string NAME_KEY = "GAMEPLAY_BoredName";
            private const string CAUSE_KEY = "GAMEPLAY_BoredomCause";
            private const string DESC_KEY = "GAMEPLAY_BoredDescription";

            public InstanceType Type { get; set; } = InstanceType.Single;
            public void OnFoundExistingInstance(CustomAffliction existingAffliction)
            {
                if (existingAffliction is BoredAffliction bored)
                {
                    bored.ResetAffliction(resetRemedies: false);
                    var now = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                    bored.EndTime = now + bored.Duration;
                }
            }

            public static bool IsActive { get; private set; } = false;
            public float Duration { get; set; }
            public float EndTime { get; set; }

            public Tuple<string, int, int>[] RemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();
            public Tuple<string, int, int>[] AltRemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();

            public bool InstantHeal { get; set; } = true;

            public BoredAffliction(AfflictionBodyArea bodyArea) : base(NAME_KEY, CAUSE_KEY, DESC_KEY, null, "TheLongMood.Resources.Icons.Bored.png", bodyArea, true)
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
                    Core.Instance.LoggerInstance.Msg($"Bored refresh -> '{oldName}' => '{m_Name}'");
                }
            }
        }
    }
}