using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lab8
{
    public class UiRoot : MonoBehaviour
    {
        private class HotbarSlot
        {
            public Image icon;
            public Image cooldownOverlay;
            public Text cooldownText;
            public Text manaText;
            public Text keyText;
            public Text nameText;
            public AbilityDefinition ability;
        }

        private class TalentNodeView
        {
            public TalentDefinition definition;
            public GameObject root;
            public Image background;
            public Text stateText;
            public Button button;
        }

        public static UiRoot Instance { get; private set; }

        public bool IsBlockingGameplayInput =>
            IsActive(menuPanel) ||
            IsActive(dialoguePanel) ||
            IsActive(deathPanel) ||
            IsActive(inventoryPanel) ||
            IsActive(questPanel) ||
            IsActive(craftPanel) ||
            IsActive(talentPanel);

        private GameDirector director;

        private Canvas canvas;
        private Font font;

        private Text hudStatsText;
        private Text hudModeText;
        private Text hudWaveText;
        private Text promptText;
        private Text hintText;
        private GameObject hotbarRoot;

        private GameObject menuPanel;
        private Text menuTitleText;

        private GameObject inventoryPanel;
        private Text inventoryText;

        private GameObject questPanel;
        private Text questText;

        private GameObject craftPanel;
        private Text craftText;
        private Text craftMessageText;

        private GameObject talentPanel;
        private Text talentPointsText;
        private Text talentStatsText;

        private GameObject dialoguePanel;
        private Text dialogueTitleText;
        private Text dialogueMessageText;

        private GameObject deathPanel;

        private readonly List<Button> dialogueButtons = new List<Button>();
        private readonly List<Text> dialogueButtonTexts = new List<Text>();
        private readonly List<Button> craftButtons = new List<Button>();
        private readonly List<TalentNodeView> talentNodeViews = new List<TalentNodeView>();
        private readonly List<HotbarSlot> hotbarSlots = new List<HotbarSlot>();
        private readonly HashSet<string> acknowledgedQuestRewardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

        public void Initialize(GameDirector gameDirector)
        {
            director = gameDirector;
            BuildUiIfNeeded();
            BuildHotbar();
            BuildTalentNodesIfNeeded();
            BuildCraftButtons();
            RefreshAll();
            HideAllModalPanels();

            if (director != null)
            {
                director.StateChanged -= RefreshAll;
                director.StateChanged += RefreshAll;
            }

            if (director != null && director.Inventory != null)
            {
                director.Inventory.Changed -= RefreshAll;
                director.Inventory.Changed += RefreshAll;
            }

            if (director != null && director.Quests != null)
            {
                director.Quests.Changed -= RefreshAll;
                director.Quests.Changed += RefreshAll;
            }

            if (director != null && director.PlayerStats != null)
            {
                director.PlayerStats.Changed -= RefreshAll;
                director.PlayerStats.Changed += RefreshAll;
            }

            if (director != null && director.Abilities != null)
            {
                director.Abilities.Changed -= RefreshAll;
                director.Abilities.Changed += RefreshAll;
            }

            if (director != null && director.Talents != null)
            {
                director.Talents.Changed -= RefreshAll;
                director.Talents.Changed += RefreshAll;
            }
        }

        private void Update()
        {
            if (director == null)
            {
                return;
            }

            bool lockToggles = IsActive(menuPanel) || IsActive(dialoguePanel) || IsActive(deathPanel);

            if (!lockToggles && Input.GetKeyDown(KeyCode.I) && director.CurrentMode != GameMode.None)
            {
                TogglePanel(inventoryPanel);
            }

            if (!lockToggles && Input.GetKeyDown(KeyCode.J) && director.CurrentMode != GameMode.None)
            {
                TogglePanel(questPanel);
            }

            if (!lockToggles && Input.GetKeyDown(KeyCode.K) && director.CurrentMode != GameMode.None)
            {
                TogglePanel(craftPanel);
                if (IsActive(craftPanel))
                {
                    BuildCraftButtons();
                }
            }

            if (!lockToggles && Input.GetKeyDown(KeyCode.T) && director.CurrentMode != GameMode.None)
            {
                TogglePanel(talentPanel);
            }

            RefreshHud();
            RefreshHotbar();
            RefreshQuestPanel();
            RefreshInventoryPanel();
            RefreshCraftPanel();
            RefreshTalentPanel();
        }

        public void ShowMainMenuAtStart()
        {
            if (menuPanel == null)
            {
                return;
            }

            HideAllModalPanels();
            menuPanel.SetActive(true);
            menuTitleText.text = "Главное меню";
            director.SetPaused(true);
            SetCursorVisible(true);
        }

        public void OnProgressReset()
        {
            acknowledgedQuestRewardIds.Clear();

            if (craftMessageText != null)
            {
                craftMessageText.text = string.Empty;
            }

            ShowMainMenuAtStart();
        }

        public void TogglePauseMenu()
        {
            if (director == null || deathPanel == null)
            {
                return;
            }

            if (IsActive(deathPanel))
            {
                return;
            }

            if (IsActive(menuPanel))
            {
                if (director.CurrentMode == GameMode.None)
                {
                    return;
                }

                menuPanel.SetActive(false);
                if (!AnyOtherModalOpen())
                {
                    director.SetPaused(false);
                    SetCursorVisible(false);
                }

                return;
            }

            menuTitleText.text = "Пауза";
            menuPanel.SetActive(true);
            director.SetPaused(true);
            SetCursorVisible(true);
        }

        public void HandleEscape()
        {
            if (director == null)
            {
                return;
            }

            if (IsActive(deathPanel))
            {
                return;
            }

            if (IsActive(dialoguePanel))
            {
                CloseDialogue();
                return;
            }

            if (IsActive(talentPanel))
            {
                talentPanel.SetActive(false);
                if (director.CurrentMode == GameMode.None)
                {
                    menuTitleText.text = "Главное меню";
                    menuPanel.SetActive(true);
                    director.SetPaused(true);
                    SetCursorVisible(true);
                }
                else
                {
                    director.SetPaused(false);
                    SetCursorVisible(false);
                }

                return;
            }

            if (IsActive(inventoryPanel) || IsActive(questPanel) || IsActive(craftPanel))
            {
                inventoryPanel.SetActive(false);
                questPanel.SetActive(false);
                craftPanel.SetActive(false);

                if (director.CurrentMode != GameMode.None)
                {
                    director.SetPaused(false);
                    SetCursorVisible(false);
                }

                return;
            }

            TogglePauseMenu();
        }

        public void ShowNpcDialogue(Npc npc)
        {
            if (npc == null || dialoguePanel == null)
            {
                return;
            }

            HideAllModalPanels();
            dialoguePanel.SetActive(true);
            director.SetPaused(true);
            SetCursorVisible(true);

            dialogueTitleText.text = npc.NpcName;

            for (int i = 0; i < dialogueButtons.Count; i++)
            {
                dialogueButtons[i].onClick.RemoveAllListeners();
                dialogueButtons[i].gameObject.SetActive(false);
            }

            if (npc.Role == NpcRole.Narrator)
            {
                ConfigureNarratorDialogue();
                return;
            }

            ConfigureQuestGiverDialogue(npc);
        }

        public void ShowDeathScreen()
        {
            HideAllModalPanels();
            deathPanel.SetActive(true);
            SetCursorVisible(true);
            director.SetPaused(true);
        }

        public void ShowRescueDialogue()
        {
            if (director == null || dialoguePanel == null)
            {
                return;
            }

            HideAllModalPanels();
            dialoguePanel.SetActive(true);
            SetCursorVisible(true);
            director.SetPaused(true);

            dialogueTitleText.text = "Рассказчик";
            dialogueMessageText.text = "Мы еле-еле тебя спасли. В качестве оплаты забрали все твои ресурсы.";

            for (int i = 0; i < dialogueButtons.Count; i++)
            {
                dialogueButtons[i].onClick.RemoveAllListeners();
                dialogueButtons[i].gameObject.SetActive(false);
            }

            SetDialogueButton(0, "Понял", CloseDialogue);
        }

        public void HideDeathScreen()
        {
            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
            }

            if (!AnyOtherModalOpen())
            {
                SetCursorVisible(false);
            }

            RefreshAll();
        }

        public void SetInteractionPrompt(string prompt)
        {
            if (promptText == null)
            {
                return;
            }

            promptText.text = string.IsNullOrWhiteSpace(prompt) ? string.Empty : prompt;
        }

        private void ConfigureNarratorDialogue()
        {
            if (director.NarratorSkillsGiven)
            {
                dialogueMessageText.text = "Ты уже владеешь тремя боевыми навыками. Открой хотбар и используй 1, 2, 3.";
                SetDialogueButton(0, "Понял", CloseDialogue);
                return;
            }

            dialogueMessageText.text = "Добро пожаловать в объединённый проект лабораторных работ.\nЯ выдаю тебе 3 способности и отправляю к жителю за заданиями.";
            SetDialogueButton(0, "Получить навыки", () =>
            {
                director.GrantNarratorSkills();
                CloseDialogue();
            });
            SetDialogueButton(1, "Позже", CloseDialogue);
        }

        private void ConfigureQuestGiverDialogue(Npc npc)
        {
            QuestSystem questSystem = director.Quests;
            if (questSystem == null)
            {
                dialogueMessageText.text = "Система квестов недоступна.";
                SetDialogueButton(0, "Закрыть", CloseDialogue);
                return;
            }

            List<QuestDefinition> newlyCompleted = new List<QuestDefinition>();
            if (npc.QuestIds != null)
            {
                for (int i = 0; i < npc.QuestIds.Length; i++)
                {
                    string questId = npc.QuestIds[i];
                    if (string.IsNullOrWhiteSpace(questId))
                    {
                        continue;
                    }

                    if (!questSystem.IsQuestCompleted(questId) || acknowledgedQuestRewardIds.Contains(questId))
                    {
                        continue;
                    }

                    for (int q = 0; q < questSystem.AllQuests.Count; q++)
                    {
                        QuestDefinition def = questSystem.AllQuests[q];
                        if (def != null && string.Equals(def.id, questId, StringComparison.OrdinalIgnoreCase))
                        {
                            newlyCompleted.Add(def);
                            break;
                        }
                    }
                }
            }

            if (newlyCompleted.Count > 0)
            {
                StringBuilder rewardSb = new StringBuilder();
                rewardSb.AppendLine("Молодец, задание выполнено.");
                rewardSb.AppendLine("Награда уже начислена:");
                for (int i = 0; i < newlyCompleted.Count; i++)
                {
                    rewardSb.AppendLine("- " + newlyCompleted[i].title);
                }

                dialogueMessageText.text = rewardSb.ToString().TrimEnd();
                SetDialogueButton(0, "Принять", () =>
                {
                    for (int i = 0; i < newlyCompleted.Count; i++)
                    {
                        if (newlyCompleted[i] != null && !string.IsNullOrWhiteSpace(newlyCompleted[i].id))
                        {
                            acknowledgedQuestRewardIds.Add(newlyCompleted[i].id);
                        }
                    }

                    CloseDialogue();
                });

                return;
            }

            List<QuestDefinition> startable = questSystem.GetStartableQuests(npc.QuestIds);
            bool allCompleted = questSystem.AreAllQuestsCompleted(npc.QuestIds);

            if (startable.Count == 0)
            {
                dialogueMessageText.text = allCompleted
                    ? "Спасибо, герой. Ты выполнил все мои квесты."
                    : "Новых поручений сейчас нет. Выполни активные задания и возвращайся.";

                SetDialogueButton(0, "Хорошо", CloseDialogue);
                return;
            }

            dialogueMessageText.text = "Выбери квест:";
            int buttonIndex = 0;
            for (int i = 0; i < startable.Count && buttonIndex < dialogueButtons.Count - 1; i++)
            {
                QuestDefinition quest = startable[i];
                SetDialogueButton(buttonIndex, "Взять: " + quest.title, () =>
                {
                    questSystem.StartQuest(quest.id);
                    CloseDialogue();
                });
                buttonIndex++;
            }

            SetDialogueButton(buttonIndex, "Закрыть", CloseDialogue);
        }

        private void SetDialogueButton(int index, string label, UnityEngine.Events.UnityAction action)
        {
            if (index < 0 || index >= dialogueButtons.Count)
            {
                return;
            }

            Button button = dialogueButtons[index];
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            dialogueButtonTexts[index].text = label;
        }

        private void CloseDialogue()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (director != null && director.CurrentMode != GameMode.None)
            {
                director.SetPaused(false);
                SetCursorVisible(false);
            }
            else
            {
                director.SetPaused(true);
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshHud();
            RefreshHotbar();
            RefreshInventoryPanel();
            RefreshQuestPanel();
            RefreshCraftPanel();
            RefreshTalentPanel();
        }

        private void RefreshHud()
        {
            if (director == null || hudStatsText == null)
            {
                return;
            }

            PlayerStats stats = director.PlayerStats;
            if (stats != null)
            {
                hudStatsText.text =
                    "HP: " + stats.CurrentHealth.ToString("0") + "/" + stats.MaxHealth.ToString("0") +
                    "   MP: " + stats.CurrentMana.ToString("0") + "/" + stats.MaxMana.ToString("0") +
                    "   Уровень: " + stats.Level +
                    "   Опыт: " + stats.CurrentExp + "/" + stats.ExpToNext;
            }

            string modeName = director.CurrentMode == GameMode.Combat ? "Опасный" : director.CurrentMode == GameMode.Peaceful ? "Мирный" : "Не выбран";
            hudModeText.text = "Режим: " + modeName;

            if (director.Arena != null && director.CurrentMode == GameMode.Combat)
            {
                hudWaveText.text =
                    "Волна: " + director.Arena.CurrentWave +
                    "   Врагов: " + director.Arena.AliveEnemies +
                    "   Убийств: " + director.Arena.TotalKills +
                    (director.Arena.AliveEnemies == 0 && director.Arena.NextWaveTimer > 0f
                        ? "   Следующая: " + director.Arena.NextWaveTimer.ToString("0.0") + "с"
                        : "");
            }
            else
            {
                hudWaveText.text = "Волны: выключены";
            }

            hintText.text =
                "Управление:\n" +
                "WASD/Space - движение\n" +
                "ЛКМ - удар/добыча, E - диалог\n" +
                "1,2,3 - способности, I/J/K/T - окна, Esc - меню";
        }

        private void RefreshHotbar()
        {
            if (director == null || director.Abilities == null)
            {
                return;
            }

            bool shouldShow =
                director.CurrentMode != GameMode.None &&
                director.Abilities.Unlocked &&
                !IsActive(menuPanel) &&
                !IsActive(dialoguePanel) &&
                !IsActive(deathPanel);

            if (hotbarRoot != null)
            {
                hotbarRoot.SetActive(shouldShow);
            }

            if (!shouldShow)
            {
                return;
            }

            IReadOnlyList<AbilityDefinition> abilities = director.Abilities.Abilities;
            for (int i = 0; i < hotbarSlots.Count; i++)
            {
                HotbarSlot slot = hotbarSlots[i];
                if (slot == null || i >= abilities.Count)
                {
                    continue;
                }

                AbilityDefinition ability = abilities[i];
                slot.ability = ability;

                slot.icon.color = director.Abilities.Unlocked ? ability.color : new Color(0.28f, 0.28f, 0.28f, 1f);
                slot.nameText.text = ability.title;
                slot.keyText.text = (i + 1).ToString();
                slot.manaText.text = ability.manaCost.ToString("0");

                float remaining = director.Abilities.GetCooldownRemaining(ability.id);
                float ratio = ability.cooldown > 0f ? remaining / ability.cooldown : 0f;
                slot.cooldownOverlay.fillAmount = Mathf.Clamp01(ratio);
                slot.cooldownText.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
            }
        }

        private void RefreshInventoryPanel()
        {
            if (inventoryText == null || director == null || director.Inventory == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Инвентарь");
            sb.AppendLine();
            sb.Append(director.Inventory.BuildSummary());
            inventoryText.text = sb.ToString();
        }

        private void RefreshQuestPanel()
        {
            if (questText == null || director == null || director.Quests == null)
            {
                return;
            }

            questText.text = director.Quests.BuildJournalText();
        }

        private void RefreshCraftPanel()
        {
            if (craftText == null || director == null || director.Crafting == null || director.Inventory == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Крафт");
            sb.AppendLine("Выбери рецепт кнопкой ниже.");
            sb.AppendLine();

            IReadOnlyList<CraftRecipe> recipes = director.Crafting.Recipes;
            for (int i = 0; i < recipes.Count; i++)
            {
                CraftRecipe recipe = recipes[i];
                sb.Append((i + 1) + ". " + recipe.title + " — " + recipe.description).AppendLine();
                sb.Append("   Нужно: ");
                if (recipe.inputs != null)
                {
                    for (int j = 0; j < recipe.inputs.Length; j++)
                    {
                        RecipeIngredient input = recipe.inputs[j];
                        if (input == null)
                        {
                            continue;
                        }

                        if (j > 0)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(InventorySystem.ResolveItemName(input.itemId)).Append(" x").Append(input.amount);
                    }
                }

                sb.AppendLine();
            }

            craftText.text = sb.ToString().TrimEnd();
        }

        private void RefreshTalentPanel()
        {
            if (director == null || director.Talents == null || director.PlayerStats == null || talentPointsText == null)
            {
                return;
            }

            talentPointsText.text = "Очки талантов: " + director.Talents.AvailablePoints;

            PlayerStats stats = director.PlayerStats;
            talentStatsText.text =
                "Текущие статы: Урон " + stats.Damage.ToString("0") +
                " | Скорость " + stats.MoveSpeed.ToString("0.0") +
                " | Реген маны " + stats.ManaRegen.ToString("0.0");

            for (int i = 0; i < talentNodeViews.Count; i++)
            {
                TalentNodeView view = talentNodeViews[i];
                if (view == null || view.definition == null)
                {
                    continue;
                }

                bool learned = director.Talents.IsLearned(view.definition.id);
                bool canLearn = director.Talents.CanLearn(view.definition);

                if (learned)
                {
                    view.background.color = new Color(0.2f, 0.55f, 0.24f, 1f);
                    view.stateText.text = "Изучено";
                    view.button.interactable = false;
                }
                else if (canLearn)
                {
                    view.background.color = new Color(0.21f, 0.36f, 0.64f, 1f);
                    view.stateText.text = "Доступно";
                    view.button.interactable = true;
                }
                else
                {
                    view.background.color = new Color(0.28f, 0.28f, 0.28f, 1f);
                    view.stateText.text = "Недоступно";
                    view.button.interactable = false;
                }
            }
        }

        private void TogglePanel(GameObject panel)
        {
            if (panel == null || director == null)
            {
                return;
            }

            bool willOpen = !panel.activeSelf;

            if (willOpen)
            {
                if (panel != inventoryPanel) inventoryPanel.SetActive(false);
                if (panel != questPanel) questPanel.SetActive(false);
                if (panel != craftPanel) craftPanel.SetActive(false);
                if (panel != talentPanel) talentPanel.SetActive(false);
                if (panel != dialoguePanel) dialoguePanel.SetActive(false);

                panel.SetActive(true);
                director.SetPaused(true);
                SetCursorVisible(true);
            }
            else
            {
                panel.SetActive(false);
                if (!AnyOtherModalOpen() && !IsActive(menuPanel) && director.CurrentMode != GameMode.None)
                {
                    director.SetPaused(false);
                    SetCursorVisible(false);
                }
            }
        }

        private bool AnyOtherModalOpen()
        {
            return IsActive(inventoryPanel) || IsActive(questPanel) || IsActive(craftPanel) || IsActive(talentPanel) || IsActive(dialoguePanel) || IsActive(deathPanel);
        }

        private void BuildUiIfNeeded()
        {
            if (canvas != null)
            {
                return;
            }

            Canvas existing = FindObjectOfType<Canvas>();
            if (existing != null)
            {
                canvas = existing;
            }
            else
            {
                GameObject canvasObj = new GameObject("Lab8_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            BuildHud();
            BuildMenu();
            BuildInventoryPanel();
            BuildQuestPanel();
            BuildCraftPanel();
            BuildTalentPanel();
            BuildDialoguePanel();
            BuildDeathPanel();
        }

        private void BuildHud()
        {
            GameObject hudRoot = new GameObject("HUD", typeof(RectTransform));
            hudRoot.transform.SetParent(canvas.transform, false);
            RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            CreateHudBacking(hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(640f, 78f), new Vector2(12f, -12f), new Vector2(0f, 1f), new Color(0f, 0f, 0f, 0.36f));
            CreateHudBacking(hudRoot.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(720f, 36f), new Vector2(-12f, -12f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0.36f));
            CreateHudBacking(hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(760f, 104f), new Vector2(12f, -84f), new Vector2(0f, 1f), new Color(0f, 0f, 0f, 0.34f));
            CreateHudBacking(hudRoot.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(780f, 44f), new Vector2(12f, 12f), new Vector2(0f, 0f), new Color(0f, 0f, 0f, 0.38f));

            hudStatsText = CreateText(hudRoot.transform, 22, FontStyle.Bold, TextAnchor.UpperLeft, Vector2.zero, new Vector2(900f, 32f), new Vector2(20f, -18f), new Vector2(0f, 1f));
            hudModeText = CreateText(hudRoot.transform, 20, FontStyle.Normal, TextAnchor.UpperLeft, Vector2.zero, new Vector2(540f, 30f), new Vector2(20f, -52f), new Vector2(0f, 1f));
            hudWaveText = CreateText(hudRoot.transform, 20, FontStyle.Normal, TextAnchor.UpperRight, Vector2.zero, new Vector2(860f, 30f), new Vector2(-20f, -18f), new Vector2(1f, 1f));
            hintText = CreateText(hudRoot.transform, 18, FontStyle.Normal, TextAnchor.UpperLeft, Vector2.zero, new Vector2(720f, 96f), new Vector2(20f, -88f), new Vector2(0f, 1f));

            promptText = CreateText(hudRoot.transform, 22, FontStyle.Bold, TextAnchor.LowerLeft, Vector2.zero, new Vector2(760f, 36f), new Vector2(20f, 20f), new Vector2(0f, 0f));

            CreateCrosshair(hudRoot.transform);
        }

        private static void CreateHudBacking(
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2 anchoredPos,
            Vector2 pivot,
            Color color)
        {
            GameObject backing = new GameObject("HudBacking", typeof(RectTransform), typeof(Image));
            backing.transform.SetParent(parent, false);

            RectTransform rt = backing.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            Image image = backing.GetComponent<Image>();
            image.color = color;
        }

        private void BuildMenu()
        {
            menuPanel = CreatePanel(canvas.transform, "MenuPanel", new Vector2(520f, 600f), new Color(0f, 0f, 0f, 0.88f));
            SetAnchoredCenter(menuPanel.GetComponent<RectTransform>(), Vector2.zero);

            menuTitleText = CreateText(menuPanel.transform, 44, FontStyle.Bold, TextAnchor.UpperCenter, Vector2.zero, new Vector2(480f, 60f), new Vector2(0f, -24f), new Vector2(0.5f, 1f));

            CreateButton(menuPanel.transform, "Мирный уровень", new Vector2(420f, 64f), new Vector2(0f, -130f), () =>
            {
                director.StartMode(GameMode.Peaceful);
                menuPanel.SetActive(false);
                director.SetPaused(false);
                SetCursorVisible(false);
            });

            CreateButton(menuPanel.transform, "Уровень со врагами", new Vector2(420f, 64f), new Vector2(0f, -206f), () =>
            {
                director.StartMode(GameMode.Combat);
                menuPanel.SetActive(false);
                director.SetPaused(false);
                SetCursorVisible(false);
            });

            CreateButton(menuPanel.transform, "Дерево талантов", new Vector2(420f, 64f), new Vector2(0f, -282f), () =>
            {
                menuPanel.SetActive(false);
                talentPanel.SetActive(true);
                director.SetPaused(true);
                SetCursorVisible(true);
            });

            CreateButton(menuPanel.transform, "Сбросить прогресс", new Vector2(420f, 64f), new Vector2(0f, -358f), () =>
            {
                director.ResetAllProgress();
            });

            CreateButton(menuPanel.transform, "Выход", new Vector2(420f, 64f), new Vector2(0f, -434f), () =>
            {
                director.SaveGame();
                RequestExit();
            });
        }

        private void BuildInventoryPanel()
        {
            inventoryPanel = CreatePanel(canvas.transform, "InventoryPanel", new Vector2(520f, 620f), new Color(0f, 0f, 0f, 0.85f));
            SetAnchorRight(inventoryPanel.GetComponent<RectTransform>(), new Vector2(-18f, 0f));
            inventoryText = CreateText(inventoryPanel.transform, 21, FontStyle.Normal, TextAnchor.UpperLeft, Vector2.zero, new Vector2(490f, 590f), new Vector2(14f, -14f), new Vector2(0f, 1f));
        }

        private void BuildQuestPanel()
        {
            questPanel = CreatePanel(canvas.transform, "QuestPanel", new Vector2(700f, 760f), new Color(0f, 0f, 0f, 0.9f));
            SetAnchorRight(questPanel.GetComponent<RectTransform>(), new Vector2(-560f, 0f));
            questText = CreateText(questPanel.transform, 20, FontStyle.Normal, TextAnchor.UpperLeft, Vector2.zero, new Vector2(670f, 730f), new Vector2(14f, -14f), new Vector2(0f, 1f));
        }

        private void BuildCraftPanel()
        {
            craftPanel = CreatePanel(canvas.transform, "CraftPanel", new Vector2(720f, 760f), new Color(0f, 0f, 0f, 0.9f));
            SetAnchorLeft(craftPanel.GetComponent<RectTransform>(), new Vector2(18f, 0f));

            craftText = CreateText(craftPanel.transform, 19, FontStyle.Normal, TextAnchor.UpperLeft, Vector2.zero, new Vector2(690f, 420f), new Vector2(14f, -12f), new Vector2(0f, 1f));
            craftMessageText = CreateText(craftPanel.transform, 21, FontStyle.Bold, TextAnchor.UpperLeft, Vector2.zero, new Vector2(690f, 40f), new Vector2(14f, -436f), new Vector2(0f, 1f));
        }

        private void BuildTalentPanel()
        {
            talentPanel = CreatePanel(canvas.transform, "TalentPanel", new Vector2(1840f, 980f), new Color(0f, 0f, 0f, 0.93f));
            SetAnchoredCenter(talentPanel.GetComponent<RectTransform>(), Vector2.zero);

            talentPointsText = CreateText(talentPanel.transform, 30, FontStyle.Bold, TextAnchor.UpperLeft, Vector2.zero, new Vector2(600f, 40f), new Vector2(20f, -18f), new Vector2(0f, 1f));
            talentStatsText = CreateText(talentPanel.transform, 20, FontStyle.Normal, TextAnchor.UpperLeft, Vector2.zero, new Vector2(1760f, 42f), new Vector2(20f, -58f), new Vector2(0f, 1f));

            CreateButton(talentPanel.transform, "Закрыть", new Vector2(150f, 44f), new Vector2(820f, -16f), () =>
            {
                talentPanel.SetActive(false);
                if (director.CurrentMode == GameMode.None)
                {
                    menuTitleText.text = "Главное меню";
                    menuPanel.SetActive(true);
                    director.SetPaused(true);
                    SetCursorVisible(true);
                }
                else if (!AnyOtherModalOpen() && !IsActive(menuPanel))
                {
                    director.SetPaused(false);
                    SetCursorVisible(false);
                }
            });
        }

        private void BuildDialoguePanel()
        {
            dialoguePanel = CreatePanel(canvas.transform, "DialoguePanel", new Vector2(840f, 430f), new Color(0f, 0f, 0f, 0.9f));
            SetAnchoredCenter(dialoguePanel.GetComponent<RectTransform>(), Vector2.zero);

            dialogueTitleText = CreateText(dialoguePanel.transform, 36, FontStyle.Bold, TextAnchor.UpperCenter, Vector2.zero, new Vector2(800f, 52f), new Vector2(0f, -10f), new Vector2(0.5f, 1f));
            dialogueMessageText = CreateText(dialoguePanel.transform, 24, FontStyle.Normal, TextAnchor.UpperLeft, Vector2.zero, new Vector2(780f, 130f), new Vector2(0f, -66f), new Vector2(0.5f, 1f));

            for (int i = 0; i < 6; i++)
            {
                Button button = CreateButton(
                    dialoguePanel.transform,
                    "Вариант",
                    new Vector2(780f, 44f),
                    new Vector2(0f, -210f - i * 48f),
                    null);

                dialogueButtons.Add(button);
                dialogueButtonTexts.Add(button.GetComponentInChildren<Text>());
                button.gameObject.SetActive(false);
            }
        }

        private void BuildDeathPanel()
        {
            deathPanel = CreatePanel(canvas.transform, "DeathPanel", new Vector2(640f, 320f), new Color(0f, 0f, 0f, 0.92f));
            SetAnchoredCenter(deathPanel.GetComponent<RectTransform>(), Vector2.zero);

            Text title = CreateText(deathPanel.transform, 46, FontStyle.Bold, TextAnchor.UpperCenter, Vector2.zero, new Vector2(600f, 60f), new Vector2(0f, -24f), new Vector2(0.5f, 1f));
            title.text = "Вы погибли";

            Text subtitle = CreateText(deathPanel.transform, 24, FontStyle.Normal, TextAnchor.UpperCenter, Vector2.zero, new Vector2(560f, 44f), new Vector2(0f, -100f), new Vector2(0.5f, 1f));
            subtitle.text = "Нажмите, чтобы возродиться";

            CreateButton(deathPanel.transform, "Возродиться", new Vector2(320f, 66f), new Vector2(0f, -190f), () =>
            {
                director.RespawnPlayer();
            });
        }

        private void BuildHotbar()
        {
            if (director == null || director.Abilities == null)
            {
                return;
            }

            Transform old = canvas.transform.Find("HotbarRoot");
            if (old != null)
            {
                Destroy(old.gameObject);
                hotbarSlots.Clear();
            }

            hotbarRoot = CreatePanel(canvas.transform, "HotbarRoot", new Vector2(600f, 132f), new Color(0f, 0f, 0f, 0.64f));
            RectTransform br = hotbarRoot.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 0f);
            br.anchorMax = new Vector2(0.5f, 0f);
            br.pivot = new Vector2(0.5f, 0f);
            br.anchoredPosition = new Vector2(0f, 12f);

            GameObject container = new GameObject("Slots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            container.transform.SetParent(hotbarRoot.transform, false);

            RectTransform cr = container.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0.5f, 0.5f);
            cr.anchorMax = new Vector2(0.5f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.sizeDelta = new Vector2(560f, 106f);

            HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            IReadOnlyList<AbilityDefinition> abilities = director.Abilities.Abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                GameObject slotObj = new GameObject("Slot_" + (i + 1), typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                slotObj.transform.SetParent(container.transform, false);

                LayoutElement le = slotObj.GetComponent<LayoutElement>();
                le.preferredWidth = 102f;
                le.preferredHeight = 102f;

                Image slotBg = slotObj.GetComponent<Image>();
                slotBg.color = new Color(0.1f, 0.1f, 0.12f, 1f);

                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(slotObj.transform, false);
                RectTransform ir = iconObj.GetComponent<RectTransform>();
                ir.anchorMin = new Vector2(0.5f, 0.5f);
                ir.anchorMax = new Vector2(0.5f, 0.5f);
                ir.pivot = new Vector2(0.5f, 0.5f);
                ir.sizeDelta = new Vector2(82f, 82f);
                ir.anchoredPosition = new Vector2(0f, 5f);
                Image icon = iconObj.GetComponent<Image>();

                GameObject overlayObj = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
                overlayObj.transform.SetParent(slotObj.transform, false);
                RectTransform or = overlayObj.GetComponent<RectTransform>();
                or.anchorMin = ir.anchorMin;
                or.anchorMax = ir.anchorMax;
                or.pivot = ir.pivot;
                or.sizeDelta = ir.sizeDelta;
                or.anchoredPosition = ir.anchoredPosition;
                Image overlay = overlayObj.GetComponent<Image>();
                overlay.color = new Color(0f, 0f, 0f, 0.68f);
                overlay.type = Image.Type.Filled;
                overlay.fillMethod = Image.FillMethod.Radial360;
                overlay.fillOrigin = 2;
                overlay.fillAmount = 0f;

                Text cdText = CreateText(slotObj.transform, 26, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(80f, 80f), new Vector2(0f, 5f), new Vector2(0.5f, 0.5f));
                Text keyText = CreateText(slotObj.transform, 16, FontStyle.Bold, TextAnchor.UpperLeft, Vector2.zero, new Vector2(24f, 18f), new Vector2(6f, -4f), new Vector2(0f, 1f));
                Text manaText = CreateText(slotObj.transform, 16, FontStyle.Bold, TextAnchor.LowerRight, Vector2.zero, new Vector2(34f, 18f), new Vector2(-6f, 6f), new Vector2(1f, 0f));
                manaText.color = new Color(0.58f, 0.92f, 1f, 1f);
                Text nameText = CreateText(slotObj.transform, 14, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(94f, 18f), new Vector2(0f, -16f), new Vector2(0.5f, 0f));

                hotbarSlots.Add(new HotbarSlot
                {
                    icon = icon,
                    cooldownOverlay = overlay,
                    cooldownText = cdText,
                    manaText = manaText,
                    keyText = keyText,
                    nameText = nameText,
                    ability = abilities[i]
                });
            }

            hotbarRoot.SetActive(false);
        }

        private void BuildCraftButtons()
        {
            if (craftPanel == null || director == null || director.Crafting == null)
            {
                return;
            }

            for (int i = 0; i < craftButtons.Count; i++)
            {
                if (craftButtons[i] != null)
                {
                    Destroy(craftButtons[i].gameObject);
                }
            }
            craftButtons.Clear();

            IReadOnlyList<CraftRecipe> recipes = director.Crafting.Recipes;
            for (int i = 0; i < recipes.Count; i++)
            {
                CraftRecipe recipe = recipes[i];
                float x = -230f + (i % 2) * 240f;
                float y = -500f - (i / 2) * 56f;

                Button button = CreateButton(craftPanel.transform, "Создать: " + recipe.title, new Vector2(220f, 46f), new Vector2(x, y), () =>
                {
                    bool result = director.Crafting.TryCraft(recipe.id, out string message);
                    craftMessageText.color = result ? new Color(0.62f, 0.95f, 0.62f, 1f) : new Color(1f, 0.62f, 0.62f, 1f);
                    craftMessageText.text = message;
                    RefreshAll();
                });

                craftButtons.Add(button);
            }
        }

        private void BuildTalentNodesIfNeeded()
        {
            if (talentPanel == null || director == null || director.Talents == null || talentNodeViews.Count > 0)
            {
                return;
            }

            IReadOnlyList<TalentDefinition> talents = director.Talents.Talents;
            for (int i = 0; i < talents.Count; i++)
            {
                TalentDefinition talent = talents[i];
                GameObject node = CreatePanel(talentPanel.transform, "Talent_" + talent.id, new Vector2(230f, 116f), new Color(0.2f, 0.2f, 0.2f, 1f));

                RectTransform nr = node.GetComponent<RectTransform>();
                nr.anchorMin = new Vector2(0.5f, 0.5f);
                nr.anchorMax = new Vector2(0.5f, 0.5f);
                nr.pivot = new Vector2(0.5f, 0.5f);
                nr.anchoredPosition = talent.uiPosition;

                Text title = CreateText(node.transform, 17, FontStyle.Bold, TextAnchor.UpperCenter, Vector2.zero, new Vector2(214f, 24f), new Vector2(0f, -6f), new Vector2(0.5f, 1f));
                title.text = talent.title;

                Text desc = CreateText(node.transform, 13, FontStyle.Normal, TextAnchor.UpperLeft, Vector2.zero, new Vector2(214f, 44f), new Vector2(0f, -30f), new Vector2(0.5f, 1f));
                desc.text = talent.description;

                Text cost = CreateText(node.transform, 13, FontStyle.Bold, TextAnchor.LowerRight, Vector2.zero, new Vector2(76f, 18f), new Vector2(-8f, 6f), new Vector2(1f, 0f));
                cost.text = "Цена: " + talent.cost;

                Text state = CreateText(node.transform, 12, FontStyle.Bold, TextAnchor.LowerLeft, Vector2.zero, new Vector2(130f, 18f), new Vector2(8f, 6f), new Vector2(0f, 0f));

                Button button = node.AddComponent<Button>();
                string talentId = talent.id;
                button.onClick.AddListener(() =>
                {
                    director.Talents.Learn(talentId);
                    RefreshTalentPanel();
                });

                talentNodeViews.Add(new TalentNodeView
                {
                    definition = talent,
                    root = node,
                    background = node.GetComponent<Image>(),
                    stateText = state,
                    button = button
                });
            }
        }

        private void HideAllModalPanels()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (questPanel != null) questPanel.SetActive(false);
            if (craftPanel != null) craftPanel.SetActive(false);
            if (talentPanel != null) talentPanel.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (deathPanel != null) deathPanel.SetActive(false);
        }

        private static bool IsActive(GameObject obj)
        {
            return obj != null && obj.activeSelf;
        }

        private static void SetCursorVisible(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private static void RequestExit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel;
        }

        private Text CreateText(Transform parent, int size, FontStyle style, TextAnchor anchor, Vector2 anchorPreset, Vector2 rectSize, Vector2 pos, Vector2 pivot)
        {
            GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            Vector2 anchorPoint = anchorPreset == Vector2.zero ? pivot : anchorPreset;
            rt.anchorMin = anchorPoint;
            rt.anchorMax = anchorPoint;
            rt.pivot = pivot;
            rt.sizeDelta = rectSize;
            rt.anchoredPosition = pos;

            Text text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = obj.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            return text;
        }

        private Button CreateButton(Transform parent, string label, Vector2 size, Vector2 anchoredPos, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObj = new GameObject(label + "_Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent, false);

            RectTransform br = buttonObj.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 1f);
            br.anchorMax = new Vector2(0.5f, 1f);
            br.pivot = new Vector2(0.5f, 1f);
            br.sizeDelta = size;
            br.anchoredPosition = anchoredPos;

            Image bi = buttonObj.GetComponent<Image>();
            bi.color = new Color(0.22f, 0.22f, 0.26f, 1f);

            Button button = buttonObj.GetComponent<Button>();
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            Text text = CreateText(buttonObj.transform, 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(size.x - 10f, size.y - 8f), Vector2.zero, new Vector2(0.5f, 0.5f));
            text.text = label;

            return button;
        }

        private static void SetAnchoredCenter(RectTransform rt, Vector2 anchored)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
        }

        private static void SetAnchorRight(RectTransform rt, Vector2 anchored)
        {
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = anchored;
        }

        private static void SetAnchorLeft(RectTransform rt, Vector2 anchored)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = anchored;
        }

        private static void CreateCrosshair(Transform parent)
        {
            GameObject root = new GameObject("Crosshair", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            RectTransform rr = root.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(0.5f, 0.5f);
            rr.anchorMax = new Vector2(0.5f, 0.5f);
            rr.pivot = new Vector2(0.5f, 0.5f);
            rr.sizeDelta = new Vector2(32f, 32f);

            CreateCrossPart(root.transform, new Vector2(0f, 6f), new Vector2(2f, 8f));
            CreateCrossPart(root.transform, new Vector2(0f, -6f), new Vector2(2f, 8f));
            CreateCrossPart(root.transform, new Vector2(6f, 0f), new Vector2(8f, 2f));
            CreateCrossPart(root.transform, new Vector2(-6f, 0f), new Vector2(8f, 2f));
            CreateCrossPart(root.transform, Vector2.zero, new Vector2(3f, 3f));
        }

        private static void CreateCrossPart(Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject part = new GameObject("Part", typeof(RectTransform), typeof(Image), typeof(Outline));
            part.transform.SetParent(parent, false);

            RectTransform rt = part.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image image = part.GetComponent<Image>();
            image.color = Color.white;

            Outline outline = part.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }
}
