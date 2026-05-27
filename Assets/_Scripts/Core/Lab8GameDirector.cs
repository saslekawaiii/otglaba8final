using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lab8
{
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }

        public event Action StateChanged;

        public GameMode CurrentMode { get; private set; } = GameMode.None;
        public bool IsPaused => isPaused;
        public bool NarratorSkillsGiven => narratorSkillsGiven;

        public PlayerController Player => player;
        public PlayerStats PlayerStats => playerStats;
        public InventorySystem Inventory => inventory;
        public AbilitySystem Abilities => abilities;
        public TalentSystem Talents => talents;
        public QuestSystem Quests => quests;
        public CraftingSystem Crafting => crafting;
        public CombatArena Arena => arena;

        [SerializeField] private bool narratorSkillsGiven;

        private PlayerController player;
        private PlayerStats playerStats;
        private InventorySystem inventory;
        private AbilitySystem abilities;
        private TalentSystem talents;
        private QuestSystem quests;
        private CraftingSystem crafting;
        private CombatArena arena;

        private UiRoot ui;

        private GameObject worldRoot;
        private GameObject peacefulRoot;
        private GameObject combatRoot;

        private Transform peacefulSpawn;
        private Transform combatSpawn;

        private bool isPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            EnsureEventSystem();
            EnsureCoreObjects();
            EnsureLighting();
            BuildWorlds();
            EnsureUi();
            LoadGame();

            ui.ShowMainMenuAtStart();
            StateChanged?.Invoke();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ui.HandleEscape();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        public void StartMode(GameMode mode)
        {
            if (mode == GameMode.None)
            {
                return;
            }

            CurrentMode = mode;

            if (peacefulRoot != null)
            {
                peacefulRoot.SetActive(mode == GameMode.Peaceful);
            }

            if (combatRoot != null)
            {
                combatRoot.SetActive(mode == GameMode.Combat);
            }

            if (mode == GameMode.Combat)
            {
                if (arena != null)
                {
                    arena.Begin();
                }

                if (combatSpawn != null && player != null)
                {
                    player.transform.position = combatSpawn.position;
                }
            }
            else
            {
                if (arena != null)
                {
                    arena.Stop();
                }

                if (peacefulSpawn != null && player != null)
                {
                    player.transform.position = peacefulSpawn.position;
                }
            }

            if (playerStats != null)
            {
                playerStats.ReviveAtFull();
            }
            SaveGame();
            StateChanged?.Invoke();
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
            Time.timeScale = isPaused ? 0f : 1f;
            StateChanged?.Invoke();
        }

        public void GrantNarratorSkills()
        {
            narratorSkillsGiven = true;
            if (abilities != null)
            {
                abilities.UnlockStarterAbilities();
            }

            SaveGame();
            StateChanged?.Invoke();
        }

        public void OnPlayerDied()
        {
            if (inventory != null)
            {
                inventory.ClearAll();
            }

            SaveGame();
            SetPaused(true);
            ui.ShowDeathScreen();
        }

        public void RespawnPlayer()
        {
            if (player == null)
            {
                return;
            }

            StartMode(GameMode.Peaceful);

            if (inventory != null)
            {
                inventory.ClearAll();
            }

            Vector3 spawn = peacefulSpawn != null ? peacefulSpawn.position : Vector3.zero;
            player.transform.position = spawn;

            if (playerStats != null)
            {
                playerStats.ReviveAtFull();
            }

            ui.HideDeathScreen();
            ui.ShowRescueDialogue();
            SaveGame();
        }

        public void ResetAllProgress()
        {
            narratorSkillsGiven = false;

            if (arena != null)
            {
                arena.Stop();
            }

            if (inventory != null)
            {
                inventory.ClearAll();
            }

            if (quests != null)
            {
                quests.ImportData(null);
            }

            if (talents != null)
            {
                talents.ImportData(new TalentSaveData
                {
                    availablePoints = 3,
                    learnedIds = new System.Collections.Generic.List<string>()
                });
            }

            if (abilities != null)
            {
                abilities.ImportData(new AbilitySaveData { unlocked = false });
            }

            if (playerStats != null)
            {
                playerStats.ResetProgress();
            }

            CurrentMode = GameMode.None;
            if (peacefulRoot != null)
            {
                peacefulRoot.SetActive(false);
            }

            if (combatRoot != null)
            {
                combatRoot.SetActive(false);
            }

            SaveSystem.Clear();
            if (ui != null)
            {
                ui.OnProgressReset();
            }

            SetPaused(true);
            StateChanged?.Invoke();
        }

        public void SaveGame()
        {
            SaveGameData save = new SaveGameData
            {
                mode = CurrentMode.ToString(),
                narratorSkillsGiven = narratorSkillsGiven,
                inventory = inventory != null ? inventory.ExportData() : new InventorySaveData(),
                quests = quests != null ? quests.ExportData() : null,
                talents = talents != null ? talents.ExportData() : new TalentSaveData(),
                abilities = abilities != null ? abilities.ExportData() : new AbilitySaveData(),
                stats = playerStats != null ? playerStats.ExportData() : new StatsSaveData()
            };

            SaveSystem.Save(save);
        }

        private void LoadGame()
        {
            SaveGameData save = SaveSystem.Load();
            if (save == null)
            {
                if (abilities != null && narratorSkillsGiven)
                {
                    abilities.UnlockStarterAbilities();
                }
                return;
            }

            narratorSkillsGiven = save.narratorSkillsGiven;

            if (inventory != null)
            {
                inventory.ImportData(save.inventory);
            }

            if (talents != null)
            {
                talents.ImportData(save.talents);
            }

            if (playerStats != null)
            {
                playerStats.ImportData(save.stats);
            }

            if (abilities != null)
            {
                abilities.ImportData(save.abilities);
                if (narratorSkillsGiven && !abilities.Unlocked)
                {
                    abilities.UnlockStarterAbilities();
                }
            }

            if (quests != null)
            {
                quests.ImportData(save.quests);
            }

            if (Enum.TryParse(save.mode, true, out GameMode mode) && mode != GameMode.None)
            {
                CurrentMode = mode;
            }
            else
            {
                CurrentMode = GameMode.None;
            }

            if (CurrentMode != GameMode.None)
            {
                StartMode(CurrentMode);
            }
        }

        private void EnsureCoreObjects()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObj.name = "Lab8_Player";
                playerObj.transform.position = new Vector3(0f, 1f, -20f);

                Renderer rendererComp = playerObj.GetComponent<Renderer>();
                if (rendererComp != null)
                {
                    rendererComp.material.color = new Color(0.76f, 0.82f, 0.94f, 1f);
                }

                playerStats = playerObj.AddComponent<PlayerStats>();
                inventory = playerObj.AddComponent<InventorySystem>();
                abilities = playerObj.AddComponent<AbilitySystem>();
                player = playerObj.AddComponent<PlayerController>();
            }

            playerStats = player.GetComponent<PlayerStats>();
            inventory = player.GetComponent<InventorySystem>();
            abilities = player.GetComponent<AbilitySystem>();

            quests = GetComponent<QuestSystem>();
            if (quests == null)
            {
                quests = gameObject.AddComponent<QuestSystem>();
            }

            talents = GetComponent<TalentSystem>();
            if (talents == null)
            {
                talents = gameObject.AddComponent<TalentSystem>();
            }

            crafting = GetComponent<CraftingSystem>();
            if (crafting == null)
            {
                crafting = gameObject.AddComponent<CraftingSystem>();
            }

            arena = GetComponent<CombatArena>();
            if (arena == null)
            {
                arena = gameObject.AddComponent<CombatArena>();
            }

            playerStats.Died -= OnPlayerDied;
            playerStats.Died += OnPlayerDied;

            inventory.Changed -= SaveGame;
            inventory.Changed += SaveGame;

            quests.Changed -= SaveGame;
            quests.Changed += SaveGame;

            talents.Changed -= OnTalentChanged;
            talents.Changed += OnTalentChanged;

            abilities.Changed -= SaveGame;
            abilities.Changed += SaveGame;

            playerStats.Changed -= SaveGame;
            playerStats.Changed += SaveGame;
        }

        private void OnTalentChanged()
        {
            playerStats.RecalculateWithTalents();
            SaveGame();
        }

        private void EnsureUi()
        {
            ui = FindObjectOfType<UiRoot>();
            if (ui == null)
            {
                GameObject uiObject = new GameObject("Lab8_UI");
                ui = uiObject.AddComponent<UiRoot>();
            }

            ui.Initialize(this);
        }

        private void BuildWorlds()
        {
            if (worldRoot != null)
            {
                return;
            }

            worldRoot = new GameObject("Lab8_Worlds");
            DontDestroyOnLoad(worldRoot);

            peacefulRoot = BuildPeacefulWorld(worldRoot.transform);
            combatRoot = BuildCombatWorld(worldRoot.transform);

            peacefulRoot.SetActive(false);
            combatRoot.SetActive(false);

            arena.Initialize(combatRoot.transform);
        }

        private GameObject BuildPeacefulWorld(Transform parent)
        {
            GameObject root = new GameObject("PeacefulWorld");
            root.transform.SetParent(parent, false);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root.transform);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(22f, 1f, 22f);
            SetColor(ground, new Color(0.42f, 0.56f, 0.34f, 1f));

            BuildVillage(root.transform);
            BuildResourceFields(root.transform);

            GameObject narratorObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            narratorObj.name = "NPC_Рассказчик";
            narratorObj.transform.SetParent(root.transform);
            narratorObj.transform.position = new Vector3(-6f, 2f, -10f);
            SetColor(narratorObj, new Color(0.84f, 0.72f, 0.3f, 1f));

            Npc narrator = narratorObj.AddComponent<Npc>();
            narrator.Configure("narrator", "Рассказчик", NpcRole.Narrator, new string[0]);

            GameObject questerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            questerObj.name = "NPC_Житель";
            questerObj.transform.SetParent(root.transform);
            questerObj.transform.position = new Vector3(4f, 2f, -10f);
            SetColor(questerObj, new Color(0.68f, 0.82f, 0.95f, 1f));

            Npc quester = questerObj.AddComponent<Npc>();
            quester.Configure(
                "villager",
                "Житель",
                NpcRole.QuestGiver,
                new[]
                {
                    "quest_wood_20",
                    "quest_stone_20",
                    "quest_combat_wave1",
                    "quest_combat_wave3",
                    "quest_kill_12"
                });

            GameObject spawn = new GameObject("PeacefulSpawn");
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.position = new Vector3(0f, 1f, -18f);
            peacefulSpawn = spawn.transform;

            return root;
        }

        private GameObject BuildCombatWorld(Transform parent)
        {
            GameObject root = new GameObject("CombatWorld");
            root.transform.SetParent(parent, false);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ArenaGround";
            ground.transform.SetParent(root.transform);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            SetColor(ground, new Color(0.30f, 0.32f, 0.36f, 1f));

            BuildLab7Walls(root.transform);
            BuildLab7Platforms(root.transform);
            BuildLab7Cover(root.transform);
            BuildLab7SpawnPoints(root.transform);

            GameObject spawn = new GameObject("CombatSpawn");
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.position = new Vector3(0f, 1f, -18f);
            combatSpawn = spawn.transform;

            return root;
        }

        private static void BuildLab7Walls(Transform parent)
        {
            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(parent);

            CreateBlock(walls.transform, new Vector3(0f, 2.5f, 41f), new Vector3(84f, 5f, 2f), new Color(0.17f, 0.18f, 0.22f, 1f));
            CreateBlock(walls.transform, new Vector3(0f, 2.5f, -41f), new Vector3(84f, 5f, 2f), new Color(0.17f, 0.18f, 0.22f, 1f));
            CreateBlock(walls.transform, new Vector3(41f, 2.5f, 0f), new Vector3(2f, 5f, 84f), new Color(0.17f, 0.18f, 0.22f, 1f));
            CreateBlock(walls.transform, new Vector3(-41f, 2.5f, 0f), new Vector3(2f, 5f, 84f), new Color(0.17f, 0.18f, 0.22f, 1f));
        }

        private static void BuildLab7Platforms(Transform parent)
        {
            GameObject platforms = new GameObject("Platforms");
            platforms.transform.SetParent(parent);

            CreateBlock(platforms.transform, new Vector3(0f, 0.75f, 0f), new Vector3(16f, 1.5f, 16f), new Color(0.26f, 0.28f, 0.32f, 1f));

            CreateBlock(platforms.transform, new Vector3(-20f, 2f, 10f), new Vector3(12f, 1f, 8f), new Color(0.25f, 0.26f, 0.31f, 1f));
            CreateBlock(platforms.transform, new Vector3(20f, 2f, 10f), new Vector3(12f, 1f, 8f), new Color(0.25f, 0.26f, 0.31f, 1f));
            CreateBlock(platforms.transform, new Vector3(-20f, 3.6f, 24f), new Vector3(10f, 1f, 8f), new Color(0.27f, 0.28f, 0.33f, 1f));
            CreateBlock(platforms.transform, new Vector3(20f, 3.6f, 24f), new Vector3(10f, 1f, 8f), new Color(0.27f, 0.28f, 0.33f, 1f));

            CreateRamp(platforms.transform, new Vector3(-15f, 1.1f, 16f), new Vector3(10f, 1f, 4f), 18f);
            CreateRamp(platforms.transform, new Vector3(15f, 1.1f, 16f), new Vector3(10f, 1f, 4f), -18f);

            CreateBlock(platforms.transform, new Vector3(0f, 1.2f, 25f), new Vector3(12f, 2.4f, 12f), new Color(0.22f, 0.23f, 0.27f, 1f));
        }

        private static void BuildLab7Cover(Transform parent)
        {
            GameObject cover = new GameObject("Cover");
            cover.transform.SetParent(parent);

            Vector3[] points =
            {
                new Vector3(-10f, 1f, -3f),
                new Vector3(12f, 1f, -6f),
                new Vector3(-26f, 1f, -12f),
                new Vector3(24f, 1f, -15f),
                new Vector3(-6f, 1f, 17f),
                new Vector3(6f, 1f, 17f),
                new Vector3(-31f, 1f, 8f),
                new Vector3(31f, 1f, 8f)
            };

            for (int i = 0; i < points.Length; i++)
            {
                float width = 2.5f + (i % 3) * 0.7f;
                float depth = 2f + (i % 2) * 0.9f;
                CreateBlock(cover.transform, points[i], new Vector3(width, 2f, depth), new Color(0.33f, 0.34f, 0.39f, 1f));
            }
        }

        private static void BuildLab7SpawnPoints(Transform parent)
        {
            GameObject spawnRoot = new GameObject("SpawnPoints");
            spawnRoot.transform.SetParent(parent);

            const int spawnCount = 12;
            const float radius = 33f;
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = (Mathf.PI * 2f / spawnCount) * i;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);

                GameObject point = new GameObject("SpawnPoint_" + i);
                point.transform.SetParent(spawnRoot.transform);
                point.transform.position = pos;
            }
        }

        private static void BuildVillage(Transform root)
        {
            CreateBlock(root, new Vector3(0f, 0.5f, -10f), new Vector3(18f, 1f, 12f), new Color(0.55f, 0.52f, 0.45f, 1f));
            CreateBlock(root, new Vector3(-12f, 1.5f, -14f), new Vector3(6f, 3f, 6f), new Color(0.65f, 0.58f, 0.46f, 1f));
            CreateBlock(root, new Vector3(12f, 1.5f, -14f), new Vector3(6f, 3f, 6f), new Color(0.62f, 0.54f, 0.44f, 1f));
            CreateBlock(root, new Vector3(0f, 1.5f, -20f), new Vector3(7f, 3f, 5f), new Color(0.58f, 0.51f, 0.42f, 1f));
        }

        private static void BuildResourceFields(Transform root)
        {
            GameObject treesRoot = new GameObject("Trees");
            treesRoot.transform.SetParent(root, false);

            for (int i = 0; i < 24; i++)
            {
                float x = UnityEngine.Random.Range(-38f, 38f);
                float z = UnityEngine.Random.Range(-38f, 38f);

                if (z > -24f && z < -2f && Mathf.Abs(x) < 20f)
                {
                    z += 26f;
                }

                GameObject tree = CreateTree(new Vector3(x, 0f, z));
                tree.transform.SetParent(treesRoot.transform, true);
            }

            GameObject rocksRoot = new GameObject("Rocks");
            rocksRoot.transform.SetParent(root, false);

            for (int i = 0; i < 18; i++)
            {
                float x = UnityEngine.Random.Range(-40f, 40f);
                float z = UnityEngine.Random.Range(-40f, 40f);

                if (z > -24f && z < -2f && Mathf.Abs(x) < 20f)
                {
                    z -= 24f;
                }

                GameObject rock = CreateRock(new Vector3(x, 0f, z));
                rock.transform.SetParent(rocksRoot.transform, true);
            }
        }

        private static GameObject CreateTree(Vector3 position)
        {
            GameObject root = new GameObject("Tree");
            root.transform.position = position;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            trunk.transform.localScale = new Vector3(0.55f, 1.6f, 0.55f);
            SetColor(trunk, new Color(0.46f, 0.3f, 0.15f, 1f));

            GameObject crownA = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crownA.transform.SetParent(root.transform, false);
            crownA.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            crownA.transform.localScale = new Vector3(2.5f, 2f, 2.5f);
            SetColor(crownA, new Color(0.2f, 0.52f, 0.21f, 1f));

            GameObject crownB = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crownB.transform.SetParent(root.transform, false);
            crownB.transform.localPosition = new Vector3(0.9f, 3.9f, 0.5f);
            crownB.transform.localScale = new Vector3(1.8f, 1.6f, 1.8f);
            SetColor(crownB, new Color(0.22f, 0.56f, 0.23f, 1f));

            ResourceNode node = root.AddComponent<ResourceNode>();
            SphereCollider rootCollider = root.AddComponent<SphereCollider>();
            rootCollider.center = new Vector3(0f, 2.2f, 0f);
            rootCollider.radius = 1.8f;
            node.Configure(ResourceType.Wood, 1);

            return root;
        }

        private static GameObject CreateRock(Vector3 position)
        {
            GameObject root = new GameObject("Rock");
            root.transform.position = position;

            GameObject basePart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePart.transform.SetParent(root.transform, false);
            basePart.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            basePart.transform.localScale = new Vector3(1.8f, 1.3f, 1.5f);
            SetColor(basePart, new Color(0.52f, 0.55f, 0.6f, 1f));

            GameObject topPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topPart.transform.SetParent(root.transform, false);
            topPart.transform.localPosition = new Vector3(0.2f, 1.25f, 0.1f);
            topPart.transform.localScale = new Vector3(1.3f, 0.9f, 1.1f);
            topPart.transform.localRotation = Quaternion.Euler(0f, 27f, 0f);
            SetColor(topPart, new Color(0.48f, 0.5f, 0.55f, 1f));

            ResourceNode node = root.AddComponent<ResourceNode>();
            SphereCollider rootCollider = root.AddComponent<SphereCollider>();
            rootCollider.center = new Vector3(0f, 0.8f, 0f);
            rootCollider.radius = 1.4f;
            node.Configure(ResourceType.Stone, 1);

            return root;
        }

        private static void CreateBlock(Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            SetColor(block, color);
        }

        private static void CreateRamp(Transform parent, Vector3 position, Vector3 scale, float xRotation)
        {
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.transform.SetParent(parent);
            ramp.transform.position = position;
            ramp.transform.localScale = scale;
            ramp.transform.rotation = Quaternion.Euler(xRotation, 0f, 0f);
            SetColor(ramp, new Color(0.24f, 0.25f, 0.30f, 1f));
        }

        private static void SetColor(GameObject gameObject, Color color)
        {
            Renderer rendererComp = gameObject.GetComponent<Renderer>();
            if (rendererComp != null)
            {
                rendererComp.material.color = color;
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }

        private static void EnsureLighting()
        {
            Light main = FindObjectOfType<Light>();
            if (main == null)
            {
                GameObject lightObj = new GameObject("Directional Light", typeof(Light));
                main = lightObj.GetComponent<Light>();
                main.type = LightType.Directional;
            }

            main.intensity = 1.15f;
            main.color = Color.white;
            main.transform.rotation = Quaternion.Euler(44f, -34f, 0f);

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.62f, 0.74f, 0.82f, 1f);
            }
        }
    }
}
