using UnityEngine;
using TMPro;
using System.Collections;
using Fusion.XR.Shared.Rig;

[RequireComponent(typeof(Rigidbody))]
public class SharkLifeBoatController : MonoBehaviour
{
    [Header("Vehicle Settings")]
    public float maxMotorTorque = 150f;
    public float maxSteeringAngle = 30f;
    public float brakeTorque = 300f;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider backLeftWheel;
    public WheelCollider backRightWheel;

    [Header("Interactions & Seats")]
    public Transform driversSeat;
    public Transform driveInteractable;
    public Transform exitInteractable;

    [Header("Audio Sources")]
    public AudioSource engineSound;
    public AudioSource engineStartSound;
    public AudioSource engineStopSound;
    public AudioSource honkSound;

    [Header("UI Styling")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.65f); // 65% opacity glass style
    public Color textColor = Color.white;

    private Rigidbody rb;
    private GameObject driveCanvas;
    private GameObject exitCanvas;
    private HardwareRig driverRig;
    private bool playerInsideInteractZone = false;
    private bool isDriving = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f); // Low center of mass for stability on water

        // Auto-detect references if not set in inspector
        AutoDetectReferences();

        // Create the world-space UI interaction canvases programmatically
        CreateCanvases();
    }

    private void AutoDetectReferences()
    {
        if (frontLeftWheel == null) frontLeftWheel = transform.Find("Wheels/WheelFrontL")?.GetComponent<WheelCollider>();
        if (frontRightWheel == null) frontRightWheel = transform.Find("Wheels/WheelFrontR")?.GetComponent<WheelCollider>();
        if (backLeftWheel == null) backLeftWheel = transform.Find("Wheels/WheelBackL")?.GetComponent<WheelCollider>();
        if (backRightWheel == null) backRightWheel = transform.Find("Wheels/WheelBackR")?.GetComponent<WheelCollider>();

        if (driversSeat == null) driversSeat = transform.Find("DriversSeat");
        if (driveInteractable == null) driveInteractable = transform.Find("DriveInteractable");
        if (exitInteractable == null) exitInteractable = transform.Find("Model/ExitInteractable");

        if (engineSound == null) engineSound = transform.Find("Sounds/EngineSound")?.GetComponent<AudioSource>();
        if (engineStartSound == null) engineStartSound = transform.Find("Sounds/EngineStartSound")?.GetComponent<AudioSource>();
        if (engineStopSound == null) engineStopSound = transform.Find("Sounds/EngineStopSound")?.GetComponent<AudioSource>();
        if (honkSound == null) honkSound = transform.Find("Sounds/HonkSound")?.GetComponent<AudioSource>();
    }

    private void CreateCanvases()
    {
        // 1. Create DRIVE Canvas on DriveInteractable
        if (driveInteractable != null)
        {
            driveCanvas = new GameObject("DriveCanvas");
            driveCanvas.transform.SetParent(driveInteractable);
            driveCanvas.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            driveCanvas.transform.localRotation = Quaternion.identity;
            driveCanvas.transform.localScale = new Vector3(0.007f, 0.007f, 0.007f);

            Canvas canvas = driveCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            driveCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            driveCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(driveCanvas.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = new Vector2(130f, 40f);

            UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = backgroundColor;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(bgObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "E / Click to Drive";
            text.fontSize = 10f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = textColor;

            UnityEngine.UI.Button btn = bgObj.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(EnterVehicle);

            // Ensure a trigger collider is set on the DriveInteractable for player detection
            Collider col = driveInteractable.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = driveInteractable.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(1.5f, 1.5f, 1.5f);
                box.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }

            driveCanvas.SetActive(false);
        }

        // 2. Create EXIT Canvas inside the cockpit
        if (driversSeat != null)
        {
            exitCanvas = new GameObject("ExitCanvas");
            exitCanvas.transform.SetParent(driversSeat);
            exitCanvas.transform.localPosition = new Vector3(0f, 1.2f, 0.5f); // Position right in front of the driving wheel
            exitCanvas.transform.localRotation = Quaternion.identity;
            exitCanvas.transform.localScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

            Canvas canvas = exitCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            exitCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            exitCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(exitCanvas.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = new Vector2(130f, 40f);

            UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = backgroundColor;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(bgObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "E / Click to Exit";
            text.fontSize = 10f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = textColor;

            UnityEngine.UI.Button btn = bgObj.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(ExitVehicle);

            exitCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // Face the canvases to camera
            if (driveCanvas != null && driveCanvas.activeSelf)
            {
                driveCanvas.transform.rotation = Quaternion.LookRotation(driveCanvas.transform.position - mainCam.transform.position);
            }
            if (exitCanvas != null && exitCanvas.activeSelf)
            {
                exitCanvas.transform.rotation = Quaternion.LookRotation(exitCanvas.transform.position - mainCam.transform.position);
            }
        }

        // Interaction bindings for E key / click trigger
        if (playerInsideInteractZone && !isDriving && driveCanvas != null && driveCanvas.activeSelf)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                EnterVehicle();
            }
#else
            if (Input.GetKeyDown(KeyCode.E))
            {
                EnterVehicle();
            }
#endif
        }
        else if (isDriving && exitCanvas != null && exitCanvas.activeSelf)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitVehicle();
            }
#else
            if (Input.GetKeyDown(KeyCode.E))
            {
                ExitVehicle();
            }
#endif
        }
    }

    private void FixedUpdate()
    {
        if (!isDriving)
        {
            // Apply braking automatically when driverless to avoid floating away
            ApplyBrake(brakeTorque);
            return;
        }

        // Get driving inputs
        float motor = 0f;
        float steering = 0f;

#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) motor = 1f;
            else if (kb.sKey.isPressed) motor = -1f;

            if (kb.aKey.isPressed) steering = -1f;
            else if (kb.dKey.isPressed) steering = 1f;
        }
