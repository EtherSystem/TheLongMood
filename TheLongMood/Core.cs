using static TheLongMood.Afflictions.Boredom.ExtremelyBored;
using static TheLongMood.Afflictions.Depression.Miserable;
using static TheLongMood.Afflictions.Depression.Hopeless;
using static TheLongMood.Afflictions.Boredom.VeryBored;
using static TheLongMood.Afflictions.Depression.Weepy;
using static TheLongMood.Afflictions.Depression.Sad;
using static TheLongMood.Afflictions.Boredom.Bored;
using AfflictionComponent.Components;
using static Il2Cpp.SaveGameSlots;
using System.Globalization;
using System.Collections;

[assembly: MelonInfo(typeof(TheLongMood.Core), "TheLongMood", "1.1.0", "EtherSystem", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace TheLongMood
{
    internal static class Persistence
    {
        private static readonly ModDataManager _manager = new("TheLongMood", false);

        public static bool TryLoad(out float boredom, out float depression, out float pool, out float remainingHours, out string? raw)
        {
            boredom = 0f;
            depression = 0f;
            pool = 0f;
            remainingHours = 0f;

            raw = _manager.Load();
            if (raw == null) return false;
            if (raw.Length == 0) return true;

            if (!TryParseFour(raw, out float b, out float d, out float p, out float t))
                return true; // data invalide -> garde 0

            boredom = Mathf.Clamp(b, 0f, 100f);
            depression = Mathf.Clamp(d, 0f, 100f);

            pool = Mathf.Max(0f, p);
            remainingHours = Mathf.Max(0f, t);

            return true;
        }

        public static void Save(float boredom, float depression, float pool, float remainingHours)
        {
            string combined =
                boredom.ToString("0.###", CultureInfo.InvariantCulture) + "|" +
                depression.ToString("0.###", CultureInfo.InvariantCulture) + "|" +
                pool.ToString("0.###", CultureInfo.InvariantCulture) + "|" +
                remainingHours.ToString("0.###", CultureInfo.InvariantCulture);

            _manager.Save(combined);
        }

        private static bool TryParseFour(string s, out float a, out float b, out float c, out float d)
        {
            a = b = c = d = 0f;

            ReadOnlySpan<char> span = s.AsSpan();

            int p1 = span.IndexOf('|');
            if (p1 <= 0) return false;

            ReadOnlySpan<char> r1 = span[(p1 + 1)..];
            int p2r = r1.IndexOf('|');
            if (p2r <= 0) return false;
            int p2 = p1 + 1 + p2r;

            ReadOnlySpan<char> r2 = span[(p2 + 1)..];
            int p3r = r2.IndexOf('|');
            if (p3r <= 0) return false;
            int p3 = p2 + 1 + p3r;

            if (!float.TryParse(span[..p1], NumberStyles.Float, CultureInfo.InvariantCulture, out a)) return false;
            if (!float.TryParse(span[(p1 + 1)..p2], NumberStyles.Float, CultureInfo.InvariantCulture, out b)) return false;
            if (!float.TryParse(span[(p2 + 1)..p3], NumberStyles.Float, CultureInfo.InvariantCulture, out c)) return false;
            if (!float.TryParse(span[(p3 + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return false;

            return true;
        }
    }

    public class Core : MelonMod
    {
        public static Core? Instance { get; private set; }

        public static float boredom = 0f;
        public static float depression = 0f;

        private int _currentBoredomTier = 0;
        private int _currentDepressionTier = 0;

        private float _realAccum = 0f;

        private bool _wasEating = false;
        private const float EATING_DEPRESSION_BONUS = 2f;

        //anti eating abuse
        private float _eatingAccumGameHours = 0f;
        private float _eatingBonusCooldownHours = 0f;
        private const float MIN_EATING_HOURS = 10f / 3600f;           // 10s in-game
        private const float EATING_BONUS_COOLDOWN_HOURS = 2f / 60f;   // 2min in-game

        private bool _wasInStruggle = false;
        private bool _instantPenaltyCached = false;
        private float _attackAftershockPool = 0f;
        private float _attackAftershockRemainingHours = 0f;
        private float _hoursToApplyCached = -1f;
        private const float MIN_HOURS_TO_APPLY = 0.01f;

        private float _idleGameHours = 0f;
        private float BOREDOM_DELAY_HOURS;

        internal bool pendingLoad = false;

        private object? _loadRoutine;
        private bool _dirty = false;

        private bool _wasBoredomBlocked = true;

        public override void OnInitializeMelon()
        {
            Instance = this;
            LoggerInstance.Msg("is already bored...");
            Settings.OnLoad();

            _instantPenaltyCached = Settings.options.InstantStrugglePenalty;
            _hoursToApplyCached = Mathf.Max(MIN_HOURS_TO_APPLY, Settings.options.HoursToApply);
            BOREDOM_DELAY_HOURS = Settings.options.TimeForBoredomIncrease / 60f;

            uConsole.RegisterCommand("reset_boredom", new Action(() =>
            {
                boredom = 0f;
                _dirty = true;
                if (Settings.options.IsLogging) LoggerInstance.Msg("Boredom reset to 0");
            }));

            uConsole.RegisterCommand("reset_depression", new Action(() =>
            {
                depression = 0f;
                _dirty = true;
                if (Settings.options.IsLogging) LoggerInstance.Msg("Depression reset to 0");
            }));

            uConsole.RegisterCommand("set_boredom", new Action(() =>
            {
                var @params = uConsole.GetAllParameters();
                if (@params == null || @params.Count < 1)
                {
                    uConsole.Log("[value]");
                    return;
                }

                if (!int.TryParse(@params[0], out int v))
                {
                    uConsole.Log("value must be a number");
                    return;
                }

                boredom = Mathf.Clamp(v, 0, 100);
                _dirty = true;
                uConsole.Log($"Boredom set to {boredom:0}");
            }));

            uConsole.RegisterCommand("set_depression", new Action(() =>
            {
                var @params = uConsole.GetAllParameters();
                if (@params == null || @params.Count < 1)
                {
                    uConsole.Log("[value]");
                    return;
                }

                if (!int.TryParse(@params[0], out int v))
                {
                    uConsole.Log("value must be a number");
                    return;
                }

                depression = Mathf.Clamp(v, 0, 100);
                _dirty = true;
                uConsole.Log($"Depression set to {depression:0}");
            }));
        }
        private IEnumerator LoadRoutine()
        {
            const int maxFrames = 120;
            for (int i = 0; i < maxFrames; i++)
            {
                if (GameManager.IsBootSceneActive() || GameManager.IsMainMenuActive() || GameManager.IsEmptySceneActive())
                    break;

                if (Persistence.TryLoad(out float b, out float d, out float p, out float t, out string? raw))
                {
                    if (raw != null)
                    {
                        if (!string.IsNullOrEmpty(raw))
                        {
                            boredom = b;
                            depression = d;
                            _attackAftershockPool = p;
                            _attackAftershockRemainingHours = t;
                            bool wipedPool = false;
                            if (Settings.options.InstantStrugglePenalty && (_attackAftershockPool > 0f || _attackAftershockRemainingHours > 0f))
                            {
                                _attackAftershockPool = 0f;
                                _attackAftershockRemainingHours = 0f;
                                wipedPool = true;
                            }

                            if (Settings.options.IsLogging) LoggerInstance.Msg($"Loaded → Boredom: {boredom} | Depression: {depression} | AftershockPool: {_attackAftershockPool} | AftershockRemainingHours: {_attackAftershockRemainingHours}");

                            _dirty = wipedPool ? true : false;
                            break;
                        }
                    }
                }
                yield return null;
            }
            _loadRoutine = null;
        }

        public void SaveToModData()
        {
            if (!_dirty) return;

            Persistence.Save(boredom, depression, _attackAftershockPool, _attackAftershockRemainingHours);
            if (Settings.options.IsLogging) LoggerInstance.Msg($"Saved → b{boredom:0.###}| d{depression:0.###}| p{_attackAftershockPool:0.###}| t{_attackAftershockRemainingHours:0.###}");
            _dirty = false;
        }

        public void ResetSlotState()
        {
            boredom = 0f;
            depression = 0f;
            pendingLoad = false;
            _dirty = false;
            _wasEating = false;
            _wasEating = false;
            _eatingAccumGameHours = 0f;
            _eatingBonusCooldownHours = 0f;
            _wasInStruggle = false;
            _attackAftershockPool = 0f;
            _attackAftershockRemainingHours = 0f;
            _instantPenaltyCached = Settings.options.InstantStrugglePenalty;
            _hoursToApplyCached = Mathf.Max(MIN_HOURS_TO_APPLY, Settings.options.HoursToApply);

            if (_loadRoutine != null)
            {
                MelonCoroutines.Stop(_loadRoutine);
                _loadRoutine = null;
            }

            _idleGameHours = 0f;
            _realAccum = 0f;
            _currentBoredomTier = 0;
            _currentDepressionTier = 0;
            _wasBoredomBlocked = true;
        }

        private void EnsureSingleBoredomTier()
        {
            int newTier = 0;
            if (boredom >= 90f) newTier = 3;
            else if (boredom >= 75f) newTier = 2;
            else if (boredom >= 50f) newTier = 1;

            if (newTier == _currentBoredomTier) return;
            _currentBoredomTier = newTier;

            var mgr = AfflictionManager.GetAfflictionManagerInstance();
            if (mgr?.m_Afflictions == null) return;

            for (int i = mgr.m_Afflictions.Count - 1; i >= 0; i--)
            {
                var a = mgr.m_Afflictions[i];
                if (a is BoredAffliction || a is VeryBoredAffliction || a is ExtremelyBoredAffliction)
                    a.Cure();
            }

            if (newTier == 3) new ExtremelyBoredAffliction(AfflictionBodyArea.Head).Start();
            else if (newTier == 2) new VeryBoredAffliction(AfflictionBodyArea.Head).Start();
            else if (newTier == 1) new BoredAffliction(AfflictionBodyArea.Head).Start();
        }

        private void EnsureSingleDepressionTier()
        {
            int newTier = 0;
            if (depression >= 80f) newTier = 4;
            else if (depression >= 60f) newTier = 3;
            else if (depression >= 45f) newTier = 2;
            else if (depression >= 20f) newTier = 1;

            if (newTier == _currentDepressionTier) return;
            _currentDepressionTier = newTier;

            var mgr = AfflictionManager.GetAfflictionManagerInstance();
            if (mgr?.m_Afflictions == null) return;

            for (int i = mgr.m_Afflictions.Count - 1; i >= 0; i--)
            {
                var a = mgr.m_Afflictions[i];
                if (a is SadAffliction || a is WeepyAffliction || a is MiserableAffliction || a is HopelessAffliction) a.Cure();
            }

            if (newTier == 4) new HopelessAffliction(AfflictionBodyArea.Head).Start();
            else if (newTier == 3) new MiserableAffliction(AfflictionBodyArea.Head).Start();
            else if (newTier == 2) new WeepyAffliction(AfflictionBodyArea.Head).Start();
            else if (newTier == 1) new SadAffliction(AfflictionBodyArea.Head).Start();
        }

        public override void OnUpdate()
        {
            if (GameManager.m_Instance == null || GameManager.m_IsPaused) return;
            if (GameManager.IsBootSceneActive() || GameManager.IsMainMenuActive() || GameManager.IsEmptySceneActive()) return;

            if (pendingLoad)
            {
                pendingLoad = false;
                _loadRoutine ??= MelonCoroutines.Start(LoadRoutine());
            }

            var player = GameManager.GetPlayerManagerComponent();
            if (player == null) return;

            var fireManager = GameManager.GetFireManagerComponent();
            var cond = GameManager.GetConditionComponent();
            var timeOfDay = GameManager.GetTimeOfDayComponent();
            if (fireManager == null || cond == null || timeOfDay == null) return;

            var playerTransform = GameManager.GetPlayerTransform();
            if (playerTransform == null) return;

            _realAccum += Time.unscaledDeltaTime;
            if (_realAccum < 0.25f) return;

            float realElapsed = _realAccum;
            _realAccum = 0f;

            float gameHoursPassed = timeOfDay.GetTODHours(realElapsed);
            if (gameHoursPassed <= 0f) return;
            if (gameHoursPassed > 12f) return;

            if (player.PlayerIsSleeping()) return;

            // wildlife attack logic
            bool instant = Settings.options.InstantStrugglePenalty;

            if (instant != _instantPenaltyCached)
            {
                _instantPenaltyCached = instant;

                if (instant)
                {
                    _attackAftershockPool = 0f;
                    _attackAftershockRemainingHours = 0f;
                    _dirty = true;
                }
            }

            float hoursToApply = 0f;
            if (!instant)
            {
                hoursToApply = Mathf.Max(MIN_HOURS_TO_APPLY, Settings.options.HoursToApply);

                if (!Mathf.Approximately(hoursToApply, _hoursToApplyCached))
                {
                    _hoursToApplyCached = hoursToApply;
                    if (_attackAftershockPool > 0f) _attackAftershockRemainingHours = hoursToApply;
                }
            }

            var struggle = GameManager.GetPlayerStruggleComponent();
            bool inStruggle = (struggle != null && struggle.InStruggle());

            if (inStruggle && !_wasInStruggle)
            {
                float penalty;
                string attacker;

                if (struggle != null && struggle.InStruggleWIthBear())
                {
                    penalty = Settings.options.BearPenalty;
                    attacker = "Bear";
                }
                else if (struggle != null && struggle.InStruggleWithMoose())
                {
                    penalty = Settings.options.MoosePenalty;
                    attacker = "Moose";
                }
                else if (struggle != null && struggle.InStruggleWithCougar())
                {
                    penalty = Settings.options.CougarPenalty;
                    attacker = "Cougar";
                }
                else if (struggle != null && struggle.InStruggleWIthWolf())
                {
                    penalty = Settings.options.WolfPenalty;
                    attacker = "Wolf";
                }
                else
                {
                    penalty = Settings.options.WolfPenalty;
                    attacker = "Struggle";
                }

                if (instant)
                {
                    float before = depression;
                    depression = Mathf.Clamp(depression + penalty, 0f, 100f);
                    _dirty = true;

                    if (Settings.options.IsLogging) LoggerInstance.Msg($"{attacker} attack detected → Depression +{penalty:0.##} (instant) ({before:0.##} -> {depression:0.##})");
                }
                else
                {
                    _attackAftershockPool += penalty;
                    _attackAftershockRemainingHours = hoursToApply;
                    _dirty = true;

                    if (Settings.options.IsLogging) LoggerInstance.Msg($"{attacker} attack detected → scheduled Depression +{penalty:0.##} over {hoursToApply:0.##}h");
                }
            }
            _wasInStruggle = inStruggle;

            bool aftershockActive = (!instant && _attackAftershockPool > 0f);
            //----------------------------

            float distanceToFire = fireManager.GetDistanceToClosestFire(playerTransform.position);
            bool isNearFire = distanceToFire < 5f;


            var examinePanel = InterfaceManager.GetPanel<Panel_Inventory_Examine>();
            bool isReading = (examinePanel != null && examinePanel.IsReading());
            bool isCleaning = (examinePanel != null && examinePanel.IsCleaning());
            bool isHarvesting = (examinePanel != null && examinePanel.IsHarvesting());
            bool isRepairing = (examinePanel != null && examinePanel.IsRepairing());
            bool isSharpening = (examinePanel != null && examinePanel.IsSharpening());

            bool isCrafting = false;
            var craftingPanel = InterfaceManager.GetPanel<Panel_Crafting>();
            if (craftingPanel != null && craftingPanel.m_CraftingOperation != null)
                isCrafting = craftingPanel.m_CraftingOperation.InProgress;

            bool isActivity =
                (player.PlayerIsWalking()
                || player.PlayerIsSprinting()
                || player.PlayerIsClimbing()
                || player.PlayerIsBreakingDown()
                || player.PlayerIsHarvestingCarcass()
                || player.PlayerIsDead()
                || isCrafting
                || isReading
                || isCleaning
                || isHarvesting
                || isRepairing
                || isSharpening);

            var heldItem = player.m_ItemInHands;
            bool holdsLightSource = false;
            if (heldItem != null)
            {
                if (heldItem.m_TorchItem?.IsBurning() == true) holdsLightSource = true;
                else if (heldItem.m_KeroseneLampItem?.IsOn() == true) holdsLightSource = true;
                else if (heldItem.m_FlareItem?.IsBurning() == true) holdsLightSource = true;
            }

            // ------eating logic--------
            var hunger = GameManager.GetHungerComponent();
            bool isEating = (hunger != null && hunger.IsEatingInProgress());
            if (_eatingBonusCooldownHours > 0f) _eatingBonusCooldownHours = Mathf.Max(0f, _eatingBonusCooldownHours - gameHoursPassed);
            if (isEating) _eatingAccumGameHours += gameHoursPassed;
            //---------------------------

            bool hasAffliction = cond.HasAffliction() || cond.HasRiskAffliction();

            bool boredomBlocked = isActivity || holdsLightSource || isNearFire;

            float oldB = boredom;
            float oldD = depression;

            if (boredomBlocked)
            {
                _idleGameHours = 0f;
                boredom -= Settings.options.RegenRate * gameHoursPassed;
            }
            else
            {
                _idleGameHours += gameHoursPassed;

                if (_idleGameHours >= BOREDOM_DELAY_HOURS)
                {
                    boredom += Settings.options.DropRate * gameHoursPassed;

                    if (_wasBoredomBlocked)
                        if (Settings.options.IsLogging) LoggerInstance.Msg("Inactivity detected");
                }
            }

            _wasBoredomBlocked = boredomBlocked;

            if (hasAffliction)
            {
                depression += (Settings.options.DropRate * 2f) * gameHoursPassed;
            }
            else if (isReading)
            {
                depression -= (Settings.options.DropRate * 1f) * gameHoursPassed;
            }
            else if (boredom >= 90f)
            {
                depression += (Settings.options.DropRate * 2.96f) * gameHoursPassed;
            }
            else if (boredom >= 75f)
            {
                depression += (Settings.options.DropRate * 2.22f) * gameHoursPassed;
            }
            else if (boredom >= 50f)
            {
                depression += (Settings.options.DropRate * 1.48f) * gameHoursPassed;
            }
            else
            {
                if (!aftershockActive) depression -= (Settings.options.DropRate * 0.5f) * gameHoursPassed;
            }
            if (hunger!= null && _wasEating && !isEating)
            {
                if (_eatingBonusCooldownHours <= 0f && _eatingAccumGameHours >= MIN_EATING_HOURS)
                {
                    depression -= EATING_DEPRESSION_BONUS;
                    _eatingBonusCooldownHours = EATING_BONUS_COOLDOWN_HOURS;
                }
                _eatingAccumGameHours = 0f;
            }
            _wasEating = isEating;

            // apply attack aftershock over time (only if not instant)
            if (!Settings.options.InstantStrugglePenalty && _attackAftershockPool > 0f)
            {
                if (_attackAftershockRemainingHours <= 0f)
                    _attackAftershockRemainingHours = Mathf.Max(MIN_HOURS_TO_APPLY, Settings.options.HoursToApply);

                float h = Mathf.Min(gameHoursPassed, _attackAftershockRemainingHours);
                float add = (_attackAftershockPool / _attackAftershockRemainingHours) * h;

                depression += add;

                _attackAftershockPool -= add;
                _attackAftershockRemainingHours -= h;
                _dirty = true;

                if (_attackAftershockPool <= 0.0001f || _attackAftershockRemainingHours <= 0.0001f)
                {
                    _attackAftershockPool = 0f;
                    _attackAftershockRemainingHours = 0f;
                }
            }

            boredom = Mathf.Clamp(boredom, 0f, 100f);
            depression = Mathf.Clamp(depression, 0f, 100f);

            if (!Mathf.Approximately(oldB, boredom) || !Mathf.Approximately(oldD, depression))
                _dirty = true;

            EnsureSingleBoredomTier();
            EnsureSingleDepressionTier();
        }
    }

    // save patches
    [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.WriteSlotToDisk), [typeof(SlotData), typeof(Timestamp)])]
    internal class TheLongMood_SavePatch
    {
        private static void Prefix()
        {
            Core.Instance?.SaveToModData();
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), [typeof(string), typeof(int)])]
    internal class TheLongMood_LoadPatch
    {
        private static void Postfix()
        {
            if (Core.Instance != null)
                Core.Instance.pendingLoad = true;
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.DoExitToMainMenu))]
    internal class TheLongMood_MainMenuPatch
    {
        private static void Postfix()
        {
            Core.Instance?.ResetSlotState();
        }
    }
}