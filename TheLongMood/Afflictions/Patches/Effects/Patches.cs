using static TheLongMood.Afflictions.Depression.Miserable;
using static TheLongMood.Afflictions.Depression.Hopeless;
using static TheLongMood.Afflictions.Depression.Weepy;
using static TheLongMood.Afflictions.Depression.Sad;
using AfflictionComponent.Components;

namespace TheLongMood.Afflictions.Patches.Effects
{
    internal class Patches
    {
        [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.UpdateDurationLabel))]
        [HarmonyPriority(99999)]
        public class Panel_BreakDown_UpdateDurationLabel
        {
            private static void Postfix(Panel_BreakDown __instance)
            {
                if (__instance == null) return;

                float mult = 1f;

                if (HopelessAffliction.IsActive) mult = 1.265f;
                else if (MiserableAffliction.IsActive) mult = 1.196f;
                else if (WeepyAffliction.IsActive) mult = 1.137f;
                else if (SadAffliction.IsActive) mult = 1.074f;

                __instance.m_DurationHours *= mult;

                if (__instance.m_BreakdownInfo != null && __instance.m_BreakdownInfo.m_DurationLabel != null)
                {
                    __instance.m_BreakdownInfo.m_DurationLabel.text = Utils.GetExpandedDurationString(Mathf.RoundToInt(__instance.m_DurationHours * 60f));
                }
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), nameof(Panel_BodyHarvest.GetHarvestDurationMinutes))]
        [HarmonyPriority(99999)]
        public class Panel_BodyHarvest_GetHarvestDurationMinutes
        {
            private static void Postfix(ref float __result)
            {
                if (HopelessAffliction.IsActive) __result *= 1.265f;
                else if (MiserableAffliction.IsActive) __result *= 1.196f;
                else if (WeepyAffliction.IsActive) __result *= 1.137f;
                else if (SadAffliction.IsActive) __result *= 1.074f;
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), nameof(Panel_BodyHarvest.GetQuarterDurationMinutes))]
        [HarmonyPriority(99999)]
        public class Panel_BodyHarvest_GetQuarterDurationMinutes
        {
            private static void Postfix(Panel_BodyHarvest __instance, ref float __result)
            {
                if (HopelessAffliction.IsActive) __result *= 1.265f;
                else if (MiserableAffliction.IsActive) __result *= 1.196f;
                else if (WeepyAffliction.IsActive) __result *= 1.137f;
                else if (SadAffliction.IsActive) __result *= 1.074f;

                if (__instance.m_Label_EstimatedTime != null)
                {
                    __instance.m_Label_EstimatedTime.text = Utils.GetExpandedDurationString(Mathf.RoundToInt(__result));
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Inventory_Examine), nameof(Panel_Inventory_Examine.GetModifiedActionDuration))]
        [HarmonyPriority(99999)]
        internal static class ActionDurationPatch
        {
            public static void Postfix(ref int __result)
            {
                if (HopelessAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.265f);
                else if (MiserableAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.196f);
                else if (WeepyAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.137f);
                else if (SadAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.074f);
            }
        }

            [HarmonyPatch(typeof(Panel_Repair), nameof(Panel_Repair.GetModifiedRepairDuration))]
        [HarmonyPriority(99999)]
        internal static class RepairDurationPatch
        {
            public static void Postfix(ref int __result)
            {
                if (HopelessAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.265f);
                else if (MiserableAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.196f);
                else if (WeepyAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.137f);
                else if (SadAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.074f);
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.GetModifiedCraftingDuration))]
        [HarmonyPriority(99999)]
        private static class CraftingDurationPatch
        {
            private static void Postfix(ref int __result)
            {
                if (HopelessAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.265f);
                else if (MiserableAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.196f);
                else if (WeepyAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.137f);
                else if (SadAffliction.IsActive) __result = Mathf.RoundToInt(__result * 1.074f);
            }
        }

        [HarmonyPatch(typeof(vp_FPSController), nameof(vp_FPSController.GetSlopeMultiplier))]
        [HarmonyPriority(99999)]
        internal static class MovementSpeedPatch
        {
            private static void Postfix(ref float __result)
            {
                var afflictionManager = AfflictionManager.GetAfflictionManagerInstance();
                if (afflictionManager == null || afflictionManager.m_Afflictions == null) return;

                var pm = GameManager.GetPlayerManagerComponent();
                if (pm == null) return;

                if (pm.PlayerIsClimbing() || pm.PlayerIsSprinting() || pm.PlayerIsWalking())
                {
                    if (HopelessAffliction.IsActive) __result *= 0.735f;
                    else if (MiserableAffliction.IsActive) __result *= 0.804f;
                    else if (WeepyAffliction.IsActive) __result *= 0.863f;
                    else if (SadAffliction.IsActive) __result *= 0.926f;
                }
            }
        }

        [HarmonyPatch(typeof(GunItem), nameof(GunItem.Update))]
        [HarmonyPriority(99999)]
        internal static class GunAimStaminaPatch
        {
            private static void Postfix(GunItem __instance)
            {
                if (__instance == null) return;

                const float BASE_INCREASE = 0.1f;
                const float BASE_DECREASE = 0.15f;

                if (!SadAffliction.IsActive && !WeepyAffliction.IsActive && !MiserableAffliction.IsActive && !HopelessAffliction.IsActive)
                {
                    __instance.m_SwayIncreasePerSecond = BASE_INCREASE;
                    __instance.m_SwayDecreasePerSecond = BASE_DECREASE;
                    return;
                }

                float increaseMult = 1f;
                float decreaseMult = 1f;

                if (HopelessAffliction.IsActive)
                {
                    increaseMult *= 1.265f;
                    decreaseMult *= 0.735f;
                }
                else if (MiserableAffliction.IsActive)
                {
                    increaseMult *= 1.196f;
                    decreaseMult *= 0.804f;
                }
                else if (WeepyAffliction.IsActive)
                {
                    increaseMult *= 1.137f;
                    decreaseMult *= 0.863f;
                }
                else if (SadAffliction.IsActive)
                {
                    increaseMult *= 1.074f;
                    decreaseMult *= 0.926f;
                }

                __instance.m_SwayIncreasePerSecond = BASE_INCREASE * increaseMult;
                __instance.m_SwayDecreasePerSecond = BASE_DECREASE * decreaseMult;
            }
        }
    }
}