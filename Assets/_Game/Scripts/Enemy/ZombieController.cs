using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DeadDawn.Combat;
using DeadDawn.Core;
using DeadDawn.Player;

namespace DeadDawn.Enemy
{
    public struct ZombieKilledEvent
    {
        public Vector3 DeathPosition;
        public bool WasHordeZombie;

        public ZombieKilledEvent(Vector3 deathPos, bool wasHorde)
        {
            DeathPosition = deathPos;
            WasHordeZombie = wasHorde;
        }
    }

    public enum ZombieState
    {
        Wandering,
        ChasingPlayer,
        AttackingPlayer,
        AttackingBaseBarrier,
        SearchingLastKnown,
        Dead
    }

    /// <summary>
    /// Core single-type Zombie AI controller.
    /// Supports ambient roaming and horde wave behaviors.
    /// Strategic aggro: does not attack base unless it actively saw the player enter inside.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ZombieController : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackInterval = 1.1f;
        [SerializeField] private float attackRange = 1.6f;

        [Header("Perception")]
        [SerializeField] private float detectionRadius = 14f;
        [SerializeField] private float fieldOfViewAngle = 140f;
        [SerializeField] private float basePerimeterRadius = 3.65f;

        [Header("State")]
        [SerializeField] private ZombieState currentState = ZombieState.Wandering;
        [SerializeField] private bool isHordeZombie = false;

        private float currentHealth;
        private float lastAttackTime;
        private bool isDead = false;

        private NavMeshAgent navAgent;
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private WorldHealthBar healthBar;

        private bool sawPlayerEnterBase = false;
        private Vector3 lastKnownPlayerPos;
        private float wanderTimer = 0f;
        private Vector3 wanderTarget;
        private IDamageable currentBarrierTarget;
        private Vector3 currentBarrierContactPoint;

        public bool IsDead => isDead;
        public bool IsHordeZombie { get => isHordeZombie; set => isHordeZombie = value; }
        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            if (basePerimeterRadius > 3.7f)
            {
                basePerimeterRadius = 3.65f;
            }

