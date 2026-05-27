using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lab8
{
    public class AbilitySystem : MonoBehaviour
    {
        public event Action Changed;

        public IReadOnlyList<AbilityDefinition> Abilities => abilities;
        public bool Unlocked => unlocked;

        [SerializeField] private bool unlocked;

        private readonly List<AbilityDefinition> abilities = new List<AbilityDefinition>();
        private readonly Dictionary<string, float> cooldowns = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private PlayerStats stats;

        private void Awake()
        {
            BuildDefaultAbilities();
            stats = GetComponent<PlayerStats>();
            if (stats == null)
            {
                stats = gameObject.AddComponent<PlayerStats>();
            }

            for (int i = 0; i < abilities.Count; i++)
            {
                cooldowns[abilities[i].id] = 0f;
            }
        }

        private void Update()
        {
            List<string> keys = new List<string>(cooldowns.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string id = keys[i];
                if (cooldowns[id] <= 0f)
                {
                    continue;
                }

                cooldowns[id] = Mathf.Max(0f, cooldowns[id] - Time.deltaTime);
            }
        }

        public void UnlockStarterAbilities()
        {
            if (unlocked)
            {
                return;
            }

            unlocked = true;
            Changed?.Invoke();
        }

        public bool TryCastBySlot(int slotIndex, Vector3 origin, Vector3 direction)
        {
            if (!unlocked || slotIndex < 0 || slotIndex >= abilities.Count)
            {
                return false;
            }

            AbilityDefinition ability = abilities[slotIndex];
            if (ability == null)
            {
                return false;
            }

            if (GetCooldownRemaining(ability.id) > 0f)
            {
                return false;
            }

            if (stats == null)
            {
                stats = GetComponent<PlayerStats>();
            }

            if (stats == null || !stats.SpendMana(ability.manaCost))
            {
                return false;
            }

            cooldowns[ability.id] = Mathf.Max(0.1f, ability.cooldown);
            SpawnProjectile(ability, origin, direction);
            Changed?.Invoke();
            return true;
        }

        public float GetCooldownRemaining(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return 0f;
            }

            if (!cooldowns.TryGetValue(abilityId.Trim(), out float value))
            {
                return 0f;
            }

            return value;
        }

        public AbilitySaveData ExportData()
        {
            return new AbilitySaveData { unlocked = unlocked };
        }

        public void ImportData(AbilitySaveData save)
        {
            unlocked = save != null && save.unlocked;

            List<string> keys = new List<string>(cooldowns.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                cooldowns[keys[i]] = 0f;
            }

            Changed?.Invoke();
        }

        private void SpawnProjectile(AbilityDefinition ability, Vector3 origin, Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
            }

            GameObject projectileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObj.name = "Скилл_" + ability.title;
            projectileObj.transform.position = origin;
            projectileObj.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            AbilityProjectile projectile = projectileObj.AddComponent<AbilityProjectile>();
            projectile.Initialize(ability.speed, ability.lifetime, ability.damage + (stats != null ? stats.Damage * 0.4f : 0f), gameObject, ability.color);
        }

        private void BuildDefaultAbilities()
        {
            if (abilities.Count > 0)
            {
                return;
            }

            abilities.Add(new AbilityDefinition
            {
                id = "fire_orb",
                title = "Fireball",
                description = "Мощный огненный снаряд",
                color = new Color(1f, 0.36f, 0.16f, 1f),
                cooldown = 3.2f,
                manaCost = 18f,
                damage = 220f,
                speed = 24f,
                lifetime = 2.5f,
                hotkey = KeyCode.Alpha1
            });

            abilities.Add(new AbilityDefinition
            {
                id = "frost_lance",
                title = "Frostbolt",
                description = "Быстрый снаряд холода",
                color = new Color(0.32f, 0.86f, 1f, 1f),
                cooldown = 2.4f,
                manaCost = 14f,
                damage = 180f,
                speed = 30f,
                lifetime = 2.2f,
                hotkey = KeyCode.Alpha2
            });

            abilities.Add(new AbilityDefinition
            {
                id = "arcane_bolt",
                title = "ArcaneShot",
                description = "Снаряд чистой энергии",
                color = new Color(0.68f, 0.4f, 1f, 1f),
                cooldown = 4.1f,
                manaCost = 24f,
                damage = 300f,
                speed = 20f,
                lifetime = 2.9f,
                hotkey = KeyCode.Alpha3
            });
        }
    }
}
