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
        // Make the canvas scale 0.01f matching Gracie Golem perfect styling
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
        if (canvasRt != null)
        {
            canvasRt.sizeDelta = new Vector2(97f, 37f);
        }
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Load high rounded background sprite
        Sprite bgSprite = null;
#if UNITY_EDITOR
        bgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/New Exported/Scene/WhiteBackground_HighRounded_1024x256px.png");
#endif
        if (bgSprite == null)
        {
            Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in sprites)
            {
                if (s.name == "WhiteBackground_HighRounded_1024x256px")
                {
                    bgSprite = s;
                    break;
                }
            }
        }

        // Background Panel
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.sprite = bgSprite;
        bgImg.type = UnityEngine.UI.Image.Type.Sliced;
        bgImg.color = new Color(0f, 0f, 0f, 1f); // Solid Black

        // Button/Text
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(bgObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = isGoingDown ? "Enter Cage" : "Return to Deck";
        text.fontSize = 11.0f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

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
