using UnityEngine;
using Fusion.XR.Shared.Rig;
using UnityEngine.AI;

namespace AB.TurtleBeach
{
    [RequireComponent(typeof(Collider))]
    public class TrainInteraction : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject interactionCanvasPrefab;
        public GameObject exitCanvasPrefab;

        [Header("Target References")]
        [Tooltip("The Train with anim parent transform to prevent scale distortion.")]
        public Transform parentTarget;
        
        [Tooltip("The local position on the train deck where the player will sit.")]
        public Vector3 rideLocalPosition = new Vector3(1.90f, 0.6f, -8.40f);

        private HardwareRig _playerRig;
        private GameObject _interactionCanvas;
        private GameObject _exitCanvas;
        private bool _playerInside = false;
        private bool _isRiding = false;

        private Vector3 _playerStartPosition;
        private Quaternion _playerStartRotation;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            SetupCanvases();
        }

        private void SetupCanvases()
        {
            // 1. First, look if the user added a custom interaction canvas directly under model_7 (this transform)
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.ToLower().Contains("interaction") || child.name.ToLower().Contains("prompt") || child.name.ToLower().Contains("ride"))
                {
                    _interactionCanvas = child.gameObject;
                    Debug.Log($"[TrainInteraction] Found manually added interaction canvas: '{child.name}'");
                    break;
                }
            }

            // If found, wire it up. Otherwise, fallback to instantiating from prefab.
            if (_interactionCanvas != null)
            {
                var btn = _interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(RideTrain);
                    Debug.Log($"[TrainInteraction] Wired RideTrain listener to button on manual interaction canvas '{_interactionCanvas.name}'");
                }

                var tmp = _interactionCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "[Ride Train]";
                }

                _interactionCanvas.SetActive(false);
            }
            else if (interactionCanvasPrefab != null)
            {
                _interactionCanvas = Instantiate(interactionCanvasPrefab, transform);
                _interactionCanvas.name = "InteractionCanvas";
                _interactionCanvas.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                _interactionCanvas.transform.localRotation = Quaternion.identity;
                _interactionCanvas.transform.localScale = new Vector3(0.000403855f, 0.000403855f, 0.000403855f);

                // Change text to [Ride Train]
                var tmp = _interactionCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "[Ride Train]";
                }

                var btn = _interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(RideTrain);
                }

                _interactionCanvas.SetActive(false);
            }

            // 2. Next, look if the user added a custom exit canvas directly under model_7 (this transform)
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.ToLower().Contains("exit") || child.name.ToLower().Contains("canvas"))
                {
                    // Avoid picking the interaction canvas
                    if (child.gameObject != _interactionCanvas)
                    {
                        _exitCanvas = child.gameObject;
                        Debug.Log($"[TrainInteraction] Found manually added exit canvas: '{child.name}'");
                        break;
                    }
                }
            }

            // If we found a manually placed exit canvas, configure it. Otherwise, fallback to instantiating from prefab.
            if (_exitCanvas != null)
            {
                var btn = _exitCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ExitTrain);
                    Debug.Log($"[TrainInteraction] Wired ExitTrain listener to button on manual canvas '{_exitCanvas.name}'");
                }
                
                // If they have text component, update it
                var tmp = _exitCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "Exit Train";
                }

                _exitCanvas.SetActive(false);
            }
            else if (exitCanvasPrefab != null)
            {
                _exitCanvas = Instantiate(exitCanvasPrefab, transform);
                _exitCanvas.name = "TrainExitCanvas";
                // Position it where it's easily visible to the riding player
                _exitCanvas.transform.localPosition = new Vector3(0f, .4f, 0.8f);
                _exitCanvas.transform.localRotation = Quaternion.identity;
                _exitCanvas.transform.localScale = new Vector3(0.000403855f, 0.000403855f, 0.000403855f);

                // Change button text to Exit Train
                var tmp = _exitCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "Exit Train";
                }

                var btn = _exitCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ExitTrain);
                }

                _exitCanvas.SetActive(false);
            }
        }

        private void Update()
        {
            if (_playerInside && !_isRiding && _interactionCanvas != null && _interactionCanvas.activeSelf)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                {
                    RideTrain();
                }
#else
                if (Input.GetKeyDown(KeyCode.E))
                {
                    RideTrain();
                }
#endif
            }
        }

        private void LateUpdate()
        {
            if (_isRiding && _playerRig != null)
            {
                // transform.up is the world-flat heading direction of model_7 (Blender coordinates pivot)
                Vector3 trainForward = transform.up;
                trainForward.y = 0f; // Keep the player perfectly upright to prevent sideways tilting/pitching
                if (trainForward.sqrMagnitude > 0.001f)
                {
                    _playerRig.transform.rotation = Quaternion.LookRotation(trainForward, Vector3.up);
                }
            }
        }

        public void RideTrain()
        {
            if (_isRiding) return;

            var localRig = FindLocalRig();
            if (localRig != null)
            {
                _playerRig = localRig;
                _playerStartPosition = localRig.transform.position;
                _playerStartRotation = localRig.transform.rotation;

                // Disable player locomotion (VR / Mobile)
                var smoothLocomotion = localRig.GetComponent<Fusion.XR.Shared.Locomotion.SmoothLocomotion>();
                if (smoothLocomotion != null)
                {
                    smoothLocomotion.enabled = false;
                }

                // Disable player locomotion (Desktop / Mouse)
                var desktopController = localRig.GetComponentInChildren<Fusion.XR.Shared.Desktop.DesktopController>();
                if (desktopController != null)
                {
                    desktopController.enabled = false;
                }

                // Disable NavMeshAgent so player moves smoothly with the parent transform
                var agent = localRig.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = false;
                }

                // Dynamically create/find an unscaled anchor under model_7
                // model_7 has localScale (100, 100, 100).
                // Setting UnscaledRideAnchor's localScale to (0.01, 0.01, 0.01) makes its world lossyScale exactly (1, 1, 1).
                Transform anchor = transform.Find("UnscaledRideAnchor");
                if (anchor == null)
                {
                    GameObject anchorGO = new GameObject("UnscaledRideAnchor");
                    anchor = anchorGO.transform;
                    anchor.SetParent(transform);
                    anchor.localPosition = new Vector3(0f, 0f, 0.008f); // 0.008f (0.8m above pivot) places player perfectly inside cabin floor
                    anchor.localRotation = Quaternion.identity;
                    anchor.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }
                else
                {
                    anchor.localPosition = new Vector3(0f, 0f, 0.008f);
                }

                // Calculate deck world position and align player rotation to face forward
                Vector3 deckWorldPos = transform.TransformPoint(new Vector3(0f, 0f, 0.008f));

                // Parent player to the unscaled anchor
                localRig.transform.SetParent(anchor);
                localRig.transform.position = deckWorldPos;
                
                // Keep the player perfectly upright in world space while retaining their starting compass direction
                localRig.transform.rotation = Quaternion.Euler(0f, _playerStartRotation.eulerAngles.y, 0f);

                _isRiding = true;

                if (_interactionCanvas != null) _interactionCanvas.SetActive(false);
                if (_exitCanvas != null) _exitCanvas.SetActive(true);

                Debug.Log($"[TrainInteraction] Parented player {_playerRig.name} to unscaled anchor under {transform.name}.");
            }
        }

        public void ExitTrain()
        {
            if (!_isRiding) return;

            if (_playerRig != null)
            {
                // Unparent player so they remain exactly where the train currently is in world space
                _playerRig.transform.SetParent(null);

                // Re-enable player locomotion (VR / Mobile)
                var smoothLocomotion = _playerRig.GetComponent<Fusion.XR.Shared.Locomotion.SmoothLocomotion>();
                if (smoothLocomotion != null)
                {
                    smoothLocomotion.enabled = true;
                }

                // Re-enable player locomotion (Desktop / Mouse)
                var desktopController = _playerRig.GetComponentInChildren<Fusion.XR.Shared.Desktop.DesktopController>();
                if (desktopController != null)
                {
                    desktopController.enabled = true;
                }

                // Re-enable NavMeshAgent
                var agent = _playerRig.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = true;
                }

                _playerRig = null;
            }

            _isRiding = false;

            if (_exitCanvas != null) _exitCanvas.SetActive(false);
            if (_playerInside && _interactionCanvas != null) _interactionCanvas.SetActive(true);

            Debug.Log("[TrainInteraction] Player unparented and locomotion restored at current world position.");
        }

        private HardwareRig FindLocalRig()
        {
            var rigs = FindObjectsOfType<HardwareRig>();
            foreach (var r in rigs)
            {
                if (r.gameObject.activeInHierarchy)
                {
                    return r;
                }
            }
            return null;
        }

        private void OnTriggerEnter(Collider other)
        {
            HardwareRig rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null && !_isRiding)
            {
                _playerInside = true;
                if (_interactionCanvas != null && !_interactionCanvas.activeSelf)
                {
                    _interactionCanvas.SetActive(true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            HardwareRig rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null)
            {
                _playerInside = false;
                if (_interactionCanvas != null)
                {
                    _interactionCanvas.SetActive(false);
                }
            }
        }

        private void OnDisable()
        {
            if (_isRiding)
            {
                ExitTrain();
            }
        }
    }
}