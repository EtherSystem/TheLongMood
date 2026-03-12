using AfflictionComponent.Components;

namespace TheLongMood.Resources.Localization
{
    internal static class LocalizationRefresh
    {
        private static bool _pendingRefresh = false;

        internal static void RequestRefresh()
        {
            _pendingRefresh = true;
        }

        internal static void FlushPendingRefresh()
        {
            if (!_pendingRefresh) return;

            string scene = GameManager.m_ActiveScene;
            if (string.IsNullOrEmpty(scene) || scene == "MainMenu" || scene == "Boot" || scene == "Empty") return;

            _pendingRefresh = false;
            RefreshActiveCustomAfflictions();
        }

        internal static void RefreshActiveCustomAfflictions()
        {
            string scene = GameManager.m_ActiveScene;
            if (string.IsNullOrEmpty(scene) || scene == "MainMenu" || scene == "Boot" || scene == "Empty") return;

            var mgr = AfflictionManager.GetAfflictionManagerInstance();
            if (mgr?.m_Afflictions == null) return;

            for (int i = 0; i < mgr.m_Afflictions.Count; i++)
            {
                var aff = mgr.m_Afflictions[i];
                if (aff is ILocalizableAffliction localizable)
                {
                    localizable.RefreshLocalization();
                }
            }

            var firstAid = InterfaceManager.GetPanel<Panel_FirstAid>();
            if (firstAid != null)
            {
                firstAid.UpdateDueToAfflictionHealed();
            }

            if (Settings.options.IsLogging && Core.Instance != null)
            {
                Core.Instance.LoggerInstance.Msg("Localization refreshed for active custom afflictions");
            }
        }
    }

    [HarmonyPatch(typeof(Il2Cpp.Localization), nameof(Il2Cpp.Localization.LoadStringTableForLanguage))]
    internal static class LocalizationRefreshPatch
    {
        private static void Postfix()
        {
            string scene = GameManager.m_ActiveScene;
            if (string.IsNullOrEmpty(scene) || scene == "MainMenu" || scene == "Boot" || scene == "Empty") return;

            LocalizationRefresh.RequestRefresh();

            if (Settings.options.IsLogging && Core.Instance != null)
            {
                Core.Instance.LoggerInstance.Msg("Localization reload detected -> queued affliction refresh");
            }
        }
    }
}