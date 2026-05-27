using UnityEngine;

namespace Lab8
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class AbilityProjectile : MonoBehaviour
    {
        private float speed;
        private float lifetime;
        private float damage;
        private GameObject owner;
        private Color color;
        private Vector3 moveDirection;

        public void Initialize(float moveSpeed, float lifeTimeSeconds, float hitDamage, GameObject projectileOwner, Color projectileColor)
        {
            speed = Mathf.Max(2f, moveSpeed);
            lifetime = Mathf.Max(0.1f, lifeTimeSeconds);
            damage = Mathf.Max(0f, hitDamage);
            owner = projectileOwner;
            color = projectileColor;
            moveDirection = transform.forward.normalized;
            if (moveDirection.sqrMagnitude < 0.001f)
            {
                moveDirection = Vector3.forward;
            }

            Renderer rendererComp = GetComponent<Renderer>();
            if (rendererComp != null)
            {
                Material material = rendererComp.material;
                material.color = color;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * 1.8f);
                }
            }

            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
            }

            trail.time = 0.35f;
            trail.startWidth = 0.26f;
            trail.endWidth = 0.05f;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color * 0.5f, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;

            Light glow = GetComponent<Light>();
            if (glow == null)
            {
                glow = gameObject.AddComponent<Light>();
            }

            glow.type = LightType.Point;
            glow.color = color;
            glow.intensity = 1.4f;
            glow.range = 3f;

            Destroy(gameObject, lifetime);
        }

        private void Awake()
        {
            SphereCollider colliderComp = GetComponent<SphereCollider>();
            colliderComp.isTrigger = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            transform.localScale = Vector3.one * 0.32f;
        }

        private void Update()
        {
            transform.position += moveDirection * speed * Time.deltaTime;
            transform.Rotate(Vector3.forward, 680f * Time.deltaTime, Space.Self);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.isTrigger)
            {
                return;
            }

            if (owner != null && other.transform.IsChildOf(owner.transform))
            {
                return;
            }

            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            SpawnImpact();
            Destroy(gameObject);
        }

        private void SpawnImpact()
        {
            GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "Удар_Скилла";
            fx.transform.position = transform.position;
            fx.transform.localScale = Vector3.one * 0.55f;

            Collider col = fx.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            Renderer rendererComp = fx.GetComponent<Renderer>();
            if (rendererComp != null)
            {
                rendererComp.material.color = new Color(color.r, color.g, color.b, 0.95f);
            }

            Destroy(fx, 0.22f);
        }
    }
}