#else
        motor = Input.GetAxis("Vertical");
        steering = Input.GetAxis("Horizontal");
#endif

        // Apply motor torque and steering angle to wheels
        float torque = motor * maxMotorTorque;
        float steer = steering * maxSteeringAngle;

        if (frontLeftWheel != null) frontLeftWheel.steerAngle = steer;
        if (frontRightWheel != null) frontRightWheel.steerAngle = steer;

        if (backLeftWheel != null) backLeftWheel.motorTorque = torque;
        if (backRightWheel != null) backRightWheel.motorTorque = torque;

        // Release brakes when driving
        ReleaseBrake();

        // Control engine sound loop pitch based on speed
        if (engineSound != null && engineSound.isPlaying)
        {
            float speedRatio = rb.linearVelocity.magnitude / 10f; // normalize speed
            engineSound.pitch = Mathf.Lerp(0.8f, 1.8f, speedRatio);
        }
    }

    public void EnterVehicle()
    {
        if (isDriving || driverRig == null) return;

        Debug.Log("[SharkLifeBoatController] Entering lifeboat...");
        isDriving = true;

        if (driveCanvas != null) driveCanvas.SetActive(false);
        if (exitCanvas != null) exitCanvas.SetActive(true);

        // Position & Anchor the local player rig in the driver's seat
        if (driversSeat != null)
        {
            driverRig.transform.SetParent(driversSeat);
            driverRig.transform.localPosition = Vector3.zero;
            driverRig.transform.localRotation = Quaternion.identity;
        }

        // Play engine start & start engine loop
        StartCoroutine(StartEngineSoundRoutine());
    }

    private IEnumerator StartEngineSoundRoutine()
    {
        if (engineStartSound != null)
        {
            engineStartSound.Play();
            yield return new WaitForSeconds(engineStartSound.clip != null ? engineStartSound.clip.length : 1.0f);
        }

        if (engineSound != null)
        {
            engineSound.loop = true;
            engineSound.Play();
        }
    }

    public void ExitVehicle()
    {
        if (!isDriving) return;

        Debug.Log("[SharkLifeBoatController] Exiting lifeboat...");
        isDriving = false;

        if (exitCanvas != null) exitCanvas.SetActive(false);
        if (driveCanvas != null && playerInsideInteractZone) driveCanvas.SetActive(true);

        // Unparent and teleport player safely to exit point
        if (driverRig != null)
        {
            driverRig.transform.SetParent(null);
            driverRig.transform.localScale = Vector3.one;

            Vector3 exitPos = exitInteractable != null ? exitInteractable.position : transform.position + new Vector3(-2f, 1.5f, 0f);
            driverRig.transform.position = exitPos;
            driverRig.transform.rotation = Quaternion.identity;
        }

        // Shut down engine loop
        if (engineSound != null) engineSound.Stop();
        if (engineStopSound != null) engineStopSound.Play();

        // Apply instant full brakes on exit
        ApplyBrake(brakeTorque);
    }

    private void ApplyBrake(float amount)
    {
        if (frontLeftWheel != null) frontLeftWheel.brakeTorque = amount;
        if (frontRightWheel != null) frontRightWheel.brakeTorque = amount;
        if (backLeftWheel != null) backLeftWheel.brakeTorque = amount;
        if (backRightWheel != null) backRightWheel.brakeTorque = amount;
    }

    private void ReleaseBrake()
    {
        ApplyBrake(0f);
    }

    // Trigger volumes on driveInteractable children redirect here via GolemTrigger patterns
    private void OnTriggerEnter(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null)
        {
            playerInsideInteractZone = true;
            driverRig = rig;
            if (driveCanvas != null && !isDriving)
            {
                driveCanvas.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null)
        {
            playerInsideInteractZone = false;
            if (driveCanvas != null)
            {
                driveCanvas.SetActive(false);
            }
            if (!isDriving)
            {
                driverRig = null;
            }
        }
    }
}
