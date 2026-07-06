using UnityEngine;
using Fusion.XR.Shared.Rig;

namespace AB.TurtleBeach
{
    [RequireComponent(typeof(Collider))]
    public class WebXRSeat : MonoBehaviour
    {
        public GameObject interactionCanvasPrefab;
        public GameObject exitCanvasPrefab;
        
        private HardwareRig _playerRig;
        private GameObject interactionCanvas;
        private GameObject exitCanvas;
        private bool playerInside = false;
        private bool isSeated = false;
        
        private Vector3 _playerStartPosition;
        private Quaternion _playerStartRotation;

        private void Awake()
        {
            // Ensure trigger collider is configured
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 1.2f;
            }
            else
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            SetupInteractionCanvas();
        }

        private void SetupInteractionCanvas()
        {
            if (interactionCanvasPrefab != null)
            {
                interactionCanvas = Instantiate(interactionCanvasPrefab, transform);
                interactionCanvas.name = "InteractionCanvas";
                interactionCanvas.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                interactionCanvas.transform.localRotation = Quaternion.identity;
                interactionCanvas.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);

                // Dynamically change text to [Sit]
                var tmp = interactionCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "[Sit]";
                }

                // Wire up click listener
                var btn = interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(SitDown);
                }

                interactionCanvas.SetActive(false);
            }

            if (exitCanvasPrefab != null)
            {
                exitCanvas = Instantiate(exitCanvasPrefab, transform);
                exitCanvas.name = "SeatExitCanvas";
                exitCanvas.transform.localPosition = new Vector3(0f, 1.1f, 0.8f);
                exitCanvas.transform.localRotation = Quaternion.identity;
                exitCanvas.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

                // Dynamically change exit button text to [Stand Up]
                var tmp = exitCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "Stand Up";
                }

                var btn = exitCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StandUp);
                }

                exitCanvas.SetActive(false);
            }
        }

        private void Update()
        {
            if (playerInside && !isSeated && interactionCanvas != null && interactionCanvas.activeSelf)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                {
                    SitDown();
                }
#else
                if (Input.GetKeyDown(KeyCode.E))
                {
                    SitDown();
                }
#endif
            }
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

        public void SitDown()
        {
            if (isSeated) return;

            var localRig = FindLocalRig();
            if (localRig != null)
            {
                _playerRig = localRig;
                _playerStartPosition = localRig.transform.position;
                _playerStartRotation = localRig.transform.rotation;

                // Move and parent player rig to this seat hotspot
                localRig.transform.SetParent(transform);
                localRig.transform.localPosition = Vector3.zero;
                localRig.transform.localRotation = Quaternion.identity;

                isSeated = true;

                if (interactionCanvas != null) interactionCanvas.SetActive(false);
                if (exitCanvas != null) exitCanvas.SetActive(true);
            }
        }

        public void StandUp()
        {
            if (!isSeated) return;

            if (_playerRig != null)
            {
                _playerRig.transform.SetParent(null);
                _playerRig.transform.position = _playerStartPosition;
                _playerRig.transform.rotation = _playerStartRotation;
                _playerRig = null;
            }

            isSeated = false;

            if (exitCanvas != null) exitCanvas.SetActive(false);
            if (playerInside && interactionCanvas != null) interactionCanvas.SetActive(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            HardwareRig rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null && !isSeated)
            {
                playerInside = true;
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
                if (interactionCanvas != null)
                {
                    interactionCanvas.SetActive(false);
                }
            }
        }
    }
}
