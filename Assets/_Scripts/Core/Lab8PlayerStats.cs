using System;
using UnityEngine;

namespace Lab8
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Базовые значения")]
        [SerializeField] private float baseHealth = 120f;
        [SerializeField] private float baseMana = 100f;
        [SerializeField] private float baseDamage = 120f;
        [SerializeField] private float baseMoveSpeed = 6.5f;
        [SerializeField] private float baseManaRegen = 7f;

        [Header("Опыт и уровень")]
        [SerializeField] private int level = 1;
        [SerializeField] private int currentExp;
        [SerializeField] private int expToNext = 120;

        public float MaxHealth { get; private set; }
        public float MaxMana { get; private set; }
        public float Damage { get; private set; }
        public float MoveSpeed { get; private set; }
        public float ManaRegen { get; private set; }

        public float CurrentHealth { get; private set; }
        public float CurrentMana { get; private set; }

        public int Level => level;
        public int CurrentExp => currentExp;
        public int ExpToNext => expToNext;

        public event Action Changed;
        public event Action Died;
        public event Action LeveledUp;

        private bool initialized;
        private bool dead;

        private void Start()
        {
            if (!initialized)
            {
                RecalculateWithTalents();
                CurrentHealth = MaxHealth;
                CurrentMana = MaxMana;
                initialized = true;
                Changed?.Invoke();
            }
        }

        private void Update()
        {
            if (!initialized || dead)
            {
                return;
            }

            if (CurrentMana < MaxMana && ManaRegen > 0f)
            {
                CurrentMana = Mathf.Min(MaxMana, CurrentMana + ManaRegen * Time.deltaTime);
                Changed?.Invoke();
            }
        }

        public void RecalculateWithTalents()
        {
            float healthRatio = MaxHealth > 0.01f ? CurrentHealth / MaxHealth : 1f;
            float manaRatio = MaxMana > 0.01f ? CurrentMana / MaxMana : 1f;

            float health = baseHealth;
            float mana = baseMana;
            float damage = baseDamage;
            float moveSpeed = baseMoveSpeed;
            float manaRegen = baseManaRegen;

            TalentSystem talentSystem = TalentSystem.Instance;
            if (talentSystem != null)
            {
                talentSystem.ApplyBonuses(ref health, ref mana, ref damage, ref moveSpeed, ref manaRegen);
            }

            MaxHealth = Mathf.Max(1f, health);
            MaxMana = Mathf.Max(1f, mana);
            Damage = Mathf.Max(1f, damage);
            MoveSpeed = Mathf.Max(1f, moveSpeed);
            ManaRegen = Mathf.Max(0f, manaRegen);

            if (initialized)
            {
                CurrentHealth = Mathf.Clamp(MaxHealth * healthRatio, 0f, MaxHealth);
                CurrentMana = Mathf.Clamp(MaxMana * manaRatio, 0f, MaxMana);
            }

            Changed?.Invoke();
        }

        public bool SpendMana(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (CurrentMana < amount)
            {
                return false;
            }

            CurrentMana -= amount;
            Changed?.Invoke();
            return true;
        }

        public void RestoreMana(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            CurrentMana = Mathf.Clamp(CurrentMana + amount, 0f, MaxMana);
            Changed?.Invoke();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, MaxHealth);
            if (CurrentHealth > 0.01f)
            {
                dead = false;
            }

            Changed?.Invoke();
        }

        public float TakeDamage(float amount)
        {
            if (amount <= 0f || dead)
            {
                return 0f;
            }

            CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, MaxHealth);
            Changed?.Invoke();

            if (CurrentHealth <= 0.01f)
            {
                dead = true;
                Died?.Invoke();
            }

            return amount;
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentExp += amount;
            bool leveled = false;
            while (currentExp >= expToNext)
            {
                currentExp -= expToNext;
                level++;
                expToNext = Mathf.RoundToInt(expToNext * 1.2f);
                leveled = true;

                TalentSystem talentSystem = TalentSystem.Instance;
                if (talentSystem != null)
                {
                    talentSystem.AddPoints(2);
                }
            }

            if (leveled)
            {
                LeveledUp?.Invoke();
            }

            Changed?.Invoke();
        }

        public void ReviveAtFull()
        {
            dead = false;
            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
            Changed?.Invoke();
        }

        public void ResetProgress()
        {
            level = 1;
            currentExp = 0;
            expToNext = 120;
            dead = false;
            initialized = true;

            RecalculateWithTalents();
            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
            Changed?.Invoke();
        }

        public StatsSaveData ExportData()
        {
            return new StatsSaveData
            {
                level = level,
                currentExp = currentExp,
                expToNext = expToNext,
                currentHealth = CurrentHealth,
                currentMana = CurrentMana
            };
        }

        public void ImportData(StatsSaveData save)
        {
            if (save == null)
            {
                return;
            }

            level = Mathf.Max(1, save.level);
            currentExp = Mathf.Max(0, save.currentExp);
            expToNext = Mathf.Max(50, save.expToNext);

            RecalculateWithTalents();
            CurrentHealth = Mathf.Clamp(save.currentHealth, 1f, MaxHealth);
            CurrentMana = Mathf.Clamp(save.currentMana, 0f, MaxMana);
            dead = false;
            initialized = true;
            Changed?.Invoke();
        }
    }
}
