using System.Collections.Generic;
using UnityEngine;

namespace Lab8
{
    public class CombatArena : MonoBehaviour
    {
        public int CurrentWave => currentWave;
        public int AliveEnemies => aliveEnemies;
        public int TotalKills => totalKills;
        public float NextWaveTimer => nextWaveTimer;

        [SerializeField] private float arenaRadius = 34f;
        [SerializeField] private float timeBetweenWaves = 5f;
        [SerializeField] private int baseEnemiesPerWave = 5;

        private readonly List<Transform> spawnPoints = new List<Transform>();
        private readonly List<Enemy> alive = new List<Enemy>();

        private Transform arenaRoot;
        private bool running;
        private int currentWave;
        private int aliveEnemies;
        private int totalKills;
        private float nextWaveTimer;

        private void OnEnable()
        {
            Enemy.EnemyDied += OnEnemyDied;
        }

        private void OnDisable()
        {
            Enemy.EnemyDied -= OnEnemyDied;
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            if (aliveEnemies > 0)
            {
                return;
            }

            if (nextWaveTimer > 0f)
            {
                nextWaveTimer -= Time.deltaTime;
                if (nextWaveTimer <= 0f)
                {
                    StartWave(currentWave + 1);
                }
            }
        }

        public void Initialize(Transform root)
        {
            arenaRoot = root;
            BuildSpawnPoints();
        }

        public void Begin()
        {
            running = true;
            totalKills = 0;
            aliveEnemies = 0;
            currentWave = 0;
            nextWaveTimer = 0f;
            ClearEnemies();
            StartWave(1);
        }

        public void Stop()
        {
            running = false;
            aliveEnemies = 0;
            nextWaveTimer = 0f;
            ClearEnemies();
        }

        private void StartWave(int wave)
        {
            currentWave = Mathf.Max(1, wave);
            int count = baseEnemiesPerWave + (currentWave - 1) * 2;
            aliveEnemies = 0;
            nextWaveTimer = 0f;

            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(i);
            }
        }

        private void SpawnEnemy(int index)
        {
            Vector3 spawnPosition;
            if (spawnPoints.Count > 0)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Count)];
                Vector2 offset2d = Random.insideUnitCircle * 2f;
                spawnPosition = point.position + new Vector3(offset2d.x, 0f, offset2d.y);
            }
            else
            {
                Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(arenaRadius * 0.45f, arenaRadius * 0.95f);
                spawnPosition = new Vector3(circle.x, 1f, circle.y);
            }

            GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObj.name = "Арена_Враг_" + currentWave + "_" + (index + 1);
            enemyObj.transform.position = spawnPosition;
            enemyObj.transform.localScale = new Vector3(1f, 1.65f, 1f);

            Renderer rendererComp = enemyObj.GetComponent<Renderer>();
            if (rendererComp != null)
            {
                rendererComp.material.color = new Color(0.75f, 0.22f, 0.22f, 1f);
            }

            Enemy enemy = enemyObj.AddComponent<Enemy>();
            enemy.SetupForWave(currentWave);
            alive.Add(enemy);
            aliveEnemies++;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            if (!running)
            {
                return;
            }

            if (enemy != null)
            {
                alive.Remove(enemy);
            }

            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
            totalKills++;

            if (aliveEnemies == 0)
            {
                nextWaveTimer = timeBetweenWaves;
                QuestSystem quest = QuestSystem.Instance;
                if (quest != null)
                {
                    quest.RegisterCombatWaveCleared(currentWave);
                }
            }
        }

        private void ClearEnemies()
        {
            Enemy[] all = FindObjectsOfType<Enemy>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                {
                    Destroy(all[i].gameObject);
                }
            }

            alive.Clear();
        }

        private void BuildSpawnPoints()
        {
            spawnPoints.Clear();
            if (arenaRoot == null)
            {
                return;
            }

            Transform existing = arenaRoot.Find("SpawnPoints");
            if (existing == null)
            {
                GameObject root = new GameObject("SpawnPoints");
                root.transform.SetParent(arenaRoot, false);
                root.transform.localPosition = Vector3.zero;

                int count = 12;
                for (int i = 0; i < count; i++)
                {
                    float angle = (Mathf.PI * 2f / count) * i;
                    Vector3 pos = new Vector3(Mathf.Cos(angle) * arenaRadius, 1f, Mathf.Sin(angle) * arenaRadius);

                    GameObject p = new GameObject("Spawn_" + i);
                    p.transform.SetParent(root.transform, false);
                    p.transform.localPosition = pos;
                }

                existing = root.transform;
            }

            for (int i = 0; i < existing.childCount; i++)
            {
                spawnPoints.Add(existing.GetChild(i));
            }
        }
    }
}
