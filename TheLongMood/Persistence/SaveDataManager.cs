using Newtonsoft.Json;

namespace TheLongMood.Persistence
{
    internal static class SaveDataManager
    {
        private static readonly ModDataManager _manager = new("TheLongMood", false);
        private const string SUFFIX = "mooddata";

        internal static void OnSave()
        {
            string json = JsonConvert.SerializeObject(Core.State);
            _manager.Save(json, SUFFIX);

            if (Settings.options.IsLogging && Core.Instance != null)
            {
                Core.Instance.LoggerInstance.Msg($"Saved → B:{Core.State.Boredom:0.###} | D:{Core.State.Depression:0.###} | " + $"P:{Core.State.AftershockPool:0.###} | T:{Core.State.AftershockRemainingHours:0.###}");
            }
        }

        internal static void OnLoad()
        {
            string json = _manager.Load(SUFFIX);

            if (string.IsNullOrEmpty(json))
            {
                Core.State = new MoodState();
                if (Settings.options.IsLogging && Core.Instance != null) Core.Instance.LoggerInstance.Msg("Loaded → no data (fresh state)");
                return;
            }

            MoodState? loaded = null;
            try
            {
                loaded = JsonConvert.DeserializeObject<MoodState>(json);
            }
            catch
            {
            }

            Core.State = loaded ?? new MoodState();

            Core.State.Boredom = Mathf.Clamp(Core.State.Boredom, 0f, 100f);
            Core.State.Depression = Mathf.Clamp(Core.State.Depression, 0f, 100f);
            Core.State.AftershockPool = Mathf.Max(0f, Core.State.AftershockPool);
            Core.State.AftershockRemainingHours = Mathf.Max(0f, Core.State.AftershockRemainingHours);

            if (Settings.options.IsLogging && Core.Instance != null)
            {
                Core.Instance.LoggerInstance.Msg($"Loaded → B:{Core.State.Boredom:0.###} | D:{Core.State.Depression:0.###} | " + $"P:{Core.State.AftershockPool:0.###} | T:{Core.State.AftershockRemainingHours:0.###}");
            }
        }

        internal static void OnNewGame()
        {
            Core.State = new MoodState();

            if (Settings.options.IsLogging && Core.Instance != null) Core.Instance.LoggerInstance.Msg("Clearing data for new game");
        }
    }

    // SAVE
    [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.WriteSlotToDisk), new Type[] { typeof(SlotData), typeof(SaveGameSlots.Timestamp) })]
    internal class TheLongMood_SavePatch
    {
        private static void Prefix()
        {
            Core.Instance?.SaveIfDirty();
        }
    }

    // LOAD
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), new Type[] { typeof(string), typeof(int) })]
    internal class TheLongMood_LoadPatch
    {
        private static void Postfix()
        {
            Core.Instance?.OnStateLoaded();
            Core.Instance?.ResetRuntime();
            SaveDataManager.OnLoad();
        }
    }

    // NEW GAME
    [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.CreateSlot), new Type[] { typeof(string), typeof(SaveSlotType), typeof(uint), typeof(Episode) })]
    internal class TheLongMood_NewGamePatch
    {
        private static void Postfix()
        {
            Core.Instance?.ResetAll();
        }
    }

    // MAIN MENU
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.DoExitToMainMenu))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadMainMenu))]
    internal class TheLongMood_MainMenuPatch
    {
        private static void Postfix()
        {
            Core.Instance?.ResetAll();
        }
    }
}