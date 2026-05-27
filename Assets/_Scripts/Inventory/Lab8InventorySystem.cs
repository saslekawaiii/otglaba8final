using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lab8
{
    public class InventorySystem : MonoBehaviour
    {
        public event Action Changed;

        private readonly Dictionary<string, int> items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public int GetCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            return items.TryGetValue(itemId.Trim(), out int count) ? count : 0;
        }

        public void Add(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return;
            }

            string key = itemId.Trim();
            items[key] = GetCount(key) + amount;

            Changed?.Invoke();

            QuestSystem questSystem = QuestSystem.Instance;
            if (questSystem != null)
            {
                questSystem.RegisterCollectedItem(key, amount);
            }
        }

        public bool CanSpend(string itemId, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            return GetCount(itemId) >= amount;
        }

        public bool TrySpend(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            string key = itemId.Trim();
            int current = GetCount(key);
            if (current < amount)
            {
                return false;
            }

            int left = current - amount;
            if (left <= 0)
            {
                items.Remove(key);
            }
            else
            {
                items[key] = left;
            }

            Changed?.Invoke();
            return true;
        }

        public bool TrySpendMany(params RecipeIngredient[] ingredients)
        {
            if (ingredients == null)
            {
                return false;
            }

            for (int i = 0; i < ingredients.Length; i++)
            {
                RecipeIngredient ingredient = ingredients[i];
                if (ingredient == null)
                {
                    continue;
                }

                if (!CanSpend(ingredient.itemId, ingredient.amount))
                {
                    return false;
                }
            }

            for (int i = 0; i < ingredients.Length; i++)
            {
                RecipeIngredient ingredient = ingredients[i];
                if (ingredient == null)
                {
                    continue;
                }

                TrySpend(ingredient.itemId, ingredient.amount);
            }

            return true;
        }

        public void ClearAll()
        {
            if (items.Count == 0)
            {
                return;
            }

            items.Clear();
            Changed?.Invoke();
        }

        public string BuildSummary()
        {
            if (items.Count == 0)
            {
                return "Инвентарь пуст";
            }

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in items.OrderBy(p => p.Key))
            {
                sb.AppendLine(ResolveItemName(pair.Key) + ": " + pair.Value);
            }

            return sb.ToString().TrimEnd();
        }

        public InventorySaveData ExportData()
        {
            InventorySaveData save = new InventorySaveData();
            foreach (KeyValuePair<string, int> pair in items)
            {
                save.entries.Add(new InventoryEntry
                {
                    itemId = pair.Key,
                    amount = pair.Value
                });
            }

            return save;
        }

        public void ImportData(InventorySaveData save)
        {
            items.Clear();
            if (save != null && save.entries != null)
            {
                for (int i = 0; i < save.entries.Count; i++)
                {
                    InventoryEntry entry = save.entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.itemId) || entry.amount <= 0)
                    {
                        continue;
                    }

                    items[entry.itemId.Trim()] = Mathf.Max(0, entry.amount);
                }
            }

            Changed?.Invoke();
        }

        public static string ResolveItemName(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return "Неизвестно";
            }

            switch (itemId.Trim().ToLowerInvariant())
            {
                case "wood":
                    return "Дерево";
                case "stone":
                    return "Камень";
                case "plank":
                    return "Доски";
                case "brick":
                    return "Каменный блок";
                case "rune":
                    return "Боевая руна";
                default:
                    return itemId;
            }
        }
    }
}
