using System.Collections;
using UnityEngine;
using DeadDawn.Combat;
using DeadDawn.Enemy;

namespace DeadDawn.Core
{
    /// <summary>
    /// Automated defensive watchtower built at Base Level 2.
    /// Scans for zombies within perimeter range, tracks them with an elevated sniper turret,
    /// and fires automated sniper rounds to defend the base.
    /// </summary>
    public class Watchtower : MonoBehaviour, IDamageable
    {
        [Header("Tower Stats")]
        [SerializeField] private float maxHealth = 350f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float detectionRange = 16f;
        [SerializeField] private float fireInterval = 1.3f;
        [SerializeField] private float shotDamage = 35f;

        [Header("Turret Elements")]
        [SerializeField] private Transform turretPivot;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private LineRenderer tracerLine;

        private float lastFireTime = 0f;
        private Transform currentTarget;
        private bool isBuilt = true;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsBuilt { get => isBuilt; set => isBuilt = value; }

        private void Awake()
        {
            currentHealth = maxHealth;
            if (tracerLine == null)
            {
                tracerLine = GetComponent<LineRenderer>();
            }
            if (tracerLine != null)
            {
                tracerLine.enabled = false;
            }
        }

        private void Update()
        {
            if (!isBuilt || Time.timeScale <= 0f) return;

            // Target scanning and tracking
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                currentTarget = FindNearestZombie();
            }

            if (currentTarget != null)
            {
                AimTowardsTarget(currentTarget.position);

                if (Time.time >= lastFireTime + fireInterval)
                {
                    FireSniperRound(currentTarget);
                }
            }
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;

            var zombie = target.GetComponent<ZombieController>();
            if (zombie != null && zombie.IsDead) return false;

            float dist = Vector3.Distance(transform.position, target.position);
            return dist <= detectionRange;
        }

        private Transform FindNearestZombie()
        {
            ZombieController[] zombies = FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
            Transform bestTarget = null;
            float closestDist = detectionRange;

            for (int i = 0; i < zombies.Length; i++)
            {
                var z = zombies[i];
                if (z == null || z.IsDead || !z.gameObject.activeInHierarchy) continue;

                float dist = Vector3.Distance(transform.position, z.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = z.transform;
                }
            }

            return bestTarget;
        }

        private void AimTowardsTarget(Vector3 targetPos)
        {
            if (turretPivot == null) return;

            Vector3 aimDir = (targetPos - turretPivot.position);
            aimDir.y = 0f; // Flat yaw tracking
            if (aimDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(aimDir.normalized);
                turretPivot.rotation = Quaternion.Slerp(turretPivot.rotation, targetRot, Time.deltaTime * 8f);
            }
        }

        private void FireSniperRound(Transform target)
        {
            lastFireTime = Time.time;

            Vector3 fireOrigin = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 3.5f;
            Vector3 hitPos = target.position + Vector3.up * 1f;

            // Deal damage to target
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(shotDamage);
            }

            // Visual tracer line
            StartCoroutine(ShowTracerRoutine(fireOrigin, hitPos));
        }

        private IEnumerator ShowTracerRoutine(Vector3 start, Vector3 end)
        {
            if (tracerLine != null)
            {
                tracerLine.enabled = true;
                tracerLine.SetPosition(0, start);
                tracerLine.SetPosition(1, end);
                yield return new WaitForSeconds(0.08f);
                tracerLine.enabled = false;
            }
        }

        public void TakeDamage(float amount)
        {
            if (!isBuilt) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            DamagePopup.Spawn(transform.position + Vector3.up * 2f, amount, new Color(1f, 0.5f, 0.2f));

            if (currentHealth <= 0f)
            {
                // Tower disabled until repaired
                isBuilt = false;
            }
        }
    }
}
