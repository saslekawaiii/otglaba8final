using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lab8
{
    public class TalentSystem : MonoBehaviour
    {
        public static TalentSystem Instance { get; private set; }

        public event Action Changed;

        public IReadOnlyList<TalentDefinition> Talents => talents;
        public int AvailablePoints => availablePoints;

        private readonly List<TalentDefinition> talents = new List<TalentDefinition>();
        private readonly HashSet<string> learned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [SerializeField] private int availablePoints = 3;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildDefaultTree();
        }

        private void BuildDefaultTree()
        {
            if (talents.Count > 0)
            {
                return;
            }

            talents.Add(Create("hp_1", "Крепость I", "Здоровье +30", 1, new Vector2(-600f, 220f), healthFlat: 30f));
            talents.Add(Create("hp_2", "Крепость II", "Здоровье +40", 1, new Vector2(-600f, 60f), new[] { "hp_1" }, healthFlat: 40f));
            talents.Add(Create("hp_3", "Живучесть", "Здоровье +20%", 2, new Vector2(-600f, -100f), new[] { "hp_2" }, healthPercent: 20f));

            talents.Add(Create("mana_1", "Источник маны", "Мана +35", 1, new Vector2(-200f, 220f), manaFlat: 35f));
            talents.Add(Create("mana_2", "Поток маны", "Реген маны +5", 1, new Vector2(-200f, 60f), new[] { "mana_1" }, manaRegenFlat: 5f));
            talents.Add(Create("mana_3", "Магический резерв", "Мана +25%", 2, new Vector2(-200f, -100f), new[] { "mana_2" }, manaPercent: 25f));

            talents.Add(Create("dmg_1", "Боевой настрой", "Урон +25", 1, new Vector2(200f, 220f), damageFlat: 25f));
            talents.Add(Create("dmg_2", "Ударная техника", "Урон +35", 1, new Vector2(200f, 60f), new[] { "dmg_1" }, damageFlat: 35f));
            talents.Add(Create("dmg_3", "Разрушитель", "Урон +25%", 2, new Vector2(200f, -100f), new[] { "dmg_2" }, damagePercent: 25f));

            talents.Add(Create("spd_1", "Шаг разведчика", "Скорость +1", 1, new Vector2(600f, 220f), moveSpeedFlat: 1f));
            talents.Add(Create("spd_2", "Лёгкость", "Скорость +1.2", 1, new Vector2(600f, 60f), new[] { "spd_1" }, moveSpeedFlat: 1.2f));
            talents.Add(Create("spd_3", "Буря", "Скорость +20%", 2, new Vector2(600f, -100f), new[] { "spd_2" }, moveSpeedPercent: 20f));

            talents.Add(Create("hybrid_1", "Боевая мудрость", "HP +15, мана +20", 1, new Vector2(0f, -260f), new[] { "hp_2", "mana_2" }, healthFlat: 15f, manaFlat: 20f));
            talents.Add(Create("hybrid_2", "Воин-чародей", "Урон +15%, реген +3", 2, new Vector2(0f, -420f), new[] { "hybrid_1", "dmg_2" }, damagePercent: 15f, manaRegenFlat: 3f));
            talents.Add(Create("master", "Ядро героя", "HP +10%, мана +10%, урон +10%, скорость +10%", 3, new Vector2(0f, -580f), new[] { "hp_3", "mana_3", "dmg_3", "spd_3", "hybrid_2" }, healthPercent: 10f, manaPercent: 10f, damagePercent: 10f, moveSpeedPercent: 10f));
        }

        public bool IsLearned(string talentId)
        {
            return !string.IsNullOrWhiteSpace(talentId) && learned.Contains(talentId.Trim());
        }

        public bool CanLearn(TalentDefinition talent)
        {
            if (talent == null || learned.Contains(talent.id))
            {
                return false;
            }

            if (availablePoints < Mathf.Max(1, talent.cost))
            {
                return false;
            }

            if (talent.prerequisites == null)
            {
                return true;
            }

            for (int i = 0; i < talent.prerequisites.Length; i++)
            {
                string id = talent.prerequisites[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!learned.Contains(id.Trim()))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Learn(string talentId)
        {
            TalentDefinition talent = talents.FirstOrDefault(t => string.Equals(t.id, talentId, StringComparison.OrdinalIgnoreCase));
            if (!CanLearn(talent))
            {
                return false;
            }

            learned.Add(talent.id);
            availablePoints -= Mathf.Max(1, talent.cost);

            PlayerStats stats = FindObjectOfType<PlayerStats>();
            if (stats != null)
            {
                stats.RecalculateWithTalents();
            }

            Changed?.Invoke();
            return true;
        }

        public void AddPoints(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            availablePoints += amount;
            Changed?.Invoke();
        }

        public void ApplyBonuses(ref float health, ref float mana, ref float damage, ref float speed, ref float manaRegen)
        {
            float healthPercent = 0f;
            float manaPercent = 0f;
            float damagePercent = 0f;
            float speedPercent = 0f;
            float regenPercent = 0f;

            for (int i = 0; i < talents.Count; i++)
            {
                TalentDefinition talent = talents[i];
                if (talent == null || !learned.Contains(talent.id))
                {
                    continue;
                }

                health += talent.healthFlat;
                mana += talent.manaFlat;
                damage += talent.damageFlat;
                speed += talent.moveSpeedFlat;
                manaRegen += talent.manaRegenFlat;

                healthPercent += talent.healthPercent;
                manaPercent += talent.manaPercent;
                damagePercent += talent.damagePercent;
                speedPercent += talent.moveSpeedPercent;
                regenPercent += talent.manaRegenPercent;
            }

            health *= 1f + healthPercent / 100f;
            mana *= 1f + manaPercent / 100f;
            damage *= 1f + damagePercent / 100f;
            speed *= 1f + speedPercent / 100f;
            manaRegen *= 1f + regenPercent / 100f;
        }

        public TalentSaveData ExportData()
        {
            return new TalentSaveData
            {
                availablePoints = availablePoints,
                learnedIds = learned.ToList()
            };
        }

        public void ImportData(TalentSaveData save)
        {
            learned.Clear();
            availablePoints = 3;

            if (save != null)
            {
                availablePoints = Mathf.Max(0, save.availablePoints);

                if (save.learnedIds != null)
                {
                    for (int i = 0; i < save.learnedIds.Count; i++)
                    {
                        string id = save.learnedIds[i];
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            continue;
                        }

                        if (talents.Any(t => string.Equals(t.id, id.Trim(), StringComparison.OrdinalIgnoreCase)))
                        {
                            learned.Add(id.Trim());
                        }
                    }
                }
            }

            PlayerStats stats = FindObjectOfType<PlayerStats>();
            if (stats != null)
            {
                stats.RecalculateWithTalents();
            }

            Changed?.Invoke();
        }

        private static TalentDefinition Create(
            string id,
            string title,
            string description,
            int cost,
            Vector2 uiPosition,
            string[] prerequisites = null,
            float healthFlat = 0f,
            float manaFlat = 0f,
            float damageFlat = 0f,
            float moveSpeedFlat = 0f,
            float manaRegenFlat = 0f,
            float healthPercent = 0f,
            float manaPercent = 0f,
            float damagePercent = 0f,
            float moveSpeedPercent = 0f,
            float manaRegenPercent = 0f)
        {
            return new TalentDefinition
            {
                id = id,
                title = title,
                description = description,
                cost = Mathf.Max(1, cost),
                prerequisites = prerequisites ?? Array.Empty<string>(),
                uiPosition = uiPosition,
                healthFlat = healthFlat,
                manaFlat = manaFlat,
                damageFlat = damageFlat,
                moveSpeedFlat = moveSpeedFlat,
                manaRegenFlat = manaRegenFlat,
                healthPercent = healthPercent,
                manaPercent = manaPercent,
                damagePercent = damagePercent,
                moveSpeedPercent = moveSpeedPercent,
                manaRegenPercent = manaRegenPercent
            };
        }
    }
}
