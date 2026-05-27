using UnityEngine;

namespace Lab8
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(AbilitySystem))]
    [RequireComponent(typeof(InventorySystem))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Движение")]
        [SerializeField] private float jumpHeight = 1.7f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float sprintMultiplier = 1.4f;

        [Header("Камера")]
        [SerializeField] private float mouseSensitivity = 560f;
        [SerializeField] private float minPitch = -60f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Бой")]
        [SerializeField] private float meleeRange = 4f;
        [SerializeField] private float meleeCooldown = 0.28f;
        [SerializeField] private float meleeStartOffset = 0.85f;
        [SerializeField] private float abilitySpawnOffset = 0.85f;

        [Header("Взаимодействие")]
        [SerializeField] private float interactDistance = 4.2f;

        private CharacterController controller;
        private PlayerStats stats;
        private AbilitySystem abilities;
        private Camera mainCamera;

        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private float meleeTimer;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = new Vector3(0f, 1f, 0f);
            controller.stepOffset = 0.4f;
            controller.slopeLimit = 52f;

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Destroy(capsule);
            }

            stats = GetComponent<PlayerStats>();
            abilities = GetComponent<AbilitySystem>();

            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam.tag = "MainCamera";
                mainCamera = cam.GetComponent<Camera>();
            }

            yaw = transform.eulerAngles.y;
            pitch = 10f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            UiRoot ui = UiRoot.Instance;
            bool blocked = ui != null && ui.IsBlockingGameplayInput;

            if (!blocked)
            {
                HandleLook();
                HandleMove();
                HandleAttack();
                HandleAbilities();
                HandleInteraction();
            }
            else
            {
                UiRoot.Instance?.SetInteractionPrompt(string.Empty);
            }

            if (meleeTimer > 0f)
            {
                meleeTimer -= Time.deltaTime;
            }
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * mouseSensitivity * 0.01f;
            pitch -= mouseY * mouseSensitivity * 0.0065f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (mainCamera != null)
            {
                mainCamera.transform.position = transform.position + Vector3.up * 1.7f;
                mainCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                mainCamera.fieldOfView = 78f;
            }
        }

        private void HandleMove()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 input = new Vector3(horizontal, 0f, vertical);
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            float speed = stats != null ? stats.MoveSpeed : 6.5f;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed *= sprintMultiplier;
            }

            Vector3 move = (transform.right * input.x + transform.forward * input.z) * speed;

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }

                if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }

        private void HandleAttack()
        {
            if (meleeTimer > 0f || !Input.GetMouseButtonDown(0) || mainCamera == null)
            {
                return;
            }

            meleeTimer = meleeCooldown;

            Vector3 origin = mainCamera.transform.position + mainCamera.transform.forward * meleeStartOffset;
            if (Physics.Raycast(origin, mainCamera.transform.forward, out RaycastHit hit, meleeRange))
            {
                ResourceNode node = hit.collider.GetComponentInParent<ResourceNode>();
                if (node != null)
                {
                    node.Harvest(this);
                    SpawnMeleeFx(hit.point, new Color(0.85f, 0.75f, 0.45f, 1f));
                    return;
                }

                Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    float dmg = stats != null ? stats.Damage : 120f;
                    enemy.TakeDamage(dmg);
                    SpawnMeleeFx(hit.point, new Color(1f, 0.46f, 0.36f, 1f));
                    return;
                }

                SpawnMeleeFx(hit.point, new Color(0.75f, 0.75f, 0.75f, 1f));
            }
        }

        private void HandleAbilities()
        {
            if (abilities == null || mainCamera == null)
            {
                return;
            }

            for (int i = 0; i < abilities.Abilities.Count; i++)
            {
                AbilityDefinition ability = abilities.Abilities[i];
                if (ability == null)
                {
                    continue;
                }

                if (Input.GetKeyDown(ability.hotkey))
                {
                    Vector3 spawnPos = mainCamera.transform.position + mainCamera.transform.forward * abilitySpawnOffset;
                    abilities.TryCastBySlot(i, spawnPos, mainCamera.transform.forward);
                }
            }
        }

        private void HandleInteraction()
        {
            if (mainCamera == null)
            {
                return;
            }

            string prompt = string.Empty;
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            {
                MonoBehaviour[] components = hit.collider.GetComponentsInParent<MonoBehaviour>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] is IInteractable interactable)
                    {
                        prompt = interactable.GetPrompt();
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            interactable.Interact(this);
                        }

                        break;
                    }
                }
            }

            UiRoot.Instance?.SetInteractionPrompt(prompt);
        }

        private static void SpawnMeleeFx(Vector3 position, Color color)
        {
            GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fx.name = "Удар";
            fx.transform.position = position;
            fx.transform.localScale = new Vector3(0.15f, 0.15f, 0.45f);

            Collider col = fx.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            Renderer rendererComp = fx.GetComponent<Renderer>();
            if (rendererComp != null)
            {
                rendererComp.material.color = color;
            }

            Object.Destroy(fx, 0.09f);
        }
    }
}
