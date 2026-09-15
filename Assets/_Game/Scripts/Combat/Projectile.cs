using UnityEngine;
using DeadDawn.Core;

namespace DeadDawn.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }

    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 30f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float lifeTime = 2.5f;
        [SerializeField] private float hitRadius = 0.35f;
        [SerializeField] private LayerMask hitMask = ~0;

        private float spawnTime;
        private bool hasHit = false;

        public void Configure(float customDamage, float customSpeed)
        {
            damage = customDamage;
            speed = customSpeed;
        }

        private void OnEnable()
        {
            spawnTime = Time.time;
            hasHit = false;
        }

        private void Update()
        {
            if (hasHit) return;

            float moveDistance = speed * Time.deltaTime;
            Vector3 direction = transform.forward;
            Vector3 origin = transform.position;

            // Continuous SphereCast sweep ensures 100% collision detection at high speed
            RaycastHit[] hits = Physics.SphereCastAll(origin, hitRadius, direction, moveDistance, hitMask, QueryTriggerInteraction.Collide);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider == null) continue;

                    // Ignore player or projectile itself
                    if (hit.collider.CompareTag("Player") || hit.collider.gameObject == gameObject) continue;

                    // Check for damageable target (zombie, barricade, etc.)
                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        hasHit = true;
                        damageable.TakeDamage(damage);
                        Recycle();
                        return;
                    }

                    // Solid environment obstacle impact (ignore ground plane)
                    if (!hit.collider.isTrigger)
                    {
                        if (hit.collider.gameObject.name.Contains("Ground") || hit.collider.gameObject.layer == 11) continue;
                        hasHit = true;
                        Recycle();
                        return;
                    }
                }
            }

            transform.position += direction * moveDistance;

            if (Time.time - spawnTime >= lifeTime)
            {
                Recycle();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;
            if (other.CompareTag("Player") || other.isTrigger || other.gameObject == gameObject) return;

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                hasHit = true;
                damageable.TakeDamage(damage);
                Recycle();
                return;
            }

            hasHit = true;
            Recycle();
        }

        private void Recycle()
        {
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.ReturnToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