            currentHealth = maxHealth;
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.speed = moveSpeed;
                // Stopping distance comfortably inside attackRange so agent always closes in
                navAgent.stoppingDistance = 1.0f;
            }

            healthBar = GetComponent<WorldHealthBar>();
            if (healthBar == null)
            {
                healthBar = gameObject.AddComponent<WorldHealthBar>();
            }
            healthBar.SetFillColor(new Color(0.95f, 0.25f, 0.25f));
        }

        private void Start()
        {
            FindPlayerReference();
            PickNewWanderTarget();

            // Horde zombies start with initial awareness of player's general sector
            if (isHordeZombie && playerTransform != null)
            {
                lastKnownPlayerPos = playerTransform.position;
                currentState = ZombieState.SearchingLastKnown;
            }
        }

        private void FindPlayerReference()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        private void Update()
        {
            if (isDead || Time.timeScale <= 0f) return;

            if (playerTransform == null)
            {
                FindPlayerReference();
                if (playerTransform == null) return;
            }

            PerceivePlayer();
            ExecuteStateMachine();
        }

        private float GetHorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void PerceivePlayer()
        {
            float horizontalDist = GetHorizontalDistance(transform.position, playerTransform.position);
            bool playerInBase = IsInsideBase(playerTransform.position);

            // Check direct sight / proximity on horizontal plane
            bool playerDetected = false;
            if (horizontalDist <= detectionRadius)
            {
                Vector3 toPlayerFlat = playerTransform.position - transform.position;
                toPlayerFlat.y = 0f;
                float angle = Vector3.Angle(transform.forward, toPlayerFlat.normalized);

                // Close proximity senses player from any angle, wider angle for frontal view
                if (horizontalDist <= 5f || angle <= fieldOfViewAngle * 0.5f)
                {
                    playerDetected = true;
                }
            }

            if (playerDetected)
            {
                lastKnownPlayerPos = playerTransform.position;

                // Requirement 6: Did the zombie see the player enter the base?
                if (playerInBase)
                {
                    sawPlayerEnterBase = true;
                }

                // Hysteresis: prevent state jitter when standing still near the player
                float enterRange = attackRange;
                float exitRange = attackRange * 1.35f;

                if (currentState == ZombieState.AttackingPlayer)
                {
                    if (horizontalDist > exitRange)
                    {
                        currentState = ZombieState.ChasingPlayer;
                    }
                }
                else if (currentState == ZombieState.AttackingBaseBarrier)
                {
                    // While attacking the barrier/door, stay in this state until:
                    // 1. The player is no longer inside the base
                    // 2. Or the barrier was destroyed / opened
                    if (!playerInBase)
                    {
                        currentBarrierTarget = null;
                        currentState = ZombieState.ChasingPlayer;
                    }
                    else if (currentBarrierTarget is BaseGate bg && bg.IsOpen)
                    {
                        currentBarrierTarget = null;
                        currentState = ZombieState.ChasingPlayer;
                    }
                }
                else
                {
                    if (horizontalDist <= enterRange)
                    {
                        currentState = ZombieState.AttackingPlayer;
                    }
                    else
                    {
                        currentState = ZombieState.ChasingPlayer;
                    }
                }
            }
            else
            {
                // Lost direct sight
                if (currentState == ZombieState.AttackingBaseBarrier)
                {
                    // Continue attacking the barrier if player is still in base and barrier is intact
                    if (!playerInBase || (currentBarrierTarget is BaseGate bg && bg.IsOpen))
                    {
                        currentBarrierTarget = null;
                        currentState = ZombieState.SearchingLastKnown;
                    }
                }
                else if (currentState == ZombieState.ChasingPlayer || currentState == ZombieState.AttackingPlayer)
                {
                    // If saw player enter base and there is a blocking barrier, engage it!
                    if (sawPlayerEnterBase && TryFindBlockingBarrier(out IDamageable barrier, out Vector3 barrierContact))
                    {
                        currentBarrierTarget = barrier;
                        currentBarrierContactPoint = barrierContact;
                        currentState = ZombieState.AttackingBaseBarrier;
                    }
                    else
                    {
                        currentState = ZombieState.SearchingLastKnown;
                    }
                }
            }
        }

        private void ExecuteStateMachine()
        {
            switch (currentState)
            {
                case ZombieState.Wandering:
                    HandleWandering();
                    break;

                case ZombieState.ChasingPlayer:
                    HandleChasingPlayer();
                    break;

                case ZombieState.AttackingPlayer:
                    HandleAttackingPlayer();
                    break;

                case ZombieState.AttackingBaseBarrier:
                    HandleAttackingBaseBarrier();
                    break;

                case ZombieState.SearchingLastKnown:
                    HandleSearchingLastKnown();
                    break;
            }
        }

        private void HandleWandering()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f || Vector3.Distance(transform.position, wanderTarget) < 1.5f)
            {
                PickNewWanderTarget();
            }

            MoveTowards(wanderTarget, moveSpeed * 0.65f);
        }

        private void PickNewWanderTarget()
        {
            wanderTimer = Random.Range(4f, 8f);
            Vector2 randomPoint = Random.insideUnitCircle * Random.Range(6f, 15f);
            Vector3 candidate = transform.position + new Vector3(randomPoint.x, 0f, randomPoint.y);

            // Do not wander inside the base if haven't seen player
            if (IsInsideBase(candidate))
            {
                candidate = (candidate.normalized) * (basePerimeterRadius + 2f);
            }

            wanderTarget = candidate;
        }

        private void HandleChasingPlayer()
        {
            float horizontalDist = GetHorizontalDistance(transform.position, playerTransform.position);
            if (horizontalDist <= attackRange)
            {
                currentState = ZombieState.AttackingPlayer;
                HandleAttackingPlayer();
                return;
            }

            bool playerInBase = IsInsideBase(playerTransform.position);
            bool zombieOutsideBase = !IsInsideBase(transform.position);

            // If player is inside the base AND zombie is outside, check for blocking door
            if (playerInBase && zombieOutsideBase)
            {
                sawPlayerEnterBase = true;
                if (TryFindBlockingBarrier(out IDamageable barrier, out Vector3 barrierContact))
                {
                    currentBarrierTarget = barrier;
                    currentBarrierContactPoint = barrierContact;
                    currentState = ZombieState.AttackingBaseBarrier;
                    HandleAttackingBaseBarrier();
                    return;
                }

                // Navigate around the perimeter to the door side of the wall
                BaseGate closedGate = GetPrimaryClosedGate();
                if (closedGate != null)
                {
                    Vector3 doorApproach = GetDoorApproachPosition(closedGate);
                    MoveTowards(doorApproach, moveSpeed);
                    return;
                }
            }

            MoveTowards(playerTransform.position, moveSpeed);
        }

        private void HandleAttackingPlayer()
        {
            StopMovement();
            LookTowards(playerTransform.position);

            float horizontalDist = GetHorizontalDistance(transform.position, playerTransform.position);
            if (horizontalDist > attackRange * 1.35f)
            {
                currentState = ZombieState.ChasingPlayer;
                return;
            }

            if (Time.time >= lastAttackTime + attackInterval)
            {
                lastAttackTime = Time.time;
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    DamagePopup.Spawn(playerTransform.position, attackDamage, Color.red);
                }
            }
        }

        private void HandleAttackingBaseBarrier()
        {
            if (currentBarrierTarget == null)
            {
                currentState = ZombieState.ChasingPlayer;
                return;
            }

            Component barrierComp = currentBarrierTarget as Component;
            if (barrierComp == null || !barrierComp.gameObject.activeInHierarchy)
            {
                currentBarrierTarget = null;
                currentState = ZombieState.ChasingPlayer;
                return;
            }

            // If barrier is a gate and it opened -> stop attacking and rush inside!
            if (currentBarrierTarget is BaseGate gate && gate.IsOpen)
            {
                currentBarrierTarget = null;
                currentState = ZombieState.ChasingPlayer;
                return;
            }

            // Target position to approach and attack
            Vector3 targetPos = currentBarrierContactPoint;
            if (currentBarrierTarget is BaseGate bg)
            {
                targetPos = bg.GetGatewayCenter();
            }
            else if (targetPos == Vector3.zero)
            {
                targetPos = barrierComp.transform.position;
            }

            float dist = GetHorizontalDistance(transform.position, targetPos);
            float effectiveAttackRange = attackRange * 1.35f;

            if (dist > effectiveAttackRange)
            {
                MoveTowards(targetPos, moveSpeed);
            }
            else
            {
                StopMovement();
                LookTowards(targetPos);

                if (Time.time >= lastAttackTime + attackInterval)
                {
                    lastAttackTime = Time.time;
                    currentBarrierTarget.TakeDamage(attackDamage);
                    Vector3 popupPos = targetPos + Vector3.up * 1.2f;
                    DamagePopup.Spawn(popupPos, attackDamage, new Color(1f, 0.6f, 0.2f));

                    // If barrier broke from this attack and opened
                    if (currentBarrierTarget is BaseGate bgAfter && bgAfter.IsOpen)
                    {
                        currentBarrierTarget = null;
                        currentState = ZombieState.ChasingPlayer;
                    }
                }
            }
        }

        private void HandleSearchingLastKnown()
        {
            // If last known position was inside the base and zombie is outside, check for barrier
            if (IsInsideBase(lastKnownPlayerPos) && !IsInsideBase(transform.position))
            {
                if (TryFindBlockingBarrier(out IDamageable barrier, out Vector3 barrierContact))
                {
                    currentBarrierTarget = barrier;
                    currentBarrierContactPoint = barrierContact;
                    currentState = ZombieState.AttackingBaseBarrier;
                    HandleAttackingBaseBarrier();
                    return;
                }

                // Navigate towards the door side of the wall to enter
                BaseGate closedGate = GetPrimaryClosedGate();
                if (closedGate != null)
                {
                    Vector3 doorApproach = GetDoorApproachPosition(closedGate);
                    MoveTowards(doorApproach, moveSpeed);
                    return;
                }
            }

            float dist = GetHorizontalDistance(transform.position, lastKnownPlayerPos);
            if (dist > 1.5f)
            {
                MoveTowards(lastKnownPlayerPos, moveSpeed);
            }
            else
            {
                // Reached last known point without seeing player -> resume wandering
                currentState = ZombieState.Wandering;
                PickNewWanderTarget();
            }
        }

        private BaseGate GetPrimaryClosedGate()
        {
            for (int i = 0; i < BaseGate.ActiveGates.Count; i++)
            {
                if (BaseGate.ActiveGates[i] != null && !BaseGate.ActiveGates[i].IsOpen)
                    return BaseGate.ActiveGates[i];
            }
            return null;
        }

        private Vector3 GetDoorApproachPosition(BaseGate gate)
        {
            if (gate == null) return new Vector3(4.95f, 0f, 0f);

            Vector3 gateCenter = gate.GetGatewayCenter();
            Vector3 outwardDir = gateCenter;
            outwardDir.y = 0f;
            if (outwardDir.sqrMagnitude > 0.01f)
            {
                outwardDir.Normalize();
            }
            else
            {
                outwardDir = Vector3.right;
            }

            return gateCenter + outwardDir * 1.2f;
        }

        private bool TryFindBlockingBarrier(out IDamageable barrier, out Vector3 barrierPos)
        {
            barrier = null;
            barrierPos = Vector3.zero;

            // 1. Proximity check for closed perimeter door on the door side
            for (int g = 0; g < BaseGate.ActiveGates.Count; g++)
            {
                var gate = BaseGate.ActiveGates[g];
                if (gate != null && !gate.IsOpen)
                {
                    Vector3 gateCenter = gate.GetGatewayCenter();
                    float distToGate = GetHorizontalDistance(transform.position, gateCenter);
                    // If zombie is on the door side near the gate (within 4.2m)
                    if (distToGate <= 4.2f)
                    {
                        barrier = gate;
                        barrierPos = gateCenter;
                        return true;
                    }
                }
            }

            // 2. Raycast towards player specifically checking for closed BaseGate
            Vector3 rayOrigin = transform.position + Vector3.up * 0.8f;
            Vector3 rayDir = (playerTransform.position - transform.position).normalized;
            float rayDist = Mathf.Min(Vector3.Distance(transform.position, playerTransform.position), 12f);

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, rayDist);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.CompareTag("Player") || hit.collider.gameObject == gameObject) continue;
                if (hit.collider.GetComponentInParent<ZombieController>() != null) continue;

                // Check exclusively for BaseGate - walls cannot be targeted or brought down
                var gate = hit.collider.GetComponentInParent<BaseGate>();
                if (gate != null)
                {
                    if (!gate.IsOpen)
                    {
                        barrier = gate;
                        barrierPos = gate.GetGatewayCenter();
                        return true;
                    }
                    continue; // Skip if open
                }
            }

            return false;
        }

        public bool IsInsideBase(Vector3 pos)
        {
            return Mathf.Abs(pos.x) <= basePerimeterRadius && Mathf.Abs(pos.z) <= basePerimeterRadius;
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.speed = speed;
                navAgent.SetDestination(target);
            }
            else
            {
                // Fail-safe Kinematic pathing
                Vector3 moveDir = (target - transform.position);
                moveDir.y = 0f;
                if (moveDir.sqrMagnitude > 0.04f)
                {
                    moveDir.Normalize();
                    transform.position += moveDir * (speed * Time.deltaTime);
                    LookTowards(target);
                }
            }
        }

        private void StopMovement()
        {
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
            }
        }

        private void LookTowards(Vector3 target)
        {
            Vector3 dir = (target - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion rot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);

            // Overhead floating damage indicator
            DamagePopup.Spawn(transform.position, amount, Color.yellow);

            // Overhead health bar update
            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth, maxHealth);
            }

            // Immediately aggro if hit while unaware
            if (playerTransform != null && (currentState == ZombieState.Wandering || currentState == ZombieState.SearchingLastKnown))
            {
                lastKnownPlayerPos = playerTransform.position;
                currentState = ZombieState.ChasingPlayer;
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            StopMovement();

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            EventBus.Publish(new ZombieKilledEvent(transform.position, isHordeZombie));

            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.position;

            // Wobble and sink into the ground
            while (elapsed < 1.1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 1.1f;
                transform.position = startPos - Vector3.up * (t * 0.9f);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
