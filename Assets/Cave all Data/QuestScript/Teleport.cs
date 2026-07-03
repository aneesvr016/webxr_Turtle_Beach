using UnityEngine;
using TMPro;
using Fusion.XR.Shared.Rig;

public class Teleport : MonoBehaviour
{
    [Tooltip("Target warp position")]
    public Transform destination;

    private GameObject interactionCanvas;
    private bool playerInside = false;
    private HardwareRig activePlayerRig;

    private void Awake()
    {
        // Setup trigger collider automatically on awake
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add capsule or box trigger
            col = gameObject.AddComponent<BoxCollider>();
        }
        col.isTrigger = true;

        SetupInteractionCanvas();
    }

    private void SetupInteractionCanvas()
    {
        // Search if we already have one
        Transform canvasT = transform.Find("InteractionCanvas");
        if (canvasT != null)
        {
            interactionCanvas = canvasT.gameObject;
            interactionCanvas.SetActive(false);
            
            var btn = interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Interact);
            }
        }
        else
        {
            // Dynamically clone from Gracie's trigger as template at runtime
            var template = GameObject.Find("Cave/Characters/Granite Golem(Gracie)/Trigger Event/InteractionCanvas");
            if (template != null)
            {
                interactionCanvas = Instantiate(template, transform);
                interactionCanvas.name = "InteractionCanvas";
                
                // Position it float above the teleport platform
                interactionCanvas.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                interactionCanvas.transform.localRotation = Quaternion.identity;
                interactionCanvas.transform.localScale = new Vector3(0.007f, 0.007f, 0.007f);
                
                // Update text to "Warp" or "Teleport"
                var tmp = interactionCanvas.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "Teleport";
                }

                interactionCanvas.SetActive(false);
                
                // Wire up listener
                var btn = interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(Interact);
                }
            }
        }
    }

    private void Update()
    {
        if (playerInside && interactionCanvas != null && interactionCanvas.activeSelf)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                Interact();
            }
#else
            if (Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
#endif
        }
    }

    private void Interact()
    {
        ExecuteTeleport();
    }

    private void ExecuteTeleport()
    {
        if (activePlayerRig != null && destination != null)
        {
            Debug.Log($"[Teleport] Local player triggered teleport to: {destination.name} ({destination.position})");
            
            // Play AudioSource if it exists
            AudioSource audio = GetComponent<AudioSource>();
            if (audio == null) audio = GetComponentInParent<AudioSource>();
            if (audio != null)
            {
                audio.Play();
            }

            // Execute teleport with polished headset screen fade (works wonderfully for VR/PC)
            activePlayerRig.StartCoroutine(activePlayerRig.FadedTeleport(destination.position));

            // Hide the interaction button once warp begins
            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detect local player HardwareRig
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null && destination != null)
        {
            playerInside = true;
            activePlayerRig = rig;

            if (interactionCanvas != null && !interactionCanvas.activeSelf)
            {
                interactionCanvas.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null)
        {
            playerInside = false;
            activePlayerRig = null;

            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }
        }
    }
}

