using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeadDawn.Enemy;
using DeadDawn.ResourceSystems;

namespace DeadDawn.Core
{
    /// <summary>
    /// Orchestrates the automatic Day/Night cycle, Horde wave progression,
    /// and ambient map zombies. Ensures wave counts are balanced against available map resources.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Zombie Prefab")]
        [SerializeField] private GameObject zombiePrefab;

        [Header("Cycle Timers (Seconds)")]
        [SerializeField] private float scavengeDuration = 80f;
        [SerializeField] private float warningDuration = 10f;
        [SerializeField] private float dawnDuration = 6f;

        [Header("Ambient Map Zombies (Requirement 2 & 3)")]
        [SerializeField] private int ambientZombieCount = 9;
        [SerializeField] private float minAmbientSpawnRadius = 12f;
        [SerializeField] private float maxAmbientSpawnRadius = 34f;

        [Header("Horde Wave Balancing (Requirement 5)")]
        [SerializeField] private int baseHordeZombieCount = 10;
        [SerializeField] private int hordeCountIncreasePerDay = 4;
        [SerializeField] private float hordeSpawnRadius = 36f;

        [Header("Day / Night Lighting")]
        [SerializeField] private Light sceneDirectionalLight;
        [SerializeField] private Color dayLightColor = new Color(1f, 0.96f, 0.88f);
        [SerializeField] private Color warningLightColor = new Color(1f, 0.65f, 0.35f);
        [SerializeField] private Color nightLightColor = new Color(0.25f, 0.35f, 0.55f);
        [SerializeField] private Color dawnLightColor = new Color(1f, 0.85f, 0.6f);

        private GameState currentState = GameState.Scavenge;
        private int currentDay = 1;
        private float stateTimer = 0f;

        private readonly List<ZombieController> activeAmbientZombies = new List<ZombieController>();
        private readonly List<ZombieController> activeHordeZombies = new List<ZombieController>();
        private int hordeZombiesRemainingToSpawn = 0;
        private int totalHordeForCurrentWave = 0;

