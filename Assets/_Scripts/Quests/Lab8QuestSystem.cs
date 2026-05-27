using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lab8
{
    public class QuestSystem : MonoBehaviour
    {
        private class QuestRuntime
        {
            public QuestDefinition definition;
            public bool isActive;
            public bool isCompleted;
            public int[] progress;
        }

        public static QuestSystem Instance { get; private set; }

        public event Action Changed;

        public IReadOnlyList<QuestDefinition> AllQuests => allQuestDefinitions;

        private readonly List<QuestDefinition> allQuestDefinitions = new List<QuestDefinition>();
        private readonly Dictionary<string, QuestRuntime> questById = new Dictionary<string, QuestRuntime>(StringComparer.OrdinalIgnoreCase);

        private PlayerStats playerStats;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildDefaultQuests();
        }

        private void Start()
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }

        public bool StartQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                return false;
            }

            if (!questById.TryGetValue(questId.Trim(), out QuestRuntime runtime) || runtime == null)
            {
                return false;
            }

            if (runtime.isCompleted || runtime.isActive)
            {
                return false;
            }

            runtime.isActive = true;
            runtime.progress = new int[runtime.definition.goals != null ? runtime.definition.goals.Length : 0];
            Changed?.Invoke();
            return true;
        }

        public bool IsQuestCompleted(string questId)
        {
            if (!questById.TryGetValue(questId, out QuestRuntime runtime) || runtime == null)
            {
                return false;
            }

            return runtime.isCompleted;
        }

        public bool IsQuestActive(string questId)
        {
            if (!questById.TryGetValue(questId, out QuestRuntime runtime) || runtime == null)
            {
                return false;
            }

            return runtime.isActive;
        }

        public List<QuestDefinition> GetStartableQuests(IEnumerable<string> allowedIds)
        {
            List<QuestDefinition> quests = new List<QuestDefinition>();
            if (allowedIds == null)
            {
                return quests;
            }

            foreach (string id in allowedIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!questById.TryGetValue(id.Trim(), out QuestRuntime runtime) || runtime == null)
                {
                    continue;
                }

                if (!runtime.isActive && !runtime.isCompleted)
                {
                    quests.Add(runtime.definition);
                }
            }

            return quests;
        }

        public bool AreAllQuestsCompleted(IEnumerable<string> allowedIds)
        {
            if (allowedIds == null)
            {
                return false;
            }

            bool any = false;
            foreach (string id in allowedIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                any = true;
                if (!questById.TryGetValue(id.Trim(), out QuestRuntime runtime) || runtime == null || !runtime.isCompleted)
                {
                    return false;
                }
            }

            return any;
        }

        public void RegisterCollectedItem(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return;
            }

            UpdateGoals(
                goal => goal.type == QuestGoalType.CollectItem && string.Equals(goal.targetId, itemId, StringComparison.OrdinalIgnoreCase),
                amount,
                0);
        }

        public void RegisterEnemyKilled(string enemyType)
        {
            string target = string.IsNullOrWhiteSpace(enemyType) ? "arena_enemy" : enemyType.Trim();
            UpdateGoals(
                goal => goal.type == QuestGoalType.KillEnemy && string.Equals(goal.targetId, target, StringComparison.OrdinalIgnoreCase),
                1,
                0);
        }

        public void RegisterCombatWaveCleared(int waveNumber)
        {
            UpdateGoals(
                goal => goal.type == QuestGoalType.ClearCombatWave,
                1,
                waveNumber);
        }

        public string BuildJournalText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Журнал квестов");
            sb.AppendLine();

            IEnumerable<QuestRuntime> active = questById.Values.Where(q => q.isActive && !q.isCompleted);
            if (!active.Any())
            {
                sb.AppendLine("Активных квестов нет.");
            }
            else
            {
                int index = 1;
                foreach (QuestRuntime runtime in active)
                {
                    QuestDefinition def = runtime.definition;
                    sb.AppendLine(index + ". " + def.title);
                    sb.AppendLine(def.description);

                    for (int i = 0; i < def.goals.Length; i++)
                    {
                        QuestGoal goal = def.goals[i];
                        int cur = runtime.progress != null && i < runtime.progress.Length ? runtime.progress[i] : 0;
                        int req = Mathf.Max(1, goal.requiredAmount);
                        sb.AppendLine("- " + goal.BuildLabel() + ": " + cur + "/" + req);
                    }

                    sb.AppendLine();
                    index++;
                }
            }

            IEnumerable<QuestRuntime> completed = questById.Values.Where(q => q.isCompleted);
            if (completed.Any())
            {
                sb.AppendLine("Выполнено:");
                foreach (QuestRuntime runtime in completed)
                {
                    sb.AppendLine("- " + runtime.definition.title);
                }
            }

            return sb.ToString().TrimEnd();
        }

        public List<QuestSaveData> ExportData()
        {
            List<QuestSaveData> list = new List<QuestSaveData>();
            foreach (KeyValuePair<string, QuestRuntime> pair in questById)
            {
                QuestRuntime runtime = pair.Value;
                QuestSaveData save = new QuestSaveData
                {
                    id = pair.Key,
                    isActive = runtime.isActive,
                    isCompleted = runtime.isCompleted,
                    progress = runtime.progress != null ? runtime.progress.ToList() : new List<int>()
                };
                list.Add(save);
            }

            return list;
        }

        public void ImportData(List<QuestSaveData> saves)
        {
            foreach (QuestRuntime runtime in questById.Values)
            {
                runtime.isActive = false;
                runtime.isCompleted = false;
                runtime.progress = new int[runtime.definition.goals != null ? runtime.definition.goals.Length : 0];
            }

            if (saves == null)
            {
                Changed?.Invoke();
                return;
            }

            for (int i = 0; i < saves.Count; i++)
            {
                QuestSaveData save = saves[i];
                if (save == null || string.IsNullOrWhiteSpace(save.id))
                {
                    continue;
                }

                if (!questById.TryGetValue(save.id.Trim(), out QuestRuntime runtime) || runtime == null)
                {
                    continue;
                }

                runtime.isActive = save.isActive;
                runtime.isCompleted = save.isCompleted;

                int goalCount = runtime.definition.goals != null ? runtime.definition.goals.Length : 0;
                runtime.progress = new int[goalCount];
                if (save.progress != null)
                {
                    for (int g = 0; g < Mathf.Min(goalCount, save.progress.Count); g++)
                    {
                        runtime.progress[g] = Mathf.Clamp(save.progress[g], 0, Mathf.Max(1, runtime.definition.goals[g].requiredAmount));
                    }
                }
            }

            Changed?.Invoke();
        }

        private void UpdateGoals(Func<QuestGoal, bool> predicate, int increment, int waveNumber)
        {
            bool changed = false;

            foreach (QuestRuntime runtime in questById.Values)
            {
                if (runtime == null || !runtime.isActive || runtime.isCompleted || runtime.definition == null || runtime.definition.goals == null)
                {
                    continue;
                }

                bool updatedQuest = false;

                for (int i = 0; i < runtime.definition.goals.Length; i++)
                {
                    QuestGoal goal = runtime.definition.goals[i];
                    if (goal == null || !predicate(goal))
                    {
                        continue;
                    }

                    int req = Mathf.Max(1, goal.requiredAmount);

                    if (goal.type == QuestGoalType.ClearCombatWave)
                    {
                        if (!int.TryParse(goal.targetId, out int requiredWave))
                        {
                            requiredWave = 1;
                        }

                        if (waveNumber >= requiredWave)
                        {
                            runtime.progress[i] = req;
                            updatedQuest = true;
                            changed = true;
                        }
                    }
                    else
                    {
                        int newValue = Mathf.Clamp(runtime.progress[i] + increment, 0, req);
                        if (newValue != runtime.progress[i])
                        {
                            runtime.progress[i] = newValue;
                            updatedQuest = true;
                            changed = true;
                        }
                    }
                }

                if (updatedQuest)
                {
                    TryComplete(runtime);
                }
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        private void TryComplete(QuestRuntime runtime)
        {
            if (runtime == null || runtime.definition == null || runtime.definition.goals == null)
            {
                return;
            }

            for (int i = 0; i < runtime.definition.goals.Length; i++)
            {
                int req = Mathf.Max(1, runtime.definition.goals[i].requiredAmount);
                if (runtime.progress == null || i >= runtime.progress.Length || runtime.progress[i] < req)
                {
                    return;
                }
            }

            runtime.isActive = false;
            runtime.isCompleted = true;

            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
            }

            if (playerStats != null)
            {
                playerStats.AddExperience(Mathf.Max(0, runtime.definition.rewardExp));
            }

            if (runtime.definition.rewardTalentPoints > 0 && TalentSystem.Instance != null)
            {
                TalentSystem.Instance.AddPoints(runtime.definition.rewardTalentPoints);
            }
        }

        private void BuildDefaultQuests()
        {
            allQuestDefinitions.Clear();
            questById.Clear();

            allQuestDefinitions.Add(new QuestDefinition
            {
                id = "quest_wood_20",
                title = "Заготовка древесины",
                description = "Добыть 20 дерева на мирном уровне.",
                rewardExp = 120,
                rewardTalentPoints = 1,
                goals = new[]
                {
                    new QuestGoal
                    {
                        type = QuestGoalType.CollectItem,
                        targetId = "wood",
                        requiredAmount = 20
                    }
                }
            });

            allQuestDefinitions.Add(new QuestDefinition
            {
                id = "quest_stone_20",
                title = "Запас камня",
                description = "Добыть 20 камня на мирном уровне.",
                rewardExp = 120,
                rewardTalentPoints = 1,
                goals = new[]
                {
                    new QuestGoal
                    {
                        type = QuestGoalType.CollectItem,
                        targetId = "stone",
                        requiredAmount = 20
                    }
                }
            });

            allQuestDefinitions.Add(new QuestDefinition
            {
                id = "quest_combat_wave1",
                title = "Первый опасный вылаз",
                description = "Пройти первую волну на опасном уровне.",
                rewardExp = 180,
                rewardTalentPoints = 2,
                goals = new[]
                {
                    new QuestGoal
                    {
                        type = QuestGoalType.ClearCombatWave,
                        targetId = "1",
                        requiredAmount = 1
                    }
                }
            });

            allQuestDefinitions.Add(new QuestDefinition
            {
                id = "quest_combat_wave3",
                title = "Зачистка сектора",
                description = "Пройти третью волну на опасном уровне.",
                rewardExp = 260,
                rewardTalentPoints = 2,
                goals = new[]
                {
                    new QuestGoal
                    {
                        type = QuestGoalType.ClearCombatWave,
                        targetId = "3",
                        requiredAmount = 1
                    }
                }
            });

            allQuestDefinitions.Add(new QuestDefinition
            {
                id = "quest_kill_12",
                title = "Охота на угрозу",
                description = "Уничтожить 12 врагов на опасном уровне.",
                rewardExp = 220,
                rewardTalentPoints = 1,
                goals = new[]
                {
                    new QuestGoal
                    {
                        type = QuestGoalType.KillEnemy,
                        targetId = "arena_enemy",
                        requiredAmount = 12
                    }
                }
            });

            for (int i = 0; i < allQuestDefinitions.Count; i++)
            {
                QuestDefinition def = allQuestDefinitions[i];
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                {
                    continue;
                }

                questById[def.id.Trim()] = new QuestRuntime
                {
                    definition = def,
                    isActive = false,
                    isCompleted = false,
                    progress = new int[def.goals != null ? def.goals.Length : 0]
                };
            }
        }
    }
}
