using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _Cars.ScriptableObjects;
using _Cars.Scripts;
using _Bot.Scripts;
using _PowerUps.Scripts;
using _PowerUps.ScriptableObjects;
using _Projectiles.ScriptableObjects;
using _UI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Prototyping.Scripts
{
    public class TuningManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════
        //  INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════

        [Header("UI References")]
        [SerializeField] private GameObject tuningPanel;
        [SerializeField] private GameObject pauseScreen;
        [SerializeField] private Button     saveButton;

        [Header("Assets to Tune")]
        [SerializeField] private List<CarStats>         carStatsList;
        [SerializeField] private List<ProjectileObject>  projectileList;
        [SerializeField] private List<PowerUpObject>    powerUpList;
        [SerializeField] private PowerUpSpawner         powerUpSpawner;

        // ═══════════════════════════════════════════════
        //  STYLE CONSTANTS
        // ═══════════════════════════════════════════════

        private const float PANEL_W     = 900f;
        private const float PANEL_H     = 700f;
        private const float TAB_H       = 56f;
        private const float ROW_H       = 72f;
        private const float LABEL_W     = 300f;
        private const float VALUE_W     = 90f;
        private const float ROW_GAP     = 8f;
        private const float SECTION_PAD = 24f;
        private const float ARROW_W     = 60f;
        private const float PICKER_H    = 64f;

        private const int F_TAB      = 20;
        private const int F_HEADER   = 24;
        private const int F_ROW      = 20;
        private const int F_VALUE    = 20;
        private const int F_PICKER   = 20;
        private const int F_ARROW    = 26;

        private static readonly Color COL_PANEL   = new Color(0.10f, 0.10f, 0.12f, 0.97f);
        private static readonly Color COL_TAB_OFF = new Color(0.15f, 0.15f, 0.20f, 1f);
        private static readonly Color COL_TAB_ON  = new Color(0.20f, 0.50f, 0.90f, 1f);
        private static readonly Color COL_ROW_A   = new Color(0.13f, 0.13f, 0.16f, 1f);
        private static readonly Color COL_ROW_B   = new Color(0.10f, 0.10f, 0.13f, 1f);
        private static readonly Color COL_HEADER  = new Color(0.10f, 0.10f, 0.14f, 1f);
        private static readonly Color COL_FILL    = new Color(0.15f, 0.55f, 1.00f, 1f);
        private static readonly Color COL_TRACK   = new Color(0.30f, 0.30f, 0.35f, 1f);
        private static readonly Color COL_HANDLE  = new Color(0.90f, 0.92f, 1.00f, 1f);
        private static readonly Color COL_PICKER  = new Color(0.16f, 0.16f, 0.24f, 1f);
        private static readonly Color COL_ARROW   = new Color(0.22f, 0.22f, 0.35f, 1f);

        // ═══════════════════════════════════════════════
        //  RUNTIME STATE
        // ═══════════════════════════════════════════════

        private bool isPanelOpen = false;

        private enum Tab { Players, Cars, Weapons, PowerUps, Spawner }

        private readonly List<Button>     tabButtons = new List<Button>();
        private readonly List<GameObject> tabPanels  = new List<GameObject>();

        private int selectedCarIndex     = 0;
        private int selectedWeaponIndex  = 0;
        private int selectedPowerUpIndex = 0;

        private Transform carStatsContainer;
        private Transform weaponStatsContainer;
        private Transform powerUpStatsContainer;

        private class PlayerEntry
        {
            public string         label;
            public CarStatsLoader statsLoader;
            public CarShooter     shooter;
            public BotAI          botAI;
        }
        private readonly List<PlayerEntry> players = new List<PlayerEntry>();

        // Per-player picker indices — preserved across respawns
        private readonly List<int> playerCarIndices    = new List<int>();
        private readonly List<int> playerWeaponIndices = new List<int>();

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Start()
        {
            StartCoroutine(InitDelayed());
        }

        private IEnumerator InitDelayed()
        {
            yield return null;
            yield return null;

            FindAllPlayers();
            BuildUI();

            if (saveButton != null)
                saveButton.onClick.AddListener(SaveAllChanges);

            tuningPanel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                TogglePanel();

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
                SaveAllChanges();
        }

        // ═══════════════════════════════════════════════
        //  PLAYER DISCOVERY
        // ═══════════════════════════════════════════════

        private void FindAllPlayers()
        {
            players.Clear();
            playerCarIndices.Clear();
            playerWeaponIndices.Clear();

            int h = 0;
            foreach (var loader in FindObjectsByType<CarStatsLoader>(FindObjectsSortMode.None))
            {
                h++;
                players.Add(new PlayerEntry
                {
                    label       = h == 1 ? "You" : $"Player {h}",
                    statsLoader = loader,
                    shooter     = loader.GetComponent<CarShooter>()
                });
                playerCarIndices.Add(0);
                playerWeaponIndices.Add(0);
            }
            int b = 0;
            foreach (var bot in FindObjectsByType<BotAI>(FindObjectsSortMode.None))
            {
                b++;
                players.Add(new PlayerEntry { label = $"Bot {b}", botAI = bot });
                playerCarIndices.Add(0);
                playerWeaponIndices.Add(0);
            }
            Debug.Log($"[TuningManager] Found {h} human(s) and {b} bot(s)");
        }

        // ═══════════════════════════════════════════════
        //  PANEL TOGGLE
        // ═══════════════════════════════════════════════

        private void TogglePanel()
        {
            isPanelOpen = !isPanelOpen;
            tuningPanel.SetActive(isPanelOpen);

            var pc = FindFirstObjectByType<PauseController>();
            if (isPanelOpen)
            {
                pc?.PauseGame();
                if (pauseScreen != null) pauseScreen.SetActive(false);
                foreach (var p in players)
                {
                    if (p.shooter == null) continue;
                    p.shooter.DisableGameplay();
                    p.shooter.enabled = false;
                }

                // Refresh player references (in case someone respawned)
                // then re-apply their last chosen loadouts
                ReapplyAllLoadouts();
            }
            else
            {
                foreach (var p in players)
                {
                    if (p.shooter == null) continue;
                    p.shooter.enabled = true;
                    p.shooter.EnableGameplay();
                }
                pc?.UnpauseGame();
            }
        }

        // ═══════════════════════════════════════════════
        //  REAPPLY LOADOUTS AFTER RESPAWN
        // ═══════════════════════════════════════════════

        private void ReapplyAllLoadouts()
        {
            // Save current indices before clearing
            var savedCarIndices    = new List<int>(playerCarIndices);
            var savedWeaponIndices = new List<int>(playerWeaponIndices);

            // Refresh player/bot references
            FindAllPlayers();

            // Restore saved indices (FindAllPlayers resets them to 0)
            for (int i = 0; i < playerCarIndices.Count && i < savedCarIndices.Count; i++)
                playerCarIndices[i] = savedCarIndices[i];
            for (int i = 0; i < playerWeaponIndices.Count && i < savedWeaponIndices.Count; i++)
                playerWeaponIndices[i] = savedWeaponIndices[i];

            // Re-apply car and weapon to each player/bot
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                // Car
                if (i < playerCarIndices.Count && playerCarIndices[i] < carStatsList.Count)
                {
                    var stats = carStatsList[playerCarIndices[i]];
                    if (player.statsLoader != null)
                    {
                        player.statsLoader.ApplyCarStats(stats);
                    }
                    else if (player.botAI != null)
                    {
                        SetPrivateField(player.botAI.GetComponent<BotController>(), "carStats", stats);
                        SetPrivateField(player.botAI.GetComponent<CarHealth>(),     "carStats", stats);
                    }
                }

                // Weapon
                if (i < playerWeaponIndices.Count && playerWeaponIndices[i] < projectileList.Count)
                {
                    var proj = projectileList[playerWeaponIndices[i]];
                    if (player.shooter != null)
                        player.shooter.SetProjectileType(proj);
                    else
                        player.botAI?.SetProjectile(proj);
                }
            }

            Debug.Log("[TuningManager] Loadouts re-applied after respawn.");
        }

        // ═══════════════════════════════════════════════
        //  BUILD ENTIRE UI
        // ═══════════════════════════════════════════════

        private void BuildUI()
        {
            var dimImg = tuningPanel.GetComponent<Image>() ?? tuningPanel.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.55f);

            // Centred card
            var card = MakeRect("Card", tuningPanel.transform);
            card.anchorMin        = new Vector2(0.5f, 0.5f);
            card.anchorMax        = new Vector2(0.5f, 0.5f);
            card.pivot            = new Vector2(0.5f, 0.5f);
            card.sizeDelta        = new Vector2(PANEL_W, PANEL_H);
            card.anchoredPosition = Vector2.zero;
            card.gameObject.AddComponent<Image>().color = COL_PANEL;

            // Tab bar
            var tabBar = MakeRect("TabBar", card);
            tabBar.anchorMin        = new Vector2(0, 1);
            tabBar.anchorMax        = new Vector2(1, 1);
            tabBar.pivot            = new Vector2(0.5f, 1);
            tabBar.sizeDelta        = new Vector2(0, TAB_H);
            tabBar.anchoredPosition = Vector2.zero;
            tabBar.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.09f, 1f);

            var tabLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.childForceExpandWidth  = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.childControlWidth      = true;
            tabLayout.childControlHeight     = true;
            tabLayout.spacing = 2f;
            tabLayout.padding = new RectOffset(2, 2, 4, 0);

            // Content area
            var content = MakeRect("Content", card);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(0, 0);
            content.offsetMax = new Vector2(0, -TAB_H);

            string[] tabNames = { "PLAYERS", "CARS", "WEAPONS", "POWER UPS", "SPAWNER" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;
                var tabBtn = MakeButton(tabNames[i], tabBar, F_TAB, Color.white, COL_TAB_OFF);
                tabButtons.Add(tabBtn);
                tabBtn.onClick.AddListener(() => SwitchTab((Tab)idx));

                var panel = MakeRect($"Panel_{tabNames[i]}", content);
                panel.anchorMin = Vector2.zero;
                panel.anchorMax = Vector2.one;
                panel.offsetMin = new Vector2(SECTION_PAD, SECTION_PAD);
                panel.offsetMax = new Vector2(-SECTION_PAD, -SECTION_PAD);
                tabPanels.Add(panel.gameObject);
            }

            BuildPlayersPanel(tabPanels[(int)Tab.Players].transform);
            BuildCarsPanel(tabPanels[(int)Tab.Cars].transform);
            BuildWeaponsPanel(tabPanels[(int)Tab.Weapons].transform);
            BuildPowerUpsPanel(tabPanels[(int)Tab.PowerUps].transform);
            BuildSpawnerPanel(tabPanels[(int)Tab.Spawner].transform);

            // Save button below card
            if (saveButton != null)
            {
                saveButton.transform.SetParent(tuningPanel.transform, false);
                var btnRt = saveButton.GetComponent<RectTransform>();
                btnRt.anchorMin        = new Vector2(0.5f, 0.5f);
                btnRt.anchorMax        = new Vector2(0.5f, 0.5f);
                btnRt.pivot            = new Vector2(0.5f, 1f);
                btnRt.sizeDelta        = new Vector2(340f, 52f);
                btnRt.anchoredPosition = new Vector2(0f, -(PANEL_H / 2f) - 12f);
            }

            SwitchTab(Tab.Players);
        }

        // ═══════════════════════════════════════════════
        //  TAB SWITCHING
        // ═══════════════════════════════════════════════

        private void SwitchTab(Tab tab)
        {
            for (int i = 0; i < tabPanels.Count; i++)
            {
                tabPanels[i].SetActive(i == (int)tab);
                var img = tabButtons[i].GetComponent<Image>();
                if (img) img.color = i == (int)tab ? COL_TAB_ON : COL_TAB_OFF;
                var txt = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.color = i == (int)tab ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            }
        }

        // ═══════════════════════════════════════════════
        //  PANEL: PLAYERS
        // ═══════════════════════════════════════════════

        private void BuildPlayersPanel(Transform parent)
        {
            var vl = MakeVLayout(parent, ROW_GAP);

            MakeHeader("Player & Bot Configuration", vl);
            MakeLabel("Use arrows to set the car and weapon for each player / bot.", vl, Color.gray);
            MakeSpacer(vl, 12f);

            for (int pi = 0; pi < players.Count; pi++)
            {
                int playerIdx = pi;
                var player    = players[pi];

                MakeLabel($"▸ {player.label}", vl, new Color(0.45f, 0.85f, 1f), bold: true);

                // Car picker
                var carNames = carStatsList.Select(c => c.CarName).ToList();
                MakeArrowPicker("Car", carNames, playerCarIndices[playerIdx], vl, (i) =>
                {
                    playerCarIndices[playerIdx] = i;
                    if (i >= carStatsList.Count) return;
                    var stats = carStatsList[i];

                    if (player.statsLoader != null)
                    {
                        player.statsLoader.ApplyCarStats(stats);
                    }
                    else if (player.botAI != null)
                    {
                        SetPrivateField(player.botAI.GetComponent<BotController>(), "carStats", stats);
                        SetPrivateField(player.botAI.GetComponent<CarHealth>(),     "carStats", stats);
                    }
                });

                // Weapon picker
                if (projectileList != null && projectileList.Count > 0)
                {
                    var wNames = projectileList.Select(p => p.ProjectileName).ToList();
                    MakeArrowPicker("Weapon", wNames, playerWeaponIndices[playerIdx], vl, (i) =>
                    {
                        playerWeaponIndices[playerIdx] = i;
                        if (i < projectileList.Count)
                        {
                            var proj = projectileList[i];
                            if (player.shooter != null) player.shooter.SetProjectileType(proj);
                            else player.botAI?.SetProjectile(proj);
                        }
                    });
                }

                MakeSpacer(vl, 16f);
            }
        }

        // ═══════════════════════════════════════════════
        //  PANEL: CARS
        // ═══════════════════════════════════════════════

        private void BuildCarsPanel(Transform parent)
        {
            var vl = MakeVLayout(parent, ROW_GAP);
            MakeHeader("Car Stats", vl);
            MakeSpacer(vl, 8f);

            var carNames = carStatsList.Select(c => c.CarName).ToList();
            MakeArrowPicker("Editing car:", carNames, selectedCarIndex, vl, (i) =>
            {
                selectedCarIndex = i;
                RebuildCarStats();
            });

            MakeSpacer(vl, 12f);

            var container = MakeRect("CarStatsContainer", vl);
            var cvl = container.gameObject.AddComponent<VerticalLayoutGroup>();
            cvl.spacing               = ROW_GAP;
            cvl.childForceExpandWidth  = true;
            cvl.childForceExpandHeight = false;
            cvl.childControlWidth      = true;
            cvl.childControlHeight     = true;
            container.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            carStatsContainer = container;

            RebuildCarStats();
        }

        private void RebuildCarStats()
        {
            if (carStatsContainer == null || carStatsList.Count == 0) return;
            foreach (Transform c in carStatsContainer) Destroy(c.gameObject);

            var car = carStatsList[selectedCarIndex];

            MakeSliderRow("Speed %",        car.SpeedStat,        0f, 100f, carStatsContainer, (v) => SetPrivateField(car, "speedStat",        v));
            MakeSliderRow("Acceleration %", car.AccelerationStat, 0f, 100f, carStatsContainer, (v) => SetPrivateField(car, "accelerationStat", v));
            MakeSliderRow("Jump %",         car.JumpForceStat,    0f, 100f, carStatsContainer, (v) => SetPrivateField(car, "jumpForceStat",    v));
            MakeSliderRow("Health %",       car.HealthStat,       0f, 100f, carStatsContainer, (v) => SetPrivateField(car, "healthStat",       v));

            MakeMiniHeader("Speed range:", carStatsContainer);
            MakeSliderRow("Min Speed", GetPrivateFloat(car, "minMaxSpeed"),     5f,  100f, carStatsContainer, (v) => SetPrivateField(car, "minMaxSpeed",    v));
            MakeSliderRow("Max Speed", GetPrivateFloat(car, "maxMaxSpeed"),     5f,  100f, carStatsContainer, (v) => SetPrivateField(car, "maxMaxSpeed",    v));

            MakeMiniHeader("Acceleration range:", carStatsContainer);
            MakeSliderRow("Min Accel", GetPrivateFloat(car, "minAcceleration"), 1f,   60f, carStatsContainer, (v) => SetPrivateField(car, "minAcceleration", v));
            MakeSliderRow("Max Accel", GetPrivateFloat(car, "maxAcceleration"), 1f,   60f, carStatsContainer, (v) => SetPrivateField(car, "maxAcceleration", v));

            MakeMiniHeader("Jump range:", carStatsContainer);
            MakeSliderRow("Min Jump",  GetPrivateFloat(car, "minJumpForce"),    1f,  100f, carStatsContainer, (v) => SetPrivateField(car, "minJumpForce",   v));
            MakeSliderRow("Max Jump",  GetPrivateFloat(car, "maxJumpForce"),    1f,  100f, carStatsContainer, (v) => SetPrivateField(car, "maxJumpForce",   v));

            MakeMiniHeader("Health range:", carStatsContainer);
            MakeSliderRow("Min Health", GetPrivateFloat(car, "minMaxHealth"),  10f,  300f, carStatsContainer, (v) => SetPrivateField(car, "minMaxHealth",  Mathf.RoundToInt(v)));
            MakeSliderRow("Max Health", GetPrivateFloat(car, "maxMaxHealth"),  10f,  300f, carStatsContainer, (v) => SetPrivateField(car, "maxMaxHealth",  Mathf.RoundToInt(v)));
        }

        // ═══════════════════════════════════════════════
        //  PANEL: WEAPONS
        // ═══════════════════════════════════════════════

        private void BuildWeaponsPanel(Transform parent)
        {
            var vl = MakeVLayout(parent, ROW_GAP);
            MakeHeader("Weapon Stats", vl);
            MakeSpacer(vl, 8f);

            var wNames = projectileList.Select(p => p.ProjectileName).ToList();
            MakeArrowPicker("Editing weapon:", wNames, selectedWeaponIndex, vl, (i) =>
            {
                selectedWeaponIndex = i;
                RebuildWeaponStats();
            });

            MakeSpacer(vl, 12f);

            var container = MakeRect("WeaponStatsContainer", vl);
            var cvl = container.gameObject.AddComponent<VerticalLayoutGroup>();
            cvl.spacing               = ROW_GAP;
            cvl.childForceExpandWidth  = true;
            cvl.childForceExpandHeight = false;
            cvl.childControlWidth      = true;
            cvl.childControlHeight     = true;
            container.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            weaponStatsContainer = container;

            RebuildWeaponStats();
        }

        private void RebuildWeaponStats()
        {
            if (weaponStatsContainer == null || projectileList.Count == 0) return;
            foreach (Transform c in weaponStatsContainer) Destroy(c.gameObject);

            var proj = projectileList[selectedWeaponIndex];

            MakeSliderRow("Damage",             proj.Damage,           1f,    100f, weaponStatsContainer, (v) => SetPrivateField(proj, "damage",           Mathf.RoundToInt(v)), "F0");
            MakeSliderRow("Fire Rate (sec)",    proj.FireRate,         0.05f,   5f, weaponStatsContainer, (v) => SetPrivateField(proj, "fireRate",          v));
            MakeSliderRow("Cooldown (sec)",     proj.CooldownDuration, 0.1f,  10f, weaponStatsContainer, (v) => SetPrivateField(proj, "cooldownDuration",  v));
            MakeSliderRow("Recoil Force",       proj.RecoilForce,      0f,    50f, weaponStatsContainer, (v) => SetPrivateField(proj, "recoilForce",       v));
            MakeSliderRow("Fire Force (speed)", proj.FireForce,        1f,   100f, weaponStatsContainer, (v) => SetPrivateField(proj, "fireForce",         v));
        }

        // ═══════════════════════════════════════════════
        //  PANEL: POWER-UPS
        // ═══════════════════════════════════════════════

        private void BuildPowerUpsPanel(Transform parent)
        {
            var vl = MakeVLayout(parent, ROW_GAP);
            MakeHeader("Power-Up Stats", vl);
            MakeSpacer(vl, 8f);

            var puNames = powerUpList.Select(p => p.powerUpName).ToList();
            MakeArrowPicker("Editing power-up:", puNames, selectedPowerUpIndex, vl, (i) =>
            {
                selectedPowerUpIndex = i;
                RebuildPowerUpStats();
            });

            MakeSpacer(vl, 12f);

            var container = MakeRect("PowerUpStatsContainer", vl);
            var cvl = container.gameObject.AddComponent<VerticalLayoutGroup>();
            cvl.spacing               = ROW_GAP;
            cvl.childForceExpandWidth  = true;
            cvl.childForceExpandHeight = false;
            cvl.childControlWidth      = true;
            cvl.childControlHeight     = true;
            container.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            powerUpStatsContainer = container;

            RebuildPowerUpStats();
        }

        private void RebuildPowerUpStats()
        {
            if (powerUpStatsContainer == null || powerUpList.Count == 0) return;
            foreach (Transform c in powerUpStatsContainer) Destroy(c.gameObject);

            var pu = powerUpList[selectedPowerUpIndex];

            MakeSliderRow("Duration (sec)", pu.duration, 0f, 30f, powerUpStatsContainer,
                (v) => pu.duration = v);

            var fields = pu.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(float))
                {
                    float val = (float)field.GetValue(pu);
                    MakeSliderRow(FormatFieldName(field.Name), val, 0f, GetReasonableMax(field.Name, val),
                        powerUpStatsContainer, (v) => field.SetValue(pu, v));
                }
                else if (field.FieldType == typeof(int))
                {
                    int val = (int)field.GetValue(pu);
                    MakeSliderRow(FormatFieldName(field.Name), val, 0f, val * 3 + 10,
                        powerUpStatsContainer, (v) => field.SetValue(pu, Mathf.RoundToInt(v)), "F0");
                }
            }
        }

        // ═══════════════════════════════════════════════
        //  PANEL: SPAWNER
        // ═══════════════════════════════════════════════

        private void BuildSpawnerPanel(Transform parent)
        {
            var vl = MakeVLayout(parent, ROW_GAP);
            MakeHeader("Power-Up Spawner", vl);
            MakeSpacer(vl, 12f);

            if (powerUpSpawner == null)
            {
                MakeLabel("No PowerUpSpawner assigned!", vl, Color.red);
                return;
            }

            MakeSliderRow("Min Spawn Interval",   powerUpSpawner.minSpawnInterval,  1f,  60f, vl, (v) => powerUpSpawner.minSpawnInterval  = v);
            MakeSliderRow("Max Spawn Interval",   powerUpSpawner.maxSpawnInterval,  1f, 120f, vl, (v) => powerUpSpawner.maxSpawnInterval  = v);
            MakeSliderRow("Max Active Power-Ups", powerUpSpawner.maxActivePowerUps, 1f,  30f, vl, (v) => powerUpSpawner.maxActivePowerUps = Mathf.RoundToInt(v), "F0");
        }

        // ═══════════════════════════════════════════════
        //  SAVE
        // ═══════════════════════════════════════════════

        public void SaveAllChanges()
        {
#if UNITY_EDITOR
            foreach (var car  in carStatsList)   if (car  != null) UnityEditor.EditorUtility.SetDirty(car);
            foreach (var proj in projectileList)  if (proj != null) UnityEditor.EditorUtility.SetDirty(proj);
            foreach (var pu   in powerUpList)    if (pu   != null) UnityEditor.EditorUtility.SetDirty(pu);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[TuningManager] ✓ Saved!</color>");
#endif
        }

        // ═══════════════════════════════════════════════
        //  UI FACTORY — ARROW PICKER
        // ═══════════════════════════════════════════════

        private void MakeArrowPicker(string label, List<string> options, int startIndex,
            Transform parent, System.Action<int> onChange)
        {
            var row = MakeRect("Picker_" + label, parent);
            var le  = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = PICKER_H;
            row.gameObject.AddComponent<Image>().color = COL_ROW_B;

            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing                = 0f;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = true;
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;
            hl.padding                = new RectOffset(16, 16, 8, 8);

            var lGo  = new GameObject("Lbl"); lGo.transform.SetParent(row, false);
            var lTmp = lGo.AddComponent<TextMeshProUGUI>();
            lTmp.text      = label;
            lTmp.fontSize  = F_ROW;
            lTmp.color     = Color.white;
            lTmp.alignment = TextAlignmentOptions.MidlineLeft;
            var lLE = lGo.AddComponent<LayoutElement>();
            lLE.preferredWidth = LABEL_W;
            lLE.minWidth       = LABEL_W;
            lLE.flexibleWidth  = 0f;

            var pickerRow = MakeRect("PickerControls", row);
            var prl = pickerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            prl.spacing                = 0f;
            prl.childForceExpandWidth  = false;
            prl.childForceExpandHeight = true;
            prl.childControlWidth      = true;
            prl.childControlHeight     = true;
            var prLE = pickerRow.gameObject.AddComponent<LayoutElement>();
            prLE.flexibleWidth = 1f;

            var leftBtn  = MakeArrowButton("◀", pickerRow);

            var nameBg = MakeRect("NameBg", pickerRow);
            nameBg.gameObject.AddComponent<Image>().color = COL_PICKER;
            var nameLE = nameBg.gameObject.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1f;

            var nameGo = new GameObject("Name"); nameGo.transform.SetParent(nameBg, false);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = Vector2.zero; nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = new Vector2(8, 0); nameRt.offsetMax = new Vector2(-8, 0);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.fontSize  = F_PICKER;
            nameTmp.color     = Color.white;
            nameTmp.alignment = TextAlignmentOptions.Center;

            var rightBtn = MakeArrowButton("▶", pickerRow);

            int current = Mathf.Clamp(startIndex, 0, options.Count - 1);

            void Refresh()
            {
                nameTmp.text = options.Count > 0 ? options[current] : "—";
                onChange?.Invoke(current);
            }

            leftBtn.onClick.AddListener(() =>
            {
                current = (current - 1 + options.Count) % options.Count;
                Refresh();
            });

            rightBtn.onClick.AddListener(() =>
            {
                current = (current + 1) % options.Count;
                Refresh();
            });

            Refresh();
        }

        private Button MakeArrowButton(string symbol, Transform parent)
        {
            var go  = MakeRect("ArrowBtn_" + symbol, parent);
            var img = go.gameObject.AddComponent<Image>();
            img.color = COL_ARROW;
            var btn = go.gameObject.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor      = COL_ARROW;
            cb.highlightedColor = COL_ARROW * 1.4f;
            cb.pressedColor     = COL_ARROW * 0.7f;
            btn.colors = cb;

            var le = go.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = ARROW_W;
            le.minWidth       = ARROW_W;
            le.flexibleWidth  = 0f;

            var tGo = new GameObject("Text"); tGo.transform.SetParent(go, false);
            var tRt = tGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = symbol;
            tmp.fontSize  = F_ARROW;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        // ═══════════════════════════════════════════════
        //  UI FACTORY — STRUCTURAL
        // ═══════════════════════════════════════════════

        private RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private RectTransform MakeRect(string name, RectTransform parent)
            => MakeRect(name, parent.transform);

        private Transform MakeVLayout(Transform parent, float spacing = 6f)
        {
            var rt = MakeRect("VLayout", parent);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var vl = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing               = spacing;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;
            vl.childControlWidth      = true;
            vl.childControlHeight     = true;
            return rt.transform;
        }

        private void MakeHeader(string text, Transform parent)
        {
            var rt = MakeRect("Header", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            rt.gameObject.AddComponent<Image>().color = COL_HEADER;
            var tGo = new GameObject("Text"); tGo.transform.SetParent(rt, false);
            var tRt = tGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(16, 0); tRt.offsetMax = new Vector2(-16, 0);
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = F_HEADER; tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.yellow; tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void MakeMiniHeader(string text, Transform parent)
        {
            var rt = MakeRect("MiniHeader", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = "  " + text; tmp.fontSize = 18; tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(1f, 0.85f, 0.2f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void MakeLabel(string text, Transform parent, Color color, bool bold = false)
        {
            var rt = MakeRect("Label", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = bold ? 21 : 18;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = color; tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(8, 0, 0, 0);
        }

        private void MakeSpacer(Transform parent, float h)
        {
            MakeRect("Spacer", parent).gameObject.AddComponent<LayoutElement>().preferredHeight = h;
        }

        private Button MakeButton(string label, Transform parent, int fontSize,
            Color textColor, Color bgColor)
        {
            var rt  = MakeRect("Btn_" + label, parent);
            var img = rt.gameObject.AddComponent<Image>(); img.color = bgColor;
            var btn = rt.gameObject.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor = bgColor; cb.highlightedColor = bgColor * 1.2f;
            cb.pressedColor = bgColor * 0.8f; btn.colors = cb;
            var tGo = new GameObject("Text"); tGo.transform.SetParent(rt, false);
            var tRt = tGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = fontSize;
            tmp.color = textColor; tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        // ── Slider row ──
        private void MakeSliderRow(string label, float current, float min, float max,
            Transform parent, System.Action<float> onChange, string fmt = "F1")
        {
            var row = MakeRect("Row_" + label, parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = ROW_H;
            row.gameObject.AddComponent<Image>().color = COL_ROW_A;

            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12f; hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true; hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.padding = new RectOffset(20, 16, 12, 12);

            var lGo = new GameObject("Lbl"); lGo.transform.SetParent(row, false);
            var lTmp = lGo.AddComponent<TextMeshProUGUI>();
            lTmp.text = label; lTmp.fontSize = F_ROW;
            lTmp.color = Color.white; lTmp.alignment = TextAlignmentOptions.MidlineLeft;
            var lLE = lGo.AddComponent<LayoutElement>();
            lLE.preferredWidth = LABEL_W; lLE.minWidth = LABEL_W; lLE.flexibleWidth = 0;

            var sGo    = BuildSlider(row);
            var slider = sGo.GetComponent<Slider>();
            slider.minValue = min; slider.maxValue = max;
            slider.value = current; slider.wholeNumbers = fmt == "F0";
            var sLE = sGo.AddComponent<LayoutElement>();
            sLE.flexibleWidth = 1; sLE.minWidth = 100;

            var vGo  = new GameObject("Val"); vGo.transform.SetParent(row, false);
            var vTmp = vGo.AddComponent<TextMeshProUGUI>();
            vTmp.text = current.ToString(fmt); vTmp.fontSize = F_VALUE;
            vTmp.color = Color.cyan; vTmp.alignment = TextAlignmentOptions.MidlineRight;
            var vLE = vGo.AddComponent<LayoutElement>();
            vLE.preferredWidth = VALUE_W; vLE.minWidth = VALUE_W; vLE.flexibleWidth = 0;

            slider.onValueChanged.AddListener(v =>
            {
                vTmp.text = v.ToString(fmt);
                onChange?.Invoke(v);
            });
        }

        private GameObject BuildSlider(Transform parent)
        {
            var go     = new GameObject("Slider");
            go.transform.SetParent(parent, false);
            var slider = go.AddComponent<Slider>();

            var bgGo = new GameObject("BG"); bgGo.transform.SetParent(go.transform, false);
            bgGo.AddComponent<Image>().color = COL_TRACK;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.42f); bgRt.anchorMax = new Vector2(1, 0.58f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var faGo = new GameObject("FillArea"); faGo.transform.SetParent(go.transform, false);
            var faRt = faGo.AddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.42f); faRt.anchorMax = new Vector2(1, 0.58f);
            faRt.offsetMin = new Vector2(5, 0); faRt.offsetMax = new Vector2(-18, 0);
            var fGo  = new GameObject("Fill"); fGo.transform.SetParent(faGo.transform, false);
            fGo.AddComponent<Image>().color = COL_FILL;
            var fRt  = fGo.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = new Vector2(0, 1);
            fRt.offsetMin = fRt.offsetMax = Vector2.zero;

            var haGo = new GameObject("HandleArea"); haGo.transform.SetParent(go.transform, false);
            var haRt = haGo.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(12, 0); haRt.offsetMax = new Vector2(-12, 0);
            var hGo  = new GameObject("Handle"); hGo.transform.SetParent(haGo.transform, false);
            var hImg = hGo.AddComponent<Image>(); hImg.color = COL_HANDLE;
            var hRt  = hGo.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(24, 24);
            hRt.anchorMin = new Vector2(0, 0); hRt.anchorMax = new Vector2(0, 1);
            hRt.pivot     = new Vector2(0.5f, 0.5f);

            slider.fillRect = fRt; slider.handleRect = hRt;
            slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight;

            return go;
        }

        // ═══════════════════════════════════════════════
        //  REFLECTION HELPERS
        // ═══════════════════════════════════════════════

        private void SetPrivateField(object target, string name, object value)
        {
            if (target == null) return;
            target.GetType()
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(target, value);
        }

        private float GetPrivateFloat(object target, string name)
        {
            var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) return 0f;
            var v = f.GetValue(target);
            if (v is int   i)  return i;
            if (v is float fl) return fl;
            return 0f;
        }

        private string FormatFieldName(string n)
        {
            var r = System.Text.RegularExpressions.Regex.Replace(n, "([A-Z])", " $1");
            return char.ToUpper(r[0]) + r.Substring(1);
        }

        private float GetReasonableMax(string n, float cur)
        {
            string l = n.ToLower();
            if (l.Contains("force")    || l.Contains("speed"))                              return Mathf.Max(cur * 4f, 50f);
            if (l.Contains("duration") || l.Contains("interval") || l.Contains("time"))    return Mathf.Max(cur * 4f, 30f);
            if (l.Contains("distance") || l.Contains("radius"))                             return Mathf.Max(cur * 4f, 50f);
            if (l.Contains("multiplier"))                                                    return Mathf.Max(cur * 3f, 10f);
            if (l.Contains("damage"))                                                        return Mathf.Max(cur * 4f, 100f);
            return Mathf.Max(cur * 3f, 10f);
        }
    }
}