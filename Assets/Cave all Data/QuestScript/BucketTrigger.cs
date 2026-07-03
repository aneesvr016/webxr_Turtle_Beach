using UnityEngine;
using Fusion.XR.Shared.Rig;

[RequireComponent(typeof(Collider))]
public class BucketTrigger : MonoBehaviour
{
    [Tooltip("Type of bucket: 'Water' or 'Vinegar'")]
    public string bucketType;

    private BucketScript bucketScript;
    private GameObject interactionCanvas;
    private bool playerInside = false;

    private void Awake()
    {
        bucketScript = GetComponentInParent<BucketScript>();
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Setup interaction canvas link if pre-created
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
    }

    private void Update()
    {
        if (playerInside && bucketScript != null && !string.IsNullOrEmpty(bucketType) && interactionCanvas != null && interactionCanvas.activeSelf)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
              //  Interact();
            }
#else
           // if (Input.GetKeyDown(KeyCode.E))
            {
             //   Interact();
            }
#endif
        }
    }

    private void Interact()
    {
        if (bucketScript != null && !string.IsNullOrEmpty(bucketType))
        {
            Debug.Log($"[BucketTrigger] Player interacted to pour {bucketType}.");
            bucketScript.OnDrop(bucketType);
            
            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null && bucketScript != null && !string.IsNullOrEmpty(bucketType))
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