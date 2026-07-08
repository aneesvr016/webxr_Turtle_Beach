using UnityEngine;
using TMPro;
using Fusion.XR.Shared.Rig;

public class SharkCageTeleport : MonoBehaviour
{
    [Header("Teleport Settings")]
    public bool isGoingDown = true; // True if teleporting to cage, False if teleporting to deck
    public Vector3 customDestination = Vector3.zero; // Optional override

    [Header("UI Styling")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.65f); // 65% opacity glass style
    public Color textColor = Color.white;

    private GameObject interactionCanvas;
    private bool playerInside = false;
    private HardwareRig activePlayerRig;

    private void Awake()
    {
        // Ensure collider is configured as a trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        col.isTrigger = true;

        // Create the interaction canvas programmatically
        CreateInteractionCanvas();
    }

    private void CreateInteractionCanvas()
    {
        GameObject canvasObj = new GameObject("CageTeleportCanvas");
        canvasObj.transform.SetParent(transform);
        
        // Position it floating nicely above the platform
        canvasObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        canvasObj.transform.localRotation = Quaternion.identity;
        // Make the canvas small (0.0018 scale) just like the clean speech bubbles
        canvasObj.transform.localScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Background Panel
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(140f, 45f);

        UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        
        // Downward teleport canvas has a clean solid white background. Upward has the dark glass look.
        Color bgCol = isGoingDown ? Color.white : backgroundColor;
        bgImg.color = bgCol;

        // Button/Text
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(bgObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = isGoingDown ? "E / Click to Enter\nResearch Cage" : "E / Click to Return\nto Boat Deck";
        text.fontSize = 9.0f;
        text.alignment = TextAlignmentOptions.Center;
        
        // Text is dark charcoal on white background, white on dark glass
        text.color = isGoingDown ? new Color(0.12f, 0.12f, 0.12f, 1f) : textColor;

        // Add Button for WebXR controller pointer support
        UnityEngine.UI.Button btn = bgObj.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(Interact);

        interactionCanvas = canvasObj;
        interactionCanvas.SetActive(false);
    }

    private void Update()
    {
        // Facing camera dynamically (Billboard effect)
        Camera mainCam = Camera.main;
        if (mainCam != null && interactionCanvas != null && interactionCanvas.activeSelf)
        {
            interactionCanvas.transform.rotation = Quaternion.LookRotation(interactionCanvas.transform.position - mainCam.transform.position);
        }

        // Allow keyboard 'E' trigger for PC WebXR players
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
        if (activePlayerRig != null)
        {
            Vector3 targetPos;
            if (customDestination != Vector3.zero)
            {
                targetPos = customDestination;
            }
            else
            {
                // Shifted destinations slightly leftwards on X axis relative to triggers to match layout request
                targetPos = isGoingDown ? new Vector3(-0.44f, 1.5f, -0.07f) : new Vector3(2.22f, 15.5f, 1.5f);
            }

            Debug.Log($"[SharkCageTeleport] Teleporting player to: {targetPos}");

            // Play teleport sound if available
            AudioSource audio = GetComponent<AudioSource>();
            if (audio == null) audio = GetComponentInParent<AudioSource>();
            if (audio != null)
            {
                audio.Play();
            }

            // Faded transition for both VR Headset users and PC players (prevents motion sickness)
            activePlayerRig.StartCoroutine(activePlayerRig.FadedTeleport(targetPos));

            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null)
        {
            playerInside = true;
            activePlayerRig = rig;
            if (interactionCanvas != null)
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
