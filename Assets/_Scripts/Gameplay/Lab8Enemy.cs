using System;
using UnityEngine;

namespace Lab8
{
    [RequireComponent(typeof(CharacterController))]
    public class Enemy : MonoBehaviour
    {
        public static event Action<Enemy> EnemyDied;

        [SerializeField] private string enemyType = "arena_enemy";
        [SerializeField] private float maxHealth = 85f;
        [SerializeField] private float moveSpeed = 3.8f;
        [SerializeField] private float attackDamage = 14f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.1f;

        private float currentHealth;
        private float attackTimer;

        private CharacterController controller;
        private Transform playerTransform;
        private PlayerStats playerStats;
        private bool dead;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = new Vector3(0f, 1f, 0f);
            controller.stepOffset = 0.4f;
            controller.slopeLimit = 55f;

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Destroy(capsule);
            }

            currentHealth = maxHealth;
        }

        private void Start()
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
                playerStats = player.GetComponent<PlayerStats>();
            }
        }

        private void Update()
        {
            if (dead)
            {
                return;
            }

            if (playerTransform == null || playerStats == null)
            {
                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerStats = player.GetComponent<PlayerStats>();
                }

                return;
            }

            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
            }

            Vector3 delta = playerTransform.position - transform.position;
            delta.y = 0f;
            float sqrDist = delta.sqrMagnitude;

            if (sqrDist > attackRange * attackRange)
            {
                Vector3 direction = delta.normalized;
                Vector3 motion = direction * moveSpeed;
                motion.y = -2f;
                controller.Move(motion * Time.deltaTime);

                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);
                }
            }
            else if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                playerStats.TakeDamage(attackDamage);
                FlashColor(new Color(1f, 0.28f, 0.28f, 1f));
            }
        }

        public void SetupForWave(int wave)
        {
            int stage = Mathf.Max(1, wave);
            maxHealth = 85f + (stage - 1) * 12f;
            currentHealth = maxHealth;
            attackDamage = 12f + (stage - 1) * 2f;
            moveSpeed = 3.8f + (stage - 1) * 0.08f;
        }

        public void TakeDamage(float amount)
        {
            if (dead || amount <= 0f)
            {
                return;
            }

            currentHealth -= amount;
            FlashColor(new Color(1f, 0.75f, 0.3f, 1f));

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (dead)
            {
                return;
            }

            dead = true;

            QuestSystem quest = QuestSystem.Instance;
            if (quest != null)
            {
                quest.RegisterEnemyKilled(enemyType);
            }

            PlayerStats stats = FindObjectOfType<PlayerStats>();
            if (stats != null)
            {
                stats.AddExperience(30);
            }

            EnemyDied?.Invoke(this);
            Destroy(gameObject, 0.05f);
        }

        private void FlashColor(Color color)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = color;
                }
            }

            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.1f);
        }

        private void RestoreColor()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = new Color(0.75f, 0.22f, 0.22f, 1f);
                }
            }
        }
    }
}
