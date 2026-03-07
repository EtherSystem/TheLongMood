namespace TheLongMood.Afflictions.Patches
{
    internal class FishingPatch
    {
        private static float _lastFishingUpdateTime = -999f;
        private const float FISHING_TIMEOUT_SECONDS = 1f;

        private static bool? _lastLoggedIsFishing = null;

        internal static bool IsFishing
        {
            get
            {
                bool value = Time.unscaledTime - _lastFishingUpdateTime <= FISHING_TIMEOUT_SECONDS;

                if (Settings.options.IsLogging && _lastLoggedIsFishing != value)
                {
                    MelonLogger.Msg($"isFishing : {value}");
                    _lastLoggedIsFishing = value;
                }

                return value;
            }
        }

        internal static void Reset()
        {
            _lastFishingUpdateTime = -999f;
            _lastLoggedIsFishing = null;
        }

        [HarmonyPatch(typeof(IceFishingHole), nameof(IceFishingHole.UpdateFishing))]
        internal static class IceFishingAction_Patch
        {
            private static void Postfix()
            {
                _lastFishingUpdateTime = Time.unscaledTime;
            }
        }
    }
}