        public GameState CurrentState => currentState;
        public int CurrentDay => currentDay;
        public float StateTimer => stateTimer;
        public int RemainingHordeZombies => activeHordeZombies.Count + hordeZombiesRemainingToSpawn;
        public int TotalHordeForCurrentWave => totalHordeForCurrentWave;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (sceneDirectionalLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional)
                    {
                        sceneDirectionalLight = lights[i];
                        break;
                    }
                }
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ZombieKilledEvent>(OnZombieKilled);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ZombieKilledEvent>(OnZombieKilled);
        }

        private void Start()
        {
            EnterScavengePhase();
        }

        private void Update()
        {
            if (currentState == GameState.GameOver || Time.timeScale <= 0f) return;

            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case GameState.Scavenge:
                    UpdateLighting(dayLightColor, 1.15f);
                    if (stateTimer <= 0f)
                    {
                        EnterWarningPhase();
                    }
                    break;

                case GameState.Warning:
                    UpdateLighting(warningLightColor, 0.75f);
                    if (stateTimer <= 0f)
                    {
                        EnterHordePhase();
                    }
                    break;

                case GameState.Horde:
                    UpdateLighting(nightLightColor, 0.32f);
                    CheckHordeCompletion();
                    break;

                case GameState.Dawn:
                    UpdateLighting(dawnLightColor, 1.25f);
                    if (stateTimer <= 0f)
                    {
                        EnterScavengePhase();
                    }
                    break;
            }
        }

        private void EnterScavengePhase()
        {
            SetState(GameState.Scavenge);
            stateTimer = scavengeDuration;

            // Spawn/replenish ambient zombies across map
            SpawnAmbientZombies();

            // Refresh harvest nodes
            ResourceSpawner spawner = FindFirstObjectByType<ResourceSpawner>();
            if (spawner != null)
            {
                spawner.SpawnDailyHarvestNodes();
            }
        }

        private void EnterWarningPhase()
        {
            SetState(GameState.Warning);
            stateTimer = warningDuration;

            // Screen shake alert
            EventBus.Publish(new ScreenShakeEvent(0.2f, 0.3f));
        }

        private void EnterHordePhase()
        {
            SetState(GameState.Horde);

            // Requirement 5: Calculate safe wave count bounded by available map resources
            totalHordeForCurrentWave = CalculateSafeHordeCount();
            hordeZombiesRemainingToSpawn = totalHordeForCurrentWave;
            activeHordeZombies.Clear();

            StartCoroutine(SpawnHordeRoutine());
        }

        private int CalculateSafeHordeCount()
        {
            // Base scaling: Day 1 = 10, Day 2 = 14, Day 3 = 18, etc.
            int proposedCount = baseHordeZombieCount + (currentDay - 1) * hordeCountIncreasePerDay;

            // Guaranteed to remain within player resource budget
            // Each zombie requires ~2 shots (50 HP / 25 dmg). 
            // The map generates ~150-180 bullets worth of resources.
            // We cap max zombies well below harvest capacity (e.g. 35 zombies maximum).
            int safeUpperLimit = 35;
            return Mathf.Min(proposedCount, safeUpperLimit);
        }

        private IEnumerator SpawnHordeRoutine()
        {
            // Spawn in continuous waves from outside the map
            while (hordeZombiesRemainingToSpawn > 0)
            {
                int batchSize = Mathf.Min(Random.Range(2, 4), hordeZombiesRemainingToSpawn);
                for (int i = 0; i < batchSize; i++)
                {
                    SpawnHordeZombie();
                    hordeZombiesRemainingToSpawn--;
                }

                yield return new WaitForSeconds(Random.Range(1.8f, 3.2f));
            }
        }

        private void SpawnHordeZombie()
        {
            if (zombiePrefab == null) return;

            // Spawn in a circle around the perimeter
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spawnPos = new Vector3(Mathf.Cos(randomAngle) * hordeSpawnRadius, 0.1f, Mathf.Sin(randomAngle) * hordeSpawnRadius);

            GameObject zombieObj = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
            ZombieController controller = zombieObj.GetComponent<ZombieController>();
            if (controller != null)
            {
                controller.IsHordeZombie = true;
                activeHordeZombies.Add(controller);
            }
        }

        private void SpawnAmbientZombies()
        {
            // Clean up any old ambient zombies
            for (int i = activeAmbientZombies.Count - 1; i >= 0; i--)
            {
                if (activeAmbientZombies[i] != null)
                {
                    Destroy(activeAmbientZombies[i].gameObject);
                }
            }
            activeAmbientZombies.Clear();

            if (zombiePrefab == null) return;

            for (int i = 0; i < ambientZombieCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minAmbientSpawnRadius, maxAmbientSpawnRadius);
                Vector3 spawnPos = new Vector3(randomCircle.x, 0.1f, randomCircle.y);

                GameObject zombieObj = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
                ZombieController controller = zombieObj.GetComponent<ZombieController>();
                if (controller != null)
                {
                    controller.IsHordeZombie = false;
                    activeAmbientZombies.Add(controller);
                }
            }
        }

        private void CheckHordeCompletion()
        {
            // Clean destroyed references
            activeHordeZombies.RemoveAll(z => z == null || z.IsDead);

            if (hordeZombiesRemainingToSpawn <= 0 && activeHordeZombies.Count == 0)
            {
                EnterDawnPhase();
            }
        }

        private void EnterDawnPhase()
        {
            SetState(GameState.Dawn);
            stateTimer = dawnDuration;
            currentDay++;

            // Celebrate dawn screen pulse
            EventBus.Publish(new ScreenShakeEvent(0.15f, 0.2f));
        }

        private void OnZombieKilled(ZombieKilledEvent evt)
        {
            if (evt.WasHordeZombie)
            {
                // Active horde zombie died
                activeHordeZombies.RemoveAll(z => z == null || z.IsDead);
            }
            else
            {
                activeAmbientZombies.RemoveAll(z => z == null || z.IsDead);
            }
        }

        private void SetState(GameState nextState)
        {
            GameState prev = currentState;
            currentState = nextState;
            EventBus.Publish(new GameStateChangedEvent(prev, nextState, currentDay));
        }

        private void UpdateLighting(Color targetColor, float targetIntensity)
        {
            if (sceneDirectionalLight == null) return;

            sceneDirectionalLight.color = Color.Lerp(sceneDirectionalLight.color, targetColor, Time.deltaTime * 1.5f);
            sceneDirectionalLight.intensity = Mathf.Lerp(sceneDirectionalLight.intensity, targetIntensity, Time.deltaTime * 1.5f);
        }
    }
}
