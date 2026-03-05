using AfflictionComponent.Components;
using AfflictionComponent.Interfaces;
using AfflictionComponent.Enums;

namespace TheLongMood.Afflictions.Depression
{
    internal class Weepy
    {
        public class WeepyAffliction : CustomAffliction, IDuration, IRemedies, IInstance
        {
            public InstanceType Type { get; set; } = InstanceType.Single;
            public void OnFoundExistingInstance(CustomAffliction existingAffliction)
            {
                if (existingAffliction is WeepyAffliction weepy)
                {
                    weepy.ResetAffliction(resetRemedies: false);
                    var now = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                    weepy.EndTime = now + weepy.Duration;
                }
            }

            public static bool IsActive { get; private set; } = false;
            public float Duration { get; set; }
            public float EndTime { get; set; }

            public Tuple<string, int, int>[] RemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();
            public Tuple<string, int, int>[] AltRemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();

            public bool InstantHeal { get; set; } = true;

            public WeepyAffliction(AfflictionBodyArea bodyArea) : base("GAMEPLAY_WeepyName", "GAMEPLAY_DepressionCause", "GAMEPLAY_WeepyDescription", null, "TheLongMood.Resources.Icons.Weepy.png", bodyArea, true)
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
        }
    }
}