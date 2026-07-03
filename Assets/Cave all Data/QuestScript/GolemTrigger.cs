using UnityEngine;
using TMPro;
using Fusion.XR.Shared.Rig;

[RequireComponent(typeof(Collider))]
public class GolemTrigger : MonoBehaviour
{
    private PlayingVideo playingVideo;
    private GameObject interactionCanvas;
    private GameObject questInfoCanvas;
    private bool playerInside = false;

    private void Awake()
    {
        // Find PlayingVideo component in parent (golem)
        playingVideo = GetComponentInParent<PlayingVideo>();
        
        // Ensure collider is configured as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Find or reference the SpeechBubbleCanvas/InteractionCanvas inside hierarchy if pre-created
        Transform canvasT = transform.Find("InteractionCanvas");
        if (canvasT != null)
        {
            interactionCanvas = canvasT.gameObject;
            interactionCanvas.SetActive(false);
            
            // Wire the button click
            var btn = interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Interact);
            }

            // Create or Find QuestInfoCanvas
            Transform questCanvasT = transform.Find("QuestInfoCanvas");
            if (questCanvasT != null)
            {
                questInfoCanvas = questCanvasT.gameObject;
                questInfoCanvas.SetActive(false);
            }
            else
            {
                questInfoCanvas = Instantiate(interactionCanvas, transform);
                questInfoCanvas.name = "QuestInfoCanvas";
                
                // Position it on the left side dynamically based on canvas width and scale
                float xOffset = -1.15f; // Default safe offset
                UnityEngine.RectTransform origRect = interactionCanvas.GetComponent<UnityEngine.RectTransform>();
                if (origRect != null)
                {
                    float canvasWidthWorld = origRect.sizeDelta.x * interactionCanvas.transform.localScale.x;
                    xOffset = -(canvasWidthWorld + 0.18f); // 0.18f gap
                }

                questInfoCanvas.transform.localPosition = interactionCanvas.transform.localPosition + new Vector3(xOffset, 0f, 0f);
                questInfoCanvas.transform.localRotation = interactionCanvas.transform.localRotation;
                questInfoCanvas.transform.localScale = interactionCanvas.transform.localScale;

                // Increase the width of the QuestInfoCanvas slightly (by 35%)
                UnityEngine.RectTransform rect = questInfoCanvas.GetComponent<UnityEngine.RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x * 1.35f, rect.sizeDelta.y);
                }

                // Adjust Alpha (transparency) of the Background Image
                Transform bgT = questInfoCanvas.transform.Find("Background");
                if (bgT != null)
                {
                    var bgImg = bgT.GetComponent<UnityEngine.UI.Image>();
                    if (bgImg != null)
                    {
                        Color bgCol = bgImg.color;
                        bgCol.a = 0.65f; // 65% opacity / 35% transparency for a beautiful glass/modern look
                        bgImg.color = bgCol;
                    }
                }

                // Disable button and visual interactions on the cloned canvas
                var qBtn = questInfoCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (qBtn != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(qBtn); // We don't want a button component here
                    }
                    else
                    {
                        DestroyImmediate(qBtn);
                    }
                }
                
                questInfoCanvas.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // Allow PC players to press 'E' to interact when inside trigger
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
        if (playingVideo != null)
        {
            Debug.Log($"[GolemTrigger] Player interacted with Golem: {playingVideo.gameObject.name}");
            playingVideo.Play();
            
            // Hide the interaction button once the talk starts
            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }
            if (questInfoCanvas != null)
            {
                questInfoCanvas.SetActive(false);
            }
        }
    }

    private void UpdateQuestInfo()
    {
        if (questInfoCanvas == null) return;

        QuestContainer container = null;
        if (playingVideo != null)
        {
            container = playingVideo.GetComponentInChildren<QuestContainer>(true);
        }

        var textMesh = questInfoCanvas.GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null)
        {
            if (container != null)
            {
                if (container.isCompleted)
                {
                    textMesh.text = "Quest Completed!";
                }
                else
                {
                    int total = container.totalPieces;
                    if (total == 0)
                    {
                        int count = 0;
                        foreach (Transform child in container.transform)
                        {
                            if (child.GetComponent<QuestPiece>() != null)
                            {
                                count++;
                            }
                        }
                        total = count;
                    }
                    textMesh.text = $"Quest Pieces: {container.collectedPieces} / {total}";
                }
            }
            else
            {
                textMesh.text = "No Active Quest";
            }
        }
    }

    private bool IsQuestActive()
    {
        QuestContainer container = null;
        if (playingVideo != null)
        {
            container = playingVideo.GetComponentInChildren<QuestContainer>(true);
        }
        return container != null && container.gameObject.activeSelf && !container.isCompleted;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object belongs to the local player (HardwareRig)
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null && playingVideo != null)
        {
            playerInside = true;
            // Only show interaction button if the Golem is not already talking
            if (interactionCanvas != null && !interactionCanvas.activeSelf)
            {
                interactionCanvas.SetActive(true);
                if (questInfoCanvas != null && IsQuestActive())
                {
                    UpdateQuestInfo();
                    questInfoCanvas.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object belongs to the local player (HardwareRig)
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null && playingVideo != null)
        {
            playerInside = false;
            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }
            if (questInfoCanvas != null)
            {
                questInfoCanvas.SetActive(false);
            }
            playingVideo.ResetPlayback();
        }
    }
}