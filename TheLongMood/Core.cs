using AfflictionComponent.Components;
using LocalizationUtilities;
using TheLongMood.Afflictions.Patches;
using TheLongMood.Persistence;
using TheLongMood.Resources.Localization;
using static TheLongMood.Afflictions.Boredom.Bored;
using static TheLongMood.Afflictions.Boredom.ExtremelyBored;
using static TheLongMood.Afflictions.Boredom.VeryBored;
using static TheLongMood.Afflictions.Depression.Hopeless;
using static TheLongMood.Afflictions.Depression.Miserable;
using static TheLongMood.Afflictions.Depression.Sad;
using static TheLongMood.Afflictions.Depression.Weepy;

[assembly: MelonInfo(typeof(TheLongMood.Core), "TheLongMood", "1.2.2", "EtherSystem, Flower Field", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace TheLongMood
{
    public class Core : MelonMod
    {
        public static string? LoadEmbeddedJSON(string Localization)
        {
            string? result = null;

            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TheLongMood.Resources.Localization.Localization.json");
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }
            return result;
        }

        public static Core? Instance { get; private set; }
        internal static MoodState State = new();

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
        private float _hoursToApplyCached = -1f;
        private const float MIN_HOURS_TO_APPLY = 0.01f;

        private float _idleGameHours = 0f;
        private float BOREDOM_DELAY_HOURS;

        private bool _dirty = false;
        private bool _wasBoredomBlocked = true;
        private bool? _lastLoggedIsClearingIce = null;

        public override void OnInitializeMelon()
        {
            LocalizationManager.LoadJsonLocalization(LoadEmbeddedJSON("Localization.json"));
            Instance = this;
            LoggerInstance.Msg("is already bored...");
            Settings.OnLoad();

            _instantPenaltyCached = Settings.options.InstantStrugglePenalty;
            _hoursToApplyCached = Mathf.Max(MIN_HOURS_TO_APPLY, Settings.options.HoursToApply);
            BOREDOM_DELAY_HOURS = Settings.options.TimeForBoredomIncrease / 60f;

            uConsole.RegisterCommand("reset_boredom", new Action(() =>
            {
                Core.State.Boredom = 0f;
                _dirty = true;
                if (Settings.options.IsLogging) LoggerInstance.Msg("Boredom reset to 0");
            }));

            uConsole.RegisterCommand("reset_depression", new Action(() =>
            {
                Core.State.Depression = 0f;
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

                Core.State.Boredom = Mathf.Clamp(v, 0, 100);
                _dirty = true;
                uConsole.Log($"Boredom set to {Core.State.Boredom:0}");
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

                Core.State.Depression = Mathf.Clamp(v, 0, 100);
                _dirty = true;
                uConsole.Log($"Depression set to {Core.State.Depression:0}");
            }));
        }

        public void ResetRuntime()
        {
            _dirty = false;

            _wasEating = false;
            _eatingAccumGameHours = 0f;
            _eatingBonusCooldownHours = 0f;

            _wasInStruggle = false;

            _idleGameHours = 0f;
            _realAccum = 0f;

            _currentBoredomTier = 0;
            _currentDepressionTier = 0;

            _wasBoredomBlocked = true;
            _lastLoggedIsClearingIce = null;

            _instantPenaltyCached = Settings.options.InstantStrugglePenalty;
            _hoursToApplyCached = Mathf.Max(MIN_HOURS_TO_APPLY, Settings.options.HoursToApply);
        }

        public void OnStateLoaded()
        {
            Core.State.Boredom = Mathf.Clamp(Core.State.Boredom, 0f, 100f);
            Core.State.Depression = Mathf.Clamp(Core.State.Depression, 0f, 100f);
            Core.State.AftershockPool = Mathf.Max(0f, Core.State.AftershockPool);
            Core.State.AftershockRemainingHours = Mathf.Max(0f, Core.State.AftershockRemainingHours);

            if (!Settings.options.IsAttackAftershock)
            {
                if (Core.State.AftershockPool > 0f || Core.State.AftershockRemainingHours > 0f)
                {
                    Core.State.AftershockPool = 0f;
                    Core.State.AftershockRemainingHours = 0f;
                    _dirty = true;
                }
            }
            else if (Settings.options.InstantStrugglePenalty && (Core.State.AftershockPool > 0f || Core.State.AftershockRemainingHours > 0f))
            {
                Core.State.AftershockPool = 0f;
                Core.State.AftershockRemainingHours = 0f;
                _dirty = true;
            }
            _currentBoredomTier = -1;
            _currentDepressionTier = -1;
        }

        public void ResetAll()
        {
            Core.State = new MoodState();
            ResetRuntime();
        }

        public void SaveIfDirty()
        {
            if (!_dirty) return;

            SaveDataManager.OnSave();
            _dirty = false;
        }

        private void EnsureSingleBoredomTier()
        {
            int newTier = 0;
            if (Core.State.Boredom >= 90f) newTier = 3;
            else if (Core.State.Boredom >= 75f) newTier = 2;
            else if (Core.State.Boredom >= 50f) newTier = 1;

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

            bool addedAffliction = false;

            if (newTier == 3)
            {
                new ExtremelyBoredAffliction(AfflictionBodyArea.Head).Start();
                addedAffliction = true;
            }
            else if (newTier == 2)
            {
                new VeryBoredAffliction(AfflictionBodyArea.Head).Start();
                addedAffliction = true;
            }
            else if (newTier == 1)
            {
                new BoredAffliction(AfflictionBodyArea.Head).Start();
                addedAffliction = true;
            }

            if (addedAffliction)
            {
                AfflictionSaveHelper.QueueSurvivalSave();
            }
        }

        private void EnsureSingleDepressionTier()
        {
            int newTier = 0;
            if (Core.State.Depression >= 80f) newTier = 4;
            else if (Core.State.Depression >= 60f) newTier = 3;
            else if (Core.State.Depression >= 45f) newTier = 2;
            else if (Core.State.Depression >= 20f) newTier = 1;

            if (newTier == _currentDepressionTier) return;
            _currentDepressionTier = newTier;

            var mgr = AfflictionManager.GetAfflictionManagerInstance();
            if (mgr?.m_Afflictions == null) return;

            for (int i = mgr.m_Afflictions.Count - 1; i >= 0; i--)
            {
                var a = mgr.m_Afflictions[i];
                if (a is SadAffliction || a is WeepyAffliction || a is MiserableAffliction || a is HopelessAffliction)
                    a.Cure();
            }

            bool addedAffliction = false;

            if (newTier == 4)
            {
                new HopelessAffliction(AfflictionBodyArea.Head).Start();
                addedAffliction = true;
            }
            else if (newTier == 3)
            {
                new MiserableAffliction(AfflictionBodyArea.Head).Start();
                addedAffliction = true;
            }
            else if (newTier == 2)
            {
                new WeepyAffliction(AfflictionBodyArea.Head).Start();
                addedAffliction = true;
            }
            else if (newTier == 1)
            {
                new SadAffliction(AfflictionBodyArea.Head).Start();
                addedAffliction = true;
            }

            if (addedAffliction)
            {
                AfflictionSaveHelper.QueueSurvivalSave();
            }
        }

        public override void OnUpdate()
        {
            LocalizationRefresh.FlushPendingRefresh();

            if (GameManager.m_Instance == null || GameManager.m_IsPaused) return;
            if (GameManager.IsBootSceneActive() || GameManager.IsMainMenuActive() || GameManager.IsEmptySceneActive()) return;

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

            // -----------fishing bools----------------

            var fishingHolePanel = InterfaceManager.GetPanel<Panel_IceFishingHoleClear>();
            bool isClearingIce = fishingHolePanel != null && fishingHolePanel.IsClearingIce();

            if (Settings.options.IsLogging && _lastLoggedIsClearingIce != isClearingIce)
            {
                LoggerInstance.Msg($"isClearingIce : {isClearingIce}");
                _lastLoggedIsClearingIce = isClearingIce;
            }

            bool isFishing = FishingPatch.IsFishing;

            // --------------------------------

            float gameHoursPassed = timeOfDay.GetTODHours(realElapsed);
            if (gameHoursPassed <= 0f) return;
            if (gameHoursPassed > 12f) return;

            if (player.PlayerIsSleeping()) return;

            // ----------------wildlife attack logic-------------------
            bool aftershockActive = false;

            var struggle = GameManager.GetPlayerStruggleComponent();
            bool inStruggle = (struggle != null && struggle.InStruggle());

            if (!Settings.options.IsAttackAftershock)
            {
                if (Core.State.AftershockPool > 0f || Core.State.AftershockRemainingHours > 0f)
                {
                    Core.State.AftershockPool = 0f;
                    Core.State.AftershockRemainingHours = 0f;
                    _dirty = true;

                    if (Settings.options.IsLogging) LoggerInstance.Msg("Attack aftershock disabled → cleared pending aftershock state");
                }

                _wasInStruggle = inStruggle;
            }
            else
            {
                bool instant = Settings.options.InstantStrugglePenalty;

                if (instant != _instantPenaltyCached)
                {
                    _instantPenaltyCached = instant;

                    if (instant)
                    {
                        if (Core.State.AftershockPool > 0f || Core.State.AftershockRemainingHours > 0f)
                        {
                            Core.State.AftershockPool = 0f;
                            Core.State.AftershockRemainingHours = 0f;
                            _dirty = true;
                        }
                    }
                }

                float hoursToApply = 0f;
                if (!instant)
                {
                    hoursToApply = Mathf.Max(MIN_HOURS_TO_APPLY, Settings.options.HoursToApply);

                    if (!Mathf.Approximately(hoursToApply, _hoursToApplyCached))
                    {
                        _hoursToApplyCached = hoursToApply;
                        if (Core.State.AftershockPool > 0f) Core.State.AftershockRemainingHours = hoursToApply;
                    }
                }

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
                        float before = Core.State.Depression;
                        Core.State.Depression = Mathf.Clamp(Core.State.Depression + penalty, 0f, 100f);
                        _dirty = true;

                        if (Settings.options.IsLogging) LoggerInstance.Msg($"{attacker} attack detected → Depression +{penalty:0.##} (instant) ({before:0.##} -> {Core.State.Depression:0.##})");
                    }
                    else
                    {
                        Core.State.AftershockPool += penalty;
                        Core.State.AftershockRemainingHours = hoursToApply;
                        _dirty = true;

                        if (Settings.options.IsLogging) LoggerInstance.Msg($"{attacker} attack detected → scheduled Depression +{penalty:0.##} over {hoursToApply:0.##}h");
                    }
                }

                _wasInStruggle = inStruggle;
                aftershockActive = (!instant && Core.State.AftershockPool > 0f);
            }
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
                || isSharpening
                || (isClearingIce && !Settings.options.IsBreakingIceGenBoredom)
                || (isFishing && !Settings.options.IsFishingGenBoredom));

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

            float oldB = Core.State.Boredom;
            float oldD = Core.State.Depression;

            if (boredomBlocked)
            {
                _idleGameHours = 0f;
                Core.State.Boredom -= Settings.options.RegenRate * gameHoursPassed;
            }
            else
            {
                _idleGameHours += gameHoursPassed;

                if (_idleGameHours >= BOREDOM_DELAY_HOURS)
                {
                    Core.State.Boredom += Settings.options.DropRate * gameHoursPassed;

                    if (_wasBoredomBlocked)
                        if (Settings.options.IsLogging) LoggerInstance.Msg("Inactivity detected");
                }
            }

            _wasBoredomBlocked = boredomBlocked;

            if (hasAffliction)
            {
                Core.State.Depression += (Settings.options.DropRate * 2f) * gameHoursPassed;
            }
            else if (isReading)
            {
                Core.State.Depression -= (Settings.options.DropRate * 1f) * gameHoursPassed;
            }
            else if (Core.State.Boredom >= 90f)
            {
                Core.State.Depression += (Settings.options.DropRate * 2.96f) * gameHoursPassed;
            }
            else if (Core.State.Boredom >= 75f)
            {
                Core.State.Depression += (Settings.options.DropRate * 2.22f) * gameHoursPassed;
            }
            else if (Core.State.Boredom >= 50f)
            {
                Core.State.Depression += (Settings.options.DropRate * 1.48f) * gameHoursPassed;
            }
            else
            {
                if (!aftershockActive) Core.State.Depression -= (Settings.options.DropRate * 0.5f) * gameHoursPassed;
            }
            if (hunger!= null && _wasEating && !isEating)
            {
                if (_eatingBonusCooldownHours <= 0f && _eatingAccumGameHours >= MIN_EATING_HOURS)
                {
                    Core.State.Depression -= EATING_DEPRESSION_BONUS;
                    _eatingBonusCooldownHours = EATING_BONUS_COOLDOWN_HOURS;
                    _dirty = true;
                }
                _eatingAccumGameHours = 0f;
            }
            _wasEating = isEating;

            // apply attack aftershock over time (only if not instant)
            if (Settings.options.IsAttackAftershock && !Settings.options.InstantStrugglePenalty && Core.State.AftershockPool > 0f)
            {
                if (Core.State.AftershockRemainingHours <= 0f) Core.State.AftershockRemainingHours = Mathf.Max(MIN_HOURS_TO_APPLY, Settings.options.HoursToApply);

                float h = Mathf.Min(gameHoursPassed, Core.State.AftershockRemainingHours);
                float add = (Core.State.AftershockPool / Core.State.AftershockRemainingHours) * h;

                Core.State.Depression += add;

                Core.State.AftershockPool -= add;
                Core.State.AftershockRemainingHours -= h;
                _dirty = true;

                if (Core.State.AftershockPool <= 0.0001f || Core.State.AftershockRemainingHours <= 0.0001f)
                {
                    Core.State.AftershockPool = 0f;
                    Core.State.AftershockRemainingHours = 0f;
                }
            }

            Core.State.Boredom = Mathf.Clamp(Core.State.Boredom, 0f, 100f);
            Core.State.Depression = Mathf.Clamp(Core.State.Depression, 0f, 100f);

            if (!Mathf.Approximately(oldB, Core.State.Boredom) || !Mathf.Approximately(oldD, Core.State.Depression))
                _dirty = true;

            EnsureSingleBoredomTier();
            EnsureSingleDepressionTier();
        }
    }
}