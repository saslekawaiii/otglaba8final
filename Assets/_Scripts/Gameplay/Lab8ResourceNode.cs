using UnityEngine;

namespace Lab8
{
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Wood;
        [SerializeField] private int amountPerHit = 1;
        [SerializeField] private float hitCooldown = 0.25f;

        private float hitTimer;

        public bool Harvest(PlayerController player)
        {
            if (player == null)
            {
                return false;
            }

            if (hitTimer > 0f)
            {
                return false;
            }

            hitTimer = hitCooldown;

            InventorySystem inventory = player.GetComponent<InventorySystem>();
            if (inventory != null)
            {
                string itemId = resourceType == ResourceType.Wood ? "wood" : "stone";
                inventory.Add(itemId, Mathf.Max(1, amountPerHit));
            }

            SpawnHitFx();
            return true;
        }

        public void Configure(ResourceType type, int amountPerHarvest)
        {
            resourceType = type;
            amountPerHit = Mathf.Max(1, amountPerHarvest);
        }

        private void Update()
        {
            if (hitTimer > 0f)
            {
                hitTimer -= Time.deltaTime;
            }
        }

        private void SpawnHitFx()
        {
            GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "Ресурс_Эффект";
            fx.transform.position = transform.position + Vector3.up * 1.2f;
            fx.transform.localScale = Vector3.one * 0.25f;

            Collider col = fx.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            Renderer rendererComp = fx.GetComponent<Renderer>();
            if (rendererComp != null)
            {
                rendererComp.material.color = resourceType == ResourceType.Wood
                    ? new Color(0.62f, 0.37f, 0.2f, 0.95f)
                    : new Color(0.7f, 0.7f, 0.75f, 0.95f);
            }

            Destroy(fx, 0.18f);
        }
    }
}
