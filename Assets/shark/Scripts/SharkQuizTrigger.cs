using UnityEngine;
using TMPro;
using Fusion.XR.Shared.Rig;

public class SharkQuizTrigger : MonoBehaviour
{
    [Header("Option Assignment")]
    public char optionLabel = 'A'; // 'A', 'B', 'C', or 'D'

    [Header("UI Styling")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.65f); // 65% opacity glass style
    public Color textColor = Color.white;

    private SharkQuizManager quizManager;
    private GameObject selectCanvas;
    private bool playerInside = false;

    private void Awake()
    {
        // Find the quiz manager in the parent hierarchy
        quizManager = GetComponentInParent<SharkQuizManager>();

        // Ensure collider is set to trigger
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Create the interaction selection canvas
        CreateSelectionCanvas();
    }

    private void CreateSelectionCanvas()
    {
        GameObject canvasObj = new GameObject("SelectOptionCanvas");
        canvasObj.transform.SetParent(transform);
        
        // Position it slightly above the trigger area at player height
        canvasObj.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        canvasObj.transform.localRotation = Quaternion.identity;
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

        // Option Text Label
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(bgObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"Option {optionLabel}";
        text.fontSize = 11.0f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        // Button component for WebXR VR controller clicks
        UnityEngine.UI.Button btn = bgObj.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(SelectThisOption);

        selectCanvas = canvasObj;
        selectCanvas.SetActive(false);
    }

    private void Update()
    {
        // Billboard rotation: Face the main camera
        Camera mainCam = Camera.main;
        if (mainCam != null && selectCanvas != null && selectCanvas.activeSelf)
        {
            selectCanvas.transform.rotation = Quaternion.LookRotation(selectCanvas.transform.position - mainCam.transform.position);
        }

        // Handle keyboard input 'E' for desktop WebXR users
        if (playerInside && selectCanvas != null && selectCanvas.activeSelf)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                SelectThisOption();
            }
#else
            if (Input.GetKeyDown(KeyCode.E))
            {
                SelectThisOption();
            }
#endif
        }
    }

    private void SelectThisOption()
    {
        if (quizManager != null)
        {
            quizManager.OnSelectOption(optionLabel);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detect local player rig or controller
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null || other.CompareTag("Player") || other.name.Contains("Rig") || other.name.Contains("Controller"))
        {
            playerInside = true;
            if (selectCanvas != null && quizManager != null && quizManager.IsQuizRunning())
            {
                selectCanvas.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null || other.CompareTag("Player") || other.name.Contains("Rig") || other.name.Contains("Controller"))
        {
            playerInside = false;
            if (selectCanvas != null)
            {
                selectCanvas.SetActive(false);
            }
        }
    }

    public void SetCanvasActive(bool active)
    {
        if (selectCanvas != null)
        {
            selectCanvas.SetActive(active && playerInside);
        }
    }
}
