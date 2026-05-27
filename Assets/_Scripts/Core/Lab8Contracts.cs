using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lab8
{
    public enum GameMode
    {
        None,
        Peaceful,
        Combat
    }

    public enum QuestGoalType
    {
        CollectItem,
        KillEnemy,
        ClearCombatWave
    }

    public enum ResourceType
    {
        Wood,
        Stone
    }

    public interface IInteractable
    {
        string GetPrompt();
        void Interact(PlayerController player);
    }

    [Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    public class InventorySaveData
    {
        public List<InventoryEntry> entries = new List<InventoryEntry>();
    }

    [Serializable]
    public class QuestGoal
    {
        public QuestGoalType type;
        public string targetId;
        public int requiredAmount = 1;

        [NonSerialized] public int progress;

        public string BuildLabel()
        {
            switch (type)
            {
                case QuestGoalType.CollectItem:
                    return "Собрать: " + targetId;
                case QuestGoalType.KillEnemy:
                    return "Победить: " + targetId;
                case QuestGoalType.ClearCombatWave:
                    return "Пройти волну: " + targetId;
                default:
                    return "Цель";
            }
        }
    }

    [Serializable]
    public class QuestDefinition
    {
        public string id;
        public string title;
        public string description;
        public QuestGoal[] goals;
        public int rewardExp;
        public int rewardTalentPoints;
    }

    [Serializable]
    public class QuestSaveData
    {
        public string id;
        public bool isActive;
        public bool isCompleted;
        public List<int> progress = new List<int>();
    }

    [Serializable]
    public class TalentDefinition
    {
        public string id;
        public string title;
        public string description;
        public int cost = 1;
        public string[] prerequisites = Array.Empty<string>();
        public Vector2 uiPosition;

        public float healthFlat;
        public float manaFlat;
        public float damageFlat;
        public float moveSpeedFlat;
        public float manaRegenFlat;

        public float healthPercent;
        public float manaPercent;
        public float damagePercent;
        public float moveSpeedPercent;
        public float manaRegenPercent;
    }

    [Serializable]
    public class TalentSaveData
    {
        public int availablePoints;
        public List<string> learnedIds = new List<string>();
    }

    [Serializable]
    public class AbilityDefinition
    {
        public string id;
        public string title;
        public string description;
        public Color color;
        public float cooldown;
        public float manaCost;
        public float damage;
        public float speed;
        public float lifetime;
        public KeyCode hotkey;
    }

    [Serializable]
    public class AbilitySaveData
    {
        public bool unlocked;
    }

    [Serializable]
    public class StatsSaveData
    {
        public int level;
        public int currentExp;
        public int expToNext;
        public float currentHealth;
        public float currentMana;
    }

    [Serializable]
    public class RecipeIngredient
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    public class CraftRecipe
    {
        public string id;
        public string title;
        public string description;
        public RecipeIngredient[] inputs;
        public RecipeIngredient[] outputs;
    }

    [Serializable]
    public class SaveGameData
    {
        public string mode;
        public bool narratorSkillsGiven;
        public bool clearedFirstCombatWave;

        public InventorySaveData inventory = new InventorySaveData();
        public List<QuestSaveData> quests = new List<QuestSaveData>();
        public TalentSaveData talents = new TalentSaveData();
        public AbilitySaveData abilities = new AbilitySaveData();
        public StatsSaveData stats = new StatsSaveData();
    }
}
