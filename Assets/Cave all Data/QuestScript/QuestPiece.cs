using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion.XR.Shared.Rig;

public class QuestPiece : MonoBehaviour
{
    [Header("Quest Assignment")]
    public string pieceName;
    
    private QuestContainer container;
    private GameObject interactionCanvas;
    private bool playerInside = false;

    private void Awake()
    {
        container = GetComponentInParent<QuestContainer>();
        
        // Ensure collider is configured as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Setup the interaction canvas link
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
                
                // Position it float above the piece
                interactionCanvas.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                interactionCanvas.transform.localRotation = Quaternion.identity;
                interactionCanvas.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);
                
                // Update text to "Collect"
                var tmp = interactionCanvas.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "Collect";
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
        Collect();
    }

    public void Collect()
    {
        Debug.Log($"[QuestPiece] Player collected piece: {gameObject.name}");

        // Trigger particles
        var ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.transform.SetParent(null);
            ps.gameObject.SetActive(true);
            ps.Play();
            Destroy(ps.gameObject, 2f);
        }

        // Notify container
        if (container != null)
        {
            container.OnPieceCollected(this);
        }

        // Hide canvas and destroy/disable this piece
        if (interactionCanvas != null)
        {
            interactionCanvas.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detect local player HardwareRig
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null)
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
