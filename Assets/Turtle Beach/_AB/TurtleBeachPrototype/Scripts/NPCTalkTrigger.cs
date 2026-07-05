using UnityEngine;
using Fusion.XR.Shared.Rig;

namespace AB.TurtleBeach
{
    [RequireComponent(typeof(Collider))]
    public class NPCTalkTrigger : MonoBehaviour
    {
        public GameObject interactionCanvasPrefab;
        private NPCTalkController talkController;
        private GameObject interactionCanvas;
        private bool playerInside = false;

        private void Awake()
        {
            talkController = GetComponentInParent<NPCTalkController>();
            
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

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
            }
            else if (interactionCanvasPrefab != null)
            {
                interactionCanvas = Instantiate(interactionCanvasPrefab, transform);
                interactionCanvas.name = "InteractionCanvas";
                
                // Position above the NPC
                interactionCanvas.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                interactionCanvas.transform.localRotation = Quaternion.identity;
                interactionCanvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                
                interactionCanvas.SetActive(false);
            }

            if (interactionCanvas != null)
            {
                // Wire up click listener
                var btn = interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(Interact);
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
            if (talkController != null)
            {
                talkController.PlayTalk();
                if (interactionCanvas != null)
                {
                    interactionCanvas.SetActive(false);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HardwareRig rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null && talkController != null)
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
                if (talkController != null)
                {
                    talkController.StopTalk();
                }
            }
        }
    }
}