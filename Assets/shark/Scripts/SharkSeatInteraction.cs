using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion.XR.Shared.Rig;

public class SharkSeatInteraction : MonoBehaviour
{
    [Header("Seating Settings")]
    public Vector3 sitOffset = new Vector3(0f, 0.45f, 0f);
    public Vector3 standOffset = new Vector3(0f, 0.05f, 0.65f); // Step forward when standing up

    [Header("UI Styling")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 1.0f); // Match solid black rounded style
    public Color textColor = Color.white;

    private GameObject interactionCanvas;
    private TextMeshProUGUI buttonText;
    private bool playerInside = false;
    private bool isSeated = false;
    private HardwareRig activePlayerRig;

    // Track disabled movement components to restore them on stand
    private List<MonoBehaviour> disabledLocomotions = new List<MonoBehaviour>();
    private UnityEngine.AI.NavMeshAgent navAgent;
    private bool originalAgentState = false;

    private Vector3 originalRigPosition;
    private Quaternion originalRigRotation;

    private void Awake()
    {
        // Add a trigger collider dynamically if one doesn't exist
        // This avoids modifying the chair's solid collider, allowing the player to walk up to it
        BoxCollider triggerCol = gameObject.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        // Set trigger size to a nice interactive zone around the chair
        triggerCol.size = new Vector3(1.2f, 1.2f, 1.2f);
        triggerCol.center = new Vector3(0f, 0.3f, 0f);

        // Create the interaction canvas programmatically
        CreateInteractionCanvas();
    }

    private void CreateInteractionCanvas()
    {
        GameObject canvasObj = new GameObject("SeatInteractionCanvas");
        canvasObj.transform.SetParent(transform);
        
        // Position it nicely above the chair seat
        canvasObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        canvasObj.transform.localRotation = Quaternion.identity;
        // Make scale small to match the beautiful WebXR style
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
        bgImg.color = backgroundColor;

        // Button/Text
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(bgObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "E / Sit Down";
        buttonText.fontSize = 11.0f;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = textColor;

        // Add Button for WebXR controller pointer support
        UnityEngine.UI.Button btn = bgObj.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(Interact);

        interactionCanvas = canvasObj;
        interactionCanvas.SetActive(false);
    }

    private void Update()
    {
        // Billboard effect: canvas always faces the main camera
        Camera mainCam = Camera.main;
        if (mainCam != null && interactionCanvas != null && interactionCanvas.activeSelf)
        {
            interactionCanvas.transform.rotation = Quaternion.LookRotation(interactionCanvas.transform.position - mainCam.transform.position);
        }

        // Allow keyboard 'E' trigger for PC players
        if (interactionCanvas != null && interactionCanvas.activeSelf)
        {
            if (isSeated || playerInside)
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
    }

    private void Interact()
    {
        if (isSeated)
        {
            StandUp();
        }
        else
        {
            SitDown();
        }
    }

    private void SitDown()
    {
        if (activePlayerRig == null) return;

        isSeated = true;
        Debug.Log($"[SharkSeatInteraction] Player sitting down on: {gameObject.name}");

        // 1. Calculate look direction based on chair rotation style
        Vector3 lookDir;
        if (Mathf.Abs(transform.eulerAngles.x - 270f) < 10f || Mathf.Abs(transform.eulerAngles.x - 90f) < 10f)
        {
            // Legacy FBX rotated chair
            lookDir = -transform.up;
        }
        else
        {
            // Standard upright chair
            lookDir = transform.forward;
        }
        lookDir.y = 0f;
        lookDir.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);

        // 2. Position and rotate player rig at seat position
        Vector3 seatWorldPos = transform.position + sitOffset;
        activePlayerRig.transform.position = seatWorldPos;
        activePlayerRig.transform.rotation = targetRotation;

        // Parent player rig to chair so they move/rotate together (important for ships)
        activePlayerRig.transform.SetParent(transform);

        // 3. Disable player movement/locomotion scripts
        disabledLocomotions.Clear();
        var behaviors = activePlayerRig.GetComponentsInChildren<MonoBehaviour>();
        foreach (var b in behaviors)
        {
            if (b == null || b == this) continue;
            string ns = b.GetType().Namespace;
            string name = b.GetType().Name;
            if (b.enabled && ns != null && (ns.Contains("Locomotion") || ns.Contains("Desktop") || name.Contains("Locomotion") || name.Contains("Movement") || name.Contains("Teleport")))
            {
                b.enabled = false;
                disabledLocomotions.Add(b);
            }
        }

        navAgent = activePlayerRig.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            originalAgentState = navAgent.enabled;
            navAgent.enabled = false;
        }

        // 4. Update UI Canvas to "Stand" and position it beautifully in front of the seated player
        if (buttonText != null)
        {
            buttonText.text = "E / Stand Up";
        }

        if (interactionCanvas != null)
        {
            // Position canvas float directly in front of the seated player
            interactionCanvas.transform.position = transform.position + lookDir * 0.6f + Vector3.up * 0.65f;
            interactionCanvas.SetActive(true);
        }
    }

    private void StandUp()
    {
        if (activePlayerRig == null) return;

        isSeated = false;
        Debug.Log($"[SharkSeatInteraction] Player standing up from: {gameObject.name}");

        // 1. Unparent player rig
        activePlayerRig.transform.SetParent(null);
        activePlayerRig.transform.localScale = Vector3.one; // Safety reset scale

        // 2. Determine look/exit direction
        Vector3 lookDir;
        if (Mathf.Abs(transform.eulerAngles.x - 270f) < 10f || Mathf.Abs(transform.eulerAngles.x - 90f) < 10f)
        {
            lookDir = -transform.up;
        }
        else
        {
            lookDir = transform.forward;
        }
        lookDir.y = 0f;
        lookDir.Normalize();

        // 3. Move player safely in front of the chair to avoid collision glitches
        Vector3 exitPos = transform.position + lookDir * standOffset.z + Vector3.up * standOffset.y;
        activePlayerRig.transform.position = exitPos;

        // 4. Re-enable locomotion and movement scripts
        foreach (var b in disabledLocomotions)
        {
            if (b != null)
            {
                b.enabled = true;
            }
        }
        disabledLocomotions.Clear();

        if (navAgent != null)
        {
            navAgent.enabled = originalAgentState;
        }

        // 5. Restore canvas text to "Sit" and position it above the chair
        if (buttonText != null)
        {
            buttonText.text = "E / Sit Down";
        }

        if (interactionCanvas != null)
        {
            interactionCanvas.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            if (!playerInside)
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

            if (interactionCanvas != null && !isSeated)
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

            if (!isSeated)
            {
                activePlayerRig = null;
                if (interactionCanvas != null)
                {
                    interactionCanvas.SetActive(false);
                }
            }
        }
    }
}
