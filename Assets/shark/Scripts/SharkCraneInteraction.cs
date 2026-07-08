using UnityEngine;
using TMPro;
using Fusion.XR.Shared.Rig;

public class SharkCraneInteraction : MonoBehaviour
{
    [Header("Crane Settings")]
    public GameObject staticBoat; // FastRescueBoat_FastRescueBoat_0 (1)
    public GameObject lifeBoatPrefab; // LifeBoat Prefab

    [Header("UI Styling")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.65f); // 65% opacity glass style
    public Color textColor = Color.white;

    private GameObject interactionCanvas;
    private bool playerInside = false;
    private bool isLowered = false;

    private void Awake()
    {
        // Try to automatically find the static boat in the scene if not assigned
        if (staticBoat == null)
        {
            staticBoat = GameObject.Find("FastRescueBoat_FastRescueBoat_0 (1)");
        }

        // Try to load the LifeBoat prefab if not assigned
        if (lifeBoatPrefab == null)
        {
#if UNITY_EDITOR
            lifeBoatPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/shark/The Great White Shark Virtual World/LifeBoat.prefab");
#endif
        }

        // Ensure collider is configured as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Create the interaction canvas programmatically
        CreateInteractionCanvas();
    }

    private void CreateInteractionCanvas()
    {
        GameObject canvasObj = new GameObject("CraneInteractionCanvas");
        // Keep it completely unparented to avoid any scale inheritance from Crane or Boat
        canvasObj.transform.SetParent(null);
        
        // Position it nicely above the crane controller in world space
        canvasObj.transform.position = transform.position + new Vector3(0f, 1.4f, 0f);
        canvasObj.transform.localRotation = Quaternion.identity;
        // Small scale (0.0018) matching other WebXR buttons perfectly
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
        bgImg.color = backgroundColor;

        // Button/Text
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(bgObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "E / Click to\nLower Lifeboat";
        text.fontSize = 9.0f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor;

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
        if (playerInside && interactionCanvas != null && interactionCanvas.activeSelf && !isLowered)
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
        if (isLowered) return;

        Debug.Log("[SharkCraneInteraction] Lowering lifeboat triggered.");
        isLowered = true;

        if (interactionCanvas != null)
        {
            interactionCanvas.SetActive(false);
        }

        // Start the realistic slow boat lowering animation
        StartCoroutine(LowerBoatRoutine());
    }

    private System.Collections.IEnumerator LowerBoatRoutine()
    {
        // Find RB_Crane003_Chrome_0 dynamically (rope/wire mechanism)
        GameObject chromeObj = GameObject.Find("RB_Crane003_Chrome_0");
        if (chromeObj == null) chromeObj = GameObject.Find("Shark data/The Great White Shark/Crane With Boat/RB_Crane003_Chrome_0");

        // Play lowering sound / motor engine sound
        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null) audio = GetComponentInParent<AudioSource>();
        if (audio != null)
        {
            audio.Play();
        }

        Vector3 startPos = Vector3.zero;
        Vector3 chromeStartPos = Vector3.zero;

        if (staticBoat != null)
        {
            startPos = staticBoat.transform.position;
        }
        if (chromeObj != null)
        {
            chromeStartPos = chromeObj.transform.position;
        }

        float duration = 4.5f; // Beautiful, slow and realistic descent over 4.5 seconds
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (staticBoat != null)
            {
                staticBoat.transform.position = Vector3.Lerp(startPos, new Vector3(startPos.x, 11.62f, startPos.z), t);
            }
            if (chromeObj != null)
            {
                // Lower the rope/chrome part down alongside the boat by the same amount (4.59 units down)
                chromeObj.transform.position = Vector3.Lerp(chromeStartPos, new Vector3(chromeStartPos.x, chromeStartPos.y - 4.59f, chromeStartPos.z), t);
            }
            yield return null;
        }

        if (staticBoat != null)
        {
            staticBoat.transform.position = new Vector3(startPos.x, 11.62f, startPos.z);
            staticBoat.SetActive(false);
        }
        if (chromeObj != null)
        {
            chromeObj.transform.position = new Vector3(chromeStartPos.x, chromeStartPos.y - 4.59f, chromeStartPos.z);
        }

        // Now spawn the dynamic playable lifeboat in the water at sea level
        if (lifeBoatPrefab != null)
        {
            Vector3 spawnPos = new Vector3(17.93f, 11.62f, 10.07f);
            Quaternion spawnRot = Quaternion.Euler(0f, 270f, 0f); // Default upright lifeboat rotation
            
            GameObject instantiatedBoat = Instantiate(lifeBoatPrefab, spawnPos, spawnRot);
            instantiatedBoat.name = "LifeBoat";
        }
        else
        {
            Debug.LogError("[SharkCraneInteraction] Lifeboat Prefab is null! Can't spawn.");
        }

        // Wait a brief moment before rewinding the crane back up for the next boat spawn
        yield return new WaitForSeconds(1.5f);

        // Reset positions back to start for the next interaction (allows infinite spawning!)
        if (staticBoat != null)
        {
            staticBoat.transform.position = startPos;
            staticBoat.SetActive(true);
        }
        if (chromeObj != null)
        {
            chromeObj.transform.position = chromeStartPos;
        }

        isLowered = false;

        // If player is still inside trigger, show the canvas again!
        if (playerInside && interactionCanvas != null)
        {
            interactionCanvas.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null || other.CompareTag("Player") || other.name.Contains("Rig") || other.name.Contains("Controller"))
        {
            playerInside = true;
            if (interactionCanvas != null && !isLowered)
            {
                interactionCanvas.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null || other.CompareTag("Player") || other.name.Contains("Rig") || other.name.Contains("Controller"))
        {
            playerInside = false;
            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }
        }
    }
}
